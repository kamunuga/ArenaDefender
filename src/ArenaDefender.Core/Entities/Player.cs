using System.Numerics;
using ArenaDefender.Core.Components;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.PowerUps;
using ArenaDefender.Core.World;

namespace ArenaDefender.Core.Entities;

/// <summary>
/// The player-controlled defender. Owns its health, lives, weapon cooldowns and active power-ups.
/// </summary>
public sealed class Player : Entity
{
    /// <summary>Collision radius.</summary>
    public const float DefaultRadius = 18f;

    /// <summary>Maximum health.</summary>
    public const float MaxHealth = 100f;

    /// <summary>Lives at the start of a game.</summary>
    public const int StartingLives = 3;

    /// <summary>Top speed in units per second without power-ups.</summary>
    public const float BaseSpeed = 280f;

    /// <summary>How quickly velocity catches up with the input (used in a lerp).</summary>
    public const float MovementSharpness = 14f;

    /// <summary>Seconds between shots without power-ups.</summary>
    public const float BaseFireInterval = 0.17f;

    /// <summary>Seconds between nova blasts.</summary>
    public const float NovaInterval = 6f;

    /// <summary>Invulnerability after taking a hit, so one enemy cannot drain health in a single touch.</summary>
    public const float HitInvulnerability = 0.6f;

    /// <summary>Invulnerability after losing a life.</summary>
    public const float RespawnInvulnerability = 2.5f;

    /// <summary>Speed multiplier while the Speed power-up is active.</summary>
    public const float SpeedBoostMultiplier = 1.5f;

    /// <summary>Fire-rate multiplier while Rapid Fire is active (2.5 = 2.5× more shots).</summary>
    public const float RapidFireMultiplier = 2.5f;

    /// <summary>Creates a player at the given position.</summary>
    /// <param name="position">Spawn position.</param>
    /// <param name="lives">Number of lives; must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="lives"/> is less than 1.</exception>
    public Player(Vector2 position, int lives = StartingLives)
        : base(position, DefaultRadius)
    {
        if (lives < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lives), lives, "The player needs at least one life.");
        }

        Lives = lives;
        Health = new Health(MaxHealth);
    }

    /// <summary>The player's health component.</summary>
    public Health Health { get; }

    /// <summary>Lives remaining (including the current one).</summary>
    public int Lives { get; private set; }

    /// <summary>Current velocity in units per second.</summary>
    public Vector2 Velocity { get; private set; }

    /// <summary>Unit vector the player is aiming in.</summary>
    public Vector2 Facing { get; private set; } = Vector2.UnitX;

    /// <summary>Active timed power-ups.</summary>
    public PowerUpEffects Effects { get; } = new();

    /// <summary>Primary weapon cooldown.</summary>
    public Cooldown FireCooldown { get; } = new(BaseFireInterval);

    /// <summary>Nova blast cooldown.</summary>
    public Cooldown NovaCooldown { get; } = new(NovaInterval);

    /// <summary>Seconds of invulnerability left.</summary>
    public float InvulnerableTime { get; private set; }

    /// <summary>True while hits are ignored because the player was just hit or respawned.</summary>
    public bool IsInvulnerable => InvulnerableTime > 0f;

    /// <summary>True while the Shield power-up is active.</summary>
    public bool IsShielded => Effects.IsActive(PowerUpType.Shield);

    /// <summary>True when the player has no lives left.</summary>
    public bool IsOutOfLives => Lives <= 0;

    /// <summary>Movement speed after power-ups: BaseSpeed × multiplier.</summary>
    public float CurrentSpeed => BaseSpeed * (Effects.IsActive(PowerUpType.Speed) ? SpeedBoostMultiplier : 1f);

    /// <summary>Seconds between shots after power-ups: BaseFireInterval ÷ multiplier.</summary>
    public float FireInterval => BaseFireInterval / (Effects.IsActive(PowerUpType.RapidFire) ? RapidFireMultiplier : 1f);

    /// <summary>
    /// Moves the player. The velocity is linearly interpolated towards the target velocity
    /// so the player accelerates and decelerates smoothly instead of starting and stopping instantly.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    /// <param name="input">This frame's input.</param>
    /// <param name="bounds">The arena, used to keep the player inside.</param>
    public void Update(float deltaTime, PlayerInput input, ArenaBounds bounds)
    {
        FireCooldown.Update(deltaTime);
        NovaCooldown.Update(deltaTime);
        Effects.Update(deltaTime);
        InvulnerableTime = MathF.Max(0f, InvulnerableTime - deltaTime);

        Vector2 moveDirection = GameMath.SafeNormalize(input.Move);
        Vector2 targetVelocity = moveDirection * CurrentSpeed;
        Velocity = GameMath.Lerp(Velocity, targetVelocity, GameMath.SmoothingFactor(MovementSharpness, deltaTime));

        Position = bounds.Clamp(Position + Velocity * deltaTime, Radius);

        Vector2 aim = GameMath.SafeNormalize(input.Aim);
        if (aim != Vector2.Zero)
        {
            Facing = aim;
        }
    }

    /// <summary>
    /// Applies damage from an enemy. Handles shields, invulnerability, losing a life and game over.
    /// </summary>
    /// <param name="damage">Damage to apply; must be non-negative.</param>
    /// <returns>What happened, so the caller can trigger the right effects.</returns>
    public HitResult TakeHit(float damage)
    {
        if (IsOutOfLives || IsInvulnerable || IsShielded)
        {
            return HitResult.Blocked;
        }

        Health.TakeDamage(damage);

        if (!Health.IsDead)
        {
            InvulnerableTime = HitInvulnerability;
            return HitResult.Damaged;
        }

        Lives--;
        if (Lives <= 0)
        {
            return HitResult.GameOver;
        }

        Health.Reset();
        Effects.Clear();
        InvulnerableTime = RespawnInvulnerability;
        return HitResult.LifeLost;
    }

    /// <summary>Returns true and restarts the fire cooldown if the weapon is ready.</summary>
    public bool TryFire()
    {
        if (!FireCooldown.IsReady)
        {
            return false;
        }

        FireCooldown.Trigger(FireInterval);
        return true;
    }

    /// <summary>Returns true and restarts the nova cooldown if the nova is ready.</summary>
    public bool TryNova()
    {
        if (!NovaCooldown.IsReady)
        {
            return false;
        }

        NovaCooldown.Trigger();
        return true;
    }
}
