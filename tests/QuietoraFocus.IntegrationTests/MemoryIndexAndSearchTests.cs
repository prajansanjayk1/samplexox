using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Indexing;
using QuietoraFocus.Infrastructure.Repositories;
using Xunit;

namespace QuietoraFocus.IntegrationTests;

public class MemoryIndexAndSearchTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _tempFilesDir;

    public MemoryIndexAndSearchTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"quietora_mem_{Guid.NewGuid():N}.db");
        _tempFilesDir = Path.Combine(Path.GetTempPath(), $"quietora_test_docs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempFilesDir);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_tempDbPath);
            }
            if (Directory.Exists(_tempFilesDir))
            {
                Directory.Delete(_tempFilesDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task IndexFolder_AndFts5Search_FindsTargetKeywords()
    {
        // 1. Create sample test documents in approved folder
        var doc1 = Path.Combine(_tempFilesDir, "QuantumPhysics_Notes.txt");
        File.WriteAllText(doc1, "Schrodinger equation and quantum harmonic oscillator state vectors in Hilbert space.");

        var doc2 = Path.Combine(_tempFilesDir, "MachineLearning_Project.py");
        File.WriteAllText(doc2, "import torch\ndef train_transformer():\n    optimizer = torch.optim.AdamW(model.parameters(), lr=1e-4)");

        var doc3 = Path.Combine(_tempFilesDir, "CollegeBudget.csv");
        File.WriteAllText(doc3, "Item,Cost,Category\nTuition,12000,Education\nBooks,450,Supplies");

        // 2. Initialize DB & indexer
        var db = new QuietoraDatabase(_tempDbPath);
        var memoryRepo = new MemoryRepository(db);
        var indexer = new LocalMemoryIndexer(memoryRepo);

        memoryRepo.AddFolder(_tempFilesDir);
        var folders = memoryRepo.GetFolders();
        Assert.Single(folders);

        // 3. Index folder
        var indexedCount = await indexer.IndexFolderAsync(folders[0]);
        Assert.Equal(3, indexedCount);

        var (filesCount, totalBytes, folderCount) = memoryRepo.GetMemoryStats();
        Assert.Equal(3, filesCount);
        Assert.True(totalBytes > 0);
        Assert.Equal(1, folderCount);

        // 4. Search via FTS5
        var quantumResults = memoryRepo.Search("Quantum");
        Assert.Single(quantumResults);
        Assert.Equal("QuantumPhysics_Notes.txt", quantumResults[0].FileName);
        Assert.Contains("Schrodinger", quantumResults[0].Snippet);

        var mlResults = memoryRepo.Search("transformer");
        Assert.Single(mlResults);
        Assert.Equal("MachineLearning_Project.py", mlResults[0].FileName);

        var budgetResults = memoryRepo.Search("Tuition");
        Assert.Single(budgetResults);
        Assert.Equal("CollegeBudget.csv", budgetResults[0].FileName);

        // 5. Test empty search returns all recent files
        var allResults = memoryRepo.Search("");
        Assert.Equal(3, allResults.Count);

        // 6. Test clear index
        memoryRepo.ClearMemoryIndex();
        var (afterClearCount, afterBytes, _) = memoryRepo.GetMemoryStats();
        Assert.Equal(0, afterClearCount);
        Assert.Equal(0, afterBytes);
    }
}
