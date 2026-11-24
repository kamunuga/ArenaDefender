using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Graphics;

/// <summary>
/// Draws the contents of a <see cref="GameWorld"/> using simple shapes. This is the "view" side of
/// the separation between logic and presentation: it reads entity state and never modifies it.
/// </summary>
public sealed class WorldRenderer
{
    private const int GridSpacing = 100;

    private readonly ShapeRenderer _shapes;
    private readonly PixelFont _font;

    /// <summary>Creates a renderer.</summary>
    public WorldRenderer(ShapeRenderer shapes, PixelFont font)
    {
        _shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
        _font = font ?? throw new ArgumentNullException(nameof(font));
    }

    /// <summary>Draws the floor, grid and border.</summary>
    public void DrawArena(ArenaBounds bounds, float time)
    {
        var size = new Vector2(bounds.Width, bounds.Height);
        _shapes.FillRectangle(Vector2.Zero, size, Palette.ArenaFloor);

        for (int x = GridSpacing; x < bounds.Width; x += GridSpacing)
        {
            _shapes.DrawLine(new Vector2(x, 0), new Vector2(x, bounds.Height), Palette.Grid, 1f);
        }

        for (int y = GridSpacing; y < bounds.Height; y += GridSpacing)
        {
            _shapes.DrawLine(new Vector2(0, y), new Vector2(bounds.Width, y), Palette.Grid, 1f);
        }

        // Border glow pulses gently between two colours.
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 2f);
        Color border = ColorUtil.Lerp(Palette.Border, new Color(160, 120, 255), pulse);
        _shapes.DrawLine(new Vector2(0, 0), new Vector2(bounds.Width, 0), border * 0.3f, 12f);
        _shapes.DrawLine(new Vector2(0, bounds.Height), new Vector2(bounds.Width, bounds.Height), border * 0.3f, 12f);
        _shapes.DrawLine(new Vector2(0, -6), new Vector2(0, bounds.Height + 6), border * 0.3f, 12f);
        _shapes.DrawLine(new Vector2(bounds.Width, -6), new Vector2(bounds.Width, bounds.Height + 6), border * 0.3f, 12f);
        _shapes.DrawRectangle(new Rectangle(-2, -2, (int)bounds.Width + 4, (int)bounds.Height + 4), border, 3);
    }

    /// <summary>Draws power-up pickups (the icon letters are drawn separately by <see cref="DrawPowerUpIcons"/>).</summary>
    public void DrawPowerUps(IEnumerable<PowerUp> powerUps, float time)
    {
        foreach (PowerUp powerUp in powerUps)
        {
            if (powerUp.IsExpiring && MathF.Sin(time * 18f) < 0f)
            {
                continue; // blink when about to disappear
            }

            Vector2 position = BobbedPosition(powerUp, time);
            Color color = Palette.ForPowerUp(powerUp.Type);
            float pulse = 1f + 0.12f * MathF.Sin(time * 5f + powerUp.Position.X);

            _shapes.FillCircle(position, powerUp.Radius * 1.9f * pulse, color * 0.18f);
            _shapes.FillCircle(position, powerUp.Radius, color);
            _shapes.FillCircle(position, powerUp.Radius - 3f, new Color(15, 20, 35));
            _shapes.DrawRing(position, powerUp.Radius * 1.35f * pulse, color * 0.8f);
        }
    }

    /// <summary>Draws the letter inside each power-up. Call in a point-sampled batch for crisp text.</summary>
    public void DrawPowerUpIcons(IEnumerable<PowerUp> powerUps, float time)
    {
        foreach (PowerUp powerUp in powerUps)
        {
            if (powerUp.IsExpiring && MathF.Sin(time * 18f) < 0f)
            {
                continue;
            }

            Vector2 position = BobbedPosition(powerUp, time);
            _font.Draw(Palette.IconForPowerUp(powerUp.Type), position - new Vector2(0, 7), Palette.ForPowerUp(powerUp.Type), 2f, TextAlign.Centre);
        }
    }

    /// <summary>Draws projectiles with a short motion trail.</summary>
    public void DrawProjectiles(IEnumerable<Projectile> projectiles)
    {
        foreach (Projectile projectile in projectiles)
        {
            if (!projectile.IsActive)
            {
                continue;
            }

            Vector2 position = projectile.Position.ToXna();
            Vector2 tail = position - projectile.Direction.ToXna() * 18f;
            _shapes.DrawLine(tail, position, Palette.Projectile * 0.5f, 4f);
            _shapes.FillCircle(position, projectile.Radius + 3f, Palette.Projectile * 0.3f);
            _shapes.FillCircle(position, projectile.Radius, Palette.Projectile);
        }
    }

    /// <summary>Draws every enemy according to its type.</summary>
    public void DrawEnemies(IEnumerable<Enemy> enemies, float time)
    {
        foreach (Enemy enemy in enemies)
        {
            // Hit flash: lerp the body colour towards white right after a hit.
            Color body = ColorUtil.Lerp(Palette.ForEnemy(enemy.Type), Color.White, enemy.HitFlash);
            Vector2 position = enemy.Position.ToXna();
            float angle = GameMath.ToAngle(enemy.Facing);

            // Soft shadow.
            _shapes.FillCircle(position + new Vector2(3, 5), enemy.Radius, Color.Black * 0.35f);

            switch (enemy)
            {
                case RunnerEnemy runner:
                    DrawRunner(runner, position, angle, body);
                    break;
                case BruteEnemy brute:
                    DrawBrute(brute, position, angle, body, time);
                    break;
                default:
                    DrawGrunt(enemy, position, body, time);
                    break;
            }

            if (enemy.Health.Fraction < 0.999f)
            {
                DrawEnemyHealthBar(enemy, position);
            }
        }
    }

    /// <summary>Draws the player, its gun barrel and any shield.</summary>
    public void DrawPlayer(Player player, float time)
    {
        // Blink while invulnerable.
        if (player.IsInvulnerable && MathF.Sin(time * 30f) < 0f)
        {
            return;
        }

        Vector2 position = player.Position.ToXna();
        Vector2 facing = player.Facing.ToXna();
        float angle = GameMath.ToAngle(player.Facing);

        _shapes.FillCircle(position + new Vector2(3, 5), player.Radius, Color.Black * 0.35f);
        _shapes.FillCircle(position, player.Radius + 8f, Palette.Player * 0.12f);

        // Gun barrel: a rotated rectangle sticking out in the aim direction.
        _shapes.FillRotatedRectangle(position + facing * (player.Radius + 2f), new Vector2(20f, 9f), angle, new Color(200, 245, 255));
        _shapes.FillCircle(position, player.Radius, Palette.Player);
        _shapes.FillCircle(position, player.Radius * 0.55f, Palette.PlayerCore);
        _shapes.FillCircle(position + facing * 4f, player.Radius * 0.25f, Color.White);

        if (player.IsShielded)
        {
            float pulse = 1f + 0.06f * MathF.Sin(time * 8f);
            Color shield = Palette.ForPowerUp(Core.PowerUpType.Shield);
            _shapes.FillCircle(position, (player.Radius + 14f) * pulse, shield * 0.15f);
            _shapes.DrawCircleOutline(position, (player.Radius + 14f) * pulse, shield, 3f, 36);
        }
    }

    /// <summary>Draws the expanding nova shockwave ring.</summary>
    public void DrawNovaWave(Vector2 centre, float radius, float opacity)
    {
        _shapes.FillCircle(centre, radius, Palette.Accent * (0.10f * opacity));
        _shapes.DrawRing(centre, radius, Palette.Accent * opacity);
    }

    private static Vector2 BobbedPosition(PowerUp powerUp, float time) =>
        powerUp.Position.ToXna() + new Vector2(0f, MathF.Sin(time * 4f + powerUp.Position.X * 0.05f) * 3f);

    private void DrawGrunt(Enemy enemy, Vector2 position, Color body, float time)
    {
        float wobble = 1f + 0.05f * MathF.Sin(time * 10f + position.X);
        _shapes.FillCircle(position, enemy.Radius * wobble, body);
        _shapes.FillCircle(position, enemy.Radius * 0.6f, ColorUtil.Lerp(body, Color.Black, 0.45f));

        // The eye looks in the direction the grunt is moving.
        Vector2 eye = position + enemy.Facing.ToXna() * (enemy.Radius * 0.45f);
        _shapes.FillCircle(eye, enemy.Radius * 0.28f, Color.White);
        _shapes.FillCircle(eye + enemy.Facing.ToXna() * 2f, enemy.Radius * 0.13f, Color.Black);
    }

    private void DrawRunner(RunnerEnemy runner, Vector2 position, float angle, Color body)
    {
        // Lean into turns: the sign comes from the cross product used for steering.
        float lean = runner.LastTurnDirection * 0.18f;
        _shapes.FillCircle(position, runner.Radius * 1.3f, body * 0.15f);
        _shapes.FillTriangle(position, runner.Radius * 2.6f, angle + lean, body);
        _shapes.FillCircle(position - runner.Facing.ToXna() * 2f, runner.Radius * 0.35f, ColorUtil.Lerp(body, Color.Black, 0.5f));
    }

    private void DrawBrute(BruteEnemy brute, Vector2 position, float angle, Color body, float time)
    {
        _shapes.FillCircle(position, brute.Radius, body);
        _shapes.FillCircle(position, brute.Radius * 0.7f, ColorUtil.Lerp(body, Color.Black, 0.35f));
        _shapes.FillCircle(position, brute.Radius * 0.3f, ColorUtil.Lerp(body, Color.White, 0.3f + 0.2f * MathF.Sin(time * 3f)));

        // Frontal armour: plates spread across the armour arc in front of the brute.
        const int plates = 7;
        float arc = MathHelper.ToRadians(BruteEnemy.ArmourArcDegrees);
        for (int i = 0; i < plates; i++)
        {
            float t = i / (float)(plates - 1);
            float plateAngle = angle + GameMath.Lerp(-arc / 2f, arc / 2f, t);
            Vector2 direction = new(MathF.Cos(plateAngle), MathF.Sin(plateAngle));
            Vector2 platePosition = position + direction * (brute.Radius + 4f);
            _shapes.FillRotatedRectangle(platePosition, new Vector2(7f, 14f), plateAngle, ColorUtil.Lerp(Palette.Armour, Color.White, brute.HitFlash));
        }
    }

    private void DrawEnemyHealthBar(Enemy enemy, Vector2 position)
    {
        float width = MathF.Max(26f, enemy.Radius * 2f);
        Vector2 topLeft = position + new Vector2(-width / 2f, -enemy.Radius - 14f);
        _shapes.FillRectangle(topLeft, new Vector2(width, 5f), Color.Black * 0.6f);
        _shapes.FillRectangle(topLeft, new Vector2(width * enemy.Health.Fraction, 5f), Palette.ForHealth(enemy.Health.Fraction));
    }
}
