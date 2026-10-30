namespace QuietoraFocus.Core.Models;

public sealed class MemoryFolderConfig
{
    public long Id { get; set; }
    public string FolderPath { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset AddedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastScannedUtc { get; set; }
    public int FileCount { get; set; }
}

public sealed class IndexedFileItem
{
    public long Id { get; set; }
    public long FolderId { get; set; }
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; }
    public DateTimeOffset IndexedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string ExtractedSummary { get; set; } = "";
    public string ContentSnippet { get; set; } = "";

    public string SizeLabel => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB"
    };

    public string ModifiedLabel => LastModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public sealed class MemorySearchResult
{
    public long Id { get; set; }
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Snippet { get; set; } = "";
    public DateTimeOffset LastModifiedUtc { get; set; }
    public double Rank { get; set; }

    public string ModifiedLabel => LastModifiedUtc.ToLocalTime().ToString("MMM dd, yyyy");
}
