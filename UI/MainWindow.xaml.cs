using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using WaterBIMToolkit.Models;
using WaterBIMToolkit.Schedule;

namespace WaterBIMToolkit.UI;

public partial class MainWindow : Window
{
    // Tracks which categories are currently visible; all on by default
    private readonly HashSet<string> _activeCategories = new()
    {
        "Pipe Slope", "Pump Connectivity", "Isolation Valve", "Pipe Size Mismatch"
    };

    public MainWindow()
    {
        InitializeComponent();
        SeverityFilter.SelectedIndex = 0;
    }

    // ── Validator tab ──────────────────────────────────────────────

    private void BtnRunValidation_Click(object sender, RoutedEventArgs e)
    {
        if (ToolkitState.ValidationEvent == null) return;

        BtnRunValidation.IsEnabled = false;
        StatusBar.Text = "Running validation checks…";
        ToolkitState.ValidationEvent.Raise();
    }

    public void RefreshValidation()
    {
        BtnRunValidation.IsEnabled = true;
        ApplyValidationFilter();

        int errors = ToolkitState.ValidationResults.Count(i => i.Severity == "Error");
        int warnings = ToolkitState.ValidationResults.Count(i => i.Severity == "Warning");
        int total = ToolkitState.ValidationResults.Count;

        ValidationSummary.Text = total == 0
            ? "✅ No issues found"
            : $"{total} issue(s) — {errors} error(s), {warnings} warning(s)";

        BtnExportValidation.IsEnabled = total > 0;
        StatusBar.Text = $"Validation complete. {total} issue(s) found.";
    }

    private void SeverityFilter_Changed(object sender, SelectionChangedEventArgs e)
        => ApplyValidationFilter();

    private void CategoryToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn) return;
        var category = btn.Tag?.ToString() ?? "";

        if (btn.IsChecked == true)
            _activeCategories.Add(category);
        else
            _activeCategories.Remove(category);

        ApplyValidationFilter();
    }

    private void ApplyValidationFilter()
    {
        if (ValidationGrid == null) return;

        var severity = (SeverityFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

        var filtered = ToolkitState.ValidationResults
            .Where(i => severity == "All" || i.Severity == severity)
            .Where(i => _activeCategories.Contains(i.Category))
            .ToList();

        ValidationGrid.ItemsSource = filtered;
    }

    private void ValidationGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ValidationGrid.SelectedItem is not ValidationIssue issue) return;
        if (string.IsNullOrEmpty(issue.ElementId)) return;
        if (ToolkitState.SelectElementEvent == null) return;

        ToolkitState.SelectedElementId = issue.ElementId;
        StatusBar.Text = $"Selecting element {issue.ElementId}…";
        ToolkitState.SelectElementEvent.Raise();
    }

    private void BtnExportValidation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Validation Issues",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"MEP_Validation_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        using var writer = new System.IO.StreamWriter(dialog.FileName);
        writer.WriteLine("Severity,Category,Description,Element ID,Location");

        foreach (var issue in ToolkitState.ValidationResults)
        {
            writer.WriteLine(string.Join(",",
                Escape(issue.Severity),
                Escape(issue.Category),
                Escape(issue.Description),
                Escape(issue.ElementId),
                Escape(issue.Location)));
        }

        StatusBar.Text = $"Exported {ToolkitState.ValidationResults.Count} issues to {dialog.FileName}";
    }

    // ── Schedule tab ───────────────────────────────────────────────

    private void BtnGenerateSchedule_Click(object sender, RoutedEventArgs e)
    {
        if (ToolkitState.ScheduleEvent == null) return;

        BtnGenerateSchedule.IsEnabled = false;
        StatusBar.Text = "Generating equipment schedule…";
        ToolkitState.ScheduleEvent.Raise();
    }

    public void RefreshSchedule()
    {
        BtnGenerateSchedule.IsEnabled = true;
        ScheduleGrid.ItemsSource = ToolkitState.PumpSchedule;

        int count = ToolkitState.PumpSchedule.Count;
        int clearanceIssues = ToolkitState.PumpSchedule.Count(p => p.ClearanceStatus != "OK" && p.ClearanceStatus != "No geometry");

        ScheduleSummary.Text = $"{count} pump(s) found" +
                               (clearanceIssues > 0 ? $" — {clearanceIssues} clearance issue(s)" : "");

        BtnExportSchedule.IsEnabled = count > 0;
        StatusBar.Text = $"Schedule generated. {count} pump(s) found.";
    }

    private void BtnExportSchedule_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Equipment Schedule",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"Pump_Schedule_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        EquipmentScheduleGenerator.ExportToCsv(ToolkitState.PumpSchedule, dialog.FileName);
        StatusBar.Text = $"Exported {ToolkitState.PumpSchedule.Count} pump(s) to {dialog.FileName}";
    }

    // ──────────────────────────────────────────────────────────────

    private static string Escape(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
}
