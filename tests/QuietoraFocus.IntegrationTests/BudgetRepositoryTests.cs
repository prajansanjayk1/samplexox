using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class BudgetRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;

    public BudgetRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_budget_test_{Guid.NewGuid():N}.db");
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
    public void BudgetRepository_SeedsDefaultBudgetsViaMigration5()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new BudgetRepository(db);

        var budgets = repo.GetAllBudgets();

        Assert.NotEmpty(budgets);
        Assert.Contains(budgets, b => b.TargetName == "youtube.com" && b.DailyLimitMinutes == 30);
        Assert.Contains(budgets, b => b.TargetName == "reddit.com" && b.DailyLimitMinutes == 20);
        Assert.Contains(budgets, b => b.TargetName == "discord" && b.DailyLimitMinutes == 45);
    }

    [Fact]
    public void BudgetRepository_IncrementsUsageAndSupportsLockCalculations()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new BudgetRepository(db);

        var custom = new AppBudget
        {
            TargetName = "twitter.com",
            TargetType = "website",
            DisplayName = "Twitter / X",
            DailyLimitMinutes = 10,
            IsActive = true
        };

        repo.AddOrUpdateBudget(custom);
        Assert.True(custom.Id > 0);

        // Increment usage by 600 seconds (10 minutes)
        repo.IncrementUsage("twitter.com", 600);

        var retrieved = repo.GetAllBudgets().First(b => b.TargetName == "twitter.com");
        Assert.Equal(600, retrieved.UsedSecondsToday);
        Assert.Equal(10, retrieved.UsedMinutesToday);
        Assert.True(retrieved.IsQuotaExceeded);
        Assert.Equal(0, retrieved.RemainingMinutes);

        // Toggle budget
        repo.ToggleBudgetActive(custom.Id, false);
        var updated = repo.GetAllBudgets().First(b => b.TargetName == "twitter.com");
        Assert.False(updated.IsActive);

        // Delete budget
        repo.DeleteBudget(custom.Id);
        Assert.DoesNotContain(repo.GetAllBudgets(), b => b.TargetName == "twitter.com");
    }
}
