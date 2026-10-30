using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using Xunit;

namespace QuietoraFocus.UnitTests;

public class V4StudentOsTests
{
    [Fact]
    public void SpacedRepetition_SuperMemo2_PerfectRecallProgression()
    {
        var card = new Flashcard
        {
            Question = "What is the time complexity of QuickSelect average case?",
            Answer = "O(N)",
            EaseFactor = 2.5,
            RepetitionLevel = 0,
            IntervalDays = 0
        };

        // First successful review (q = 5)
        SpacedRepetitionEngine.ProcessReview(card, 5);
        Assert.Equal(1, card.RepetitionLevel);
        Assert.Equal(1, card.IntervalDays);
        Assert.True(card.EaseFactor > 2.5); // EF increases

        // Second successful review (q = 5)
        var previousEf = card.EaseFactor;
        SpacedRepetitionEngine.ProcessReview(card, 5);
        Assert.Equal(2, card.RepetitionLevel);
        Assert.Equal(6, card.IntervalDays);
        Assert.True(card.EaseFactor >= previousEf);

        // Third successful review (q = 5)
        SpacedRepetitionEngine.ProcessReview(card, 5);
        Assert.Equal(3, card.RepetitionLevel);
        Assert.True(card.IntervalDays >= 16); // 6 * ~2.7 = ~17 days
    }

    [Fact]
    public void SpacedRepetition_SuperMemo2_FailureResetsRepetition()
    {
        var card = new Flashcard
        {
            Question = "Explain Amdahl's Law",
            Answer = "Speedup is bounded by sequential fraction",
            EaseFactor = 2.5,
            RepetitionLevel = 3,
            IntervalDays = 16
        };

        // Failure rating (q = 1: complete blackout)
        SpacedRepetitionEngine.ProcessReview(card, 1);
        Assert.Equal(0, card.RepetitionLevel);
        Assert.Equal(1, card.IntervalDays);
        Assert.True(card.EaseFactor < 2.5);
        Assert.True(card.EaseFactor >= 1.3); // SM-2 lower bound
    }

    [Fact]
    public void SpacedRepetition_EaseFactor_NeverDropsBelowMinimum()
    {
        var card = new Flashcard
        {
            Question = "Test Card",
            Answer = "Test Answer",
            EaseFactor = 1.35,
            RepetitionLevel = 1,
            IntervalDays = 1
        };

        // Review with low rating repeatedly
        SpacedRepetitionEngine.ProcessReview(card, 0);
        SpacedRepetitionEngine.ProcessReview(card, 0);
        SpacedRepetitionEngine.ProcessReview(card, 0);

        Assert.Equal(1.3, card.EaseFactor, 2); // Cannot drop below 1.3
    }

    [Fact]
    public void DnsGuidanceService_GeneratesValidPowerShellScripts()
    {
        Assert.True(DnsGuidanceService.Providers.TryGetValue(FamilyDnsProvider.CloudflareFamily, out var provider));
        Assert.NotNull(provider);
        Assert.Equal("Cloudflare 1.1.1.3 (Malware + Adult Filter)", provider.DisplayName);
        Assert.Contains("1.1.1.3", provider.PrimaryIpv4);

        var psScript = DnsGuidanceService.GeneratePowerShellApplyCommand(FamilyDnsProvider.CloudflareFamily, "Wi-Fi");
        Assert.Contains("Set-DnsClientServerAddress", psScript);
        Assert.Contains("Wi-Fi", psScript);
        Assert.Contains("1.1.1.3", psScript);
        Assert.Contains("1.0.0.3", psScript);

        var resetScript = DnsGuidanceService.GeneratePowerShellResetCommand("Wi-Fi");
        Assert.Contains("-ResetServerAddresses", resetScript);
    }

    [Fact]
    public void GameDiscoveryService_RecognizesCommonCompetitiveGames()
    {
        Assert.True(GameDiscoveryService.IsKnownGameProcess("cs2"));
        Assert.True(GameDiscoveryService.IsKnownGameProcess("VALORANT"));
        Assert.True(GameDiscoveryService.IsKnownGameProcess("dota2"));
        Assert.True(GameDiscoveryService.IsKnownGameProcess("League of Legends"));
        Assert.True(GameDiscoveryService.IsKnownGameProcess("FortniteClient-Win64-Shipping"));
        Assert.True(GameDiscoveryService.IsKnownGameProcess("GTA5"));
        Assert.False(GameDiscoveryService.IsKnownGameProcess("notepad"));
        Assert.False(GameDiscoveryService.IsKnownGameProcess("code"));
    }
}
