using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Logging;
using QuietoraFocus.Infrastructure.Repositories;

namespace QuietoraFocus.Infrastructure.Indexing;

public sealed class IndexingProgressReport
{
    public int FilesProcessed { get; init; }
    public int TotalDiscovered { get; init; }
    public string CurrentFile { get; init; } = "";
    public bool IsComplete { get; init; }
}

public sealed class LocalMemoryIndexer
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".cs", ".py", ".js", ".jsx", ".ts", ".tsx",
        ".json", ".csv", ".xml", ".html", ".htm", ".sql", ".ini", ".yaml", ".yml",
        ".cpp", ".c", ".h", ".hpp", ".java", ".rs", ".go", ".ps1", ".bat", ".cmd"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB limit
    private const int MaxContentReadChars = 64 * 1024; // 64 KB indexed text

    private readonly MemoryRepository _memoryRepo;

    public LocalMemoryIndexer(MemoryRepository memoryRepo)
    {
        _memoryRepo = memoryRepo;
    }

    public async Task<int> IndexFolderAsync(
        MemoryFolderConfig folder,
        IProgress<IndexingProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folder.FolderPath))
        {
            AppLogger.LogWarning($"Cannot index non-existent folder: {folder.FolderPath}");
            return 0;
        }

        AppLogger.LogInfo($"Beginning memory indexing for approved folder: {folder.FolderPath}");

        var files = new List<FileInfo>();
        try
        {
            var dirInfo = new DirectoryInfo(folder.FolderPath);
            var enumerated = dirInfo.EnumerateFiles("*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
            });

            foreach (var fi in enumerated)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (SupportedExtensions.Contains(fi.Extension) && fi.Length <= MaxFileSizeBytes)
                {
                    files.Add(fi);
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Error enumerating files in folder {folder.FolderPath}", ex);
            return 0;
        }

        var total = files.Count;
        var processed = 0;

        foreach (var fi in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AppLogger.LogInfo("Indexing was paused/cancelled by user.");
                break;
            }

            try
            {
                var content = await ReadFileContentAsync(fi.FullName, cancellationToken);
                var snippet = ExtractSnippet(content);

                var item = new IndexedFileItem
                {
                    FolderId = folder.Id,
                    FilePath = fi.FullName,
                    FileName = fi.Name,
                    Extension = fi.Extension.ToLowerInvariant(),
                    FileSizeBytes = fi.Length,
                    LastModifiedUtc = fi.LastWriteTimeUtc,
                    IndexedUtc = DateTimeOffset.UtcNow,
                    ExtractedSummary = $"{fi.Name} ({fi.Extension})",
                    ContentSnippet = snippet
                };

                _memoryRepo.UpsertIndexedFile(item, content);
                processed++;

                progress?.Report(new IndexingProgressReport
                {
                    FilesProcessed = processed,
                    TotalDiscovered = total,
                    CurrentFile = fi.Name,
                    IsComplete = false
                });
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Skipped unreadable file {fi.FullName}: {ex.Message}");
            }
        }

        _memoryRepo.UpdateFolderScanStats(folder.Id, processed);
        progress?.Report(new IndexingProgressReport
        {
            FilesProcessed = processed,
            TotalDiscovered = total,
            CurrentFile = "Complete",
            IsComplete = true
        });

        AppLogger.LogInfo($"Completed memory indexing for folder {folder.FolderPath}: {processed}/{total} files.");
        return processed;
    }

    private static async Task<string> ReadFileContentAsync(string filePath, CancellationToken ct)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);

        var buffer = new char[MaxContentReadChars];
        var readCount = await reader.ReadAsync(buffer.AsMemory(0, MaxContentReadChars), ct);
        return new string(buffer, 0, readCount);
    }

    private static string ExtractSnippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "Empty file";
        var singleLine = string.Join(" ", content
            .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Trim();

        return singleLine.Length > 220 ? singleLine[..220] + "..." : singleLine;
    }
}
