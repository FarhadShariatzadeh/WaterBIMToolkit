using Autodesk.Revit.UI;
using WaterBIMToolkit.Models;
using WaterBIMToolkit.UI;

namespace WaterBIMToolkit;

internal static class ToolkitState
{
    public static UIApplication? UiApp { get; set; }
    public static MainWindow? Window { get; set; }
    public static ExternalEvent? ValidationEvent { get; set; }
    public static ExternalEvent? ScheduleEvent { get; set; }
    public static List<ValidationIssue> ValidationResults { get; set; } = new();
    public static List<PumpData> PumpSchedule { get; set; } = new();
}
