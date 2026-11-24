using ArenaDefender.Core.Persistence;
using ArenaDefender.Graphics;
using ArenaDefender.Input;
using ArenaDefender.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender;

/// <summary>
/// The shared services every screen needs (drawing, input, screen switching, high scores).
/// Passing one context object to each screen keeps constructors short and avoids global statics.
/// </summary>
public sealed class GameContext
{
    private readonly Action _exit;

    /// <summary>Creates the context.</summary>
    public GameContext(
        SpriteBatch spriteBatch,
        ShapeRenderer shapes,
        PixelFont font,
        InputState input,
        ScreenManager screens,
        IHighScoreRepository highScoreRepository,
        HighScoreTable highScores,
        Point screenSize,
        Action exit)
    {
        SpriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        Shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
        Font = font ?? throw new ArgumentNullException(nameof(font));
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Screens = screens ?? throw new ArgumentNullException(nameof(screens));
        HighScoreRepository = highScoreRepository ?? throw new ArgumentNullException(nameof(highScoreRepository));
        HighScores = highScores ?? throw new ArgumentNullException(nameof(highScores));
        ScreenSize = screenSize;
        _exit = exit ?? throw new ArgumentNullException(nameof(exit));
    }

    /// <summary>The shared sprite batch.</summary>
    public SpriteBatch SpriteBatch { get; }

    /// <summary>Draws code-generated shapes.</summary>
    public ShapeRenderer Shapes { get; }

    /// <summary>Draws text.</summary>
    public PixelFont Font { get; }

    /// <summary>Keyboard and mouse state.</summary>
    public InputState Input { get; }

    /// <summary>Screen stack.</summary>
    public ScreenManager Screens { get; }

    /// <summary>Saves and loads the leaderboard.</summary>
    public IHighScoreRepository HighScoreRepository { get; }

    /// <summary>The in-memory leaderboard.</summary>
    public HighScoreTable HighScores { get; }

    /// <summary>Window size in pixels.</summary>
    public Point ScreenSize { get; }

    /// <summary>Screen centre in pixels.</summary>
    public Vector2 ScreenCentre => new(ScreenSize.X / 2f, ScreenSize.Y / 2f);

    /// <summary>Seconds since the game launched (for animations).</summary>
    public float TotalTime { get; set; }

    /// <summary>Closes the game.</summary>
    public void Exit() => _exit();

    /// <summary>
    /// Adds a finished run to the leaderboard and saves it.
    /// </summary>
    /// <returns>The 1-based rank achieved, or 0 if the run did not make the table.</returns>
    public int SubmitHighScore(HighScoreEntry entry)
    {
        if (!HighScores.TryAdd(entry, out int rank))
        {
            return 0;
        }

        if (!HighScoreRepository.Save(HighScores.Entries) && HighScoreRepository is JsonHighScoreRepository json)
        {
            GameLog.Warning(json.LastError ?? "High scores could not be saved.");
        }

        return rank;
    }
}
