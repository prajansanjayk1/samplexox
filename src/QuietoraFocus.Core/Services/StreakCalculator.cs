using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class StreakCalculator
{
    public static (int currentStreak, int bestStreak) Calculate(IEnumerable<SessionLog> sessions, DateTime? referenceDate = null)
    {
        var targetDate = (referenceDate ?? DateTime.Today).Date;

        var activeDates = sessions
            .Where(s => s.ActualSeconds >= 60) // At least 1 minute of real focus
            .Select(s => s.StartedUtc.ToLocalTime().Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (activeDates.Count == 0) return (0, 0);

        int currentStreak = 0;
        var checkDate = targetDate;

        // If today has no sessions, allow streak to continue if yesterday was active
        if (!activeDates.Contains(checkDate))
        {
            checkDate = checkDate.AddDays(-1);
        }

        while (activeDates.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        // Calculate best historical streak
        var sortedAsc = activeDates.OrderBy(d => d).ToList();
        int bestStreak = 0;
        int runningStreak = 0;
        DateTime? previousDate = null;

        foreach (var date in sortedAsc)
        {
            if (previousDate == null || date == previousDate.Value.AddDays(1))
            {
                runningStreak++;
            }
            else
            {
                runningStreak = 1;
            }

            if (runningStreak > bestStreak)
            {
                bestStreak = runningStreak;
            }

            previousDate = date;
        }

        return (currentStreak, Math.Max(currentStreak, bestStreak));
    }
}
