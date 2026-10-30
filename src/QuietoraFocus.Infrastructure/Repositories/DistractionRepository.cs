using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class DistractionRepository
{
    private readonly QuietoraDatabase _db;

    public DistractionRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<string> ReadBlockedApps()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT process_name FROM blocked_apps ORDER BY process_name COLLATE NOCASE";
        using var reader = command.ExecuteReader();
        var apps = new List<string>();
        while (reader.Read()) apps.Add(reader.GetString(0));
        return apps;
    }

    public void AddBlockedApp(string name)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO blocked_apps(process_name) VALUES ($name)";
        command.Parameters.AddWithValue("$name", name);
        command.ExecuteNonQuery();
    }

    public void RemoveBlockedApp(string name)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM blocked_apps WHERE process_name = $name";
        command.Parameters.AddWithValue("$name", name);
        command.ExecuteNonQuery();
    }

    public void RecordEvent(DistractionEvent evt)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO distraction_events(session_id, process_name, timestamp_utc, policy_applied, action_taken)
            VALUES ($sid, $pname, $ts, $pol, $act)
            """;
        command.Parameters.AddWithValue("$sid", evt.SessionId.HasValue ? evt.SessionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$pname", evt.ProcessName);
        command.Parameters.AddWithValue("$ts", evt.TimestampUtc.ToString("O"));
        command.Parameters.AddWithValue("$pol", (int)evt.PolicyApplied);
        command.Parameters.AddWithValue("$act", evt.ActionTaken);
        command.ExecuteNonQuery();
    }

    public List<DistractionEvent> GetRecentEvents(int limit = 25)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, session_id, process_name, timestamp_utc, policy_applied, action_taken
            FROM distraction_events ORDER BY id DESC LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var list = new List<DistractionEvent>();
        while (reader.Read())
        {
            list.Add(new DistractionEvent
            {
                Id = reader.GetInt64(0),
                SessionId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                ProcessName = reader.GetString(2),
                TimestampUtc = DateTimeOffset.Parse(reader.GetString(3)),
                PolicyApplied = (DistractionPolicyLevel)reader.GetInt32(4),
                ActionTaken = reader.GetString(5)
            });
        }
        return list;
    }

    public List<AppDistractionSummary> GetTopDistractingApps(int limit = 5)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT process_name, COUNT(*) as cnt
            FROM distraction_events
            GROUP BY process_name
            ORDER BY cnt DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var list = new List<AppDistractionSummary>();
        var total = 0;
        var temp = new List<(string Name, int Count)>();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var cnt = reader.GetInt32(1);
            total += cnt;
            temp.Add((name, cnt));
        }

        foreach (var item in temp)
        {
            list.Add(new AppDistractionSummary
            {
                ProcessName = item.Name,
                Count = item.Count,
                Percentage = total > 0 ? Math.Round((double)item.Count / total * 100.0, 1) : 0
            });
        }

        return list;
    }
}
