using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public sealed class AutomationTriggerContext
{
    public AutomationTriggerType TriggerType { get; init; }
    public SessionLog? CompletedSession { get; init; }
    public int TodayDistractionsCount { get; init; }
    public DateTime CurrentTime { get; init; } = DateTime.Now;
}

public sealed class AutomationExecutionResult
{
    public AutomationRule Rule { get; init; } = new();
    public bool DidTrigger { get; init; }
    public string ExecutionMessage { get; init; } = "";
}

public sealed class AutomationEngine
{
    public List<AutomationExecutionResult> EvaluateRules(
        IEnumerable<AutomationRule> rules,
        AutomationTriggerContext context)
    {
        var results = new List<AutomationExecutionResult>();

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            if (rule.TriggerType != context.TriggerType) continue;

            bool shouldFire = false;
            string message = "";

            switch (rule.TriggerType)
            {
                case AutomationTriggerType.ScheduledTime:
                    var currentHhMm = context.CurrentTime.ToString("HH:mm");
                    if (string.Equals(rule.TriggerConfig.Trim(), currentHhMm, StringComparison.OrdinalIgnoreCase))
                    {
                        shouldFire = true;
                        message = $"Scheduled time reached: {currentHhMm}";
                    }
                    break;

                case AutomationTriggerType.FocusSessionCompleted:
                    if (context.CompletedSession != null)
                    {
                        shouldFire = true;
                        message = $"Focus session completed: '{context.CompletedSession.Task}' ({context.CompletedSession.ActualSeconds / 60}m)";
                    }
                    break;

                case AutomationTriggerType.DistractionThresholdExceeded:
                    if (int.TryParse(rule.TriggerConfig, out var threshold) && context.TodayDistractionsCount >= threshold)
                    {
                        // Rate limit rule to once per hour
                        if (!rule.LastTriggeredUtc.HasValue || (DateTimeOffset.UtcNow - rule.LastTriggeredUtc.Value).TotalHours >= 1)
                        {
                            shouldFire = true;
                            message = $"Today's distraction count ({context.TodayDistractionsCount}) exceeded threshold ({threshold}).";
                        }
                    }
                    break;
            }

            if (shouldFire)
            {
                rule.LastTriggeredUtc = DateTimeOffset.UtcNow;
                results.Add(new AutomationExecutionResult
                {
                    Rule = rule,
                    DidTrigger = true,
                    ExecutionMessage = message
                });
            }
        }

        return results;
    }
}
