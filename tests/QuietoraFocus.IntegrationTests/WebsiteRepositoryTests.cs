using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class WebsiteRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;

    public WebsiteRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_web_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_tempDbPath);
            }
        }
        catch { }
    }

    [Fact]
    public void FreshDatabase_SeedsDefaultWebsitesViaMigration3()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new WebsiteRepository(db);

        var sites = repo.GetAllWebsites();
        Assert.NotEmpty(sites);
        Assert.Contains(sites, s => s.Domain == "youtube.com");
        Assert.Contains(sites, s => s.Domain == "instagram.com");
        Assert.Contains(sites, s => s.Domain == "reddit.com");
        Assert.Contains(sites, s => s.Domain == "tiktok.com");
    }

    [Fact]
    public void WebsiteRepository_AddRemoveAndToggleOperationsWork()
    {
        var db = new QuietoraDatabase(_tempDbPath);
        var repo = new WebsiteRepository(db);

        // Add custom site
        var custom = new BlockedWebsite
        {
            Domain = "distractinggame.com",
            KeywordPattern = "distractinggame,gameplay",
            DisplayName = "Distracting Game",
            Category = "Gaming",
            IsActive = true
        };

        repo.AddWebsite(custom);
        var sites = repo.GetAllWebsites();
        Assert.Contains(sites, s => s.Domain == "distractinggame.com");

        // Toggle active
        repo.ToggleWebsiteActive("distractinggame.com", false);
        var updated = repo.GetAllWebsites().First(s => s.Domain == "distractinggame.com");
        Assert.False(updated.IsActive);

        // Remove
        repo.RemoveWebsite("distractinggame.com");
        var afterRemove = repo.GetAllWebsites();
        Assert.DoesNotContain(afterRemove, s => s.Domain == "distractinggame.com");
    }
}
