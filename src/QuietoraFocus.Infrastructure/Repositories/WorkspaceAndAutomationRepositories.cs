using System.Text.Json;
using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class WorkspaceRepository
{
    private readonly QuietoraDatabase _db;

    public WorkspaceRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<WorkspaceSnapshot> GetAllSnapshots()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, project_path, description, recent_files_json, associated_task_ids_json, notes, saved_utc FROM workspace_snapshots ORDER BY id DESC;";
        using var reader = cmd.ExecuteReader();
        var list = new List<WorkspaceSnapshot>();
        while (reader.Read())
        {
            var filesJson = reader.GetString(4);
            var tasksJson = reader.GetString(5);

            list.Add(new WorkspaceSnapshot
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                ProjectPath = reader.GetString(2),
                Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                RecentFilePaths = string.IsNullOrWhiteSpace(filesJson) ? new() : JsonSerializer.Deserialize<List<string>>(filesJson) ?? new(),
                AssociatedTaskIds = string.IsNullOrWhiteSpace(tasksJson) ? new() : JsonSerializer.Deserialize<List<long>>(tasksJson) ?? new(),
                Notes = reader.IsDBNull(6) ? "" : reader.GetString(6),
                SavedUtc = DateTimeOffset.Parse(reader.GetString(7))
            });
        }
        return list;
    }

    public WorkspaceSnapshot? GetById(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, project_path, description, recent_files_json, associated_task_ids_json, notes, saved_utc FROM workspace_snapshots WHERE id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var filesJson = reader.GetString(4);
            var tasksJson = reader.GetString(5);

            return new WorkspaceSnapshot
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                ProjectPath = reader.GetString(2),
                Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                RecentFilePaths = string.IsNullOrWhiteSpace(filesJson) ? new() : JsonSerializer.Deserialize<List<string>>(filesJson) ?? new(),
                AssociatedTaskIds = string.IsNullOrWhiteSpace(tasksJson) ? new() : JsonSerializer.Deserialize<List<long>>(tasksJson) ?? new(),
                Notes = reader.IsDBNull(6) ? "" : reader.GetString(6),
                SavedUtc = DateTimeOffset.Parse(reader.GetString(7))
            };
        }
        return null;
    }

    public void AddSnapshot(WorkspaceSnapshot snapshot)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO workspace_snapshots (name, project_path, description, recent_files_json, associated_task_ids_json, notes, saved_utc)
            VALUES ($name, $path, $desc, $files, $tasks, $notes, $saved);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$name", snapshot.Name);
        cmd.Parameters.AddWithValue("$path", snapshot.ProjectPath);
        cmd.Parameters.AddWithValue("$desc", snapshot.Description);
        cmd.Parameters.AddWithValue("$files", JsonSerializer.Serialize(snapshot.RecentFilePaths));
        cmd.Parameters.AddWithValue("$tasks", JsonSerializer.Serialize(snapshot.AssociatedTaskIds));
        cmd.Parameters.AddWithValue("$notes", snapshot.Notes);
        cmd.Parameters.AddWithValue("$saved", snapshot.SavedUtc.ToString("O"));

        snapshot.Id = Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void UpdateLastRestored(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE workspace_snapshots SET saved_utc = $saved WHERE id = $id;";
        cmd.Parameters.AddWithValue("$saved", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSnapshot(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM workspace_snapshots WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}

public sealed class AutomationRepository
{
    private readonly QuietoraDatabase _db;

    public AutomationRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<AutomationRule> GetAllRules()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, title, description, trigger_type, trigger_config, action_type, action_payload, is_enabled, last_triggered_utc FROM automation_rules ORDER BY id ASC;";
        using var reader = cmd.ExecuteReader();
        var list = new List<AutomationRule>();
        while (reader.Read())
        {
            list.Add(new AutomationRule
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                TriggerType = (AutomationTriggerType)reader.GetInt32(3),
                TriggerConfig = reader.IsDBNull(4) ? "" : reader.GetString(4),
                ActionType = (AutomationActionType)reader.GetInt32(5),
                ActionPayload = reader.IsDBNull(6) ? "" : reader.GetString(6),
                IsEnabled = reader.GetInt32(7) == 1,
                LastTriggeredUtc = reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8))
            });
        }
        return list;
    }

    public void AddRule(AutomationRule rule)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO automation_rules (title, description, trigger_type, trigger_config, action_type, action_payload, is_enabled)
            VALUES ($title, $desc, $trigger, $tcfg, $action, $apld, $enabled);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", rule.Title);
        cmd.Parameters.AddWithValue("$desc", rule.Description);
        cmd.Parameters.AddWithValue("$trigger", (int)rule.TriggerType);
        cmd.Parameters.AddWithValue("$tcfg", rule.TriggerConfig);
        cmd.Parameters.AddWithValue("$action", (int)rule.ActionType);
        cmd.Parameters.AddWithValue("$apld", rule.ActionPayload);
        cmd.Parameters.AddWithValue("$enabled", rule.IsEnabled ? 1 : 0);

        rule.Id = Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void ToggleRule(long id, bool isEnabled)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE automation_rules SET is_enabled = $enabled WHERE id = $id;";
        cmd.Parameters.AddWithValue("$enabled", isEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateLastTriggered(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE automation_rules SET last_triggered_utc = $now WHERE id = $id;";
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteRule(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM automation_rules WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
