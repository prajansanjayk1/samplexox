using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class SettingsRepository
{
    private readonly QuietoraDatabase _db;

    public SettingsRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public string GetSetting(string key, string fallback)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string ?? fallback;
    }

    public void SetSetting(string key, string value)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO settings(key,value) VALUES ($key,$value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    public int GetInt(string key, int fallback)
    {
        var raw = GetSetting(key, fallback.ToString());
        return int.TryParse(raw, out var val) ? val : fallback;
    }

    public void SetInt(string key, int value) => SetSetting(key, value.ToString());

    public bool GetBool(string key, bool fallback)
    {
        var raw = GetSetting(key, fallback ? "true" : "false");
        return bool.TryParse(raw, out var val) ? val : fallback;
    }

    public void SetBool(string key, bool value) => SetSetting(key, value ? "true" : "false");
}

public sealed class ProjectRepository
{
    private readonly QuietoraDatabase _db;

    public ProjectRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public void AddProject(Project project)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO projects (name, description, color_hex, created_utc)
            VALUES ($name, $desc, $color, $created);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$name", project.Name);
        cmd.Parameters.AddWithValue("$desc", project.Description);
        cmd.Parameters.AddWithValue("$color", project.ColorHex);
        cmd.Parameters.AddWithValue("$created", project.CreatedUtc.ToString("O"));
        project.Id = Convert.ToInt64(cmd.ExecuteScalar());
    }

    public List<Project> GetAllProjects()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, description, color_hex, created_utc FROM projects ORDER BY name ASC";
        using var reader = cmd.ExecuteReader();
        var list = new List<Project>();
        while (reader.Read())
        {
            list.Add(new Project
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ColorHex = reader.IsDBNull(3) ? "#6759E8" : reader.GetString(3),
                CreatedUtc = DateTimeOffset.Parse(reader.GetString(4))
            });
        }
        return list;
    }

    public void DeleteProject(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM projects WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
