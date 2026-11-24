using ArenaDefender.Core.Mathematics;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Effects;

/// <summary>
/// Short-lived text that floats up from a point in the world, e.g. "+150" when an enemy dies or
/// "SHIELD" when a power-up is collected. Drawn in screen space so the pixel font stays crisp.
/// </summary>
public sealed class FloatingTextSystem
{
    private const int MaxTexts = 40;
    private readonly List<FloatingText> _texts = new();

    /// <summary>Adds a floating text at a world position.</summary>
    public void Add(string text, Vector2 worldPosition, Color color, float scale = 2f, float life = 0.9f)
    {
        if (_texts.Count >= MaxTexts)
        {
            _texts.RemoveAt(0); // Oldest first: the list is in creation order.
        }

        _texts.Add(new FloatingText(text, worldPosition, color, scale, life));
    }

    /// <summary>Moves texts upward and removes expired ones.</summary>
    public void Update(float deltaTime)
    {
        for (int i = _texts.Count - 1; i >= 0; i--)
        {
            FloatingText t = _texts[i];
            t.Age += deltaTime;
            t.Position += new Vector2(0f, -45f * deltaTime);
            if (t.Age >= t.Life)
            {
                _texts.RemoveAt(i);
            }
        }
    }

    /// <summary>Draws the texts, converting world positions to screen positions with the camera.</summary>
    public void Draw(PixelFont font, Camera2D camera)
    {
        foreach (FloatingText t in _texts)
        {
            float progress = t.Age / t.Life;

            // Pop in quickly, then fade out during the second half of the life.
            float scale = t.Scale * GameMath.Lerp(1.4f, 1f, GameMath.Clamp01(progress * 5f));
            float opacity = 1f - GameMath.Clamp01((progress - 0.5f) * 2f);
            font.DrawShadowed(t.Text, camera.WorldToScreen(t.Position), ColorUtil.Fade(t.Color, opacity), scale, TextAlign.Centre);
        }
    }

    /// <summary>Removes all texts.</summary>
    public void Clear() => _texts.Clear();

    private sealed class FloatingText
    {
        public FloatingText(string text, Vector2 position, Color color, float scale, float life)
        {
            Text = text;
            Position = position;
            Color = color;
            Scale = scale;
            Life = life;
        }

        public string Text { get; }

        public Vector2 Position { get; set; }

        public Color Color { get; }

        public float Scale { get; }

        public float Life { get; }

        public float Age { get; set; }
    }
}
