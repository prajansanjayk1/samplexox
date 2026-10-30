namespace QuietoraFocus.Core.Models;

public enum AiAssistantMode
{
    Lite = 1,     // Zero-download rule-based grounded RAG (100% offline, CPU fast)
    Standard = 2, // Local Vector / TF-IDF semantic hybrid embeddings
    Enhanced = 3, // Local ONNX / LLM inference endpoint
    Fast = 1,
    Balanced = 2,
    DeepAnalysis = 3
}

public sealed class AiSourceCitation
{
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Snippet { get; set; } = "";
    public string SourceType { get; set; } = "Indexed Document";
    public double RelevanceScore { get; set; }
}

public sealed class AiQueryResponse
{
    public string Query { get; set; } = "";
    public string Answer { get; set; } = "";
    public List<AiSourceCitation> Citations { get; set; } = new();
    public bool HasSufficientEvidence { get; set; } = true;
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AiChatMessage
{
    public string Sender { get; set; } = "User"; // "User" or "Quietora AI"
    public string Role
    {
        get => Sender == "User" ? "user" : "assistant";
        set => Sender = string.Equals(value, "user", StringComparison.OrdinalIgnoreCase) ? "User" : "Quietora AI";
    }
    public string Content { get; set; } = "";
    public List<AiSourceCitation> Citations { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset TimestampUtc
    {
        get => Timestamp;
        set => Timestamp = value;
    }
    public bool IsUser => Sender == "User";
}

public sealed class MemoryChunk
{
    public long Id { get; set; }
    public long FileId { get; set; }
    public string FilePath { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTimeOffset IndexedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WorkspaceSnapshot
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public string RootDirectory
    {
        get => ProjectPath;
        set => ProjectPath = value;
    }
    public string Description { get; set; } = "";
    public List<string> RecentFilePaths { get; set; } = new();
    public List<long> AssociatedTaskIds { get; set; } = new();
    public string Notes { get; set; } = "";
    public DateTimeOffset SavedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CapturedUtc
    {
        get => SavedUtc;
        set => SavedUtc = value;
    }

    public string FormattedDate => SavedUtc.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt");
}

public enum AutomationTriggerType
{
    ScheduledTime = 1,
    FocusSessionCompleted = 2,
    DistractionThresholdExceeded = 3,
    UnfinishedTaskDue = 4,
    TimeOfDay = 1,
    OnSessionEnd = 2
}

public enum AutomationActionType
{
    NotifyUser = 1,
    StartFocusSession = 2,
    OpenWorkspace = 3,
    SuggestBreak = 4,
    GenerateReport = 5,
    StartFocusMode = 2,
    MinimizeDistractions = 6,
    ToggleHostsBlock = 7
}

public sealed class AutomationRule
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Name
    {
        get => Title;
        set => Title = value;
    }
    public string Description { get; set; } = "";
    public AutomationTriggerType TriggerType { get; set; } = AutomationTriggerType.ScheduledTime;
    public string TriggerConfig { get; set; } = ""; // E.g. "09:00" or threshold value
    public string TriggerValue
    {
        get => TriggerConfig;
        set => TriggerConfig = value;
    }
    public AutomationActionType ActionType { get; set; } = AutomationActionType.NotifyUser;
    public string ActionPayload { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastTriggeredUtc { get; set; }

    public string TriggerLabel => TriggerType switch
    {
        AutomationTriggerType.ScheduledTime => $"Scheduled Time ({TriggerConfig})",
        AutomationTriggerType.FocusSessionCompleted => "On Session Completed",
        AutomationTriggerType.DistractionThresholdExceeded => "When Distractions > Threshold",
        _ => "When Task Due Soon"
    };

    public string ActionLabel => ActionType switch
    {
        AutomationActionType.NotifyUser => "Display Reminder Notification",
        AutomationActionType.StartFocusSession => "Auto-Start Focus Mode",
        AutomationActionType.OpenWorkspace => "Resume Saved Workspace",
        AutomationActionType.SuggestBreak => "Suggest Healthy Rest Break",
        AutomationActionType.MinimizeDistractions => "Minimize Distractions",
        AutomationActionType.ToggleHostsBlock => "Toggle Network Blocker",
        _ => "Generate Productivity Report"
    };
}

public sealed class PaletteCommandItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Icon { get; set; } = "⚡";
    public string Shortcut { get; set; } = "";
    public Action Action { get; set; } = () => { };
}
