using Autodesk.Revit.DB;
using WaterBIMToolkit.Models;

namespace WaterBIMToolkit.Validators;

/// <summary>
/// Verifies each pump has at least one isolation valve within 3 pipe segments
/// on each connected pipe run (inlet and outlet). Missing isolation valves
/// prevent maintenance isolation of the pump.
/// </summary>
public class IsolationValveValidator
{
    private const int MaxSegmentsToWalk = 3;

    public List<ValidationIssue> Validate(Document doc)
    {
        var issues = new List<ValidationIssue>();
        var pumps = PumpConnectivityValidator.CollectPumps(doc);

        foreach (var pump in pumps)
        {
            var pipeConnectors = pump.MEPModel?.ConnectorManager?.Connectors?
                .Cast<Connector>()
                .Where(c => c.Domain == Domain.DomainPiping && c.IsConnected)
                .ToList();

            if (pipeConnectors == null || pipeConnectors.Count == 0) continue;

            bool missingInlet = false;
            bool missingOutlet = false;

            foreach (var (connector, index) in pipeConnectors.Select((c, i) => (c, i)))
            {
                bool hasValve = HasValveNearby(connector, pump.Id, doc);
                if (!hasValve)
                {
                    string side = index == 0 ? "inlet" : index == 1 ? "outlet" : $"connector {index + 1}";
                    if (index == 0) missingInlet = true;
                    else missingOutlet = true;

                    issues.Add(new ValidationIssue
                    {
                        Category = "Isolation Valve",
                        Severity = "Warning",
                        ElementId = pump.Id.ToString(),
                        Description = $"Pump \"{pump.Symbol.FamilyName} : {pump.Symbol.Name}\" — no isolation valve found within {MaxSegmentsToWalk} segments of {side}",
                        Location = GetLevel(pump, doc)
                    });
                }
            }

            _ = missingInlet; _ = missingOutlet; // used for per-side reporting above
        }

        return issues;
    }

    private bool HasValveNearby(Connector startConnector, ElementId pumpId, Document doc)
    {
        // Walk up to MaxSegmentsToWalk pipe elements from this connector
        var visited = new HashSet<ElementId> { pumpId };
        var queue = new Queue<(Connector connector, int depth)>();
        queue.Enqueue((startConnector, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (depth > MaxSegmentsToWalk) continue;

            // Get the element connected to this connector
            foreach (Connector ref_ in current.AllRefs)
            {
                var element = doc.GetElement(ref_.Owner.Id);
                if (element == null || visited.Contains(element.Id)) continue;
                visited.Add(element.Id);

                if (IsIsolationValve(element)) return true;

                // Continue walking through pipes
                if (element is Autodesk.Revit.DB.Plumbing.Pipe pipe)
                {
                    var otherConnectors = pipe.ConnectorManager?.Connectors?
                        .Cast<Connector>()
                        .Where(c => !c.Origin.IsAlmostEqualTo(current.Origin));

                    if (otherConnectors != null)
                        foreach (var c in otherConnectors)
                            queue.Enqueue((c, depth + 1));
                }
            }
        }

        return false;
    }

    private static bool IsIsolationValve(Element element)
    {
        if (element is not FamilyInstance fi) return false;
        if (fi.Category?.Id.Value != (long)BuiltInCategory.OST_PipeAccessory) return false;

        var name = (fi.Symbol?.FamilyName ?? "").ToLower();
        return name.Contains("gate") ||
               name.Contains("butterfly") ||
               name.Contains("ball") ||
               name.Contains("plug") ||
               name.Contains("isolation") ||
               name.Contains("shutoff") ||
               name.Contains("stop");
    }

    private static string GetLevel(FamilyInstance fi, Document doc)
    {
        if (fi.LevelId == ElementId.InvalidElementId) return "Unknown Level";
        return (doc.GetElement(fi.LevelId) as Level)?.Name ?? "Unknown Level";
    }
}
