using System.Text.Json;

namespace ArenaDefender.Core.Persistence;

/// <summary>One leaderboard entry.</summary>
/// <param name="Score">Final score.</param>
/// <param name="Wave">Wave reached.</param>
/// <param name="SurvivalSeconds">How long the run lasted.</param>
/// <param name="Date">When the run finished (UTC).</param>
public sealed record HighScoreEntry(int Score, int Wave, float SurvivalSeconds, DateTime Date);

/// <summary>
/// The local leaderboard: the best <see cref="Capacity"/> runs, highest score first.
/// Pure logic with no file access, so it is easy to unit test.
/// </summary>
public sealed class HighScoreTable
{
    /// <summary>Number of entries kept by default.</summary>
    public const int DefaultCapacity = 5;

    private readonly List<HighScoreEntry> _entries = new();

    /// <summary>Creates an empty table.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    public HighScoreTable(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");
        }

        Capacity = capacity;
    }

    /// <summary>Creates a table pre-filled with entries (invalid ones are skipped).</summary>
    public HighScoreTable(IEnumerable<HighScoreEntry>? entries, int capacity = DefaultCapacity)
        : this(capacity)
    {
        if (entries is null)
        {
            return;
        }

        foreach (HighScoreEntry entry in entries)
        {
            TryAdd(entry, out _);
        }
    }

    /// <summary>Maximum number of entries.</summary>
    public int Capacity { get; }

    /// <summary>Entries sorted from best to worst.</summary>
    public IReadOnlyList<HighScoreEntry> Entries => _entries;

    /// <summary>The best score so far, or 0 when empty.</summary>
    public int BestScore => _entries.Count > 0 ? _entries[0].Score : 0;

    /// <summary>True when a score would earn a place on the table.</summary>
    public bool Qualifies(int score) =>
        score > 0 && (_entries.Count < Capacity || score > _entries[^1].Score);

    /// <summary>
    /// Inserts an entry in the correct sorted position if it qualifies.
    /// </summary>
    /// <param name="entry">The run to add.</param>
    /// <param name="rank">1-based position it was inserted at, or 0 if it did not qualify.</param>
    /// <returns>True if the entry was added.</returns>
    public bool TryAdd(HighScoreEntry? entry, out int rank)
    {
        rank = 0;
        if (entry is null || entry.Score < 0 || !Qualifies(entry.Score))
        {
            return false;
        }

        // Find the first entry with a lower score; ties keep the older run in front.
        int index = _entries.FindIndex(e => entry.Score > e.Score);
        if (index < 0)
        {
            index = _entries.Count;
        }

        _entries.Insert(index, entry);
        if (_entries.Count > Capacity)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        rank = index + 1;
        return true;
    }
}

/// <summary>Loads and saves the leaderboard. An interface so storage can be swapped (file, cloud, fake for tests).</summary>
public interface IHighScoreRepository
{
    /// <summary>Loads the saved entries. Never throws; returns an empty list if nothing can be read.</summary>
    IReadOnlyList<HighScoreEntry> Load();

    /// <summary>Saves the entries. Returns false (instead of crashing the game) if saving fails.</summary>
    bool Save(IEnumerable<HighScoreEntry> entries);
}

/// <summary>
/// Stores the leaderboard as JSON in a file. All I/O errors are caught and reported through
/// <see cref="LastError"/>: a missing, locked or corrupted save file must never crash the game.
/// </summary>
public sealed class JsonHighScoreRepository : IHighScoreRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Creates a repository that reads and writes the given file.</summary>
    /// <exception cref="ArgumentException">Thrown when the path is empty.</exception>
    public JsonHighScoreRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        FilePath = filePath;
    }

    /// <summary>The default save location in the user's local application data folder.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArenaDefender", "highscores.json");

    /// <summary>The JSON file used for storage.</summary>
    public string FilePath { get; }

    /// <summary>A description of the last error, or null if the last operation succeeded.</summary>
    public string? LastError { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<HighScoreEntry> Load()
    {
        LastError = null;
        try
        {
            if (!File.Exists(FilePath))
            {
                return Array.Empty<HighScoreEntry>();
            }

            string json = File.ReadAllText(FilePath);
            List<HighScoreEntry>? entries = JsonSerializer.Deserialize<List<HighScoreEntry>>(json, JsonOptions);
            return entries?.Where(e => e is not null && e.Score >= 0).ToList() ?? new List<HighScoreEntry>();
        }
        catch (JsonException ex)
        {
            LastError = $"High score file is corrupted and was ignored: {ex.Message}";
        }
        catch (IOException ex)
        {
            LastError = $"Could not read high scores: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            LastError = $"No permission to read high scores: {ex.Message}";
        }

        return Array.Empty<HighScoreEntry>();
    }

    /// <inheritdoc />
    public bool Save(IEnumerable<HighScoreEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        LastError = null;
        try
        {
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temporary file first, then replace, so a crash mid-write cannot corrupt the save.
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(entries.ToList(), JsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
            return true;
        }
        catch (IOException ex)
        {
            LastError = $"Could not save high scores: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            LastError = $"No permission to save high scores: {ex.Message}";
        }

        return false;
    }
}
