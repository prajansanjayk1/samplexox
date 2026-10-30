using System.Text;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using Xunit;

namespace QuietoraFocus.UnitTests;

public class RegainDetoxTests
{
    [Theory]
    [InlineData(AmbientSoundType.BrownNoise)]
    [InlineData(AmbientSoundType.WhiteNoise)]
    [InlineData(AmbientSoundType.GentleRain)]
    [InlineData(AmbientSoundType.BinauralGamma40Hz)]
    [InlineData(AmbientSoundType.BinauralAlpha10Hz)]
    public void AmbientSoundGenerator_GeneratesValidWavHeaderAndPcmBytes(AmbientSoundType type)
    {
        var bytes = AmbientSoundGenerator.GenerateWavBytes(type, durationSeconds: 1.0);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 44);

        // Verify RIFF and WAVE header tokens
        var riff = Encoding.ASCII.GetString(bytes, 0, 4);
        var wave = Encoding.ASCII.GetString(bytes, 8, 4);
        var fmt = Encoding.ASCII.GetString(bytes, 12, 4);

        Assert.Equal("RIFF", riff);
        Assert.Equal("WAVE", wave);
        Assert.Equal("fmt ", fmt);
    }

    [Fact]
    public void DetoxCalculator_ComputesAccurateDopamineScoreAndTimeSaved()
    {
        var budgets = new List<AppBudget>
        {
            new() { TargetName = "youtube.com", DailyLimitMinutes = 30, UsedSecondsToday = 1800, IsActive = true }, // 30m used (Quota hit)
            new() { TargetName = "discord", DailyLimitMinutes = 45, UsedSecondsToday = 600, IsActive = true }       // 10m used
        };

        var report = DetoxCalculator.Calculate(
            focusMinutesToday: 90,
            distractionsToday: 2,
            doomscrollsAvertedToday: 6,
            distractingScreenMinutesToday: 40,
            budgets: budgets,
            bedtimeStart: new TimeSpan(22, 30, 0),
            bedtimeEnd: new TimeSpan(7, 0, 0));

        // 6 averted doomscrolls * 8.5m = 51m
        Assert.Equal(51, report.TotalMinutesSavedToday);
        Assert.Equal("51 mins", report.TimeSavedFormatted);
        Assert.True(report.DetoxScore is >= 50 and <= 100);
        Assert.False(string.IsNullOrWhiteSpace(report.MindfulTip));
    }

    [Fact]
    public void WebsiteMatcher_DetectsShortFormDoomscrollFeeds()
    {
        var shortsTitles = new[]
        {
            "Crazy Parkour Stunt #shorts - YouTube - Google Chrome",
            "Shorts - YouTube - Brave",
            "Feed • Reels • Instagram - Google Chrome",
            "Reels | Instagram - Microsoft Edge",
            "TikTok - Make Your Day - Opera",
            "r/popular: Hot discussions on Reddit - Mozilla Firefox"
        };

        foreach (var title in shortsTitles)
        {
            var isDoomscroll = WebsiteMatcher.IsShortFormDoomscrollFeed(title, out var feedName);
            Assert.True(isDoomscroll, $"Expected doomscroll feed for title: {title}");
            Assert.False(string.IsNullOrWhiteSpace(feedName));
        }
    }

    [Fact]
    public void WebsiteMatcher_RespectsBlockShortsReelsOnlyToggle()
    {
        var blockedSites = new List<BlockedWebsite>
        {
            new() { Domain = "youtube.com", KeywordPattern = "youtube", DisplayName = "YouTube", IsActive = true }
        };

        // 1. Regular educational tutorial video with blockShortsReelsOnly = true
        var tutorialTitle = "Calculus 1 Full College Course - YouTube - Google Chrome";
        var matchedTutorial = WebsiteMatcher.TryMatchBlockedWebsite(
            tutorialTitle, blockedSites, blockShortsReelsOnly: true, out var site1, out var isShorts1);

        Assert.False(matchedTutorial, "Educational video should be allowed when Shorts-Only block is active");
        Assert.False(isShorts1);

        // 2. Addictive shorts video with blockShortsReelsOnly = true
        var shortsTitle = "Instant Dopamine Loop #shorts - YouTube - Google Chrome";
        var matchedShorts = WebsiteMatcher.TryMatchBlockedWebsite(
            shortsTitle, blockedSites, blockShortsReelsOnly: true, out var site2, out var isShorts2);

        Assert.True(matchedShorts, "Shorts video should be intercepted even if regular YouTube is allowed");
        Assert.True(isShorts2);
        Assert.Contains("YouTube", site2?.DisplayName);
    }
}
