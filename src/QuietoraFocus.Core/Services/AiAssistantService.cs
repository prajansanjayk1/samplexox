using System.Text;
using System.Text.RegularExpressions;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public sealed class AiAssistantService
{
    private static readonly Regex InjectionPattern = new(
        @"(ignore\s+(all\s+)?(previous|prior)\s+instructions|system\s+prompt|powershell|cmd\.exe|rmdir|format\s+c:|<script>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AiQueryResponse ProcessQuery(
        string userQuery,
        IEnumerable<MemorySearchResult> retrievedDocs,
        IEnumerable<SessionLog> sessionHistory,
        IEnumerable<FocusTask> tasks,
        AiAssistantMode mode = AiAssistantMode.Lite)
    {
        var cleanQuery = (userQuery ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return new AiQueryResponse
            {
                Query = cleanQuery,
                Answer = "Please enter a question or topic to search across your local workspace.",
                HasSufficientEvidence = false
            };
        }

        // 1. Prompt Injection & Adversarial Security Guard
        if (InjectionPattern.IsMatch(cleanQuery))
        {
            return new AiQueryResponse
            {
                Query = cleanQuery,
                Answer = "Security Notice: The input contains potentially unsafe instructional directives. As a local privacy-first assistant, system commands and script execution are strictly disallowed.",
                HasSufficientEvidence = false
            };
        }

        var lowerQuery = cleanQuery.ToLowerInvariant();
        var citations = new List<AiSourceCitation>();

        // 2. Focus History Question Check
        if (lowerQuery.Contains("focus") || lowerQuery.Contains("streak") || lowerQuery.Contains("session") || lowerQuery.Contains("minutes"))
        {
            var sessions = sessionHistory.ToList();
            var totalMins = sessions.Sum(s => s.ActualSeconds) / 60;
            var completed = sessions.Count(s => s.EndReason == "completed");
            var (streak, best) = StreakCalculator.Calculate(sessions);

            var sb = new StringBuilder();
            sb.AppendLine($"Based on your local SQLite records ({sessions.Count} recorded sessions):");
            sb.AppendLine($"• Total focused time: {totalMins} minutes ({totalMins / 60.0:F1} hours).");
            sb.AppendLine($"• Completed sessions: {completed} out of {sessions.Count} sessions.");
            sb.AppendLine($"• Current daily streak: {streak} consecutive active days (All-time best: {best} days).");

            if (sessions.Count > 0)
            {
                var last = sessions.First();
                sb.AppendLine($"• Most recent session: '{last.Task}' ({last.ActualSeconds / 60}m) on {last.DateLabel}.");
            }

            return new AiQueryResponse
            {
                Query = cleanQuery,
                Answer = sb.ToString(),
                HasSufficientEvidence = true
            };
        }

        // 3. Tasks Question Check
        if (lowerQuery.Contains("task") || lowerQuery.Contains("todo") || lowerQuery.Contains("overdue") || lowerQuery.Contains("assignment"))
        {
            var allTasks = tasks.ToList();
            var pending = allTasks.Where(t => t.Status != TaskStatusEnum.Completed).ToList();
            var overdue = pending.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value < DateTimeOffset.UtcNow).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Here is the current status of your local tasks ({allTasks.Count} total):");
            sb.AppendLine($"• Pending tasks: {pending.Count}");
            sb.AppendLine($"• Overdue tasks: {overdue.Count}");

            if (overdue.Count > 0)
            {
                sb.AppendLine("\n⚠️ Overdue Bottlenecks:");
                foreach (var t in overdue.Take(5))
                {
                    sb.AppendLine($"  - [{t.Priority}] {t.Title} ({t.Subject}) - Due: {t.DueDateLabel}");
                }
            }

            if (pending.Count > 0)
            {
                sb.AppendLine("\n📋 Upcoming Priorities:");
                foreach (var t in pending.OrderByDescending(t => t.Priority).Take(5))
                {
                    sb.AppendLine($"  - [{t.Priority}] {t.Title} (Est: {t.EstimatedMinutes}m)");
                }
            }

            return new AiQueryResponse
            {
                Query = cleanQuery,
                Answer = sb.ToString(),
                HasSufficientEvidence = true
            };
        }

        // 4. Document Grounded Retrieval (RAG)
        var docs = retrievedDocs.Take(5).ToList();
        if (docs.Count == 0)
        {
            return new AiQueryResponse
            {
                Query = cleanQuery,
                Answer = $"No matching content found in your currently approved folders for '{cleanQuery}'.\n\nTips:\n• Verify that the target folder is added in PC Memory.\n• Click 'Scan & Build Memory' to refresh indexed content.",
                HasSufficientEvidence = false
            };
        }

        foreach (var doc in docs)
        {
            citations.Add(new AiSourceCitation
            {
                Title = doc.FileName,
                FilePath = doc.FilePath,
                Snippet = doc.Snippet,
                RelevanceScore = doc.Rank
            });
        }

        var ansBuilder = new StringBuilder();
        ansBuilder.AppendLine($"Grounded search across your local indexed workspace found {citations.Count} relevant source(s):");
        ansBuilder.AppendLine();

        foreach (var c in citations)
        {
            ansBuilder.AppendLine($"📄 **{c.Title}**");
            ansBuilder.AppendLine($"   Path: `{c.FilePath}`");
            ansBuilder.AppendLine($"   Relevant Excerpt: \"{c.Snippet}\"");
            ansBuilder.AppendLine();
        }

        ansBuilder.AppendLine("All information is referenced directly from local documents on this PC.");

        return new AiQueryResponse
        {
            Query = cleanQuery,
            Answer = ansBuilder.ToString().TrimEnd(),
            Citations = citations,
            HasSufficientEvidence = true
        };
    }
}
