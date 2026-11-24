using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Enemies;

/// <summary>
/// Standard enemy. Moves straight towards the player every frame using the normalised
/// direction vector (player − enemy) / |player − enemy|.
/// </summary>
public sealed class GruntEnemy : Enemy
{
    /// <summary>Base stats: medium health, medium speed.</summary>
    public static readonly EnemyStats BaseStats = new(
        MaxHealth: 30f, Speed: 115f, ContactDamage: 15f, ScoreValue: 100, Radius: 16f,
        KnockbackResistance: 0f, PowerUpDropChance: 0.08f);

    /// <summary>Creates a grunt.</summary>
    public GruntEnemy(Vector2 position, float healthMultiplier = 1f, float speedMultiplier = 1f)
        : base(EnemyType.Grunt, BaseStats, position, healthMultiplier, speedMultiplier)
    {
    }

    /// <inheritdoc />
    protected override void Move(float deltaTime, Vector2 playerPosition)
    {
        Vector2 direction = GameMath.DirectionTo(Position, playerPosition);
        if (direction != Vector2.Zero)
        {
            Facing = direction;
        }

        Position += direction * Speed * deltaTime;
    }
}

/// <summary>
/// Fast but weak enemy. It cannot turn instantly: each frame it rotates towards the player by at
/// most <see cref="TurnRate"/> × dt radians. The CROSS PRODUCT of its facing and the direction to the
/// player tells it whether to turn left or right, which makes it overshoot and swoop – so the player
/// can dodge it by side-stepping at the last moment.
/// </summary>
public sealed class RunnerEnemy : Enemy
{
    /// <summary>Base stats: low health, high speed.</summary>
    public static readonly EnemyStats BaseStats = new(
        MaxHealth: 12f, Speed: 235f, ContactDamage: 10f, ScoreValue: 150, Radius: 12f,
        KnockbackResistance: 0f, PowerUpDropChance: 0.06f);

    /// <summary>Maximum turning speed in radians per second.</summary>
    public const float TurnRate = 2.6f;

    /// <summary>Creates a runner.</summary>
    public RunnerEnemy(Vector2 position, float healthMultiplier = 1f, float speedMultiplier = 1f)
        : base(EnemyType.Runner, BaseStats, position, healthMultiplier, speedMultiplier)
    {
    }

    /// <summary>−1, 0 or +1: the direction it turned last frame (from the cross product). Used to tilt the sprite.</summary>
    public int LastTurnDirection { get; private set; }

    /// <summary>Initial facing so the runner leaves its spawn point heading at the player.</summary>
    public void FaceTowards(Vector2 target)
    {
        Vector2 direction = GameMath.DirectionTo(Position, target);
        if (direction != Vector2.Zero)
        {
            Facing = direction;
        }
    }

    /// <inheritdoc />
    protected override void Move(float deltaTime, Vector2 playerPosition)
    {
        Vector2 desired = GameMath.DirectionTo(Position, playerPosition);
        LastTurnDirection = GameMath.TurnDirection(Facing, desired);
        Facing = GameMath.SteerTowards(Facing, desired, TurnRate * deltaTime);
        Position += Facing * Speed * deltaTime;
    }
}

/// <summary>
/// Slow but strong enemy carrying a shield on its front. Attacks that hit the front arc deal only a
/// fraction of their damage. "Is this hit on the front?" is answered with the DOT PRODUCT of the
/// brute's facing and the direction back towards the attacker – no angles or trigonometry needed
/// per hit. Players are rewarded for flanking it.
/// </summary>
public sealed class BruteEnemy : Enemy
{
    /// <summary>Base stats: high health, low speed, hard to push.</summary>
    public static readonly EnemyStats BaseStats = new(
        MaxHealth: 120f, Speed: 62f, ContactDamage: 30f, ScoreValue: 400, Radius: 30f,
        KnockbackResistance: 0.75f, PowerUpDropChance: 0.35f);

    /// <summary>Total angle (degrees) covered by the frontal armour.</summary>
    public const float ArmourArcDegrees = 110f;

    /// <summary>Fraction of damage that gets through the armour.</summary>
    public const float ArmouredDamageFactor = 0.3f;

    /// <summary>Maximum turning speed in radians per second (slow, so it can be out-manoeuvred).</summary>
    public const float TurnRate = 1.1f;

    /// <summary>Creates a brute.</summary>
    public BruteEnemy(Vector2 position, float healthMultiplier = 1f, float speedMultiplier = 1f)
        : base(EnemyType.Brute, BaseStats, position, healthMultiplier, speedMultiplier)
    {
    }

    /// <summary>Sets the facing direction directly (used when spawning and in tests).</summary>
    public void FaceTowards(Vector2 target)
    {
        Vector2 direction = GameMath.DirectionTo(Position, target);
        if (direction != Vector2.Zero)
        {
            Facing = direction;
        }
    }

    /// <summary>
    /// True when an attack travelling in <paramref name="hitDirection"/> strikes the armoured front.
    /// The attack comes FROM −hitDirection, so we test whether −hitDirection is inside the armour cone.
    /// </summary>
    public bool IsArmouredHit(Vector2 hitDirection) =>
        GameMath.IsInFieldOfView(Facing, -hitDirection, ArmourArcDegrees);

    /// <inheritdoc />
    public override float ModifyIncomingDamage(float damage, Vector2 hitDirection) =>
        IsArmouredHit(hitDirection) ? damage * ArmouredDamageFactor : damage;

    /// <inheritdoc />
    protected override void Move(float deltaTime, Vector2 playerPosition)
    {
        Vector2 desired = GameMath.DirectionTo(Position, playerPosition);
        Facing = GameMath.SteerTowards(Facing, desired, TurnRate * deltaTime);
        Position += Facing * Speed * deltaTime;
    }
}
