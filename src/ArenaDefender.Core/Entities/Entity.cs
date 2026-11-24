using System.Numerics;

namespace ArenaDefender.Core.Entities;

/// <summary>
/// Anything that can take part in circle collision checks.
/// The collision system depends on this small interface rather than on concrete classes
/// (Interface Segregation and Dependency Inversion principles).
/// </summary>
public interface ICircleCollider
{
    /// <summary>Centre of the collision circle in world units.</summary>
    Vector2 Position { get; }

    /// <summary>Radius of the collision circle in world units.</summary>
    float Radius { get; }
}

/// <summary>
/// Abstract base class for every object that lives in the arena: the player, enemies,
/// projectiles and power-ups. It holds the shared state (position, radius, active flag)
/// so that sub-classes only describe what makes them different.
/// </summary>
public abstract class Entity : ICircleCollider
{
    /// <summary>Initialises the shared entity state.</summary>
    /// <param name="position">Starting position.</param>
    /// <param name="radius">Collision radius; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the radius is not positive.</exception>
    protected Entity(Vector2 position, float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be positive.");
        }

        Position = position;
        Radius = radius;
    }

    /// <inheritdoc />
    public Vector2 Position { get; set; }

    /// <inheritdoc />
    public float Radius { get; protected set; }

    /// <summary>
    /// False once the entity should be removed from the world (destroyed, collected, expired).
    /// The world removes inactive entities at the end of each update.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Marks the entity for removal.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Re-enables an entity (used by object pooling).</summary>
    protected void Activate() => IsActive = true;
}
