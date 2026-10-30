namespace QuietoraFocus.Core.Models;

public enum AmbientSoundType
{
    None = 0,
    BrownNoise = 1,
    WhiteNoise = 2,
    GentleRain = 3,
    BinauralGamma40Hz = 4,
    BinauralAlpha10Hz = 5
}

public enum InterceptMode
{
    HardcoreClose = 1,      // Instantly close tab and minimize (Hell mode)
    MindfulFriction = 2,    // Show calm breathing pause screen (Regain / One Sec style)
    QuotaLockOnly = 3       // Block only after daily budget is exceeded
}

public sealed class AppBudget
{
    public long Id { get; set; }
    public string TargetName { get; set; } = ""; // e.g. "youtube.com", "discord", "steam"
    public string TargetType { get; set; } = "website"; // "website" or "app"
    public string DisplayName { get; set; } = "";
    public int DailyLimitMinutes { get; set; } = 30;
    public int UsedSecondsToday { get; set; }
    public string LastResetDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public bool IsActive { get; set; } = true;

    public int UsedMinutesToday { get => UsedSecondsToday / 60; set { } }
    public int RemainingMinutes { get => Math.Max(0, DailyLimitMinutes - UsedMinutesToday); set { } }
    public double UsagePercent { get => DailyLimitMinutes > 0 ? Math.Clamp((double)UsedMinutesToday / DailyLimitMinutes * 100.0, 0, 100) : 0; set { } }
    public bool IsQuotaExceeded { get => UsedMinutesToday >= DailyLimitMinutes; set { } }

    public string StatusLabel { get => IsQuotaExceeded ? "QUOTA EXCEEDED (LOCKED)" : $"{RemainingMinutes}m remaining today"; set { } }
}

public sealed class CoworkingPeer
{
    public string Name { get; set; } = "";
    public string AvatarEmoji { get; set; } = "👩‍💻";
    public string ActiveTask { get; set; } = "";
    public string Status { get; set; } = "Deep Focus"; // "Deep Focus", "5m Break", "Pomodoro Cycle 3"
    public int ElapsedMinutes { get; set; } = 25;
    public int TodayStreakDays { get; set; } = 7;
    public bool IsOnline { get; set; } = true;
}

public sealed class DetoxScoreReport
{
    public int DetoxScore { get; set; } = 88; // 0 - 100
    public int DoomscrollsAvertedToday { get; set; }
    public int TotalMinutesSavedToday { get; set; }
    public int ProductiveFocusMinutes { get; set; }
    public int TotalDistractingScreenMinutes { get; set; }
    public bool IsBedtimeShieldActive { get; set; }
    public string MindfulTip { get; set; } = "";

    public string TimeSavedFormatted { get => TotalMinutesSavedToday < 60
        ? $"{TotalMinutesSavedToday} mins"
        : $"{TotalMinutesSavedToday / 60}h {TotalMinutesSavedToday % 60}m"; set { } }
}

public sealed class MindfulFrictionState
{
    public bool IsActive { get; set; }
    public string TriggerTarget { get; set; } = "";
    public int SecondsRemaining { get; set; } = 5;
    public string PromptQuestion { get; set; } = "Take a breath. What is your intention right now?";
    public string ReasonChosen { get; set; } = "";
}
