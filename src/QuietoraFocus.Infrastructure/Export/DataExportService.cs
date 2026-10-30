using System.Text;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Repositories;

namespace QuietoraFocus.Infrastructure.Export;

public sealed class DataExportService
{
    private readonly SessionRepository _sessionRepo;
    private readonly TaskRepository _taskRepo;
    private readonly DistractionRepository _distractionRepo;

    public DataExportService(
        SessionRepository sessionRepo,
        TaskRepository taskRepo,
        DistractionRepository distractionRepo)
    {
        _sessionRepo = sessionRepo;
        _taskRepo = taskRepo;
        _distractionRepo = distractionRepo;
    }

    public string ExportSessionsCsv()
    {
        var sessions = _sessionRepo.GetAllSessions();
        var sb = new StringBuilder();
        sb.AppendLine("Id,Task,Subject,ProjectName,Mode,StartedUtc,EndedUtc,PlannedSeconds,ActualSeconds,PausedSeconds,Distractions,EndReason,Notes");

        foreach (var s in sessions)
        {
            sb.AppendLine(string.Join(",",
                s.Id,
                EscapeCsv(s.Task),
                EscapeCsv(s.Subject),
                EscapeCsv(s.ProjectName),
                EscapeCsv(s.Mode),
                s.StartedUtc.ToString("O"),
                s.EndedUtc.ToString("O"),
                s.PlannedSeconds,
                s.ActualSeconds,
                s.PausedSeconds,
                s.Distractions,
                EscapeCsv(s.EndReason),
                EscapeCsv(s.Notes)
            ));
        }

        return sb.ToString();
    }

    public string ExportTasksCsv()
    {
        var tasks = _taskRepo.GetAllTasks();
        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Description,Priority,Status,DueDateUtc,Subject,EstimatedMinutes,ActualMinutes,CreatedUtc,CompletedUtc");

        foreach (var t in tasks)
        {
            sb.AppendLine(string.Join(",",
                t.Id,
                EscapeCsv(t.Title),
                EscapeCsv(t.Description),
                t.Priority,
                t.Status,
                t.DueDateUtc?.ToString("O") ?? "",
                EscapeCsv(t.Subject),
                t.EstimatedMinutes,
                t.ActualMinutes,
                t.CreatedUtc.ToString("O"),
                t.CompletedUtc?.ToString("O") ?? ""
            ));
        }

        return sb.ToString();
    }

    public string ExportDistractionsCsv()
    {
        var events = _distractionRepo.GetRecentEvents(500);
        var sb = new StringBuilder();
        sb.AppendLine("Id,SessionId,ProcessName,TimestampUtc,PolicyApplied,ActionTaken");

        foreach (var e in events)
        {
            sb.AppendLine(string.Join(",",
                e.Id,
                e.SessionId?.ToString() ?? "",
                EscapeCsv(e.ProcessName),
                e.TimestampUtc.ToString("O"),
                e.PolicyApplied,
                EscapeCsv(e.ActionTaken)
            ));
        }

        return sb.ToString();
    }

    public string GenerateSummaryReport(ProductivityMetrics metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=================================================");
        sb.AppendLine("          QUIETORA FOCUS — PRODUCTIVITY REPORT   ");
        sb.AppendLine($"          Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}       ");
        sb.AppendLine("=================================================");
        sb.AppendLine();
        sb.AppendLine("1. SUMMARY METRICS");
        sb.AppendLine($"   • Total Focus Time:       {metrics.TotalFocusMinutes} minutes ({metrics.TotalFocusMinutes / 60.0:F1} hours)");
        sb.AppendLine($"   • Today's Focus:          {metrics.TodayFocusMinutes} minutes");
        sb.AppendLine($"   • This Week's Focus:      {metrics.WeeklyFocusMinutes} minutes");
        sb.AppendLine($"   • Session Completion:     {metrics.SessionCompletionRate}% ({metrics.CompletedSessionsCount}/{metrics.TotalSessionsCount} sessions)");
        sb.AppendLine($"   • Current Daily Streak:   {metrics.CurrentStreakDays} days (Best: {metrics.BestStreakDays} days)");
        sb.AppendLine($"   • Total Interventions:    {metrics.TotalDistractions} distractions");
        sb.AppendLine($"   • Completed Tasks:        {metrics.CompletedTasksCount} tasks ({metrics.PendingTasksCount} pending)");
        sb.AppendLine();
        sb.AppendLine("2. PRODUCTIVITY SCORE NOTE");
        sb.AppendLine($"   {metrics.ScoreFormulaExplanation}");
        sb.AppendLine();
        sb.AppendLine("=================================================");
        sb.AppendLine("   QuietoraLabs • 100% Offline • Private to PC  ");
        sb.AppendLine("=================================================");
        return sb.ToString();
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
