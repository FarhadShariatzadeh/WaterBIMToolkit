using Autodesk.Revit.UI;
using WaterBIMToolkit.Validators;

namespace WaterBIMToolkit.Events;

public class RunValidationEvent : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        var doc = app.ActiveUIDocument?.Document;
        if (doc == null) return;

        var results = new List<Models.ValidationIssue>();
        results.AddRange(new PipeSlopeValidator().Validate(doc));
        results.AddRange(new PumpConnectivityValidator().Validate(doc));
        results.AddRange(new IsolationValveValidator().Validate(doc));
        results.AddRange(new PipeSizeValidator().Validate(doc));

        ToolkitState.ValidationResults = results;
        ToolkitState.Window?.Dispatcher.Invoke(() => ToolkitState.Window.RefreshValidation());
    }

    public string GetName() => "RunValidation";
}
