using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class SemanticRanker
{
    public static List<MemorySearchResult> HybridRerank(
        string query,
        IEnumerable<MemorySearchResult> candidateResults)
    {
        var candidates = candidateResults.ToList();
        if (string.IsNullOrWhiteSpace(query) || candidates.Count <= 1)
            return candidates;

        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0) return candidates;

        // Compute semantic cosine score for each candidate snippet
        var scoredList = new List<(MemorySearchResult Result, double HybridScore)>();

        foreach (var c in candidates)
        {
            var docText = $"{c.FileName} {c.Snippet}";
            var docTerms = Tokenize(docText);
            var cosineSim = ComputeCosineSimilarity(queryTerms, docTerms);

            // Hybrid score: normalized FTS5 BM25 + Cosine Semantic Similarity
            // In SQLite FTS5, lower BM25 is better (negative), so we invert or blend
            var normalizedFts = c.Rank != 0 ? Math.Abs(c.Rank) : 1.0;
            var hybridScore = (cosineSim * 0.7) + (1.0 / (1.0 + normalizedFts) * 0.3);

            scoredList.Add((c, hybridScore));
        }

        return scoredList
            .OrderByDescending(x => x.HybridScore)
            .Select(x =>
            {
                x.Result.Rank = Math.Round(x.HybridScore, 3);
                return x.Result;
            })
            .ToList();
    }

    public static List<string> Tokenize(string text)
    {
        return text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '-', '_', '/', '\\', '\"', '\'' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .ToList();
    }

    public static double ComputeCosineSimilarity(List<string> queryTerms, List<string> docTerms)
    {
        if (queryTerms.Count == 0 || docTerms.Count == 0) return 0.0;

        var allWords = queryTerms.Union(docTerms).Distinct().ToList();
        var qVec = new double[allWords.Count];
        var dVec = new double[allWords.Count];

        var qFreq = queryTerms.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        var dFreq = docTerms.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());

        for (int i = 0; i < allWords.Count; i++)
        {
            var word = allWords[i];
            qVec[i] = qFreq.TryGetValue(word, out var qCount) ? qCount : 0;
            dVec[i] = dFreq.TryGetValue(word, out var dCount) ? dCount : 0;
        }

        double dotProduct = 0.0;
        double qNorm = 0.0;
        double dNorm = 0.0;

        for (int i = 0; i < allWords.Count; i++)
        {
            dotProduct += qVec[i] * dVec[i];
            qNorm += qVec[i] * qVec[i];
            dNorm += dVec[i] * dVec[i];
        }

        if (qNorm == 0 || dNorm == 0) return 0.0;
        return dotProduct / (Math.Sqrt(qNorm) * Math.Sqrt(dNorm));
    }
}
