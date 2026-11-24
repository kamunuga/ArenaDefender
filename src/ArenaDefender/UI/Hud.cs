using ArenaDefender.Core;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Systems;
using ArenaDefender.Core.World;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.UI;

/// <summary>
/// The in-game heads-up display: health, lives, score, combo, wave, nova cooldown and active
/// power-ups. It only READS the game world; it never changes it.
/// </summary>
public sealed class Hud
{
    private readonly AnimatedBar _healthBar = new();
    private readonly AnimatedBar _novaBar = new();
    private float _displayedScore;
    private float _scorePulse;
    private string _banner = string.Empty;
    private string _bannerSubtitle = string.Empty;
    private float _bannerTime;
    private float _damageFlash;

    private const float BannerDuration = 2.6f;

    /// <summary>Shows a large message in the middle of the screen, e.g. "WAVE 3".</summary>
    public void ShowBanner(string title, string subtitle = "")
    {
        _banner = title;
        _bannerSubtitle = subtitle;
        _bannerTime = BannerDuration;
    }

    /// <summary>Makes the score text pulse (call when points are scored).</summary>
    public void PulseScore() => _scorePulse = 1f;

    /// <summary>Flashes the screen edges red (call when the player is hurt).</summary>
    public void FlashDamage() => _damageFlash = 1f;

    /// <summary>Animates the HUD towards the world's current values.</summary>
    public void Update(float deltaTime, GameWorld world)
    {
        _healthBar.Update(deltaTime, world.Player.Health.Fraction);
        _novaBar.Update(deltaTime, world.Player.NovaCooldown.Progress);

        // The score counts up smoothly rather than jumping (lerp), but never lags far behind.
        _displayedScore = GameMath.Lerp(_displayedScore, world.Score.Score, GameMath.SmoothingFactor(10f, deltaTime));
        if (MathF.Abs(_displayedScore - world.Score.Score) < 1f)
        {
            _displayedScore = world.Score.Score;
        }

        _scorePulse = MathF.Max(0f, _scorePulse - deltaTime * 4f);
        _bannerTime = MathF.Max(0f, _bannerTime - deltaTime);
        _damageFlash = MathF.Max(0f, _damageFlash - deltaTime * 2.5f);
    }

    /// <summary>Draws the HUD in screen space.</summary>
    public void Draw(ShapeRenderer shapes, PixelFont font, GameWorld world, Point screen, float time)
    {
        DrawDamageFlash(shapes, screen);
        DrawHealthAndLives(shapes, font, world);
        DrawScore(shapes, font, world, screen);
        DrawWave(font, world, screen);
        DrawNova(shapes, font, world, screen);
        DrawPowerUps(shapes, font, world, screen);
        DrawBanner(font, screen);

        if (world.Player.IsInvulnerable && !world.IsGameOver && world.Player.InvulnerableTime > Core.Entities.Player.HitInvulnerability)
        {
            float blink = 0.5f + 0.5f * MathF.Sin(time * 10f);
            font.DrawShadowed("RESPAWNED - INVULNERABLE", new Vector2(screen.X / 2f, screen.Y - 110), ColorUtil.Fade(Palette.Accent, blink), 2f, TextAlign.Centre);
        }
    }

    private void DrawDamageFlash(ShapeRenderer shapes, Point screen)
    {
        if (_damageFlash <= 0f)
        {
            return;
        }

        Color c = new Color(255, 40, 40) * (0.35f * _damageFlash);
        const int thickness = 18;
        shapes.FillRectangle(new Rectangle(0, 0, screen.X, thickness), c);
        shapes.FillRectangle(new Rectangle(0, screen.Y - thickness, screen.X, thickness), c);
        shapes.FillRectangle(new Rectangle(0, 0, thickness, screen.Y), c);
        shapes.FillRectangle(new Rectangle(screen.X - thickness, 0, thickness, screen.Y), c);
    }

    private void DrawHealthAndLives(ShapeRenderer shapes, PixelFont font, GameWorld world)
    {
        var player = world.Player;
        font.DrawShadowed("HEALTH", new Vector2(24, 20), Palette.TextDim, 2f);
        _healthBar.Draw(shapes, new Rectangle(24, 40, 280, 22));
        font.DrawShadowed($"{MathF.Ceiling(player.Health.Current)}/{player.Health.Max}", new Vector2(164, 44), Color.White, 2f, TextAlign.Centre);

        font.DrawShadowed("LIVES", new Vector2(24, 74), Palette.TextDim, 2f);
        for (int i = 0; i < Core.Entities.Player.StartingLives; i++)
        {
            Vector2 centre = new(100 + i * 26, 81);
            bool alive = i < player.Lives;
            shapes.FillCircle(centre, 9f, alive ? Palette.Player : new Color(40, 50, 80));
            if (alive)
            {
                shapes.FillCircle(centre, 4f, Palette.PlayerCore);
            }
        }
    }

