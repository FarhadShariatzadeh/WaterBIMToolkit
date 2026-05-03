using Autodesk.Revit.UI;
using WaterBIMToolkit.Schedule;

namespace WaterBIMToolkit.Events;

public class GenerateScheduleEvent : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument?.Document;
        if (doc == null) return;

        ToolkitState.PumpSchedule = EquipmentScheduleGenerator.Generate(doc);
        ToolkitState.Window?.Dispatcher.Invoke(() => ToolkitState.Window.RefreshSchedule());
    }

    public string GetName() => "GenerateSchedule";
}
