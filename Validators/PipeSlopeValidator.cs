using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using WaterBIMToolkit.Models;

namespace WaterBIMToolkit.Validators;

/// <summary>
/// Checks gravity pipes (sanitary/storm/sewer) for minimum slope compliance.
/// Small pipes (≤6"): min 1/4" per foot. Larger pipes: min 1/8" per foot.
/// </summary>
public class PipeSlopeValidator
{
    private const double MinSlopeSmallPipe = 0.020833; // 1/4" per foot (0.25/12)
    private const double MinSlopeLargePipe = 0.010417; // 1/8" per foot (0.125/12)
    private const double SmallPipeMaxDiameterInches = 6.0;

    public List<ValidationIssue> Validate(Document doc)
    {
        var issues = new List<ValidationIssue>();

        var pipes = new FilteredElementCollector(doc)
            .OfClass(typeof(Pipe))
            .Cast<Pipe>()
            .Where(p => IsGravityPipe(p));

        foreach (var pipe in pipes)
        {
            double slope = CalculateSlope(pipe);
            if (slope < 0) continue; // vertical or indeterminate

            double diamInches = GetDiameterInches(pipe);
            double minSlope = diamInches <= SmallPipeMaxDiameterInches
                ? MinSlopeSmallPipe
                : MinSlopeLargePipe;

            if (slope >= minSlope) continue;

            string slopeDisplay = $"{slope * 12:F3}\"/ft";
            string minDisplay = $"{minSlope * 12:F3}\"/ft";
            string severity = slope < 0.0001 ? "Error" : "Warning";

            issues.Add(new ValidationIssue
            {
                Category = "Pipe Slope",
                Severity = severity,
                ElementId = pipe.Id.ToString(),
                Description = $"{diamInches:F1}\" gravity pipe slope {slopeDisplay} is below minimum {minDisplay}",
                Location = GetLevel(pipe, doc)
            });
        }

        return issues;
    }

    private static bool IsGravityPipe(Pipe pipe)
    {
        var systemName = (pipe.MEPSystem?.Name ?? "").ToLower();
        return systemName.Contains("sanitary") ||
               systemName.Contains("storm") ||
               systemName.Contains("gravity") ||
               systemName.Contains("drain") ||
               systemName.Contains("sewer") ||
               systemName.Contains("waste");
    }

    private static double CalculateSlope(Pipe pipe)
    {
        var connectors = pipe.ConnectorManager?.Connectors?.Cast<Connector>().ToList();
        if (connectors == null || connectors.Count < 2) return -1;

        var a = connectors[0].Origin;
        var b = connectors[1].Origin;
        double horizontalRun = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

        if (horizontalRun < 0.001) return -1; // vertical pipe

        return Math.Abs(b.Z - a.Z) / horizontalRun;
    }

    private static double GetDiameterInches(Pipe pipe)
    {
        var param = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        if (param == null) return 0;
        return UnitUtils.ConvertFromInternalUnits(param.AsDouble(), UnitTypeId.Inches);
    }

    private static string GetLevel(Pipe pipe, Document doc)
    {
        if (pipe.LevelId == ElementId.InvalidElementId) return "Unknown Level";
        return (doc.GetElement(pipe.LevelId) as Level)?.Name ?? "Unknown Level";
    }
}
