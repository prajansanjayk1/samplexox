namespace QuietoraFocus.Core.Models;

public enum SessionMode
{
    DeepFocus,
    Pomodoro,
    StudyMode,
    WorkMode,
    Custom
}

public enum SessionState
{
    Idle,
    Running,
    Paused,
    Completed,
    Stopped,
    EmergencyStopped
}

public enum PomodoroPhase
{
    Work,
    ShortBreak,
    LongBreak
}

public enum DistractionPolicyLevel
{
    Observe = 1,
    Remind = 2,
    Minimize = 3
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum TaskStatusEnum
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
