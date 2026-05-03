using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using WaterBIMToolkit.Models;

namespace WaterBIMToolkit.Validators;

/// <summary>
/// Finds size changes in pipe runs where a reducer/transition fitting is missing.
/// A coupling or union connecting two different-diameter pipes is flagged.
/// </summary>
public class PipeSizeValidator
{
    private const double SizeTolerance = 0.01; // inches — below this diff is treated as same size

    public List<ValidationIssue> Validate(Document doc)
    {
        var issues = new List<ValidationIssue>();

        var fittings = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_PipeFitting)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>();

        foreach (var fitting in fittings)
        {
            if (IsReducerType(fitting)) continue;

            var connectors = fitting.MEPModel?.ConnectorManager?.Connectors?
                .Cast<Connector>()
                .Where(c => c.Domain == Domain.DomainPiping)
                .ToList();

            if (connectors == null || connectors.Count < 2) continue;

            var sizes = connectors
                .Select(c => UnitUtils.ConvertFromInternalUnits(c.Radius * 2, UnitTypeId.Inches))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // More than one distinct size on a non-reducer fitting = missing reducer
            if (sizes.Count <= 1 || sizes.Max() - sizes.Min() < SizeTolerance) continue;

            string sizeList = string.Join(" → ", sizes.Select(s => $"{s:F2}\""));
            issues.Add(new ValidationIssue
            {
                Category = "Pipe Size Mismatch",
                Severity = "Warning",
                ElementId = fitting.Id.ToString(),
                Description = $"Non-reducer fitting \"{fitting.Symbol?.FamilyName}\" connects pipes of different sizes: {sizeList}. A reducer/transition may be missing.",
                Location = GetLevel(fitting, doc)
            });
        }

        return issues;
    }

    private static bool IsReducerType(FamilyInstance fi)
    {
        var name = (fi.Symbol?.FamilyName ?? "").ToLower();
        return name.Contains("reducer") ||
               name.Contains("transition") ||
               name.Contains("concentric") ||
               name.Contains("eccentric") ||
               name.Contains("increaser");
    }

    private static string GetLevel(FamilyInstance fi, Document doc)
    {
        if (fi.LevelId == ElementId.InvalidElementId) return "Unknown Level";
        return (doc.GetElement(fi.LevelId) as Level)?.Name ?? "Unknown Level";
    }
}
