using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using WaterBIMToolkit.Events;
using WaterBIMToolkit.UI;

namespace WaterBIMToolkit.Commands;

[Transaction(TransactionMode.Manual)]
public class ShowToolkitCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            ToolkitState.UiApp = commandData.Application;

            if (ToolkitState.Window is { IsLoaded: true })
            {
                ToolkitState.Window.Activate();
                return Result.Succeeded;
            }

            // Revit 2027 (.NET 10) does not guarantee a WPF Application instance exists.
            // Without one, WPF resource resolution fails with NullReferenceException.
            if (Application.Current == null)
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            ToolkitState.ValidationEvent = ExternalEvent.Create(new RunValidationEvent());
            ToolkitState.ScheduleEvent = ExternalEvent.Create(new GenerateScheduleEvent());
            ToolkitState.SelectElementEvent = ExternalEvent.Create(new SelectElementEvent());

            ToolkitState.Window = new MainWindow();
            ToolkitState.Window.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            return Result.Failed;
        }
    }
}