    private void DrawScore(ShapeRenderer shapes, PixelFont font, GameWorld world, Point screen)
    {
        float scale = 4f + _scorePulse * 1.2f;
        font.DrawShadowed("SCORE", new Vector2(screen.X / 2f, 14), Palette.TextDim, 2f, TextAlign.Centre);
        font.DrawShadowed(((int)_displayedScore).ToString("N0"), new Vector2(screen.X / 2f, 32), ColorUtil.Lerp(Color.White, Palette.Gold, _scorePulse), scale, TextAlign.Centre);

        ScoreManager score = world.Score;
        if (score.ComboCount > 1)
        {
            float comboFraction = score.ComboTimeRemaining / ScoreManager.ComboWindow;
            string comboText = $"COMBO {score.ComboCount}  X{score.Multiplier:0.00}";
            font.DrawShadowed(comboText, new Vector2(screen.X / 2f, 76), Palette.Gold, 2f, TextAlign.Centre);
            shapes.FillRectangle(new Rectangle(screen.X / 2 - 80, 96, (int)(160 * comboFraction), 4), Palette.Gold);
        }
    }

    private static void DrawWave(PixelFont font, GameWorld world, Point screen)
    {
        DifficultyManager difficulty = world.Difficulty;
        font.DrawShadowed($"WAVE {difficulty.Wave}", new Vector2(screen.X - 24, 20), Palette.Accent, 3f, TextAlign.Right);
        font.DrawShadowed($"NEXT IN {MathF.Ceiling(difficulty.TimeUntilNextWave)}S", new Vector2(screen.X - 24, 50), Palette.TextDim, 2f, TextAlign.Right);

        int minutes = (int)(world.ElapsedTime / 60f);
        int seconds = (int)(world.ElapsedTime % 60f);
        font.DrawShadowed($"TIME {minutes}:{seconds:00}", new Vector2(screen.X - 24, 72), Palette.TextDim, 2f, TextAlign.Right);
        font.DrawShadowed($"KILLS {world.EnemiesDefeated}", new Vector2(screen.X - 24, 94), Palette.TextDim, 2f, TextAlign.Right);
    }

    private void DrawNova(ShapeRenderer shapes, PixelFont font, GameWorld world, Point screen)
    {
        bool ready = world.Player.NovaCooldown.IsReady;
        font.DrawShadowed(ready ? "NOVA READY  [RIGHT CLICK / E]" : "NOVA CHARGING", new Vector2(24, screen.Y - 50), ready ? Palette.Accent : Palette.TextDim, 2f);
        _novaBar.Draw(shapes, new Rectangle(24, screen.Y - 30, 220, 12), ready ? Palette.Accent : new Color(60, 110, 160));
    }

    private static void DrawPowerUps(ShapeRenderer shapes, PixelFont font, GameWorld world, Point screen)
    {
        var effects = world.Player.Effects.Snapshot();
        float y = screen.Y - 34;
        foreach ((PowerUpType type, float remaining) in effects.OrderBy(e => e.Type))
        {
            Color color = Palette.ForPowerUp(type);
            string name = type switch
            {
                PowerUpType.Speed => "SPEED",
                PowerUpType.Shield => "SHIELD",
                PowerUpType.RapidFire => "RAPID FIRE",
                _ => type.ToString().ToUpperInvariant(),
            };

            font.DrawShadowed($"{name} {remaining:0.0}S", new Vector2(screen.X - 24, y - 18), color, 2f, TextAlign.Right);
            float fraction = GameMath.Clamp01(remaining / Core.PowerUps.PowerUpCatalog.DefaultEffectDuration);
            shapes.FillRectangle(new Rectangle(screen.X - 24 - (int)(160 * fraction), (int)y, (int)(160 * fraction), 4), color);
            y -= 34;
        }
    }

    private void DrawBanner(PixelFont font, Point screen)
    {
        if (_bannerTime <= 0f)
        {
            return;
        }

        float elapsed = BannerDuration - _bannerTime;

        // Fade in over 0.3 s and out over the last 0.6 s (both are lerps of the opacity).
        float fadeIn = GameMath.Clamp01(elapsed / 0.3f);
        float fadeOut = GameMath.Clamp01(_bannerTime / 0.6f);
        float opacity = MathF.Min(fadeIn, fadeOut);
        float scale = GameMath.Lerp(9f, 7f, fadeIn);

        font.DrawShadowed(_banner, new Vector2(screen.X / 2f, screen.Y * 0.3f), ColorUtil.Fade(Palette.Gold, opacity), scale, TextAlign.Centre);
        if (!string.IsNullOrEmpty(_bannerSubtitle))
        {
            font.DrawShadowed(_bannerSubtitle, new Vector2(screen.X / 2f, screen.Y * 0.3f + 80), ColorUtil.Fade(Palette.Text, opacity), 2f, TextAlign.Centre);
        }
    }
}
