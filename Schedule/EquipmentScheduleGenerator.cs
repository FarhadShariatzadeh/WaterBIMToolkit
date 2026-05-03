using Autodesk.Revit.DB;
using WaterBIMToolkit.Models;
using WaterBIMToolkit.Validators;

namespace WaterBIMToolkit.Schedule;

public static class EquipmentScheduleGenerator
{
    private const double MaintenanceClearanceFt = 3.0; // minimum 3-foot clearance around pumps

    public static List<PumpData> Generate(Document doc)
    {
        var pumps = PumpConnectivityValidator.CollectPumps(doc);
        return pumps.Select(p => BuildPumpData(p, doc)).ToList();
    }

    private static PumpData BuildPumpData(FamilyInstance pump, Document doc)
    {
        var data = new PumpData
        {
            ElementId = pump.Id.ToString(),
            FamilyName = pump.Symbol?.FamilyName ?? "Unknown",
            TypeName = pump.Symbol?.Name ?? "Unknown",
            Level = GetLevel(pump, doc),
            FlowRate = ReadParam(pump, "Flow", "GPM", "Rated Flow", "Design Flow Rate"),
            TotalDynamicHead = ReadParam(pump, "Total Head", "TDH", "Total Dynamic Head", "Head"),
            MotorHP = ReadParam(pump, "Motor HP", "BHP", "Brake Horsepower", "Motor Power", "Electrical Power"),
            Material = ReadParam(pump, "Body Material", "Casing Material", "Material"),
            ClearanceStatus = CheckClearance(pump, doc)
        };

        var notes = new List<string>();
        if (data.FlowRate == "—") notes.Add("Flow rate parameter not found");
        if (data.TotalDynamicHead == "—") notes.Add("TDH parameter not found");
        if (data.ClearanceStatus != "OK") notes.Add(data.ClearanceStatus);
        data.Notes = string.Join("; ", notes);

        return data;
    }

    private static string ReadParam(FamilyInstance fi, params string[] paramNames)
    {
        foreach (var name in paramNames)
        {
            var param = fi.LookupParameter(name) ?? fi.Symbol?.LookupParameter(name);
            if (param == null) continue;

            string val = param.StorageType switch
            {
                StorageType.Double => FormatDouble(param),
                StorageType.Integer => param.AsInteger().ToString(),
                StorageType.String => param.AsString() ?? "",
                StorageType.ElementId => (fi.Document.GetElement(param.AsElementId()) as Element)?.Name ?? "",
                _ => ""
            };

            if (!string.IsNullOrWhiteSpace(val)) return val;
        }

        return "—";
    }

    private static string FormatDouble(Parameter param)
    {
        double raw = param.AsDouble();
        // Convert common engineering units from Revit internal units
        var unitType = param.Definition.GetDataType();

        if (unitType == SpecTypeId.Flow)
            return $"{UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.GallonsPerMinute):F1} GPM";
        if (unitType == SpecTypeId.HvacPressure || unitType == SpecTypeId.PipingPressure)
            return $"{UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.FeetOfWater):F1} ft";
        if (unitType == SpecTypeId.ElectricalPower)
            return $"{UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.Horsepower):F2} HP";

        return raw.ToString("F2");
    }

    private static string CheckClearance(FamilyInstance pump, Document doc)
    {
        var bbox = pump.get_BoundingBox(null);
        if (bbox == null) return "No geometry";

        // Expand bounding box by required clearance distance
        double clearance = MaintenanceClearanceFt;
        var outline = new Outline(
            new XYZ(bbox.Min.X - clearance, bbox.Min.Y - clearance, bbox.Min.Z - 0.5),
            new XYZ(bbox.Max.X + clearance, bbox.Max.Y + clearance, bbox.Max.Z + 0.5));

        var intersecting = new FilteredElementCollector(doc)
            .WherePasses(new BoundingBoxIntersectsFilter(outline))
            .Where(e => e.Id != pump.Id)
            .Where(IsObstruction)
            .ToList();

        if (intersecting.Count == 0) return "OK";

        var names = intersecting
            .Take(3)
            .Select(e => e.Category?.Name ?? e.GetType().Name);

        return $"Clearance conflict: {string.Join(", ", names)}" +
               (intersecting.Count > 3 ? $" +{intersecting.Count - 3} more" : "");
    }

    private static bool IsObstruction(Element e)
    {
        var cat = e.Category?.Id.Value;
        return cat == (long)BuiltInCategory.OST_Walls ||
               cat == (long)BuiltInCategory.OST_StructuralColumns ||
               cat == (long)BuiltInCategory.OST_StructuralFraming ||
               cat == (long)BuiltInCategory.OST_MechanicalEquipment ||
               cat == (long)BuiltInCategory.OST_PipingEquipment;
    }

    private static string GetLevel(FamilyInstance fi, Document doc)
    {
        if (fi.LevelId == ElementId.InvalidElementId) return "Unknown";
        return (doc.GetElement(fi.LevelId) as Level)?.Name ?? "Unknown";
    }

    public static void ExportToCsv(List<PumpData> data, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("Element ID,Family Name,Type Name,Level,Flow Rate,Total Dynamic Head,Motor HP,Material,Clearance Status,Notes");

        foreach (var pump in data)
        {
            writer.WriteLine(string.Join(",",
                Escape(pump.ElementId),
                Escape(pump.FamilyName),
                Escape(pump.TypeName),
                Escape(pump.Level),
                Escape(pump.FlowRate),
                Escape(pump.TotalDynamicHead),
                Escape(pump.MotorHP),
                Escape(pump.Material),
                Escape(pump.ClearanceStatus),
                Escape(pump.Notes)));
        }
    }

    private static string Escape(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
}
