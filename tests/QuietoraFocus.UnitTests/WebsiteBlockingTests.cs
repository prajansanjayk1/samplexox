using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using Xunit;

namespace QuietoraFocus.UnitTests;

public class WebsiteBlockingTests
{
    private readonly List<BlockedWebsite> _defaultSites = new()
    {
        new BlockedWebsite { Domain = "youtube.com", KeywordPattern = "youtube", DisplayName = "YouTube", IsActive = true },
        new BlockedWebsite { Domain = "instagram.com", KeywordPattern = "instagram,insta", DisplayName = "Instagram", IsActive = true },
        new BlockedWebsite { Domain = "reddit.com", KeywordPattern = "reddit", DisplayName = "Reddit", IsActive = true },
        new BlockedWebsite { Domain = "twitter.com", KeywordPattern = "twitter,x.com", DisplayName = "Twitter", IsActive = true },
        new BlockedWebsite { Domain = "tiktok.com", KeywordPattern = "tiktok", DisplayName = "TikTok", IsActive = true }
    };

    [Theory]
    [InlineData("Never Gonna Give You Up - YouTube - Google Chrome", "YouTube")]
    [InlineData("YouTube - Google Chrome (Incognito)", "YouTube")] // Incognito Chrome
    [InlineData("Relaxing Lofi Beats - YouTube - InPrivate - Microsoft Edge", "YouTube")] // InPrivate Edge
    [InlineData("Instagram - Google Chrome", "Instagram")]
    [InlineData("Feed • Instagram - Google Chrome (Incognito)", "Instagram")] // Incognito Instagram
    [InlineData("Instagram - InPrivate - Microsoft Edge", "Instagram")] // InPrivate Edge
    [InlineData("reddit: the front page of the internet - Mozilla Firefox", "Reddit")]
    [InlineData("r/popular - reddit - Brave (Private Window)", "Reddit")] // Brave Private Window
    [InlineData("TikTok - Make Your Day - Opera", "TikTok")]
    public void WebsiteMatcher_MatchesStandardAndIncognitoTitles(string windowTitle, string expectedName)
    {
        var matched = WebsiteMatcher.TryMatchBlockedWebsite(windowTitle, _defaultSites, out var site);

        Assert.True(matched);
        Assert.NotNull(site);
        Assert.Equal(expectedName, site.DisplayName);
    }

    [Theory]
    [InlineData("GitHub - QuietoraLabs/QuietoraFocus - Google Chrome")]
    [InlineData("Stack Overflow - Where Developers Learn - Microsoft Edge")]
    [InlineData("System Architecture Documentation - Mozilla Firefox")]
    [InlineData("ChatGPT - OpenAI - Google Chrome (Incognito)")]
    public void WebsiteMatcher_IgnoresProductiveAndWorkWebsites(string windowTitle)
    {
        var matched = WebsiteMatcher.TryMatchBlockedWebsite(windowTitle, _defaultSites, out var site);

        Assert.False(matched);
        Assert.Null(site);
    }

    [Theory]
    [InlineData("chrome", true)]
    [InlineData("msedge", true)]
    [InlineData("firefox", true)]
    [InlineData("brave", true)]
    [InlineData("opera", true)]
    [InlineData("code", false)]
    [InlineData("devenv", false)]
    [InlineData("explorer", false)]
    public void WebsiteMatcher_CorrectlyIdentifiesBrowserProcesses(string processName, bool expectedIsBrowser)
    {
        Assert.Equal(expectedIsBrowser, WebsiteMatcher.IsBrowser(processName));
    }

    [Fact]
    public void WebsiteMatcher_RespectsIsActiveToggle()
    {
        var sites = new List<BlockedWebsite>
        {
            new BlockedWebsite { Domain = "youtube.com", KeywordPattern = "youtube", DisplayName = "YouTube", IsActive = false }
        };

        var matched = WebsiteMatcher.TryMatchBlockedWebsite("YouTube - Google Chrome", sites, out var site);
        Assert.False(matched);
        Assert.Null(site);
    }

    [Fact]
    public void WebsiteMatcher_EmptyList_FallsBackToDefaultSites()
    {
        var matched = WebsiteMatcher.TryMatchBlockedWebsite("YouTube - Google Chrome", new List<BlockedWebsite>(), out var site);
        Assert.True(matched);
        Assert.NotNull(site);
        Assert.Equal("YouTube", site.DisplayName);

        var matchedIg = WebsiteMatcher.TryMatchBlockedWebsite("Instagram - Google Chrome", new List<BlockedWebsite>(), out var siteIg);
        Assert.True(matchedIg);
        Assert.NotNull(siteIg);
        Assert.Equal("Instagram", siteIg.DisplayName);
    }
}
