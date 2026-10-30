namespace QuietoraFocus.Core.Models;

public enum GameEnforcementLevel
{
    DetectionOnly = 0,
    DiscouragementNotification = 1,
    StrictMinimizeAndPause = 2
}

public sealed class DetectedGame
{
    public long Id { get; set; }
    public string GameName { get; set; } = "";
    public string ProcessName { get; set; } = ""; // e.g. "cs2", "valorant", "gta5"
    public string ExecutablePath { get; set; } = "";
    public string Launcher { get; set; } = "Standalone"; // "Steam", "Epic", "Riot", "Xbox", "Standalone"
    public bool IsBlocked { get; set; } = true;
    public string DiscoveredUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string? LastLaunchedUtc { get; set; }
}

public enum FamilyDnsProvider
{
    Standard = 0,
    CloudflareFamily = 1, // 1.1.1.3 / 1.0.0.3
    AdGuardFamily = 2,     // 94.140.14.15 / 94.140.15.16
    CleanBrowsingFamily = 3 // 185.228.168.168
}

public sealed class AdultProtectionConfig
{
    public bool IsCategoryFilterEnabled { get; set; } = true;
    public bool BlockGambling { get; set; } = true;
    public bool BlockAdultSites { get; set; } = true;
    public FamilyDnsProvider DnsProvider { get; set; } = FamilyDnsProvider.CloudflareFamily;
    public bool SafeSearchEnforced { get; set; } = true;
    public string LastUpdatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
}

public sealed class AcademicSubject
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = ""; // e.g. "CS301", "MATH201"
    public string Semester { get; set; } = "Fall 2026";
    public int TargetHours { get; set; } = 40;
    public int CompletedMinutes { get; set; }
    public string ColorHex { get; set; } = "#4F46E5";
    public string Notes { get; set; } = "";
    public List<AcademicTopic> Topics { get; set; } = new();

    public double CompletedHours => Math.Round(CompletedMinutes / 60.0, 1);
    public double ProgressPercent => TargetHours > 0 ? Math.Clamp(CompletedHours / TargetHours * 100.0, 0, 100) : 0;
}

public sealed class AcademicTopic
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string Title { get; set; } = "";
    public string Chapter { get; set; } = "Chapter 1";
    public int EstimatedMinutes { get; set; } = 60;
    public bool IsCompleted { get; set; }
}

public enum AssignmentStatus
{
    Pending = 0,
    InProgress = 1,
    Submitted = 2
}

public sealed class AcademicAssignment
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public int Priority { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;

    public int DaysRemaining => Math.Max(0, (int)Math.Ceiling((DueDateUtc - DateTime.UtcNow).TotalDays));
    public bool IsOverdue => DueDateUtc < DateTime.UtcNow && Status != AssignmentStatus.Submitted;
}

public sealed class AcademicExam
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string Title { get; set; } = "Midterm Examination";
    public DateTime ExamDateUtc { get; set; } = DateTime.UtcNow.AddDays(14);
    public int TargetScore { get; set; } = 90;
    public int SyllabusCompletedPercent { get; set; }
    public string Notes { get; set; } = "";

    public int DaysRemaining => Math.Max(0, (int)Math.Ceiling((ExamDateUtc - DateTime.UtcNow).TotalDays));
}

public sealed class Flashcard
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int RepetitionLevel { get; set; } = 0; // Box 0 - 5
    public int IntervalDays { get; set; } = 1;
    public double EaseFactor { get; set; } = 2.5; // SM-2 default
    public DateTime NextReviewUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastReviewedUtc { get; set; }

    public bool IsDue => NextReviewUtc <= DateTime.UtcNow;
}

public sealed class StudyRoom
{
    public string RoomId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string RoomCode { get; set; } = ""; // 6-character invitation code e.g. "DSA901"
    public string RoomName { get; set; } = "DSA Exam Revision Room";
    public string Subject { get; set; } = "Computer Science";
    public string Objective { get; set; } = "Complete Graph Algorithms Practice";
    public int PlannedMinutes { get; set; } = 60;
    public string OwnerNickname { get; set; } = "Host";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsSessionActive { get; set; }
    public int ElapsedSeconds { get; set; }
    public List<RoomParticipant> Participants { get; set; } = new();
    public List<RoomChatMessage> ChatMessages { get; set; } = new();
}

public sealed class RoomParticipant
{
    public string ConnectionId { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string AvatarEmoji { get; set; } = "👩‍💻";
    public string CurrentTask { get; set; } = "";
    public bool IsReady { get; set; } = true;
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RoomChatMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public string SenderNickname { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsSystem { get; set; }
}
