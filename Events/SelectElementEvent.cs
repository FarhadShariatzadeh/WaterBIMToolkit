using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace WaterBIMToolkit.Events;

/// <summary>
/// Selects and zooms to the element whose ID is stored in ToolkitState.SelectedElementId.
/// Must run on the Revit API thread via ExternalEvent.
/// </summary>
public class SelectElementEvent : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        if (uidoc == null) return;
        if (!long.TryParse(ToolkitState.SelectedElementId, out long idValue)) return;

        var elementId = new ElementId(idValue);
        if (uidoc.Document.GetElement(elementId) == null) return;

        var ids = new List<ElementId> { elementId };
        uidoc.Selection.SetElementIds(ids);
        uidoc.ShowElements(ids);
    }

    public string GetName() => "SelectElement";
}
