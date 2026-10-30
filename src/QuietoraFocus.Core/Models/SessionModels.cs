namespace QuietoraFocus.Core.Models;

public sealed class FocusSessionConfig
{
    public SessionMode Mode { get; set; } = SessionMode.DeepFocus;
    public string TaskName { get; set; } = "Focused Work";
    public string Subject { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public int PlannedSeconds { get; set; } = 25 * 60;
    public int WorkIntervalSeconds { get; set; } = 25 * 60;
    public int ShortBreakSeconds { get; set; } = 5 * 60;
    public int LongBreakSeconds { get; set; } = 15 * 60;
    public int CyclesBeforeLongBreak { get; set; } = 4;
    public DistractionPolicyLevel PolicyLevel { get; set; } = DistractionPolicyLevel.Remind;
    public DistractionEnforcementMode EnforcementMode { get; set; } = DistractionEnforcementMode.Strict;
    public string SessionNotes { get; set; } = "";
}

public sealed class SessionLog
{
    public long Id { get; init; }
    public string Task { get; init; } = "";
    public string Subject { get; init; } = "";
    public string ProjectName { get; init; } = "";
    public string Mode { get; init; } = nameof(SessionMode.DeepFocus);
    public DateTimeOffset StartedUtc { get; init; }
    public DateTimeOffset EndedUtc { get; init; }
    public int PlannedSeconds { get; init; }
    public int ActualSeconds { get; init; }
    public int PausedSeconds { get; init; }
    public int Distractions { get; init; }
    public string EndReason { get; init; } = "";
    public string Notes { get; init; } = "";

    // Backward compatible display properties
    public string DateLabel => StartedUtc.ToLocalTime().ToString("dd MMM yyyy  •  h:mm tt");
    public string DurationLabel => $"{ActualSeconds / 60}m {ActualSeconds % 60:D2}s";
    public string ResultLabel => EndReason switch
    {
        "completed" => "Completed",
        "emergency" => "Emergency stop",
        "app_closed" => "Closed app",
        _ => "Stopped early"
    };
}
