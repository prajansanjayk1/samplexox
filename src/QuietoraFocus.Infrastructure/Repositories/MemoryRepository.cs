using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class MemoryRepository
{
    private readonly QuietoraDatabase _db;

    public MemoryRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    public List<MemoryFolderConfig> GetFolders()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, folder_path, is_active, added_utc, last_scanned_utc, file_count FROM memory_folders ORDER BY id ASC";
        using var reader = cmd.ExecuteReader();
        var list = new List<MemoryFolderConfig>();
        while (reader.Read())
        {
            list.Add(new MemoryFolderConfig
            {
                Id = reader.GetInt64(0),
                FolderPath = reader.GetString(1),
                IsActive = reader.GetInt32(2) == 1,
                AddedUtc = DateTimeOffset.Parse(reader.GetString(3)),
                LastScannedUtc = reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4)),
                FileCount = reader.GetInt32(5)
            });
        }
        return list;
    }

    public void AddFolder(string folderPath)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO memory_folders (folder_path, is_active, added_utc, file_count)
            VALUES ($path, 1, $added, 0)
            """;
        cmd.Parameters.AddWithValue("$path", folderPath);
        cmd.Parameters.AddWithValue("$added", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void RemoveFolder(long id)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            // Remove from FTS
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    DELETE FROM memory_fts WHERE file_path IN (
                        SELECT file_path FROM indexed_files WHERE folder_id = $id
                    );
                    DELETE FROM indexed_files WHERE folder_id = $id;
                    DELETE FROM memory_folders WHERE id = $id;
                    """;
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            AppLogger.LogError($"Failed to remove memory folder {id}", ex);
            throw;
        }
    }

    public void UpdateFolderScanStats(long id, int fileCount)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE memory_folders
            SET file_count = $cnt, last_scanned_utc = $scanned
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$cnt", fileCount);
        cmd.Parameters.AddWithValue("$scanned", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpsertIndexedFile(IndexedFileItem item, string fullText)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO indexed_files
                      (folder_id, file_path, file_name, extension, file_size_bytes, last_modified_utc, indexed_utc, extracted_summary, content_snippet)
                    VALUES
                      ($folderId, $path, $name, $ext, $size, $mod, $indexed, $summary, $snippet)
                    ON CONFLICT(file_path) DO UPDATE SET
                      folder_id = excluded.folder_id,
                      file_size_bytes = excluded.file_size_bytes,
                      last_modified_utc = excluded.last_modified_utc,
                      indexed_utc = excluded.indexed_utc,
                      extracted_summary = excluded.extracted_summary,
                      content_snippet = excluded.content_snippet;

                    DELETE FROM memory_fts WHERE file_path = $path;

                    INSERT INTO memory_fts(file_path, file_name, content)
                    VALUES ($path, $name, $content);
                    """;

                cmd.Parameters.AddWithValue("$folderId", item.FolderId);
                cmd.Parameters.AddWithValue("$path", item.FilePath);
                cmd.Parameters.AddWithValue("$name", item.FileName);
                cmd.Parameters.AddWithValue("$ext", item.Extension);
                cmd.Parameters.AddWithValue("$size", item.FileSizeBytes);
                cmd.Parameters.AddWithValue("$mod", item.LastModifiedUtc.ToString("O"));
                cmd.Parameters.AddWithValue("$indexed", item.IndexedUtc.ToString("O"));
                cmd.Parameters.AddWithValue("$summary", item.ExtractedSummary);
                cmd.Parameters.AddWithValue("$snippet", item.ContentSnippet);
                cmd.Parameters.AddWithValue("$content", fullText);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            AppLogger.LogError($"Failed to index file {item.FilePath}", ex);
        }
    }

    public List<MemorySearchResult> Search(string query, int limit = 30)
    {
        var clean = (query ?? "").Trim();
        var results = new List<MemorySearchResult>();

        if (string.IsNullOrWhiteSpace(clean))
        {
            // Return recent indexed files
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, file_path, file_name, extension, content_snippet, last_modified_utc, 0.0 as rank
                FROM indexed_files
                ORDER BY last_modified_utc DESC
                LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$limit", limit);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new MemorySearchResult
                {
                    Id = reader.GetInt64(0),
                    FilePath = reader.GetString(1),
                    FileName = reader.GetString(2),
                    Extension = reader.GetString(3),
                    Snippet = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LastModifiedUtc = DateTimeOffset.Parse(reader.GetString(5)),
                    Rank = 0
                });
            }
            return results;
        }

        // FTS5 Full-Text Search with fallback
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();

            // Prepare safe FTS5 query token string (quotes or terms)
            var ftsToken = clean.Replace("'", "''").Replace("\"", "");
            // Support prefix matching by appending * to alphanumeric tokens
            var terms = ftsToken.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ftsQuery = string.Join(" AND ", terms.Select(t => $"\"{t}\"*"));

            cmd.CommandText = """
                SELECT f.id, f.file_path, f.file_name, f.extension,
                       snippet(memory_fts, 2, '«', '»', '...', 20) as matched_snippet,
                       f.last_modified_utc, bm25(memory_fts) as rank
                FROM memory_fts
                JOIN indexed_files f ON memory_fts.file_path = f.file_path
                WHERE memory_fts MATCH $fts
                ORDER BY rank
                LIMIT $limit;
                """;
            cmd.Parameters.AddWithValue("$fts", ftsQuery);
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new MemorySearchResult
                {
                    Id = reader.GetInt64(0),
                    FilePath = reader.GetString(1),
                    FileName = reader.GetString(2),
                    Extension = reader.GetString(3),
                    Snippet = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LastModifiedUtc = DateTimeOffset.Parse(reader.GetString(5)),
                    Rank = reader.GetDouble(6)
                });
            }

            if (results.Count > 0) return results;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"FTS5 search error on '{clean}'; falling back to filename LIKE search: {ex.Message}");
        }

        // Fallback: LIKE query on file name and path
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, file_path, file_name, extension, content_snippet, last_modified_utc, 0.0 as rank
                FROM indexed_files
                WHERE file_name LIKE $pattern OR content_snippet LIKE $pattern
                ORDER BY last_modified_utc DESC
                LIMIT $limit
                """;
            cmd.Parameters.AddWithValue("$pattern", $"%{clean}%");
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new MemorySearchResult
                {
                    Id = reader.GetInt64(0),
                    FilePath = reader.GetString(1),
                    FileName = reader.GetString(2),
                    Extension = reader.GetString(3),
                    Snippet = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    LastModifiedUtc = DateTimeOffset.Parse(reader.GetString(5)),
                    Rank = 0
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Fallback LIKE search failed", ex);
        }

        return results;
    }

    public (int fileCount, long totalBytes, int folderCount) GetMemoryStats()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM indexed_files),
                (SELECT COALESCE(SUM(file_size_bytes), 0) FROM indexed_files),
                (SELECT COUNT(*) FROM memory_folders)
            """;
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt64(1), reader.GetInt32(2));
        }
        return (0, 0, 0);
    }

    public void ClearMemoryIndex()
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM indexed_files;
                DELETE FROM memory_fts;
                UPDATE memory_folders SET file_count = 0, last_scanned_utc = NULL;
                """;
            cmd.ExecuteNonQuery();
            tx.Commit();
            AppLogger.LogInfo("PC Memory index cleared.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            AppLogger.LogError("Failed to clear memory index", ex);
            throw;
        }
    }
}
