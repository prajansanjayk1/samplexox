namespace QuietoraFocus.Core.Models;

public sealed class FocusTask
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;
    public DateTimeOffset? DueDateUtc { get; set; }
    public long? ProjectId { get; set; }
    public string Subject { get; set; } = "";
    public int EstimatedMinutes { get; set; } = 25;
    public int ActualMinutes { get; set; } = 0;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedUtc { get; set; }

    public bool IsCompleted => Status == TaskStatusEnum.Completed;
    public string PriorityLabel => Priority.ToString();
    public string DueDateLabel => DueDateUtc.HasValue
        ? DueDateUtc.Value.ToLocalTime().ToString("MMM dd, yyyy")
        : "No deadline";
}

public sealed class Project
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ColorHex { get; set; } = "#6759E8";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
