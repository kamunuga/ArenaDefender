using System.Numerics;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Systems;

/// <summary>
/// Gentle aim assistance. If an enemy lies inside a narrow cone in front of the player's aim
/// (tested with the DOT PRODUCT) and within range (DISTANCE), the shot direction is blended
/// (LERP) part of the way towards that enemy.
/// </summary>
public static class AimAssist
{
    /// <summary>Total width of the assist cone in degrees.</summary>
    public const float ConeDegrees = 14f;

    /// <summary>Maximum distance at which assistance applies.</summary>
    public const float Range = 650f;

    /// <summary>How far to bend the shot towards the target (0 = none, 1 = perfect aim).</summary>
    public const float Strength = 0.6f;

    /// <summary>
    /// Returns the (unit) direction to fire in after aim assistance.
    /// </summary>
    /// <param name="origin">Where the shot starts.</param>
    /// <param name="aim">The player's raw aim direction.</param>
    /// <param name="enemies">Candidate targets.</param>
    public static Vector2 Adjust(Vector2 origin, Vector2 aim, IEnumerable<Enemy> enemies)
    {
        Vector2 rawAim = GameMath.SafeNormalize(aim);
        if (rawAim == Vector2.Zero)
        {
            return rawAim;
        }

        float cosLimit = MathF.Cos(GameMath.DegreesToRadians(ConeDegrees * 0.5f));
        Enemy? best = null;
        float bestDot = cosLimit;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsActive || GameMath.Distance(origin, enemy.Position) > Range)
            {
                continue;
            }

            float dot = GameMath.Dot(rawAim, GameMath.DirectionTo(origin, enemy.Position));
            if (dot >= bestDot)
            {
                bestDot = dot;
                best = enemy;
            }
        }

        if (best is null)
        {
            return rawAim;
        }

        Vector2 toTarget = GameMath.DirectionTo(origin, best.Position);
        return GameMath.SafeNormalize(GameMath.Lerp(rawAim, toTarget, Strength));
    }
}

/// <summary>
/// The secondary attack: a shockwave around the player that damages and pushes back every enemy
/// within <see cref="Radius"/>. Damage falls off linearly with distance.
/// </summary>
public static class NovaBlast
{
    /// <summary>Blast radius in world units.</summary>
    public const float Radius = 210f;

    /// <summary>Damage at the centre of the blast.</summary>
    public const float MaxDamage = 60f;

    /// <summary>Fraction of <see cref="MaxDamage"/> still dealt at the very edge.</summary>
    public const float MinDamageFraction = 0.35f;

    /// <summary>Knockback speed applied at the centre.</summary>
    public const float Knockback = 650f;

    /// <summary>
    /// Damage for an enemy at <paramref name="distance"/>:
    /// damage = maxDamage × lerp(1, minFraction, distance ÷ radius). Outside the radius it is 0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the radius is not positive.</exception>
    public static float CalculateDamage(float distance, float radius = Radius, float maxDamage = MaxDamage, float minFraction = MinDamageFraction)
    {
        if (radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be positive.");
        }

        if (distance < 0f || distance > radius)
        {
            return distance < 0f ? maxDamage : 0f;
        }

        float t = distance / radius;
        return maxDamage * GameMath.Lerp(1f, minFraction, t);
    }

    /// <summary>
    /// Applies the blast to all enemies in range.
    /// </summary>
    /// <param name="centre">Centre of the blast.</param>
    /// <param name="enemies">Enemies to test.</param>
    /// <param name="onKilled">Called for each enemy the blast kills.</param>
    /// <returns>Number of enemies hit.</returns>
    public static int Apply(Vector2 centre, IEnumerable<Enemy> enemies, Action<Enemy>? onKilled = null)
    {
        int hits = 0;
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsActive)
            {
                continue;
            }

            float distance = GameMath.Distance(centre, enemy.Position);
            if (distance > Radius + enemy.Radius)
            {
                continue;
            }

            Vector2 push = GameMath.DirectionTo(centre, enemy.Position);
            float damage = CalculateDamage(MathF.Min(distance, Radius));

            // The blast hits each enemy from the centre outwards; armour still applies to brutes facing the player.
            enemy.ApplyHit(damage, push);
            enemy.ApplyKnockback(push * Knockback * (1f - distance / (Radius + enemy.Radius) * 0.5f));
            hits++;

            if (enemy.IsDead)
            {
                onKilled?.Invoke(enemy);
            }
        }

        return hits;
    }
}

/// <summary>
/// Collision helpers. Every object in the game is approximated by a circle, which keeps the
/// test cheap (one squared distance) and rotation-independent.
/// </summary>
public static class CollisionSystem
{
    /// <summary>True when two circle colliders overlap.</summary>
    public static bool Intersects(ICircleCollider a, ICircleCollider b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return GameMath.CirclesOverlap(a.Position, a.Radius, b.Position, b.Radius);
    }

    /// <summary>
    /// How far two circles overlap (positive) or how far apart their edges are (negative).
    /// </summary>
    public static float Penetration(ICircleCollider a, ICircleCollider b) =>
        a.Radius + b.Radius - GameMath.Distance(a.Position, b.Position);

    /// <summary>
    /// Pushes overlapping enemies apart so they form a crowd instead of stacking on one point.
    /// Each enemy moves half the overlap along the line between their centres.
    /// </summary>
    public static void SeparateEnemies(IReadOnlyList<Enemy> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy a = enemies[i];
            if (!a.IsActive)
            {
                continue;
            }

            for (int j = i + 1; j < enemies.Count; j++)
            {
                Enemy b = enemies[j];
                if (!b.IsActive || !Intersects(a, b))
                {
                    continue;
                }

                float overlap = Penetration(a, b);
                Vector2 normal = GameMath.DirectionTo(a.Position, b.Position);
                if (normal == Vector2.Zero)
                {
                    normal = Vector2.UnitX; // exactly on top of each other: pick any direction
                }

                Vector2 correction = normal * (overlap * 0.5f);
                a.Position -= correction;
                b.Position += correction;
            }
        }
    }
}
