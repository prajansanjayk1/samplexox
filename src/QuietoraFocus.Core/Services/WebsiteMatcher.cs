using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class WebsiteMatcher
{
    private static readonly HashSet<string> KnownBrowserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "edge", "firefox", "brave", "opera", "operagx", "vivaldi", "arc",
        "iexplore", "waterfox", "tor", "sidekick", "chromium", "zen", "floorp", "safari",
        "maxthon", "whale", "yandex", "browser", "applicationframehost"
    };

    public static bool IsBrowser(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var clean = processName.Trim().ToLowerInvariant();
        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) clean = clean[..^4];
        return KnownBrowserProcesses.Contains(clean);
    }

    /// <summary>
    /// Checks if a window title represents an algorithmic short-form video or doomscroll feed
    /// (e.g. YouTube Shorts, Instagram Reels, TikTok, Reddit Endless Feeds).
    /// </summary>
    public static bool IsShortFormDoomscrollFeed(string windowTitle, out string feedName)
    {
        feedName = string.Empty;
        if (string.IsNullOrWhiteSpace(windowTitle)) return false;

        var lower = windowTitle.ToLowerInvariant();

        // 1. YouTube Shorts
        if (lower.Contains("shorts - youtube") || lower.Contains("#shorts") || lower.Contains("/shorts") || (lower.Contains("shorts") && lower.Contains("youtube")))
        {
            feedName = "YouTube Shorts";
            return true;
        }

        // 2. Instagram Reels & Explore
        if (lower.Contains("reels • instagram") || lower.Contains("reels | instagram") || lower.Contains("instagram reels") || lower.Contains("explore • instagram"))
        {
            feedName = "Instagram Reels";
            return true;
        }

        // 3. TikTok
        if (lower.Contains("tiktok"))
        {
            feedName = "TikTok";
            return true;
        }

        // 4. Facebook Reels & Watch
        if ((lower.Contains("reels") || lower.Contains("watch")) && lower.Contains("facebook"))
        {
            feedName = "Facebook Reels";
            return true;
        }

        // 5. Reddit Endless Feeds
        if (lower.Contains("r/popular") || lower.Contains("r/all") || lower.Contains("reddit - dive into anything"))
        {
            feedName = "Reddit Endless Feed";
            return true;
        }

        return false;
    }

    private static readonly List<BlockedWebsite> DefaultBlockedWebsites = new()
    {
        new() { Domain = "youtube.com", KeywordPattern = "youtube", DisplayName = "YouTube", Category = "Video / Streaming", IsActive = true },
        new() { Domain = "instagram.com", KeywordPattern = "instagram,insta", DisplayName = "Instagram", Category = "Social Media", IsActive = true },
        new() { Domain = "reddit.com", KeywordPattern = "reddit", DisplayName = "Reddit", Category = "Forums / Social", IsActive = true },
        new() { Domain = "tiktok.com", KeywordPattern = "tiktok", DisplayName = "TikTok", Category = "Short Video", IsActive = true },
        new() { Domain = "twitter.com", KeywordPattern = "twitter,x.com", DisplayName = "Twitter / X", Category = "Social Media", IsActive = true },
        new() { Domain = "netflix.com", KeywordPattern = "netflix", DisplayName = "Netflix", Category = "Streaming", IsActive = true },
        new() { Domain = "twitch.tv", KeywordPattern = "twitch", DisplayName = "Twitch", Category = "Streaming", IsActive = true },
        new() { Domain = "facebook.com", KeywordPattern = "facebook", DisplayName = "Facebook", Category = "Social Media", IsActive = true }
    };

    public static bool TryMatchBlockedWebsite(
        string windowTitle,
        IEnumerable<BlockedWebsite> blockedWebsites,
        bool blockShortsReelsOnly,
        out BlockedWebsite? matchedWebsite,
        out bool isShortsOrReels)
    {
        matchedWebsite = null;
        isShortsOrReels = false;
        if (string.IsNullOrWhiteSpace(windowTitle)) return false;

        var titleLower = windowTitle.ToLowerInvariant();
        var effectiveSites = (blockedWebsites != null && blockedWebsites.Any())
            ? blockedWebsites
            : DefaultBlockedWebsites;

        // Check if it's specifically a Shorts/Reels feed first
        if (IsShortFormDoomscrollFeed(windowTitle, out var feed))
        {
            isShortsOrReels = true;
            var existing = effectiveSites.FirstOrDefault(s =>
                feed.Contains(s.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                s.DisplayName.Contains(feed, StringComparison.OrdinalIgnoreCase));

            matchedWebsite = existing ?? new BlockedWebsite
            {
                DisplayName = feed,
                Domain = feed.ToLowerInvariant().Replace(" ", "") + ".com",
                Category = "Doomscroll Feed",
                IsActive = true
            };
            return true;
        }

        // If user set "Block Shorts & Reels Only", don't block full websites if not a short-form feed!
        if (blockShortsReelsOnly)
        {
            return false;
        }

        // Standard whole-website block check
        foreach (var site in effectiveSites)
        {
            if (!site.IsActive) continue;

            // 1. Check keyword pattern (e.g. "youtube", "instagram", "insta")
            if (!string.IsNullOrWhiteSpace(site.KeywordPattern))
            {
                var patterns = site.KeywordPattern.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pattern in patterns)
                {
                    var p = pattern.Trim().ToLowerInvariant();
                    if (p.Length > 0 && titleLower.Contains(p))
                    {
                        matchedWebsite = site;
                        return true;
                    }
                }
            }

            // 2. Check domain (e.g. "youtube.com")
            if (!string.IsNullOrWhiteSpace(site.Domain))
            {
                var cleanDomain = site.Domain.Trim().ToLowerInvariant()
                    .Replace("https://", "")
                    .Replace("http://", "")
                    .Replace("www.", "");

                var domainNameOnly = cleanDomain.Contains('.') ? cleanDomain.Split('.')[0] : cleanDomain;

                if (titleLower.Contains(cleanDomain) || (domainNameOnly.Length >= 4 && titleLower.Contains(domainNameOnly)))
                {
                    matchedWebsite = site;
                    return true;
                }
            }
        }

        return false;
    }

    // Overload for backward compatibility
    public static bool TryMatchBlockedWebsite(
        string windowTitle,
        IEnumerable<BlockedWebsite> blockedWebsites,
        out BlockedWebsite? matchedWebsite)
    {
        return TryMatchBlockedWebsite(windowTitle, blockedWebsites, false, out matchedWebsite, out _);
    }
}
