using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.World;

/// <summary>
/// The rectangular play area, from (0, 0) to (<see cref="Width"/>, <see cref="Height"/>).
/// An immutable value type: bounds never change during a game.
/// </summary>
public readonly record struct ArenaBounds
{
    /// <summary>Creates arena bounds.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is not positive.</exception>
    public ArenaBounds(float width, float height)
    {
        if (!float.IsFinite(width) || width <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Arena width must be positive.");
        }

        if (!float.IsFinite(height) || height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Arena height must be positive.");
        }

        Width = width;
        Height = height;
    }

    /// <summary>Arena width in world units.</summary>
    public float Width { get; }

    /// <summary>Arena height in world units.</summary>
    public float Height { get; }

    /// <summary>The centre point of the arena.</summary>
    public Vector2 Centre => new(Width / 2f, Height / 2f);

    /// <summary>Keeps a circle of the given radius fully inside the arena.</summary>
    public Vector2 Clamp(Vector2 position, float radius) => new(
        GameMath.Clamp(position.X, radius, Width - radius),
        GameMath.Clamp(position.Y, radius, Height - radius));

    /// <summary>True when a point is inside the arena, optionally with a margin outside the edge.</summary>
    public bool Contains(Vector2 position, float margin = 0f) =>
        position.X >= -margin && position.X <= Width + margin &&
        position.Y >= -margin && position.Y <= Height + margin;
}
