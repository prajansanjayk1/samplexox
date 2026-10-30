using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class SpacedRepetitionEngine
{
    /// <summary>
    /// Updates flashcard scheduling using the explainable SM-2 spaced-repetition algorithm.
    /// </summary>
    /// <param name="card">The flashcard to update</param>
    /// <param name="confidenceRating">Rating from 1 (Blackout) to 5 (Perfect Recall)</param>
    public static void ProcessReview(Flashcard card, int confidenceRating)
    {
        var q = Math.Clamp(confidenceRating, 1, 5);
        card.LastReviewedUtc = DateTime.UtcNow;

        // Calculate new Ease Factor (EF)
        // EF' = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
        var newEf = card.EaseFactor + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02));
        card.EaseFactor = Math.Max(1.3, Math.Round(newEf, 2));

        if (q < 3)
        {
            // Failed review: reset interval and repetition level
            card.RepetitionLevel = 0;
            card.IntervalDays = 1;
        }
        else
        {
            // Successful recall
            if (card.RepetitionLevel == 0)
            {
                card.IntervalDays = 1;
            }
            else if (card.RepetitionLevel == 1)
            {
                card.IntervalDays = 6;
            }
            else
            {
                card.IntervalDays = (int)Math.Ceiling(card.IntervalDays * card.EaseFactor);
            }

            card.RepetitionLevel++;
        }

        card.NextReviewUtc = DateTime.UtcNow.AddDays(card.IntervalDays);
    }
}
