namespace QuietoraFocus.Core.Models;

public enum DistractionEnforcementMode
{
    Gentle = 1,   // 15s cooldown, gentle reminder
    Strict = 2,   // 2s cooldown, persistent minimization
    Hardcore = 3  // Instant tab close (Ctrl+W) + minimize, 1s cooldown (Hell Mode)
}

public sealed class DistractionRule
{
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "Custom";
    public bool IsActive { get; set; } = true;
}

public sealed class BlockedWebsite
{
    public string Domain { get; set; } = "";
    public string KeywordPattern { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "Social / Media";
    public bool IsActive { get; set; } = true;

    public string FormattedLabel => $"{DisplayName} ({Domain})";
}

public sealed class DistractionEvent
{
    public long Id { get; set; }
    public long? SessionId { get; set; }
    public string ProcessName { get; set; } = "";
    public string TargetDetail { get; set; } = ""; // E.g. "youtube.com" or process name
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public DistractionPolicyLevel PolicyApplied { get; set; }
    public string ActionTaken { get; set; } = "";

    public string TimestampLabel => TimestampUtc.ToLocalTime().ToString("h:mm:ss tt");
}
