using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class TaskRepository
{
    private readonly QuietoraDatabase _db;

    public TaskRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public void AddTask(FocusTask task)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tasks
              (title, description, priority, status, due_date_utc, project_id, subject, estimated_minutes, actual_minutes, created_utc)
            VALUES
              ($title, $desc, $priority, $status, $due, $projId, $subject, $est, $act, $created);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", task.Description);
        cmd.Parameters.AddWithValue("$priority", (int)task.Priority);
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$due", task.DueDateUtc.HasValue ? task.DueDateUtc.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$projId", task.ProjectId.HasValue ? task.ProjectId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$subject", task.Subject);
        cmd.Parameters.AddWithValue("$est", task.EstimatedMinutes);
        cmd.Parameters.AddWithValue("$act", task.ActualMinutes);
        cmd.Parameters.AddWithValue("$created", task.CreatedUtc.ToString("O"));

        task.Id = Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void UpdateTask(FocusTask task)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE tasks SET
                title = $title,
                description = $desc,
                priority = $priority,
                status = $status,
                due_date_utc = $due,
                project_id = $projId,
                subject = $subject,
                estimated_minutes = $est,
                actual_minutes = $act,
                completed_utc = $completed
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$desc", task.Description);
        cmd.Parameters.AddWithValue("$priority", (int)task.Priority);
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$due", task.DueDateUtc.HasValue ? task.DueDateUtc.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$projId", task.ProjectId.HasValue ? task.ProjectId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$subject", task.Subject);
        cmd.Parameters.AddWithValue("$est", task.EstimatedMinutes);
        cmd.Parameters.AddWithValue("$act", task.ActualMinutes);
        cmd.Parameters.AddWithValue("$completed", task.CompletedUtc.HasValue ? task.CompletedUtc.Value.ToString("O") : DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void ToggleTaskStatus(long taskId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT status FROM tasks WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", taskId);
        var currentStatusObj = cmd.ExecuteScalar();
        if (currentStatusObj == null) return;

        var currentStatus = (TaskStatusEnum)Convert.ToInt32(currentStatusObj);
        var newStatus = currentStatus == TaskStatusEnum.Completed ? TaskStatusEnum.Pending : TaskStatusEnum.Completed;
        var completedUtc = newStatus == TaskStatusEnum.Completed ? DateTimeOffset.UtcNow.ToString("O") : null;

        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE tasks SET status = $status, completed_utc = $comp WHERE id = $id";
        updateCmd.Parameters.AddWithValue("$status", (int)newStatus);
        updateCmd.Parameters.AddWithValue("$comp", (object?)completedUtc ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$id", taskId);
        updateCmd.ExecuteNonQuery();
    }

    public void DeleteTask(long taskId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tasks WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", taskId);
        cmd.ExecuteNonQuery();
    }

    public List<FocusTask> GetAllTasks()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, description, priority, status, due_date_utc, project_id,
                   subject, estimated_minutes, actual_minutes, created_utc, completed_utc
            FROM tasks
            ORDER BY status ASC, priority DESC, due_date_utc ASC, id DESC
            """;
        using var reader = cmd.ExecuteReader();
        var tasks = new List<FocusTask>();
        while (reader.Read())
        {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
    }

    private static FocusTask ReadTask(SqliteDataReader reader)
    {
        return new FocusTask
        {
            Id = reader.GetInt64(0),
            Title = reader.GetString(1),
            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Priority = (TaskPriority)reader.GetInt32(3),
            Status = (TaskStatusEnum)reader.GetInt32(4),
            DueDateUtc = reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5)),
            ProjectId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
            Subject = reader.IsDBNull(7) ? "" : reader.GetString(7),
            EstimatedMinutes = reader.GetInt32(8),
            ActualMinutes = reader.GetInt32(9),
            CreatedUtc = DateTimeOffset.Parse(reader.GetString(10)),
            CompletedUtc = reader.IsDBNull(11) ? null : DateTimeOffset.Parse(reader.GetString(11))
        };
    }
}
