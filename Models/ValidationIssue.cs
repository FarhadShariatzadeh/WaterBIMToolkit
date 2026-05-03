namespace WaterBIMToolkit.Models;

public class ValidationIssue
{
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "Warning"; // "Error" | "Warning" | "Info"
    public string Description { get; set; } = "";
    public string ElementId { get; set; } = "";
    public string Location { get; set; } = "";
}
