using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Entities;

/// <summary>
/// A collectible lying in the arena. It disappears after <see cref="Lifetime"/> seconds and is
/// pulled towards the player when they come within <see cref="MagnetRadius"/>.
/// </summary>
public sealed class PowerUp : Entity
{
    /// <summary>Collision radius.</summary>
    public const float DefaultRadius = 13f;

    /// <summary>Distance at which the pickup starts drifting towards the player.</summary>
    public const float MagnetRadius = 120f;

    /// <summary>How strongly the magnet pulls (used in a lerp).</summary>
    public const float MagnetSharpness = 5f;

    /// <summary>Seconds the pickup stays on the ground.</summary>
    public const float Lifetime = 12f;

    /// <summary>Seconds before disappearing when the pickup starts blinking.</summary>
    public const float WarningTime = 3f;

    /// <summary>Creates a power-up pickup.</summary>
    public PowerUp(PowerUpType type, Vector2 position)
        : base(position, DefaultRadius)
    {
        Type = type;
    }

    /// <summary>What this pickup gives the player.</summary>
    public PowerUpType Type { get; }

    /// <summary>Seconds since it appeared.</summary>
    public float Age { get; private set; }

    /// <summary>True during the last few seconds, so the renderer can make it blink.</summary>
    public bool IsExpiring => Lifetime - Age <= WarningTime;

    /// <summary>
    /// Ages the pickup and, if the player is within <see cref="MagnetRadius"/> (a distance check),
    /// lerps its position towards the player.
    /// </summary>
    public void Update(float deltaTime, Vector2 playerPosition)
    {
        Age += deltaTime;
        if (Age >= Lifetime)
        {
            Deactivate();
            return;
        }

        if (GameMath.Distance(Position, playerPosition) <= MagnetRadius)
        {
            Position = GameMath.Lerp(Position, playerPosition, GameMath.SmoothingFactor(MagnetSharpness, deltaTime));
        }
    }
}
