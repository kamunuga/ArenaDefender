namespace ArenaDefender;

/// <summary>
/// Minimal logger that writes to the console and to a log file. Logging must never crash the
/// game, so every file operation is wrapped in a try/catch.
/// </summary>
public static class GameLog
{
    private static readonly object Sync = new();

    /// <summary>Where the log file is written.</summary>
    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArenaDefender", "log.txt");

    /// <summary>Logs an informational message.</summary>
    public static void Info(string message) => Write("INFO", message);

    /// <summary>Logs a warning.</summary>
    public static void Warning(string message) => Write("WARN", message);

    /// <summary>Logs an error, including the exception details when available.</summary>
    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private static void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        Console.WriteLine(line);

        lock (Sync)
        {
            try
            {
                string? directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // Ignore: being unable to write a log must not stop the game.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore for the same reason.
            }
        }
    }
}
