using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class ProductivityCalculator
{
    public static ProductivityMetrics CalculateMetrics(
        IEnumerable<SessionLog> sessions,
        IEnumerable<FocusTask> tasks,
        DateTime? referenceDate = null)
    {
        var targetDate = (referenceDate ?? DateTime.Today).Date;
        var startOfWeek = targetDate.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday);
        if (targetDate.DayOfWeek == DayOfWeek.Sunday)
            startOfWeek = targetDate.AddDays(-6);

        var sessionList = sessions.ToList();
        var taskList = tasks.ToList();

        var totalSessions = sessionList.Count;
        var completedSessions = sessionList.Count(s => s.EndReason == "completed");
        var totalMinutes = sessionList.Sum(s => s.ActualSeconds) / 60;

        var todaySessions = sessionList.Where(s => s.StartedUtc.ToLocalTime().Date == targetDate).ToList();
        var todayMinutes = todaySessions.Sum(s => s.ActualSeconds) / 60;
        var todayDistractions = todaySessions.Sum(s => s.Distractions);

        var weekSessions = sessionList.Where(s => s.StartedUtc.ToLocalTime().Date >= startOfWeek).ToList();
        var weekMinutes = weekSessions.Sum(s => s.ActualSeconds) / 60;

        var totalDistractions = sessionList.Sum(s => s.Distractions);
        var longestSession = sessionList.Count > 0 ? sessionList.Max(s => s.ActualSeconds) / 60 : 0;

        var (currentStreak, bestStreak) = StreakCalculator.Calculate(sessionList, targetDate);

        var completionRate = totalSessions > 0
            ? Math.Round((double)completedSessions / totalSessions * 100.0, 1)
            : 0.0;

        var completedTasks = taskList.Count(t => t.Status == TaskStatusEnum.Completed);
        var pendingTasks = taskList.Count(t => t.Status is TaskStatusEnum.Pending or TaskStatusEnum.InProgress);

        return new ProductivityMetrics
        {
            TotalSessionsCount = totalSessions,
            CompletedSessionsCount = completedSessions,
            TotalFocusMinutes = totalMinutes,
            TodayFocusMinutes = todayMinutes,
            WeeklyFocusMinutes = weekMinutes,
            SessionCompletionRate = completionRate,
            TotalDistractions = totalDistractions,
            TodayDistractions = todayDistractions,
            LongestSessionMinutes = longestSession,
            CurrentStreakDays = currentStreak,
            BestStreakDays = bestStreak,
            CompletedTasksCount = completedTasks,
            PendingTasksCount = pendingTasks
        };
    }

    public static List<DailyActivityPoint> GetPastSevenDaysActivity(IEnumerable<SessionLog> sessions, DateTime? referenceDate = null)
    {
        var targetDate = (referenceDate ?? DateTime.Today).Date;
        var result = new List<DailyActivityPoint>();
        var sessionList = sessions.ToList();

        for (int i = 6; i >= 0; i--)
        {
            var date = targetDate.AddDays(-i);
            var daySessions = sessionList.Where(s => s.StartedUtc.ToLocalTime().Date == date).ToList();

            result.Add(new DailyActivityPoint
            {
                Date = date,
                DayName = date.ToString("ddd"),
                FocusMinutes = daySessions.Sum(s => s.ActualSeconds) / 60,
                Distractions = daySessions.Sum(s => s.Distractions),
                CompletedSessions = daySessions.Count(s => s.EndReason == "completed")
            });
        }

        return result;
    }

    public static List<RuleBasedRecommendation> GenerateRecommendations(
        ProductivityMetrics metrics,
        IEnumerable<SessionLog> recentSessions,
        IEnumerable<FocusTask> tasks)
    {
        var recommendations = new List<RuleBasedRecommendation>();
        var sessionList = recentSessions.ToList();

        // 1. Duration recommendation
        if (sessionList.Count >= 3)
        {
            var stoppedEarly = sessionList.Count(s => s.EndReason != "completed");
            if ((double)stoppedEarly / sessionList.Count > 0.4)
            {
                recommendations.Add(new RuleBasedRecommendation
                {
                    Title = "Try shorter 20-25 minute sessions",
                    Description = "Over 40% of recent sessions were ended early. Shortening session lengths builds momentum with higher completion confidence.",
                    Category = "Duration",
                    Severity = "Tip"
                });
            }
            else
            {
                recommendations.Add(new RuleBasedRecommendation
                {
                    Title = "Great completion stamina!",
                    Description = "Your completion rate is high. You can experiment with 40-50 minute deep focus blocks for intensive tasks.",
                    Category = "Duration",
                    Severity = "Info"
                });
            }
        }

        // 2. Distraction patterns
        if (metrics.TodayDistractions > 5)
        {
            recommendations.Add(new RuleBasedRecommendation
            {
                Title = "High distraction count detected today",
                Description = $"You recorded {metrics.TodayDistractions} distraction prompts today. Consider setting policy to Level 3 (Minimize) or closing background chat apps before starting.",
                Category = "Distraction",
                Severity = "Warning"
            });
        }

        // 3. Streak recommendation
        if (metrics.CurrentStreakDays >= 3)
        {
            recommendations.Add(new RuleBasedRecommendation
            {
                Title = $"{metrics.CurrentStreakDays}-Day Focus Streak Active",
                Description = "Consistent daily practice produces lasting cognitive retention. Keep at least one short focus block today to maintain your streak.",
                Category = "General",
                Severity = "Info"
            });
        }

        // 4. Overdue or urgent tasks
        var overdueTasks = tasks.Count(t => t.Status == TaskStatusEnum.Pending && t.DueDateUtc.HasValue && t.DueDateUtc.Value < DateTimeOffset.UtcNow);
        if (overdueTasks > 0)
        {
            recommendations.Add(new RuleBasedRecommendation
            {
                Title = $"{overdueTasks} Overdue Task(s)",
                Description = "You have tasks past their target deadline. Schedule a dedicated 'Work Mode' session to clear these bottlenecks.",
                Category = "Task",
                Severity = "Warning"
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new RuleBasedRecommendation
            {
                Title = "Ready for your next deep focus block",
                Description = "Select an active task and start a 25-minute Pomodoro or Deep Focus session.",
                Category = "General",
                Severity = "Info"
            });
        }

        return recommendations;
    }
}
