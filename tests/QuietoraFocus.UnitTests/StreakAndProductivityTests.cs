using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using Xunit;

namespace QuietoraFocus.UnitTests;

public class StreakAndProductivityTests
{
    [Fact]
    public void Streak_EmptyList_ReturnsZero()
    {
        var (current, best) = StreakCalculator.Calculate(Enumerable.Empty<SessionLog>());
        Assert.Equal(0, current);
        Assert.Equal(0, best);
    }

    [Fact]
    public void Streak_ConsecutiveDays_CalculatesAccurately()
    {
        var today = new DateTime(2026, 10, 29);
        var sessions = new List<SessionLog>
        {
            new() { StartedUtc = today.AddDays(0).ToUniversalTime(), ActualSeconds = 1500, EndReason = "completed" },
            new() { StartedUtc = today.AddDays(-1).ToUniversalTime(), ActualSeconds = 1200, EndReason = "completed" },
            new() { StartedUtc = today.AddDays(-2).ToUniversalTime(), ActualSeconds = 900, EndReason = "completed" },
            new() { StartedUtc = today.AddDays(-5).ToUniversalTime(), ActualSeconds = 1800, EndReason = "completed" }
        };

        var (current, best) = StreakCalculator.Calculate(sessions, today);
        Assert.Equal(3, current);
        Assert.Equal(3, best);
    }

    [Fact]
    public void Streak_IgnoresVeryShortAccidentalSessions()
    {
        var today = new DateTime(2026, 10, 29);
        var sessions = new List<SessionLog>
        {
            // Only 15 seconds: should not count toward streak
            new() { StartedUtc = today.ToUniversalTime(), ActualSeconds = 15, EndReason = "stopped" }
        };

        var (current, _) = StreakCalculator.Calculate(sessions, today);
        Assert.Equal(0, current);
    }

    [Fact]
    public void ProductivityMetrics_CalculatesCorrectAggregates()
    {
        var today = new DateTime(2026, 10, 29);
        var sessions = new List<SessionLog>
        {
            new()
            {
                StartedUtc = today.ToUniversalTime(),
                ActualSeconds = 1800,
                Distractions = 1,
                EndReason = "completed"
            },
            new()
            {
                StartedUtc = today.ToUniversalTime(),
                ActualSeconds = 1200,
                Distractions = 2,
                EndReason = "completed"
            },
            new()
            {
                StartedUtc = today.AddDays(-1).ToUniversalTime(),
                ActualSeconds = 3000,
                Distractions = 0,
                EndReason = "completed"
            }
        };

        var tasks = new List<FocusTask>
        {
            new() { Title = "Task 1", Status = TaskStatusEnum.Completed },
            new() { Title = "Task 2", Status = TaskStatusEnum.Pending }
        };

        var metrics = ProductivityCalculator.CalculateMetrics(sessions, tasks, today);

        Assert.Equal(3, metrics.TotalSessionsCount);
        Assert.Equal(3, metrics.CompletedSessionsCount);
        Assert.Equal(100.0, metrics.SessionCompletionRate);
        Assert.Equal(100, metrics.TotalFocusMinutes); // (1800 + 1200 + 3000) / 60
        Assert.Equal(50, metrics.TodayFocusMinutes);  // (1800 + 1200) / 60
        Assert.Equal(3, metrics.TotalDistractions);
        Assert.Equal(3, metrics.TodayDistractions);
        Assert.Equal(50, metrics.LongestSessionMinutes);
        Assert.Equal(1, metrics.CompletedTasksCount);
        Assert.Equal(1, metrics.PendingTasksCount);
    }

    [Fact]
    public void PastSevenDaysActivity_ReturnsSevenOrderedDays()
    {
        var today = new DateTime(2026, 10, 29);
        var sessions = new List<SessionLog>
        {
            new() { StartedUtc = today.ToUniversalTime(), ActualSeconds = 1200, Distractions = 1, EndReason = "completed" }
        };

        var points = ProductivityCalculator.GetPastSevenDaysActivity(sessions, today);

        Assert.Equal(7, points.Count);
        Assert.Equal(today.Date, points.Last().Date);
        Assert.Equal(20, points.Last().FocusMinutes);
        Assert.Equal(1, points.Last().Distractions);
    }

    [Fact]
    public void RuleBasedRecommendations_GeneratesSensibleTips()
    {
        var metrics = new ProductivityMetrics
        {
            TodayDistractions = 8,
            CurrentStreakDays = 4,
            SessionCompletionRate = 90
        };

        var tasks = new List<FocusTask>
        {
            new()
            {
                Title = "Overdue assignment",
                Status = TaskStatusEnum.Pending,
                DueDateUtc = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        var recommendations = ProductivityCalculator.GenerateRecommendations(metrics, Enumerable.Empty<SessionLog>(), tasks);

        Assert.Contains(recommendations, r => r.Category == "Distraction");
        Assert.Contains(recommendations, r => r.Category == "General" && r.Title.Contains("Streak"));
        Assert.Contains(recommendations, r => r.Category == "Task" && r.Title.Contains("Overdue"));
    }
}
