using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public sealed class WorkspaceRecoveryReport
{
    public WorkspaceSnapshot Snapshot { get; init; } = new();
    public bool DirectoryExists { get; init; }
    public List<string> AccessibleFiles { get; init; } = new();
    public List<FocusTask> AssociatedTasks { get; init; } = new();
    public string SuggestedNextAction { get; init; } = "";
}

public static class WorkspaceRecoveryService
{
    public static WorkspaceSnapshot CreateSnapshotFromDirectory(string name, string rootDir, string notes = "")
    {
        var recentFiles = new List<string>();
        if (Directory.Exists(rootDir))
        {
            try
            {
                var di = new DirectoryInfo(rootDir);
                recentFiles = di.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Take(10)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch { }
        }

        return new WorkspaceSnapshot
        {
            Name = name,
            ProjectPath = rootDir,
            Description = $"Workspace at {rootDir}",
            RecentFilePaths = recentFiles,
            Notes = notes,
            SavedUtc = DateTimeOffset.UtcNow
        };
    }

    public static WorkspaceRecoveryReport InspectSnapshot(
        WorkspaceSnapshot snapshot,
        IEnumerable<FocusTask> allTasks)
    {
        var dirExists = !string.IsNullOrWhiteSpace(snapshot.ProjectPath) && Directory.Exists(snapshot.ProjectPath);
        var accessibleFiles = snapshot.RecentFilePaths.Where(File.Exists).ToList();

        var taskLookup = allTasks.ToDictionary(t => t.Id);
        var associated = new List<FocusTask>();
        foreach (var id in snapshot.AssociatedTaskIds)
        {
            if (taskLookup.TryGetValue(id, out var t))
            {
                associated.Add(t);
            }
        }

        var pendingTasks = associated.Where(t => t.Status != TaskStatusEnum.Completed).ToList();
        var suggestion = pendingTasks.Count > 0
            ? $"Resume pending objective: '{pendingTasks[0].Title}'"
            : (accessibleFiles.Count > 0
                ? $"Reopen recent file: {Path.GetFileName(accessibleFiles[0])}"
                : "Start a 25-minute deep focus block on this project.");

        return new WorkspaceRecoveryReport
        {
            Snapshot = snapshot,
            DirectoryExists = dirExists,
            AccessibleFiles = accessibleFiles,
            AssociatedTasks = associated,
            SuggestedNextAction = suggestion
        };
    }
}
