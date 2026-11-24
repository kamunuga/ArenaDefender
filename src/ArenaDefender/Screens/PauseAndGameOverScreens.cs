using ArenaDefender.Core;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Graphics;
using ArenaDefender.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Screens;

/// <summary>
/// Overlay shown on top of the paused game. Because it is pushed onto the screen stack,
/// the gameplay screen underneath keeps its exact state and is still drawn behind the overlay.
/// </summary>
public sealed class PauseScreen : GameScreen
{
    private readonly Menu _menu;
    private float _fade;

    /// <summary>Creates the pause overlay.</summary>
    public PauseScreen(GameContext context)
        : base(context)
    {
        _menu = Menu.CreateVertical(
            Context.ScreenCentre.X, 290, 320, 56, 16,
            ("RESUME", () => Context.Screens.Pop()),
            ("RESTART", () => Context.Screens.Replace(new GameplayScreen(Context))),
            ("MAIN MENU", () => Context.Screens.Replace(new MainMenuScreen(Context))),
            ("QUIT GAME", Context.Exit));
    }

    /// <inheritdoc />
    public override bool IsOverlay => true;

    /// <inheritdoc />
    public override GameState State => GameState.Paused;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        _fade = GameMath.Lerp(_fade, 1f, GameMath.SmoothingFactor(12f, deltaTime));

        if (Context.Input.IsPausePressed)
        {
            Context.Screens.Pop();
            return;
        }

        _menu.Update(deltaTime, Context.Input);
    }

    /// <inheritdoc />
    public override void Draw()
    {
        Context.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        Context.Shapes.FillRectangle(new Rectangle(0, 0, Context.ScreenSize.X, Context.ScreenSize.Y), Color.Black * (0.65f * _fade));
        Context.Font.DrawShadowed("PAUSED", new Vector2(Context.ScreenCentre.X, 160), ColorUtil.Fade(Color.White, _fade), 8f, TextAlign.Centre);
        _menu.Draw(Context.Shapes, Context.Font, _fade);
        Context.Font.Draw("ESC TO RESUME", new Vector2(Context.ScreenCentre.X, Context.ScreenSize.Y - 60), ColorUtil.Fade(Palette.TextDim, _fade), 2f, TextAlign.Centre);
        Context.SpriteBatch.End();
    }
}

/// <summary>Shown after the last life is lost: final statistics and leaderboard placement.</summary>
public sealed class GameOverScreen : GameScreen
{
    private readonly GameSummary _summary;
    private readonly Menu _menu;
    private readonly MenuBackground _background = new();
    private float _fade;
    private float _countUp;

    /// <summary>Creates the game-over screen.</summary>
    public GameOverScreen(GameContext context, GameSummary summary)
        : base(context)
    {
        _summary = summary ?? throw new ArgumentNullException(nameof(summary));
        _menu = Menu.CreateVertical(
            Context.ScreenCentre.X, 500, 320, 52, 14,
            ("PLAY AGAIN", () => Context.Screens.Replace(new GameplayScreen(Context))),
            ("MAIN MENU", () => Context.Screens.Replace(new MainMenuScreen(Context))));
    }

    /// <inheritdoc />
    public override GameState State => GameState.GameOver;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        _fade = GameMath.Lerp(_fade, 1f, GameMath.SmoothingFactor(4f, deltaTime));

        // The final score counts up from 0 (lerp), finishing in about a second.
        _countUp = GameMath.Lerp(_countUp, _summary.Score, GameMath.SmoothingFactor(5f, deltaTime));
        if (_summary.Score - _countUp < 1f)
        {
            _countUp = _summary.Score;
        }

        _menu.Update(deltaTime, Context.Input);
    }

    /// <inheritdoc />
    public override void Draw()
    {
        SpriteBatch batch = Context.SpriteBatch;
        PixelFont font = Context.Font;
        Point screen = Context.ScreenSize;
        float cx = screen.X / 2f;

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        _background.Draw(Context.Shapes, screen, Context.TotalTime);
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        font.DrawShadowed("GAME OVER", new Vector2(cx, 60), ColorUtil.Fade(Palette.HealthLow, _fade), 10f, TextAlign.Centre);

        font.DrawShadowed("FINAL SCORE", new Vector2(cx, 160), ColorUtil.Fade(Palette.TextDim, _fade), 3f, TextAlign.Centre);
        font.DrawShadowed(((int)_countUp).ToString("N0"), new Vector2(cx, 194), ColorUtil.Fade(Palette.Gold, _fade), 8f, TextAlign.Centre);

        if (_summary.Rank > 0)
        {
            float pulse = 0.6f + 0.4f * MathF.Sin(Context.TotalTime * 6f);
            string label = _summary.Rank == 1 ? "NEW HIGH SCORE!" : $"LEADERBOARD RANK #{_summary.Rank}";
            font.DrawShadowed(label, new Vector2(cx, 270), ColorUtil.Fade(Palette.Accent, _fade * pulse), 3f, TextAlign.Centre);
        }
        else
        {
            font.Draw($"BEST: {Context.HighScores.BestScore:N0}", new Vector2(cx, 270), ColorUtil.Fade(Palette.TextDim, _fade), 3f, TextAlign.Centre);
        }

        int minutes = (int)(_summary.SurvivalSeconds / 60f);
        int seconds = (int)(_summary.SurvivalSeconds % 60f);
        string[] stats =
        {
            $"WAVE REACHED   {_summary.Wave}",
            $"TIME SURVIVED  {minutes}:{seconds:00}",
            $"ENEMIES KILLED {_summary.EnemiesDefeated}",
            $"ACCURACY       {_summary.Accuracy:0}%",
        };

        var panel = new Rectangle((int)cx - 220, 320, 440, 160);
        Context.Shapes.FillRectangle(panel, ColorUtil.Fade(Palette.Panel, _fade * 0.9f));
        Context.Shapes.DrawRectangle(panel, ColorUtil.Fade(new Color(50, 70, 110), _fade), 2);
        for (int i = 0; i < stats.Length; i++)
        {
            font.Draw(stats[i], new Vector2(panel.X + 40, panel.Y + 24 + i * 32), ColorUtil.Fade(Palette.Text, _fade), 2f);
        }

        _menu.Draw(Context.Shapes, font, _fade);
        batch.End();
    }
}
