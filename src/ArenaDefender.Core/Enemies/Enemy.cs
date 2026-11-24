using System.Numerics;
using ArenaDefender.Core.Components;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;

namespace ArenaDefender.Core.Enemies;

/// <summary>
/// The tuning numbers that make one enemy type different from another.
/// Kept as data so balancing the game does not require changing behaviour code.
/// </summary>
/// <param name="MaxHealth">Health before wave scaling.</param>
/// <param name="Speed">Movement speed in units per second before wave scaling.</param>
/// <param name="ContactDamage">Damage dealt to the player on touch.</param>
/// <param name="ScoreValue">Base points for defeating it.</param>
/// <param name="Radius">Collision radius.</param>
/// <param name="KnockbackResistance">0 = full knockback, 1 = immovable.</param>
/// <param name="PowerUpDropChance">0..1 chance of dropping a power-up when killed.</param>
public readonly record struct EnemyStats(
    float MaxHealth,
    float Speed,
    float ContactDamage,
    int ScoreValue,
    float Radius,
    float KnockbackResistance,
    float PowerUpDropChance);

/// <summary>
/// Abstract base for all enemies (Template Method pattern): <see cref="Update"/> does the shared
/// work – hit-flash fading, knockback, staying in the arena – and calls the abstract
/// <see cref="Move"/> that each enemy type implements in its own way.
/// </summary>
public abstract class Enemy : Entity
{
    /// <summary>How fast knockback velocity fades (used in a lerp).</summary>
    protected const float KnockbackDamping = 7f;

    /// <summary>Initialises shared enemy state with wave scaling applied.</summary>
    /// <param name="type">The enemy type.</param>
    /// <param name="stats">Base stats for the type.</param>
    /// <param name="position">Spawn position.</param>
    /// <param name="healthMultiplier">Difficulty multiplier for health (≥ 1 in normal play).</param>
    /// <param name="speedMultiplier">Difficulty multiplier for speed.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a multiplier is not positive.</exception>
    protected Enemy(EnemyType type, EnemyStats stats, Vector2 position, float healthMultiplier, float speedMultiplier)
        : base(position, stats.Radius)
    {
        if (!float.IsFinite(healthMultiplier) || healthMultiplier <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(healthMultiplier), healthMultiplier, "Multiplier must be positive.");
        }

        if (!float.IsFinite(speedMultiplier) || speedMultiplier <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(speedMultiplier), speedMultiplier, "Multiplier must be positive.");
        }

        Type = type;
        Stats = stats;
        Health = new Health(stats.MaxHealth * healthMultiplier);
        Speed = stats.Speed * speedMultiplier;
    }

    /// <summary>Which kind of enemy this is.</summary>
    public EnemyType Type { get; }

    /// <summary>The base stats this enemy was created from.</summary>
    public EnemyStats Stats { get; }

    /// <summary>Health component (already scaled for the wave).</summary>
    public Health Health { get; }

    /// <summary>Movement speed after wave scaling.</summary>
    public float Speed { get; }

    /// <summary>Damage dealt to the player on contact.</summary>
    public float ContactDamage => Stats.ContactDamage;

    /// <summary>Base points awarded for a kill.</summary>
    public int ScoreValue => Stats.ScoreValue;

    /// <summary>Unit vector the enemy is facing / moving in.</summary>
    public Vector2 Facing { get; protected set; } = Vector2.UnitX;

    /// <summary>1 right after being hit, fading to 0. The renderer lerps the colour towards white with it.</summary>
    public float HitFlash { get; private set; }

    /// <summary>Extra velocity from being pushed (by the nova or by touching the player).</summary>
    public Vector2 KnockbackVelocity { get; private set; }

    /// <summary>True once health has reached zero.</summary>
    public bool IsDead => Health.IsDead;

    /// <summary>Runs one frame of enemy behaviour.</summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    /// <param name="playerPosition">Where the player is.</param>
    /// <param name="bounds">The arena.</param>
    public void Update(float deltaTime, Vector2 playerPosition, ArenaBounds bounds)
    {
        HitFlash = MathF.Max(0f, HitFlash - deltaTime * 5f);

        Move(deltaTime, playerPosition);

        Position += KnockbackVelocity * deltaTime;
        KnockbackVelocity = GameMath.Lerp(KnockbackVelocity, Vector2.Zero, GameMath.SmoothingFactor(KnockbackDamping, deltaTime));

        Position = bounds.Clamp(Position, Radius);
    }

    /// <summary>Type-specific movement. Called once per frame by <see cref="Update"/>.</summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    /// <param name="playerPosition">Where the player is.</param>
    protected abstract void Move(float deltaTime, Vector2 playerPosition);

    /// <summary>
    /// Lets an enemy type change incoming damage (e.g. armour). The default takes full damage.
    /// </summary>
    /// <param name="damage">Raw damage.</param>
    /// <param name="hitDirection">Direction the attack is travelling in.</param>
    /// <returns>The damage to actually apply.</returns>
    public virtual float ModifyIncomingDamage(float damage, Vector2 hitDirection) => damage;

    /// <summary>Damages the enemy, triggers the hit flash and removes it if it dies.</summary>
    /// <param name="damage">Raw damage.</param>
    /// <param name="hitDirection">Direction the attack is travelling in.</param>
    /// <returns>The damage actually applied after armour.</returns>
    public float ApplyHit(float damage, Vector2 hitDirection)
    {
        float finalDamage = ModifyIncomingDamage(damage, hitDirection);
        float applied = Health.TakeDamage(finalDamage);
        HitFlash = 1f;

        if (Health.IsDead)
        {
            Deactivate();
        }

        return applied;
    }

    /// <summary>Pushes the enemy. Heavier enemies (higher resistance) move less.</summary>
    /// <param name="impulse">Velocity to add, in units per second.</param>
    public void ApplyKnockback(Vector2 impulse)
    {
        if (!GameMath.IsFinite(impulse))
        {
            return;
        }

        KnockbackVelocity += impulse * (1f - GameMath.Clamp01(Stats.KnockbackResistance));
    }
}
