namespace QuietoraFocus.Core.Models;

public sealed class ProductivityMetrics
{
    public int TotalSessionsCount { get; set; }
    public int CompletedSessionsCount { get; set; }
    public int TotalFocusMinutes { get; set; }
    public int TodayFocusMinutes { get; set; }
    public int WeeklyFocusMinutes { get; set; }
    public double MonthlyFocusHours => Math.Round(WeeklyFocusMinutes * 4.0 / 60.0, 1);
    public double SessionCompletionRate { get; set; }
    public int TotalDistractions { get; set; }
    public int TodayDistractions { get; set; }
    public int LongestSessionMinutes { get; set; }
    public int CurrentStreakDays { get; set; }
    public int BestStreakDays { get; set; }
    public int CompletedTasksCount { get; set; }
    public int PendingTasksCount { get; set; }

    public string ScoreFormulaExplanation =>
        "Productivity Score is a simple heuristic ratio: [Completed Focus Minutes / (Planned Minutes + (Distractions * 2))] * CompletionRate. " +
        "It provides a rough personal consistency indicator and is not a clinical or psychological evaluation.";
}

public sealed class DailyActivityPoint
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = "";
    public int FocusMinutes { get; set; }
    public int Distractions { get; set; }
    public int CompletedSessions { get; set; }
}

public sealed class AppDistractionSummary
{
    public string ProcessName { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public sealed class RuleBasedRecommendation
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General"; // Duration, Distraction, Break, Task
    public string Severity { get; set; } = "Info"; // Info, Tip, Warning
}
