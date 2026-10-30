using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class WebsiteRepository
{
    private readonly QuietoraDatabase _db;

    public WebsiteRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<BlockedWebsite> GetAllWebsites()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT domain, keyword_pattern, display_name, category, is_active FROM blocked_websites ORDER BY display_name ASC;";
        using var reader = cmd.ExecuteReader();
        var list = new List<BlockedWebsite>();
        while (reader.Read())
        {
            list.Add(new BlockedWebsite
            {
                Domain = reader.GetString(0),
                KeywordPattern = reader.GetString(1),
                DisplayName = reader.GetString(2),
                Category = reader.IsDBNull(3) ? "Social / Entertainment" : reader.GetString(3),
                IsActive = reader.GetInt32(4) == 1
            });
        }
        return list;
    }

    public void AddWebsite(BlockedWebsite site)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO blocked_websites (domain, keyword_pattern, display_name, category, is_active)
            VALUES ($domain, $pattern, $name, $cat, $active)
            ON CONFLICT(domain) DO UPDATE SET
                keyword_pattern = excluded.keyword_pattern,
                display_name = excluded.display_name,
                is_active = excluded.is_active;
            """;
        cmd.Parameters.AddWithValue("$domain", site.Domain.ToLowerInvariant());
        cmd.Parameters.AddWithValue("$pattern", site.KeywordPattern.ToLowerInvariant());
        cmd.Parameters.AddWithValue("$name", site.DisplayName);
        cmd.Parameters.AddWithValue("$cat", site.Category);
        cmd.Parameters.AddWithValue("$active", site.IsActive ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void RemoveWebsite(string domain)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM blocked_websites WHERE domain = $domain COLLATE NOCASE;";
        cmd.Parameters.AddWithValue("$domain", domain);
        cmd.ExecuteNonQuery();
    }

    public void ToggleWebsiteActive(string domain, bool isActive)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE blocked_websites SET is_active = $active WHERE domain = $domain COLLATE NOCASE;";
        cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$domain", domain);
        cmd.ExecuteNonQuery();
    }
}
