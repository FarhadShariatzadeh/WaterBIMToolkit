using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using WaterBIMToolkit.Models;

namespace WaterBIMToolkit.Validators;

/// <summary>
/// Checks that all pipe connectors on pump family instances are connected.
/// Flags pumps with open/unconnected pipe connectors.
/// </summary>
public class PumpConnectivityValidator
{
    public List<ValidationIssue> Validate(Document doc)
    {
        var issues = new List<ValidationIssue>();

        var pumps = CollectPumps(doc);

        foreach (var pump in pumps)
        {
            var connectors = pump.MEPModel?.ConnectorManager?.Connectors;
            if (connectors == null) continue;

            var pipeConnectors = connectors
                .Cast<Connector>()
                .Where(c => c.Domain == Domain.DomainPiping)
                .ToList();

            if (pipeConnectors.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Category = "Pump Connectivity",
                    Severity = "Warning",
                    ElementId = pump.Id.ToString(),
                    Description = $"Pump \"{pump.Symbol.FamilyName} : {pump.Symbol.Name}\" has no pipe connectors defined in the family",
                    Location = GetLevel(pump, doc)
                });
                continue;
            }

            var open = pipeConnectors.Where(c => !c.IsConnected).ToList();
            if (open.Count == 0) continue;

            string direction = open.Count == pipeConnectors.Count ? "all connectors" :
                               $"{open.Count} of {pipeConnectors.Count} connector(s)";

            issues.Add(new ValidationIssue
            {
                Category = "Pump Connectivity",
                Severity = "Error",
                ElementId = pump.Id.ToString(),
                Description = $"Pump \"{pump.Symbol.FamilyName} : {pump.Symbol.Name}\" has {direction} unconnected",
                Location = GetLevel(pump, doc)
            });
        }

        return issues;
    }

    internal static List<FamilyInstance> CollectPumps(Document doc)
    {
        var equipment = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>();

        var piping = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_PipeAccessory)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>();

        return equipment.Concat(piping)
            .Where(fi => IsPump(fi))
            .ToList();
    }

    private static bool IsPump(FamilyInstance fi)
    {
        var name = (fi.Symbol?.FamilyName ?? "").ToLower();
        return name.Contains("pump") || name.Contains("booster") || name.Contains("lift station");
    }

    private static string GetLevel(FamilyInstance fi, Document doc)
    {
        if (fi.LevelId == ElementId.InvalidElementId) return "Unknown Level";
        return (doc.GetElement(fi.LevelId) as Level)?.Name ?? "Unknown Level";
    }
}
