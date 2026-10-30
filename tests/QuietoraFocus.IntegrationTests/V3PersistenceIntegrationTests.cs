using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class V3PersistenceIntegrationTests : IDisposable
{
    private readonly string _tempDbPath;

    public V3PersistenceIntegrationTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_v3_test_{Guid.NewGuid():N}.db");
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
    public void WorkspaceRepository_CanAddRetrieveAndDeleteSnapshots()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new WorkspaceRepository(db);

        var snapshot = new WorkspaceSnapshot
        {
            Name = "Machine Learning Project",
            ProjectPath = @"C:\Projects\ML",
            Description = "Transformer attention weights training",
            RecentFilePaths = new List<string> { @"C:\Projects\ML\train.py", @"C:\Projects\ML\config.yaml" },
            AssociatedTaskIds = new List<long> { 1, 2, 3 },
            Notes = "Check validation loss curves",
            SavedUtc = DateTimeOffset.UtcNow
        };

        repo.AddSnapshot(snapshot);
        Assert.True(snapshot.Id > 0);

        var retrieved = repo.GetById(snapshot.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Machine Learning Project", retrieved.Name);
        Assert.Equal(@"C:\Projects\ML", retrieved.ProjectPath);
        Assert.Equal(2, retrieved.RecentFilePaths.Count);
        Assert.Equal(3, retrieved.AssociatedTaskIds.Count);

        var all = repo.GetAllSnapshots();
        Assert.Contains(all, s => s.Id == snapshot.Id);

        repo.DeleteSnapshot(snapshot.Id);
        Assert.Null(repo.GetById(snapshot.Id));
    }

    [Fact]
    public void AutomationRepository_HasDefaultPresetsAndSupportsCrud()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new AutomationRepository(db);

        // Verify default preset automations added by migration 4
        var rules = repo.GetAllRules();
        Assert.NotEmpty(rules);
        Assert.Contains(rules, r => r.Title == "Morning Focus Reminder");

        // Add custom rule
        var custom = new AutomationRule
        {
            Title = "Stand Up and Stretch",
            Description = "Health prompt every hour",
            TriggerType = AutomationTriggerType.ScheduledTime,
            TriggerConfig = "14:00",
            ActionType = AutomationActionType.NotifyUser,
            ActionPayload = "Stand up and drink some water!",
            IsEnabled = true
        };

        repo.AddRule(custom);
        Assert.True(custom.Id > 0);

        // Toggle rule
        repo.ToggleRule(custom.Id, false);
        var updated = repo.GetAllRules().First(r => r.Id == custom.Id);
        Assert.False(updated.IsEnabled);

        // Delete rule
        repo.DeleteRule(custom.Id);
        Assert.DoesNotContain(repo.GetAllRules(), r => r.Id == custom.Id);
    }
}
