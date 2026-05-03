using Autodesk.Revit.UI;
using System.Reflection;

namespace WaterBIMToolkit;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication app)
    {
        var panel = app.CreateRibbonPanel("Water BIM Toolkit");

        var btnData = new PushButtonData(
            "WaterBIMToolkit",
            "Water BIM\nToolkit",
            Assembly.GetExecutingAssembly().Location,
            "WaterBIMToolkit.Commands.ShowToolkitCommand")
        {
            ToolTip = "Open the Water/Wastewater BIM Toolkit — MEP Validator and Equipment Schedule"
        };

        panel.AddItem(btnData);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication app)
    {
        ToolkitState.Window?.Close();
        return Result.Succeeded;
    }
}
