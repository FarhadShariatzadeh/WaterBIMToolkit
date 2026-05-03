namespace WaterBIMToolkit.Models;

public class PumpData
{
    public string ElementId { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string Level { get; set; } = "";
    public string FlowRate { get; set; } = "—";
    public string TotalDynamicHead { get; set; } = "—";
    public string MotorHP { get; set; } = "—";
    public string Material { get; set; } = "—";
    public string ClearanceStatus { get; set; } = "OK";
    public string Notes { get; set; } = "";
}
