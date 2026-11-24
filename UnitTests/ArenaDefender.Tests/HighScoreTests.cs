using ArenaDefender.Core.Persistence;

namespace ArenaDefender.Tests;

/// <summary>Tests for the leaderboard logic and its JSON storage (including error handling).</summary>
[TestFixture]
public class HighScoreTests
{
    private string _tempFile = null!;

    [SetUp]
    public void SetUp() =>
        _tempFile = Path.Combine(Path.GetTempPath(), $"arena-defender-test-{Guid.NewGuid():N}.json");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    private static HighScoreEntry Entry(int score) => new(score, 1, 10f, new DateTime(2026, 1, 1));

    [Test]
    public void TryAdd_KeepsEntriesSortedHighestFirst()
    {
        var table = new HighScoreTable();
        table.TryAdd(Entry(300), out _);
        table.TryAdd(Entry(900), out _);
        table.TryAdd(Entry(500), out int rank);

        Assert.Multiple(() =>
        {
            Assert.That(table.Entries.Select(e => e.Score), Is.EqualTo(new[] { 900, 500, 300 }));
            Assert.That(rank, Is.EqualTo(2));
        });
    }

    [Test]
    public void TryAdd_TableFull_DropsTheLowestScore()
    {
        var table = new HighScoreTable(capacity: 3);
        foreach (int score in new[] { 100, 200, 300 })
        {
            table.TryAdd(Entry(score), out _);
        }

        table.TryAdd(Entry(250), out _);

        Assert.That(table.Entries.Select(e => e.Score), Is.EqualTo(new[] { 300, 250, 200 }));
    }

    [Test]
    public void TryAdd_ScoreTooLowForFullTable_IsRejected()
    {
        var table = new HighScoreTable(new[] { Entry(500), Entry(400) }, capacity: 2);

        bool added = table.TryAdd(Entry(100), out int rank);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.False);
            Assert.That(rank, Is.EqualTo(0));
            Assert.That(table.Entries, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Qualifies_ZeroScore_IsNeverAHighScore()
    {
        Assert.That(new HighScoreTable().Qualifies(0), Is.False);
    }

    [Test]
    public void Repository_SaveThenLoad_RoundTripsEntries()
    {
        var repository = new JsonHighScoreRepository(_tempFile);
        var entries = new[] { Entry(1200), Entry(800) };

        bool saved = repository.Save(entries);
        var loaded = repository.Load();

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(loaded, Is.EqualTo(entries));
        });
    }

    [Test]
    public void Repository_MissingFile_ReturnsEmptyListWithoutError()
    {
        var repository = new JsonHighScoreRepository(_tempFile);

        var loaded = repository.Load();

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Empty);
            Assert.That(repository.LastError, Is.Null);
        });
    }

    [Test]
    public void Repository_CorruptedFile_ReturnsEmptyListAndReportsError()
    {
        File.WriteAllText(_tempFile, "{ this is not valid json ]");
        var repository = new JsonHighScoreRepository(_tempFile);

        var loaded = repository.Load();

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Empty);
            Assert.That(repository.LastError, Does.Contain("corrupted"));
        });
    }

    [Test]
    public void Repository_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _ = new JsonHighScoreRepository("  "));
    }
}
