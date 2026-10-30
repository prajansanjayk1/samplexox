using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class DetoxCalculator
{
    public static DetoxScoreReport Calculate(
        int focusMinutesToday,
        int distractionsToday,
        int doomscrollsAvertedToday,
        int distractingScreenMinutesToday,
        IEnumerable<AppBudget> budgets,
        TimeSpan bedtimeStart,
        TimeSpan bedtimeEnd)
    {
        // 1. Calculate Dopamine Time Saved
        // Research shows the average social doomscroll session is ~8.5 minutes
        var minutesSaved = (int)Math.Round(doomscrollsAvertedToday * 8.5);

        // 2. Calculate Digital Detox Score (0 - 100)
        double score = 70.0;
        score += Math.Min(focusMinutesToday / 3.0, 25.0); // Up to +25 for focus
        score -= (distractionsToday * 2.5);              // -2.5 per distraction
        score += (doomscrollsAvertedToday * 2.0);         // +2 per averted impulse

        // Check quota violations
        bool allBudgetsRespected = true;
        foreach (var b in budgets.Where(b => b.IsActive))
        {
            if (b.IsQuotaExceeded)
            {
                score -= 8.0;
                allBudgetsRespected = false;
            }
        }
        if (allBudgetsRespected && budgets.Any()) score += 5.0;

        var finalScore = (int)Math.Clamp(Math.Round(score), 15, 100);

        // 3. Bedtime Shield Status Check
        var nowTime = DateTime.Now.TimeOfDay;
        bool isBedtime;
        if (bedtimeStart > bedtimeEnd)
        {
            // Crosses midnight, e.g. 22:30 to 07:00
            isBedtime = nowTime >= bedtimeStart || nowTime < bedtimeEnd;
        }
        else
        {
            isBedtime = nowTime >= bedtimeStart && nowTime < bedtimeEnd;
        }

        // 4. Mindful Tip Synthesis
        string tip;
        if (isBedtime)
        {
            tip = "🌙 Bedtime Shield Active: Screen time is restricted to protect your sleep quality and circadian rhythm.";
        }
        else if (finalScore >= 85)
        {
            tip = "🌟 Exceptional Mindful Focus: Your dopamine levels are calm and regulated today. Keep up the deep momentum!";
        }
        else if (doomscrollsAvertedToday > 0)
        {
            tip = $"🎯 Digital Win: You prevented {doomscrollsAvertedToday} doomscroll sessions today, saving ~{minutesSaved} minutes of life.";
        }
        else
        {
            tip = "💡 Mindful Reminder: When you feel the urge to open an infinite feed, take 3 deep breaths and write down your immediate task.";
        }

        return new DetoxScoreReport
        {
            DetoxScore = finalScore,
            DoomscrollsAvertedToday = doomscrollsAvertedToday,
            TotalMinutesSavedToday = minutesSaved,
            ProductiveFocusMinutes = focusMinutesToday,
            TotalDistractingScreenMinutes = distractingScreenMinutesToday,
            IsBedtimeShieldActive = isBedtime,
            MindfulTip = tip
        };
    }
}
