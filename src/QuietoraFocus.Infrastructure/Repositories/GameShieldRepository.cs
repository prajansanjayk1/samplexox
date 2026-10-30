using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class GameShieldRepository
{
    private readonly QuietoraDatabase _db;

    public GameShieldRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<DetectedGame> GetAllDetectedGames()
    {
        var list = new List<DetectedGame>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, game_name, process_name, executable_path, launcher, is_blocked, discovered_utc, last_launched_utc FROM detected_games ORDER BY game_name ASC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new DetectedGame
            {
                Id = r.GetInt64(0),
                GameName = r.GetString(1),
                ProcessName = r.GetString(2),
                ExecutablePath = r.GetString(3),
                Launcher = r.GetString(4),
                IsBlocked = r.GetInt32(5) == 1,
                DiscoveredUtc = r.GetString(6),
                LastLaunchedUtc = r.IsDBNull(7) ? null : r.GetString(7)
            });
        }
        return list;
    }

    public void AddOrUpdateDetectedGame(DetectedGame game)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO detected_games (game_name, process_name, executable_path, launcher, is_blocked, discovered_utc, last_launched_utc)
            VALUES ($name, $proc, $path, $launch, $block, $disc, $last)
            ON CONFLICT(id) DO UPDATE SET
                is_blocked = excluded.is_blocked,
                executable_path = excluded.executable_path,
                last_launched_utc = excluded.last_launched_utc;
            """;
        cmd.Parameters.AddWithValue("$name", game.GameName);
        cmd.Parameters.AddWithValue("$proc", game.ProcessName);
        cmd.Parameters.AddWithValue("$path", game.ExecutablePath);
        cmd.Parameters.AddWithValue("$launch", game.Launcher);
        cmd.Parameters.AddWithValue("$block", game.IsBlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("$disc", game.DiscoveredUtc);
        cmd.Parameters.AddWithValue("$last", game.LastLaunchedUtc ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void ToggleGameBlocked(long gameId, bool isBlocked)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE detected_games SET is_blocked = $b WHERE id = $id;";
        cmd.Parameters.AddWithValue("$b", isBlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", gameId);
        cmd.ExecuteNonQuery();
    }

    public void RecordGameLaunch(string processName)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE detected_games SET last_launched_utc = $now WHERE process_name = $proc;";
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$proc", processName);
        cmd.ExecuteNonQuery();
    }

    public void DeleteGame(long gameId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM detected_games WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", gameId);
        cmd.ExecuteNonQuery();
    }
}
