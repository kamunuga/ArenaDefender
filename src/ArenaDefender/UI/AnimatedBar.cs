using ArenaDefender.Core.Mathematics;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.UI;

/// <summary>
/// A health-style bar that animates towards its target value using LERP.
/// The coloured fill moves quickly; a pale "damage trail" behind it catches up more slowly,
/// so the player can see how much they just lost.
/// </summary>
public sealed class AnimatedBar
{
    private const float FillSharpness = 12f;
    private const float TrailSharpness = 3f;

    /// <summary>Creates a bar that starts at the given fraction.</summary>
    public AnimatedBar(float initial = 1f)
    {
        DisplayedValue = initial;
        TrailValue = initial;
    }

    /// <summary>The value currently drawn (0..1).</summary>
    public float DisplayedValue { get; private set; }

    /// <summary>The slower damage-trail value (0..1).</summary>
    public float TrailValue { get; private set; }

    /// <summary>Moves the displayed values towards the target.</summary>
    public void Update(float deltaTime, float target)
    {
        target = GameMath.Clamp01(target);
        DisplayedValue = GameMath.Lerp(DisplayedValue, target, GameMath.SmoothingFactor(FillSharpness, deltaTime));

        // The trail only lags behind when the value goes DOWN; when healing it snaps up.
        TrailValue = TrailValue > DisplayedValue
            ? GameMath.Lerp(TrailValue, DisplayedValue, GameMath.SmoothingFactor(TrailSharpness, deltaTime))
            : DisplayedValue;
    }

    /// <summary>Draws the bar. If no colour is given, it changes from green to red with the value.</summary>
    public void Draw(ShapeRenderer shapes, Rectangle bounds, Color? fillColor = null)
    {
        shapes.FillRectangle(bounds, new Color(8, 10, 20) * 0.85f);

        Rectangle inner = new(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
        int trailWidth = (int)(inner.Width * GameMath.Clamp01(TrailValue));
        int fillWidth = (int)(inner.Width * GameMath.Clamp01(DisplayedValue));

        shapes.FillRectangle(new Rectangle(inner.X, inner.Y, trailWidth, inner.Height), new Color(255, 240, 220) * 0.8f);
        shapes.FillRectangle(new Rectangle(inner.X, inner.Y, fillWidth, inner.Height), fillColor ?? Palette.ForHealth(DisplayedValue));

        // A lighter strip on top gives the bar a little depth.
        shapes.FillRectangle(new Rectangle(inner.X, inner.Y, fillWidth, Math.Max(1, inner.Height / 3)), Color.White * 0.18f);
        shapes.DrawRectangle(bounds, new Color(90, 110, 160), 1);
    }
}
