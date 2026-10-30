using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class BudgetRepository
{
    private readonly QuietoraDatabase _db;

    public BudgetRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<AppBudget> GetAllBudgets()
    {
        CheckAndResetDailyUsage();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, target_name, target_type, display_name, daily_limit_minutes, used_seconds_today, last_reset_date, is_active FROM app_budgets ORDER BY id ASC;";
        using var reader = cmd.ExecuteReader();
        var list = new List<AppBudget>();
        while (reader.Read())
        {
            list.Add(new AppBudget
            {
                Id = reader.GetInt64(0),
                TargetName = reader.GetString(1),
                TargetType = reader.GetString(2),
                DisplayName = reader.GetString(3),
                DailyLimitMinutes = reader.GetInt32(4),
                UsedSecondsToday = reader.GetInt32(5),
                LastResetDate = reader.GetString(6),
                IsActive = reader.GetInt32(7) == 1
            });
        }
        return list;
    }

    public void AddOrUpdateBudget(AppBudget budget)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO app_budgets (target_name, target_type, display_name, daily_limit_minutes, used_seconds_today, last_reset_date, is_active)
            VALUES ($target, $type, $name, $limit, 0, $date, 1)
            ON CONFLICT(id) DO UPDATE SET
                daily_limit_minutes = $limit,
                display_name = $name,
                is_active = 1;
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$target", budget.TargetName.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("$type", budget.TargetType);
        cmd.Parameters.AddWithValue("$name", budget.DisplayName);
        cmd.Parameters.AddWithValue("$limit", budget.DailyLimitMinutes);
        cmd.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("yyyy-MM-dd"));

        budget.Id = Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void IncrementUsage(string targetName, int secondsToAdd = 1)
    {
        if (string.IsNullOrWhiteSpace(targetName)) return;

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE app_budgets 
            SET used_seconds_today = used_seconds_today + $secs 
            WHERE target_name = $target COLLATE NOCASE;
            """;
        cmd.Parameters.AddWithValue("$secs", secondsToAdd);
        cmd.Parameters.AddWithValue("$target", targetName.Trim().ToLowerInvariant());
        cmd.ExecuteNonQuery();
    }

    public void ToggleBudgetActive(long id, bool isActive)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE app_budgets SET is_active = $active WHERE id = $id;";
        cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteBudget(long id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM app_budgets WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private void CheckAndResetDailyUsage()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE app_budgets SET used_seconds_today = 0, last_reset_date = $today WHERE last_reset_date != $today;";
        cmd.Parameters.AddWithValue("$today", today);
        cmd.ExecuteNonQuery();
    }
}
