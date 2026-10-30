using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class SessionRepository
{
    private readonly QuietoraDatabase _db;

    public SessionRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public void AddSession(SessionLog entry)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sessions
              (task, subject, project_name, mode, started_utc, ended_utc,
               planned_seconds, actual_seconds, paused_seconds, distractions, end_reason, notes)
            VALUES
              ($task, $subject, $project_name, $mode, $started, $ended,
               $planned, $actual, $paused, $distractions, $reason, $notes)
            """;
        command.Parameters.AddWithValue("$task", entry.Task);
        command.Parameters.AddWithValue("$subject", entry.Subject);
        command.Parameters.AddWithValue("$project_name", entry.ProjectName);
        command.Parameters.AddWithValue("$mode", entry.Mode);
        command.Parameters.AddWithValue("$started", entry.StartedUtc.ToString("O"));
        command.Parameters.AddWithValue("$ended", entry.EndedUtc.ToString("O"));
        command.Parameters.AddWithValue("$planned", entry.PlannedSeconds);
        command.Parameters.AddWithValue("$actual", entry.ActualSeconds);
        command.Parameters.AddWithValue("$paused", entry.PausedSeconds);
        command.Parameters.AddWithValue("$distractions", entry.Distractions);
        command.Parameters.AddWithValue("$reason", entry.EndReason);
        command.Parameters.AddWithValue("$notes", entry.Notes);
        command.ExecuteNonQuery();
    }

    public List<SessionLog> GetAllSessions()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, task, subject, project_name, mode, started_utc, ended_utc,
                   planned_seconds, actual_seconds, paused_seconds, distractions, end_reason, notes
            FROM sessions ORDER BY id DESC
            """;
        using var reader = command.ExecuteReader();
        var result = new List<SessionLog>();
        while (reader.Read())
        {
            result.Add(ReadSession(reader));
        }
        return result;
    }

    public List<SessionLog> GetRecentSessions(int limit = 15)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, task, subject, project_name, mode, started_utc, ended_utc,
                   planned_seconds, actual_seconds, paused_seconds, distractions, end_reason, notes
            FROM sessions ORDER BY id DESC LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var result = new List<SessionLog>();
        while (reader.Read())
        {
            result.Add(ReadSession(reader));
        }
        return result;
    }

    public void ClearHistory()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM sessions";
        command.ExecuteNonQuery();
    }

    private static SessionLog ReadSession(SqliteDataReader reader)
    {
        return new SessionLog
        {
            Id = reader.GetInt64(0),
            Task = reader.GetString(1),
            Subject = reader.IsDBNull(2) ? "" : reader.GetString(2),
            ProjectName = reader.IsDBNull(3) ? "" : reader.GetString(3),
            Mode = reader.IsDBNull(4) ? "DeepFocus" : reader.GetString(4),
            StartedUtc = DateTimeOffset.Parse(reader.GetString(5)),
            EndedUtc = DateTimeOffset.Parse(reader.GetString(6)),
            PlannedSeconds = reader.GetInt32(7),
            ActualSeconds = reader.GetInt32(8),
            PausedSeconds = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
            Distractions = reader.GetInt32(10),
            EndReason = reader.GetString(11),
            Notes = reader.IsDBNull(12) ? "" : reader.GetString(12)
        };
    }
}
