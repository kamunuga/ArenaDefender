using ArenaDefender.Core;
using ArenaDefender.Core.Mathematics;
using Microsoft.Xna.Framework;
using NumVector2 = System.Numerics.Vector2;

namespace ArenaDefender.Graphics;

/// <summary>All colours used by the game in one place, so the look can be changed easily.</summary>
public static class Palette
{
    /// <summary>Clear colour behind everything.</summary>
    public static readonly Color Background = new(10, 12, 24);

    /// <summary>Arena floor.</summary>
    public static readonly Color ArenaFloor = new(18, 22, 40);

    /// <summary>Grid lines on the floor.</summary>
    public static readonly Color Grid = new(32, 40, 70);

    /// <summary>Glowing arena border.</summary>
    public static readonly Color Border = new(80, 200, 255);

    /// <summary>Player body.</summary>
    public static readonly Color Player = new(70, 220, 255);

    /// <summary>Player core.</summary>
    public static readonly Color PlayerCore = new(20, 70, 110);

    /// <summary>Player bullets.</summary>
    public static readonly Color Projectile = new(255, 245, 160);

    /// <summary>Grunt enemy.</summary>
    public static readonly Color Grunt = new(255, 80, 90);

    /// <summary>Runner enemy.</summary>
    public static readonly Color Runner = new(255, 200, 60);

    /// <summary>Brute enemy.</summary>
    public static readonly Color Brute = new(170, 90, 255);

    /// <summary>Brute armour plate.</summary>
    public static readonly Color Armour = new(220, 220, 240);

    /// <summary>Main UI text.</summary>
    public static readonly Color Text = new(235, 240, 255);

    /// <summary>Secondary UI text.</summary>
    public static readonly Color TextDim = new(130, 145, 180);

    /// <summary>Highlight / accent.</summary>
    public static readonly Color Accent = new(80, 200, 255);

    /// <summary>Warning / score highlight.</summary>
    public static readonly Color Gold = new(255, 210, 80);

    /// <summary>Panel backgrounds.</summary>
    public static readonly Color Panel = new(20, 26, 48);

    /// <summary>Health bar when full.</summary>
    public static readonly Color HealthHigh = new(90, 230, 120);

    /// <summary>Health bar at half.</summary>
    public static readonly Color HealthMid = new(255, 210, 70);

    /// <summary>Health bar when nearly empty.</summary>
    public static readonly Color HealthLow = new(255, 70, 70);

    /// <summary>Colour for an enemy type.</summary>
    public static Color ForEnemy(EnemyType type) => type switch
    {
        EnemyType.Grunt => Grunt,
        EnemyType.Runner => Runner,
        EnemyType.Brute => Brute,
        _ => Color.White,
    };

    /// <summary>Colour for a power-up type.</summary>
    public static Color ForPowerUp(PowerUpType type) => type switch
    {
        PowerUpType.Health => new Color(90, 230, 120),
        PowerUpType.Speed => new Color(80, 170, 255),
        PowerUpType.Shield => new Color(255, 210, 80),
        PowerUpType.RapidFire => new Color(255, 110, 200),
        _ => Color.White,
    };

    /// <summary>Short icon text drawn inside a power-up.</summary>
    public static string IconForPowerUp(PowerUpType type) => type switch
    {
        PowerUpType.Health => "+",
        PowerUpType.Speed => ">",
        PowerUpType.Shield => "S",
        PowerUpType.RapidFire => "R",
        _ => "?",
    };

    /// <summary>
    /// Health bar colour: lerps red → yellow for the bottom half and yellow → green for the top half.
    /// </summary>
    public static Color ForHealth(float fraction)
    {
        fraction = GameMath.Clamp01(fraction);
        return fraction < 0.5f
            ? ColorUtil.Lerp(HealthLow, HealthMid, fraction / 0.5f)
            : ColorUtil.Lerp(HealthMid, HealthHigh, (fraction - 0.5f) / 0.5f);
    }
}

/// <summary>Colour helpers.</summary>
public static class ColorUtil
{
    /// <summary>
    /// Linear interpolation between two colours, channel by channel, using <see cref="GameMath.Lerp(float, float, float)"/>.
    /// Used for health-bar colours, enemy hit flashes, button hovers and particle fades.
    /// </summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = GameMath.Clamp01(t);
        return new Color(
            (int)MathF.Round(GameMath.Lerp(a.R, b.R, t)),
            (int)MathF.Round(GameMath.Lerp(a.G, b.G, t)),
            (int)MathF.Round(GameMath.Lerp(a.B, b.B, t)),
            (int)MathF.Round(GameMath.Lerp(a.A, b.A, t)));
    }

    /// <summary>Returns the colour with its opacity scaled (MonoGame uses premultiplied alpha).</summary>
    public static Color Fade(Color color, float opacity) => color * GameMath.Clamp01(opacity);
}

/// <summary>Converts between the Core's System.Numerics vectors and MonoGame's vectors.</summary>
public static class VectorExtensions
{
    /// <summary>System.Numerics → MonoGame.</summary>
    public static Vector2 ToXna(this NumVector2 v) => new(v.X, v.Y);

    /// <summary>MonoGame → System.Numerics.</summary>
    public static NumVector2 ToCore(this Vector2 v) => new(v.X, v.Y);
}
