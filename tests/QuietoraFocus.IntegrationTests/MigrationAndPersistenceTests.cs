using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class MigrationAndPersistenceTests : IDisposable
{
    private readonly string _tempDbPath;

    public MigrationAndPersistenceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_test_{Guid.NewGuid():N}.db");
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
    public void FreshDatabase_AppliesAllMigrationsSuccessfully()
    {
        var db = new QuietoraDatabase(_tempDbPath);

        using var conn = db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM schema_migrations;";
        var count = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(6, count); // Migrations 1 (baseline), 2 (v2.0), 3 (website shield), 4 (workspaces/automations), 5 (digital detox & budgets), 6 (v4.0 Student OS)
    }

    [Fact]
    public void AcademicRepository_SyllabusAndFlashcardReview_PersistsCorrectly()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var academicRepo = new AcademicRepository(db);

        // Subjects & Topics
        var subjects = academicRepo.GetAllSubjects();
        Assert.NotEmpty(subjects);
        var sub = subjects.First();
        Assert.NotEmpty(sub.Topics);

        // Assignments
        var assignments = academicRepo.GetAllAssignments();
        Assert.NotEmpty(assignments);
        var firstAssignment = assignments.First();
        academicRepo.UpdateAssignmentStatus(firstAssignment.Id, AssignmentStatus.Submitted);
        var updatedAssignments = academicRepo.GetAllAssignments();
        Assert.Equal(AssignmentStatus.Submitted, updatedAssignments.First(a => a.Id == firstAssignment.Id).Status);

        // Flashcards & SuperMemo SM-2 Review
        var cards = academicRepo.GetAllFlashcards();
        Assert.NotEmpty(cards);
        var card = cards.First();

        SpacedRepetitionEngine.ProcessReview(card, 5); // Perfect recall
        academicRepo.UpdateFlashcardReview(card);

        var refreshedCards = academicRepo.GetAllFlashcards();
        var refreshedCard = refreshedCards.First(c => c.Id == card.Id);
        Assert.Equal(2, refreshedCard.RepetitionLevel);
        Assert.Equal(6, refreshedCard.IntervalDays);
    }

    [Fact]
    public void GameShieldRepository_GameDetectionAndBlocking_OperatesProperly()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var gameRepo = new GameShieldRepository(db);

        var game = new DetectedGame
        {
            GameName = "Counter-Strike 2",
            ProcessName = "cs2",
            ExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe",
            Launcher = "Steam",
            IsBlocked = true
        };

        gameRepo.AddOrUpdateDetectedGame(game);
        var allGames = gameRepo.GetAllDetectedGames();
        Assert.Contains(allGames, g => g.ProcessName == "cs2");

        var cs2 = allGames.First(g => g.ProcessName == "cs2");
        Assert.True(cs2.IsBlocked);

        gameRepo.ToggleGameBlocked(cs2.Id, false);
        allGames = gameRepo.GetAllDetectedGames();
        Assert.False(allGames.First(g => g.ProcessName == "cs2").IsBlocked);

        gameRepo.RecordGameLaunch("cs2");
        allGames = gameRepo.GetAllDetectedGames();
        Assert.NotNull(allGames.First(g => g.ProcessName == "cs2").LastLaunchedUtc);
    }

    [Fact]
    public void LegacyV011Database_IsSafelyMigratedWithoutDataLoss()
    {
        // 1. Create a simulated v0.1.1 legacy database with exact old schema
        var connStr = new SqliteConnectionStringBuilder
        {
            DataSource = _tempDbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        using (var legacyConn = new SqliteConnection(connStr))
        {
            legacyConn.Open();
            using var cmd = legacyConn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE blocked_apps (
                    process_name TEXT PRIMARY KEY COLLATE NOCASE NOT NULL
                );
                CREATE TABLE sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    task TEXT NOT NULL,
                    started_utc TEXT NOT NULL,
                    ended_utc TEXT NOT NULL,
                    planned_seconds INTEGER NOT NULL,
                    actual_seconds INTEGER NOT NULL,
                    distractions INTEGER NOT NULL,
                    end_reason TEXT NOT NULL
                );
                CREATE TABLE settings (
                    key TEXT PRIMARY KEY NOT NULL,
                    value TEXT NOT NULL
                );

                INSERT INTO blocked_apps(process_name) VALUES ('discord');
                INSERT INTO settings(key, value) VALUES ('response', 'warn');
                INSERT INTO sessions(task, started_utc, ended_utc, planned_seconds, actual_seconds, distractions, end_reason)
                VALUES ('Legacy Study Session', '2026-10-28T10:00:00.0000000Z', '2026-10-28T10:25:00.0000000Z', 1500, 1500, 0, 'completed');
                """;
            cmd.ExecuteNonQuery();
        }

        // 2. Open with v2.0 QuietoraDatabase to execute automated migration
        var db = new QuietoraDatabase(_tempDbPath);
        var sessionRepo = new SessionRepository(db);
        var distractionRepo = new DistractionRepository(db);
        var settingsRepo = new SettingsRepository(db);

        // 3. Verify legacy data is preserved perfectly
        var sessions = sessionRepo.GetAllSessions();
        Assert.Single(sessions);
        Assert.Equal("Legacy Study Session", sessions[0].Task);
        Assert.Equal(1500, sessions[0].ActualSeconds);
        Assert.Equal("completed", sessions[0].EndReason);

        var blocked = distractionRepo.ReadBlockedApps();
        Assert.Contains("discord", blocked);

        var responseSetting = settingsRepo.GetSetting("response", "warn");
        Assert.Equal("warn", responseSetting);

        // 4. Verify v2.0 columns exist on the migrated table
        var newSession = new SessionLog
        {
            Task = "v2.0 Deep Focus",
            Subject = "Computer Science",
            ProjectName = "Quietora Core",
            Mode = "DeepFocus",
            StartedUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            EndedUtc = DateTimeOffset.UtcNow,
            PlannedSeconds = 1800,
            ActualSeconds = 1800,
            PausedSeconds = 45,
            Distractions = 1,
            EndReason = "completed",
            Notes = "Studying SQLite migration patterns"
        };
        sessionRepo.AddSession(newSession);

        var all = sessionRepo.GetAllSessions();
        Assert.Equal(2, all.Count);
        Assert.Equal("Computer Science", all[0].Subject);
        Assert.Equal("Quietora Core", all[0].ProjectName);
        Assert.Equal(45, all[0].PausedSeconds);
    }

    [Fact]
    public void TaskRepository_LifecycleOperationsWorkCorrectly()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var taskRepo = new TaskRepository(db);

        var task = new FocusTask
        {
            Title = "Implement FTS5 PC Memory",
            Description = "Full-text indexing with SQLite FTS5",
            Priority = TaskPriority.Urgent,
            Status = TaskStatusEnum.Pending,
            DueDateUtc = DateTimeOffset.UtcNow.AddDays(2),
            Subject = "Engineering",
            EstimatedMinutes = 60
        };

        taskRepo.AddTask(task);
        Assert.True(task.Id > 0);

        var tasks = taskRepo.GetAllTasks();
        Assert.Single(tasks);
        Assert.Equal("Implement FTS5 PC Memory", tasks[0].Title);
        Assert.Equal(TaskPriority.Urgent, tasks[0].Priority);

        // Toggle status
        taskRepo.ToggleTaskStatus(task.Id);
        var updated = taskRepo.GetAllTasks();
        Assert.Equal(TaskStatusEnum.Completed, updated[0].Status);
        Assert.NotNull(updated[0].CompletedUtc);

        // Delete
        taskRepo.DeleteTask(task.Id);
        Assert.Empty(taskRepo.GetAllTasks());
    }
}
