using ArenaDefender.Core;
using ArenaDefender.Core.Persistence;
using ArenaDefender.Graphics;
using ArenaDefender.Input;
using ArenaDefender.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender;

/// <summary>
/// The MonoGame <see cref="Game"/> class. It creates the shared services, hands them to the
/// screens through a <see cref="GameContext"/> and forwards Update/Draw to the
/// <see cref="ScreenManager"/>. It contains no gameplay logic itself.
/// </summary>
public sealed class ArenaDefenderGame : Game
{
    /// <summary>Width of the window in pixels.</summary>
    public const int ScreenWidth = 1280;

    /// <summary>Height of the window in pixels.</summary>
    public const int ScreenHeight = 720;

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private GameContext? _context;
    private SpriteBatch? _spriteBatch;
    private ShapeRenderer? _shapes;
    private PixelFont? _font;
    private GameState? _lastState;

    /// <summary>Configures the window and graphics device.</summary>
    public ArenaDefenderGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight,
            SynchronizeWithVerticalRetrace = true,
            PreferMultiSampling = true,
        };

        IsMouseVisible = true;
        IsFixedTimeStep = false; // GameWorld uses delta time, so variable frame rates are fine.
        Window.Title = "Arena Defender";
    }

    /// <inheritdoc />
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _shapes = new ShapeRenderer(GraphicsDevice, _spriteBatch);
        _font = new PixelFont(GraphicsDevice, _spriteBatch);

        var repository = new JsonHighScoreRepository(JsonHighScoreRepository.DefaultPath);
        var highScores = new HighScoreTable(repository.Load());
        if (repository.LastError is not null)
        {
            GameLog.Warning(repository.LastError);
        }

        _context = new GameContext(
            _spriteBatch,
            _shapes,
            _font,
            _input,
            new ScreenManager(),
            repository,
            highScores,
            new Point(ScreenWidth, ScreenHeight),
            Exit);

        _context.Screens.Replace(new MainMenuScreen(_context));
        GameLog.Info("Arena Defender started.");
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        if (_context is null)
        {
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _input.Update(IsActive);
        _context.TotalTime += deltaTime;

        try
        {
            _context.Screens.Update(deltaTime);
        }
        catch (Exception ex)
        {
            // Recover instead of crashing: log the problem and go back to the main menu.
            GameLog.Error("Unexpected error during update – returning to the main menu.", ex);
            _context.Screens.ResetTo(new MainMenuScreen(_context, "SOMETHING WENT WRONG - THE ERROR WAS LOGGED"));
        }

        // The enum decides cursor visibility: hidden while playing (a crosshair is drawn instead),
        // visible in every menu. One rule here replaces show/hide calls scattered across screens.
        GameState state = _context.Screens.CurrentState;
        if (state != _lastState)
        {
            IsMouseVisible = state != GameState.Playing;
            Window.Title = state == GameState.Paused ? "Arena Defender (paused)" : "Arena Defender";
            _lastState = state;
        }

        if (_context.Screens.IsEmpty)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    /// <inheritdoc />
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Palette.Background);
        _context?.Screens.Draw();
        base.Draw(gameTime);
    }

    /// <inheritdoc />
    protected override void UnloadContent()
    {
        _shapes?.Dispose();
        _font?.Dispose();
        _spriteBatch?.Dispose();
        base.UnloadContent();
    }
}
