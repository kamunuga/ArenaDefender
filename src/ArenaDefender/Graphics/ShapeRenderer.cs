using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Graphics;

/// <summary>
/// Draws simple shapes (rectangles, lines, circles, rings, triangles) with a <see cref="SpriteBatch"/>.
/// The textures are generated in code when the game starts, so no image files are needed.
/// Several circle sizes are generated and the closest one is picked when drawing, so small and
/// large circles both keep smooth edges.
/// </summary>
public sealed class ShapeRenderer : IDisposable
{
    private static readonly int[] CircleSizes = { 16, 32, 64, 128, 256 };

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly Texture2D[] _circles;
    private readonly Texture2D _ring;
    private readonly Texture2D _triangle;

    /// <summary>Generates the shape textures.</summary>
    public ShapeRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _circles = new Texture2D[CircleSizes.Length];
        for (int i = 0; i < CircleSizes.Length; i++)
        {
            _circles[i] = CreateCircle(graphicsDevice, CircleSizes[i]);
        }

        _ring = CreateRing(graphicsDevice, 256, 0.06f);
        _triangle = CreateTriangle(graphicsDevice, 64);
    }

    /// <summary>A 1×1 white texture, useful for custom drawing.</summary>
    public Texture2D Pixel => _pixel;

    /// <summary>Fills a rectangle.</summary>
    public void FillRectangle(Rectangle rectangle, Color color) => _spriteBatch.Draw(_pixel, rectangle, color);

    /// <summary>Fills a rectangle given position and size as floats.</summary>
    public void FillRectangle(Vector2 position, Vector2 size, Color color) =>
        _spriteBatch.Draw(_pixel, position, null, color, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);

    /// <summary>Draws a rectangle outline.</summary>
    public void DrawRectangle(Rectangle rectangle, Color color, int thickness = 2)
    {
        FillRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        FillRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        FillRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        FillRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    /// <summary>Draws a straight line of the given thickness.</summary>
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 2f)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 0.001f)
        {
            return;
        }

        float angle = MathF.Atan2(delta.Y, delta.X);
        _spriteBatch.Draw(_pixel, start, null, color, angle, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    /// <summary>Fills a rectangle rotated around its centre.</summary>
    public void FillRotatedRectangle(Vector2 centre, Vector2 size, float rotation, Color color) =>
        _spriteBatch.Draw(_pixel, centre, null, color, rotation, new Vector2(0.5f, 0.5f), size, SpriteEffects.None, 0f);

    /// <summary>Fills a circle.</summary>
    public void FillCircle(Vector2 centre, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        Texture2D texture = PickCircle(radius * 2f);
        float scale = radius * 2f / texture.Width;
        Vector2 origin = new(texture.Width / 2f, texture.Height / 2f);
        _spriteBatch.Draw(texture, centre, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>Draws a ring (circle outline). The ring's thickness scales with its radius.</summary>
    public void DrawRing(Vector2 centre, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        float scale = radius * 2f / _ring.Width;
        Vector2 origin = new(_ring.Width / 2f, _ring.Height / 2f);
        _spriteBatch.Draw(_ring, centre, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>Draws a ring with a fixed thickness by drawing many short line segments.</summary>
    public void DrawCircleOutline(Vector2 centre, float radius, Color color, float thickness = 2f, int segments = 40)
    {
        Vector2 previous = centre + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            DrawLine(previous, next, color, thickness);
            previous = next;
        }
    }

    /// <summary>Fills a triangle that points along <paramref name="rotation"/> (0 = pointing right).</summary>
    public void FillTriangle(Vector2 centre, float size, float rotation, Color color)
    {
        float scale = size / _triangle.Width;
        Vector2 origin = new(_triangle.Width / 2f, _triangle.Height / 2f);
        _spriteBatch.Draw(_triangle, centre, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pixel.Dispose();
        foreach (Texture2D circle in _circles)
        {
            circle.Dispose();
        }

        _ring.Dispose();
        _triangle.Dispose();
    }

    private Texture2D PickCircle(float diameter)
    {
        for (int i = 0; i < _circles.Length; i++)
        {
            if (CircleSizes[i] >= diameter)
            {
                return _circles[i];
            }
        }

        return _circles[^1];
    }

    // ------------------------------------------------------------------
    // Texture generation. Each pixel's coverage gives an anti-aliased edge.
    // Colours are premultiplied (MonoGame's default blend mode expects this).
    // ------------------------------------------------------------------

    private static Texture2D CreateCircle(GraphicsDevice device, int size)
    {
        var data = new Color[size * size];
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                float alpha = Math.Clamp(radius - distance, 0f, 1f);
                data[y * size + x] = Color.White * alpha;
            }
        }

        var texture = new Texture2D(device, size, size);
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateRing(GraphicsDevice device, int size, float thicknessFraction)
    {
        var data = new Color[size * size];
        float outer = size / 2f;
        float inner = outer - size * thicknessFraction;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - outer;
                float dy = y + 0.5f - outer;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                float alpha = Math.Clamp(outer - distance, 0f, 1f) * Math.Clamp(distance - inner, 0f, 1f);
                data[y * size + x] = Color.White * alpha;
            }
        }

        var texture = new Texture2D(device, size, size);
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateTriangle(GraphicsDevice device, int size)
    {
        // Isosceles triangle pointing right. 4×4 super-sampling per pixel for smooth edges.
        Vector2 tip = new(size - 1f, size / 2f);
        Vector2 top = new(1f, 2f);
        Vector2 bottom = new(1f, size - 2f);
        var data = new Color[size * size];
        const int samples = 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Vector2 p = new(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                        if (PointInTriangle(p, tip, top, bottom))
                        {
                            inside++;
                        }
                    }
                }

                data[y * size + x] = Color.White * (inside / (float)(samples * samples));
            }
        }

        var texture = new Texture2D(device, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>Point-in-triangle test using the sign of 2D cross products for each edge.</summary>
    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        static float Cross(Vector2 o, Vector2 u, Vector2 v) => (u.X - o.X) * (v.Y - o.Y) - (u.Y - o.Y) * (v.X - o.X);

        float d1 = Cross(p, a, b);
        float d2 = Cross(p, b, c);
        float d3 = Cross(p, c, a);
        bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }
}
