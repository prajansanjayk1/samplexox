using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Export;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class ExportTests : IDisposable
{
    private readonly string _tempDbPath;

    public ExportTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_export_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }

    [Fact]
    public void ExportCsvAndSummaryReport_ProducesValidOutput()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var sessionRepo = new SessionRepository(db);
        var taskRepo = new TaskRepository(db);
        var distractionRepo = new DistractionRepository(db);
        var exportService = new DataExportService(sessionRepo, taskRepo, distractionRepo);

        // Add dummy data
        sessionRepo.AddSession(new SessionLog
        {
            Task = "Test \"Quoted\" Task",
            Subject = "Mathematics",
            StartedUtc = DateTimeOffset.UtcNow.AddMinutes(-25),
            EndedUtc = DateTimeOffset.UtcNow,
            PlannedSeconds = 1500,
            ActualSeconds = 1500,
            Distractions = 1,
            EndReason = "completed"
        });

        taskRepo.AddTask(new FocusTask
        {
            Title = "Finish Thesis Intro",
            Status = TaskStatusEnum.Completed
        });

        distractionRepo.RecordEvent(new DistractionEvent
        {
            ProcessName = "steam",
            PolicyApplied = DistractionPolicyLevel.Remind,
            ActionTaken = "Reminded"
        });

        // Test Sessions CSV
        var sessionsCsv = exportService.ExportSessionsCsv();
        Assert.Contains("Test \"\"Quoted\"\" Task", sessionsCsv);
        Assert.Contains("Mathematics", sessionsCsv);

        // Test Tasks CSV
        var tasksCsv = exportService.ExportTasksCsv();
        Assert.Contains("Finish Thesis Intro", tasksCsv);

        // Test Distractions CSV
        var distractionsCsv = exportService.ExportDistractionsCsv();
        Assert.Contains("steam", distractionsCsv);

        // Test Summary Report
        var metrics = ProductivityCalculator.CalculateMetrics(sessionRepo.GetAllSessions(), taskRepo.GetAllTasks());
        var report = exportService.GenerateSummaryReport(metrics);
        Assert.Contains("QUIETORA FOCUS — PRODUCTIVITY REPORT", report);
        Assert.Contains("Total Focus Time:", report);
    }
}
