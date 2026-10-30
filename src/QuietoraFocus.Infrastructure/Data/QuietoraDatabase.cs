using System.Data;
using Microsoft.Data.Sqlite;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Infrastructure.Data;

public sealed class QuietoraDatabase
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public QuietoraDatabase(string? customDbPath = null)
    {
        if (string.IsNullOrWhiteSpace(customDbPath))
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuietoraLabs", "QuietoraFocus");
            Directory.CreateDirectory(directory);
            DatabasePath = Path.Combine(directory, "focus.db");
        }
        else
        {
            DatabasePath = customDbPath;
            var dir = Path.GetDirectoryName(customDbPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        AppLogger.LogInfo($"Configured Quietora Database at: {DatabasePath}");
        ApplyMigrations();
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode for optimal concurrent performance and foreign keys
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();

        return connection;
    }

    public void ApplyMigrations()
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            // Create migrations table
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS schema_migrations (
                        version INTEGER PRIMARY KEY NOT NULL,
                        applied_utc TEXT NOT NULL,
                        description TEXT NOT NULL
                    );
                    """;
                cmd.ExecuteNonQuery();
            }

            var appliedVersions = new HashSet<int>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT version FROM schema_migrations ORDER BY version;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    appliedVersions.Add(reader.GetInt32(0));
                }
            }

            // Migration 1: Baseline v0.1.1
            if (!appliedVersions.Contains(1))
            {
                AppLogger.LogInfo("Applying Migration 1 (v0.1.1 baseline tables)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS blocked_apps (
                        process_name TEXT PRIMARY KEY COLLATE NOCASE NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS sessions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        task TEXT NOT NULL,
                        started_utc TEXT NOT NULL,
                        ended_utc TEXT NOT NULL,
                        planned_seconds INTEGER NOT NULL,
                        actual_seconds INTEGER NOT NULL,
                        distractions INTEGER NOT NULL,
                        end_reason TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS settings (
                        key TEXT PRIMARY KEY NOT NULL,
                        value TEXT NOT NULL
                    );
                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (1, datetime('now'), 'Baseline v0.1.1 tables');
                    """;
                cmd.ExecuteNonQuery();
            }

            // Migration 2: v2.0 Advanced additions
            if (!appliedVersions.Contains(2))
            {
                AppLogger.LogInfo("Applying Migration 2 (v2.0 tasks, projects, distraction events, memory tables)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                // Safely add missing columns to sessions if they don't already exist
                EnsureColumnExists(conn, tx, "sessions", "subject", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, tx, "sessions", "project_name", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(conn, tx, "sessions", "mode", "TEXT NOT NULL DEFAULT 'DeepFocus'");
                EnsureColumnExists(conn, tx, "sessions", "paused_seconds", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumnExists(conn, tx, "sessions", "notes", "TEXT NOT NULL DEFAULT ''");

                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS projects (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL COLLATE NOCASE,
                        description TEXT NOT NULL DEFAULT '',
                        color_hex TEXT NOT NULL DEFAULT '#6759E8',
                        created_utc TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS tasks (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT NOT NULL,
                        description TEXT NOT NULL DEFAULT '',
                        priority INTEGER NOT NULL DEFAULT 2,
                        status INTEGER NOT NULL DEFAULT 0,
                        due_date_utc TEXT,
                        project_id INTEGER,
                        subject TEXT NOT NULL DEFAULT '',
                        estimated_minutes INTEGER NOT NULL DEFAULT 25,
                        actual_minutes INTEGER NOT NULL DEFAULT 0,
                        created_utc TEXT NOT NULL,
                        completed_utc TEXT
                    );

                    CREATE TABLE IF NOT EXISTS distraction_events (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        session_id INTEGER,
                        process_name TEXT NOT NULL COLLATE NOCASE,
                        timestamp_utc TEXT NOT NULL,
                        policy_applied INTEGER NOT NULL,
                        action_taken TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS memory_folders (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        folder_path TEXT UNIQUE NOT NULL,
                        is_active INTEGER NOT NULL DEFAULT 1,
                        added_utc TEXT NOT NULL,
                        last_scanned_utc TEXT,
                        file_count INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS indexed_files (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        folder_id INTEGER NOT NULL,
                        file_path TEXT UNIQUE NOT NULL,
                        file_name TEXT NOT NULL,
                        extension TEXT NOT NULL,
                        file_size_bytes INTEGER NOT NULL,
                        last_modified_utc TEXT NOT NULL,
                        indexed_utc TEXT NOT NULL,
                        extracted_summary TEXT NOT NULL DEFAULT '',
                        content_snippet TEXT NOT NULL DEFAULT ''
                    );

                    -- FTS5 virtual table for lightning-fast keyword and phrase memory search
                    CREATE VIRTUAL TABLE IF NOT EXISTS memory_fts USING fts5(
                        file_path UNINDEXED,
                        file_name,
                        content,
                        tokenize = 'porter unicode61'
                    );

                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (2, datetime('now'), 'v2.0 tasks, projects, memory indexing, and analytics');
                    """;
                cmd.ExecuteNonQuery();
            }

            // Migration 3: Website blocking & Hell/Hardcore mode support
            if (!appliedVersions.Contains(3))
            {
                AppLogger.LogInfo("Applying Migration 3 (Website blocking table and initial presets)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                EnsureColumnExists(conn, tx, "distraction_events", "target_detail", "TEXT NOT NULL DEFAULT ''");

                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS blocked_websites (
                        domain TEXT PRIMARY KEY COLLATE NOCASE NOT NULL,
                        keyword_pattern TEXT NOT NULL,
                        display_name TEXT NOT NULL,
                        category TEXT NOT NULL DEFAULT 'Social / Entertainment',
                        is_active INTEGER NOT NULL DEFAULT 1
                    );

                    INSERT OR IGNORE INTO blocked_websites (domain, keyword_pattern, display_name, category, is_active)
                    VALUES
                        ('youtube.com', 'youtube', 'YouTube', 'Video / Streaming', 1),
                        ('instagram.com', 'instagram,insta', 'Instagram', 'Social Media', 1),
                        ('reddit.com', 'reddit', 'Reddit', 'Forums / Social', 1),
                        ('twitter.com', 'twitter,x.com', 'Twitter / X', 'Social Media', 1),
                        ('tiktok.com', 'tiktok', 'TikTok', 'Short Video', 1),
                        ('netflix.com', 'netflix', 'Netflix', 'Streaming', 1),
                        ('twitch.tv', 'twitch', 'Twitch', 'Streaming', 1),
                        ('facebook.com', 'facebook', 'Facebook', 'Social Media', 1);

                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (3, datetime('now'), 'v2.1 website blocking table and presets');
                    """;
                cmd.ExecuteNonQuery();
            }

            // Migration 4: v3.0 Intelligence Edition (Workspace Snapshots & Automations)
            if (!appliedVersions.Contains(4))
            {
                AppLogger.LogInfo("Applying Migration 4 (v3.0 Workspace snapshots and Automation rules)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS workspace_snapshots (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        project_path TEXT NOT NULL,
                        description TEXT NOT NULL DEFAULT '',
                        recent_files_json TEXT NOT NULL DEFAULT '[]',
                        associated_task_ids_json TEXT NOT NULL DEFAULT '[]',
                        notes TEXT NOT NULL DEFAULT '',
                        saved_utc TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS automation_rules (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT NOT NULL,
                        description TEXT NOT NULL DEFAULT '',
                        trigger_type INTEGER NOT NULL,
                        trigger_config TEXT NOT NULL DEFAULT '',
                        action_type INTEGER NOT NULL,
                        action_payload TEXT NOT NULL DEFAULT '',
                        is_enabled INTEGER NOT NULL DEFAULT 1,
                        last_triggered_utc TEXT
                    );

                    INSERT OR IGNORE INTO automation_rules (title, description, trigger_type, trigger_config, action_type, action_payload, is_enabled)
                    VALUES
                        ('Morning Focus Reminder', 'Prompts to start a 50m deep focus block at 09:00 AM', 1, '09:00', 1, 'Time for your morning deep focus block!', 1),
                        ('Post-Session Rest Suggestion', 'Recommends a healthy rest break after completing a session', 2, '', 4, 'Great work! Take a 5-minute stretch and hydration break.', 1),
                        ('Distraction Spike Alert', 'Notifies user when interventions exceed 5 occurrences in a day', 3, '5', 1, 'Notice: Distractions exceeded 5 today. Consider enabling Hell Mode or closing background chat apps.', 1);

                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (4, datetime('now'), 'v3.0 workspace snapshots and automations');
                    """;
                cmd.ExecuteNonQuery();
            }

            // Migration 5: Digital Detox, App Budgets & Screen Time Quotas (Regain Windows Edition)
            if (!appliedVersions.Contains(5))
            {
                AppLogger.LogInfo("Applying Migration 5 (Digital Detox quotas, daily budgets & screen time)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS app_budgets (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        target_name TEXT NOT NULL COLLATE NOCASE,
                        target_type TEXT NOT NULL DEFAULT 'website',
                        display_name TEXT NOT NULL,
                        daily_limit_minutes INTEGER NOT NULL DEFAULT 30,
                        used_seconds_today INTEGER NOT NULL DEFAULT 0,
                        last_reset_date TEXT NOT NULL,
                        is_active INTEGER NOT NULL DEFAULT 1
                    );

                    CREATE TABLE IF NOT EXISTS screen_time_records (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        record_date TEXT NOT NULL,
                        target_name TEXT NOT NULL COLLATE NOCASE,
                        target_type TEXT NOT NULL DEFAULT 'app',
                        seconds_active INTEGER NOT NULL DEFAULT 0
                    );

                    INSERT OR IGNORE INTO app_budgets (target_name, target_type, display_name, daily_limit_minutes, used_seconds_today, last_reset_date, is_active)
                    VALUES
                        ('youtube.com', 'website', 'YouTube', 30, 0, strftime('%Y-%m-%d', 'now'), 1),
                        ('reddit.com', 'website', 'Reddit', 20, 0, strftime('%Y-%m-%d', 'now'), 1),
                        ('instagram.com', 'website', 'Instagram', 15, 0, strftime('%Y-%m-%d', 'now'), 1),
                        ('discord', 'app', 'Discord', 45, 0, strftime('%Y-%m-%d', 'now'), 1),
                        ('steam', 'app', 'Steam', 30, 0, strftime('%Y-%m-%d', 'now'), 1);

                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (5, datetime('now'), 'v3.1 digital detox quotas and app budgets');
                    """;
                cmd.ExecuteNonQuery();
            }

            // Migration 6: Student OS Edition (Academic Workspace, Game Shield & Safe Browsing)
            if (!appliedVersions.Contains(6))
            {
                AppLogger.LogInfo("Applying Migration 6 (Student OS academic workspace, game shield, and revision scheduler)...");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;

                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS academic_subjects (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        code TEXT NOT NULL DEFAULT '',
                        semester TEXT NOT NULL DEFAULT 'Semester 1',
                        target_hours INTEGER NOT NULL DEFAULT 40,
                        completed_minutes INTEGER NOT NULL DEFAULT 0,
                        color_hex TEXT NOT NULL DEFAULT '#4F46E5',
                        notes TEXT NOT NULL DEFAULT '',
                        created_utc TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS academic_topics (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        subject_id INTEGER NOT NULL REFERENCES academic_subjects(id) ON DELETE CASCADE,
                        title TEXT NOT NULL,
                        chapter TEXT NOT NULL DEFAULT '',
                        estimated_minutes INTEGER NOT NULL DEFAULT 60,
                        is_completed INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS academic_assignments (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        subject_id INTEGER NOT NULL REFERENCES academic_subjects(id) ON DELETE CASCADE,
                        title TEXT NOT NULL,
                        description TEXT NOT NULL DEFAULT '',
                        due_date_utc TEXT NOT NULL,
                        priority INTEGER NOT NULL DEFAULT 2,
                        status INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS academic_exams (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        subject_id INTEGER NOT NULL REFERENCES academic_subjects(id) ON DELETE CASCADE,
                        title TEXT NOT NULL,
                        exam_date_utc TEXT NOT NULL,
                        target_score INTEGER NOT NULL DEFAULT 90,
                        syllabus_completed_percent INTEGER NOT NULL DEFAULT 0,
                        notes TEXT NOT NULL DEFAULT ''
                    );

                    CREATE TABLE IF NOT EXISTS academic_flashcards (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        subject_id INTEGER NOT NULL REFERENCES academic_subjects(id) ON DELETE CASCADE,
                        question TEXT NOT NULL,
                        answer TEXT NOT NULL,
                        repetition_level INTEGER NOT NULL DEFAULT 0,
                        interval_days INTEGER NOT NULL DEFAULT 1,
                        ease_factor REAL NOT NULL DEFAULT 2.5,
                        next_review_utc TEXT NOT NULL,
                        last_reviewed_utc TEXT
                    );

                    CREATE TABLE IF NOT EXISTS detected_games (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        game_name TEXT NOT NULL,
                        process_name TEXT NOT NULL COLLATE NOCASE,
                        executable_path TEXT NOT NULL DEFAULT '',
                        launcher TEXT NOT NULL DEFAULT 'Standalone',
                        is_blocked INTEGER NOT NULL DEFAULT 1,
                        discovered_utc TEXT NOT NULL,
                        last_launched_utc TEXT
                    );

                    -- Pre-seed core academic subjects for student onboarding
                    INSERT INTO academic_subjects (id, name, code, semester, target_hours, completed_minutes, color_hex, notes, created_utc)
                    VALUES 
                        (1, 'Data Structures & Algorithms', 'CS301', 'Fall 2026', 50, 180, '#4F46E5', 'Core computer science syllabus: graphs, dynamic programming, trees', datetime('now')),
                        (2, 'Operating Systems', 'CS302', 'Fall 2026', 40, 120, '#06B6D4', 'Concurrency, virtual memory, scheduling, file systems', datetime('now')),
                        (3, 'Database Management Systems', 'CS303', 'Fall 2026', 35, 90, '#10B981', 'Relational algebra, normalization, transactions, SQL', datetime('now'));

                    -- Pre-seed topics
                    INSERT INTO academic_topics (subject_id, title, chapter, estimated_minutes, is_completed)
                    VALUES
                        (1, 'Graph Representation & BFS/DFS', 'Module 4', 90, 1),
                        (1, 'Dijkstra and Shortest Paths', 'Module 4', 75, 0),
                        (1, 'Dynamic Programming Fundamentals', 'Module 5', 120, 0),
                        (2, 'Process Synchronization & Semaphores', 'Module 2', 90, 1),
                        (2, 'Page Replacement Algorithms', 'Module 3', 80, 0),
                        (3, 'B+ Tree Indexing & Query Plans', 'Module 3', 90, 0);

                    -- Pre-seed upcoming assignments & exams
                    INSERT INTO academic_assignments (subject_id, title, description, due_date_utc, priority, status)
                    VALUES
                        (1, 'Graph Algorithm Programming Lab', 'Implement Prim and Kruskal algorithm with performance benchmarks.', datetime('now', '+4 days'), 3, 1),
                        (2, 'Producer-Consumer Synchronization', 'Build thread-safe circular buffer with mutex in C/C++.', datetime('now', '+7 days'), 2, 0);

                    INSERT INTO academic_exams (subject_id, title, exam_date_utc, target_score, syllabus_completed_percent, notes)
                    VALUES
                        (1, 'DSA Midterm Examination', datetime('now', '+12 days'), 95, 60, 'Covers Modules 1-4. Focus on asymptotic complexity.'),
                        (2, 'Operating Systems Finals', datetime('now', '+21 days'), 90, 45, 'Formula sheet allowed.');

                    -- Pre-seed starter flashcards for spaced repetition
                    INSERT INTO academic_flashcards (subject_id, question, answer, repetition_level, interval_days, ease_factor, next_review_utc)
                    VALUES
                        (1, 'What is the time complexity of Dijkstra with a Fibonacci Heap?', 'O(V log V + E)', 1, 1, 2.5, datetime('now')),
                        (2, 'What are the 4 Coffman conditions for Deadlock?', '1. Mutual Exclusion\n2. Hold and Wait\n3. No Preemption\n4. Circular Wait', 2, 6, 2.6, datetime('now')),
                        (3, 'Explain ACID in database transactions.', 'Atomicity, Consistency, Isolation, Durability.', 0, 1, 2.5, datetime('now'));

                    INSERT INTO schema_migrations (version, applied_utc, description)
                    VALUES (6, datetime('now'), 'v4.0 student academic planner, game shield, and spaced repetition');
                    """;
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            AppLogger.LogInfo("All database migrations verified and applied successfully.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            AppLogger.LogError("Migration failed; transaction rolled back.", ex);
            throw;
        }
    }

    private static void EnsureColumnExists(SqliteConnection conn, SqliteTransaction tx, string table, string column, string columnDef)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.Transaction = tx;
        checkCmd.CommandText = $"PRAGMA table_info({table});";
        using var reader = checkCmd.ExecuteReader();
        var exists = false;
        while (reader.Read())
        {
            var colName = reader.GetString(1);
            if (string.Equals(colName, column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        reader.Close();

        if (!exists)
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.Transaction = tx;
            alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDef};";
            alterCmd.ExecuteNonQuery();
        }
    }

    public void WipeAllData()
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM academic_flashcards;
                DELETE FROM academic_exams;
                DELETE FROM academic_assignments;
                DELETE FROM academic_topics;
                DELETE FROM academic_subjects;
                DELETE FROM detected_games;
                DELETE FROM app_budgets;
                DELETE FROM screen_time_records;
                DELETE FROM workspace_snapshots;
                DELETE FROM automation_rules;
                DELETE FROM distraction_events;
                DELETE FROM indexed_files;
                DELETE FROM memory_folders;
                DELETE FROM tasks;
                DELETE FROM projects;
                DELETE FROM sessions;
                DELETE FROM blocked_apps;
                DELETE FROM blocked_websites;
                DELETE FROM settings;
                DELETE FROM memory_fts;
                """;
            cmd.ExecuteNonQuery();
            tx.Commit();
            AppLogger.LogWarning("All user data wiped via Privacy Center request.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            AppLogger.LogError("Failed to wipe data.", ex);
            throw;
        }
    }
}
