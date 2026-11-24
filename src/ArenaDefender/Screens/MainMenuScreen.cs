using ArenaDefender.Core;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Persistence;
using ArenaDefender.Graphics;
using ArenaDefender.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Screens;

/// <summary>Title screen: game name, Play and Quit buttons, controls and the leaderboard.</summary>
public sealed class MainMenuScreen : GameScreen
{
    private readonly Menu _menu;
    private readonly MenuBackground _background = new();
    private readonly string? _message;
    private float _fade;

    /// <summary>Creates the main menu.</summary>
    /// <param name="context">Shared services.</param>
    /// <param name="message">Optional message to show (for example after recovering from an error).</param>
    public MainMenuScreen(GameContext context, string? message = null)
        : base(context)
    {
        _message = message;
        _menu = Menu.CreateVertical(
            Context.ScreenCentre.X, 300, 300, 58, 18,
            ("PLAY", () => Context.Screens.Replace(new GameplayScreen(Context))),
            ("QUIT", Context.Exit));
    }

    /// <inheritdoc />
    public override GameState State => GameState.MainMenu;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Fade the menu in with a lerp of the opacity.
        _fade = GameMath.Lerp(_fade, 1f, GameMath.SmoothingFactor(4f, deltaTime));
        _menu.Update(deltaTime, Context.Input);
    }

    /// <inheritdoc />
    public override void Draw()
    {
        SpriteBatch batch = Context.SpriteBatch;
        ShapeRenderer shapes = Context.Shapes;
        PixelFont font = Context.Font;
        Point screen = Context.ScreenSize;
        float time = Context.TotalTime;

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        _background.Draw(shapes, screen, time);
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        // Title colour shifts smoothly between two colours.
        float wave = 0.5f + 0.5f * MathF.Sin(time * 1.5f);
        Color titleColor = ColorUtil.Lerp(Palette.Accent, new Color(180, 120, 255), wave);
        float titleY = GameMath.Lerp(80f, 110f, _fade);
        font.DrawShadowed("ARENA DEFENDER", new Vector2(screen.X / 2f, titleY), ColorUtil.Fade(titleColor, _fade), 10f, TextAlign.Centre);
        font.DrawShadowed("SURVIVE THE ENDLESS WAVES", new Vector2(screen.X / 2f, titleY + 92), ColorUtil.Fade(Palette.TextDim, _fade), 3f, TextAlign.Centre);

        _menu.Draw(shapes, font, _fade);

        DrawControls(shapes, font, _fade);
        DrawHighScores(shapes, font, Context.HighScores, screen, _fade);

        font.Draw("W/S OR ARROWS + ENTER, OR USE THE MOUSE", new Vector2(screen.X / 2f, screen.Y - 40), ColorUtil.Fade(Palette.TextDim, _fade * 0.8f), 2f, TextAlign.Centre);

        if (!string.IsNullOrEmpty(_message))
        {
            font.DrawShadowed(_message, new Vector2(screen.X / 2f, screen.Y - 80), Palette.HealthLow, 2f, TextAlign.Centre);
        }

        batch.End();
    }

    private static void DrawControls(ShapeRenderer shapes, PixelFont font, float opacity)
    {
        var panel = new Rectangle(50, 290, 330, 290);
        shapes.FillRectangle(panel, ColorUtil.Fade(Palette.Panel, opacity * 0.85f));
        shapes.DrawRectangle(panel, ColorUtil.Fade(new Color(50, 70, 110), opacity), 2);

        font.Draw("CONTROLS", new Vector2(panel.Center.X, panel.Y + 16), ColorUtil.Fade(Palette.Accent, opacity), 3f, TextAlign.Centre);
        string[] lines =
        {
            "MOVE    WASD/ARROWS",
            "AIM     MOUSE",
            "SHOOT   LMB / SPACE",
            "NOVA    RMB / E",
            "PAUSE   ESC / P",
        };

        for (int i = 0; i < lines.Length; i++)
        {
            font.Draw(lines[i], new Vector2(panel.X + 20, panel.Y + 64 + i * 30), ColorUtil.Fade(Palette.Text, opacity), 2f);
        }

        font.Draw("BRUTES BLOCK SHOTS\nFROM THE FRONT!", new Vector2(panel.X + 20, panel.Y + 226), ColorUtil.Fade(Palette.Gold, opacity), 2f);
    }

    private static void DrawHighScores(ShapeRenderer shapes, PixelFont font, HighScoreTable table, Point screen, float opacity)
    {
        var panel = new Rectangle(screen.X - 380, 290, 330, 290);
        shapes.FillRectangle(panel, ColorUtil.Fade(Palette.Panel, opacity * 0.85f));
        shapes.DrawRectangle(panel, ColorUtil.Fade(new Color(50, 70, 110), opacity), 2);
        font.Draw("HIGH SCORES", new Vector2(panel.Center.X, panel.Y + 16), ColorUtil.Fade(Palette.Gold, opacity), 3f, TextAlign.Centre);

        if (table.Entries.Count == 0)
        {
            font.Draw("NO SCORES YET\n\nBE THE FIRST!", new Vector2(panel.Center.X, panel.Y + 110), ColorUtil.Fade(Palette.TextDim, opacity), 2f, TextAlign.Centre);
            return;
        }

        for (int i = 0; i < table.Entries.Count; i++)
        {
            HighScoreEntry entry = table.Entries[i];
            float y = panel.Y + 66 + i * 40;
            Color color = i == 0 ? Palette.Gold : Palette.Text;
            font.Draw($"{i + 1}.", new Vector2(panel.X + 20, y), ColorUtil.Fade(color, opacity), 2f);
            font.Draw(entry.Score.ToString("N0"), new Vector2(panel.X + 60, y), ColorUtil.Fade(color, opacity), 2f);
            font.Draw($"WAVE {entry.Wave}", new Vector2(panel.Right - 20, y), ColorUtil.Fade(Palette.TextDim, opacity), 2f, TextAlign.Right);
        }
    }
}
