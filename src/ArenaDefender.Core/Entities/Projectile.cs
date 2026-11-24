using System.Numerics;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;

namespace ArenaDefender.Core.Entities;

/// <summary>
/// A bullet fired by the player. Projectiles are re-used through <see cref="ProjectilePool"/>
/// instead of being created and garbage collected every shot.
/// </summary>
public sealed class Projectile : Entity
{
    /// <summary>Collision radius.</summary>
    public const float DefaultRadius = 5f;

    /// <summary>Speed in units per second.</summary>
    public const float DefaultSpeed = 780f;

    /// <summary>Damage per hit.</summary>
    public const float DefaultDamage = 10f;

    /// <summary>Seconds before the projectile disappears on its own.</summary>
    public const float DefaultLifetime = 1.4f;

    /// <summary>Creates an inactive projectile, ready to be launched from the pool.</summary>
    public Projectile()
        : base(Vector2.Zero, DefaultRadius)
    {
        Deactivate();
    }

    /// <summary>Unit direction of travel.</summary>
    public Vector2 Direction { get; private set; }

    /// <summary>Velocity = direction × speed.</summary>
    public Vector2 Velocity { get; private set; }

    /// <summary>Damage dealt to an enemy on hit.</summary>
    public float Damage { get; private set; }

    /// <summary>Seconds of flight left.</summary>
    public float RemainingLifetime { get; private set; }

    /// <summary>Fires the projectile from a position in a direction.</summary>
    /// <exception cref="ArgumentException">Thrown when the direction is zero or not finite.</exception>
    public void Launch(Vector2 position, Vector2 direction, float speed = DefaultSpeed, float damage = DefaultDamage, float lifetime = DefaultLifetime)
    {
        Vector2 unit = GameMath.SafeNormalize(direction);
        if (unit == Vector2.Zero)
        {
            throw new ArgumentException("A projectile needs a non-zero direction.", nameof(direction));
        }

        Position = position;
        Direction = unit;
        Velocity = unit * speed;
        Damage = damage;
        RemainingLifetime = lifetime;
        Activate();
    }

    /// <summary>Moves the projectile in a straight line: position += velocity × dt.</summary>
    public void Update(float deltaTime, ArenaBounds bounds)
    {
        Position += Velocity * deltaTime;
        RemainingLifetime -= deltaTime;

        if (RemainingLifetime <= 0f || !bounds.Contains(Position, margin: 40f))
        {
            Deactivate();
        }
    }
}

/// <summary>
/// Object pool for projectiles, backed by a <see cref="Stack{T}"/>.
/// A stack is ideal here: we only ever need "give me any free projectile" (Pop) and
/// "this one is free again" (Push), both O(1), and the most recently used object is
/// re-used first, which is friendly to the CPU cache.
/// </summary>
public sealed class ProjectilePool
{
    private readonly Stack<Projectile> _free = new();

    /// <summary>How many projectiles are waiting to be re-used.</summary>
    public int AvailableCount => _free.Count;

    /// <summary>Total projectiles ever created by the pool.</summary>
    public int CreatedCount { get; private set; }

    /// <summary>Takes a projectile from the pool, creating one if the pool is empty.</summary>
    public Projectile Rent()
    {
        if (_free.Count > 0)
        {
            return _free.Pop();
        }

        CreatedCount++;
        return new Projectile();
    }

    /// <summary>Returns a projectile so it can be re-used.</summary>
    public void Return(Projectile projectile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        projectile.Deactivate();
        _free.Push(projectile);
    }
}
