using System;
using System.Collections.Generic;

namespace Task1_ResilientAutomation.Models;

public class FieldExecutionResult
{
    public string FieldName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string DesiredValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string StrategyUsed { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string ElementDetails { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class AutomationReport
{
    public string TargetUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool AllPassed { get; set; }
    public List<FieldExecutionResult> FieldResults { get; set; } = new();
    public string? ScreenshotPath { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

