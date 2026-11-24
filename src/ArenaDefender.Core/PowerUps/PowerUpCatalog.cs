using ArenaDefender.Core.Entities;

namespace ArenaDefender.Core.PowerUps;

/// <summary>
/// Strategy interface: describes what a power-up does when collected.
/// New power-ups can be added by writing a new class and registering it in
/// <see cref="PowerUpCatalog"/> – the player and the world do not need to change
/// (Open/Closed principle).
/// </summary>
public interface IPowerUpEffect
{
    /// <summary>The power-up this effect belongs to.</summary>
    PowerUpType Type { get; }

    /// <summary>Short name shown on screen when collected, e.g. "SHIELD".</summary>
    string DisplayName { get; }

    /// <summary>Applies the effect to the player.</summary>
    void Apply(Player player);
}

/// <summary>Instantly restores a fixed amount of health.</summary>
public sealed class HealthPackEffect : IPowerUpEffect
{
    /// <summary>Creates a health pack.</summary>
    /// <param name="amount">Health restored; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a non-positive amount.</exception>
    public HealthPackEffect(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Heal amount must be positive.");
        }

        Amount = amount;
    }

    /// <summary>Health restored when collected.</summary>
    public float Amount { get; }

    /// <inheritdoc />
    public PowerUpType Type => PowerUpType.Health;

    /// <inheritdoc />
    public string DisplayName => "+HEALTH";

    /// <inheritdoc />
    public void Apply(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Health.Heal(Amount);
    }
}

/// <summary>An effect that lasts for a number of seconds (speed, shield, rapid fire).</summary>
public sealed class TimedPowerUpEffect : IPowerUpEffect
{
    /// <summary>Creates a timed effect.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a non-positive duration.</exception>
    public TimedPowerUpEffect(PowerUpType type, float duration, string displayName)
    {
        if (!float.IsFinite(duration) || duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be positive.");
        }

        Type = type;
        Duration = duration;
        DisplayName = displayName;
    }

    /// <summary>How long the effect lasts in seconds.</summary>
    public float Duration { get; }

    /// <inheritdoc />
    public PowerUpType Type { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public void Apply(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Effects.Activate(Type, Duration);
    }
}

/// <summary>
/// Registry that maps each <see cref="PowerUpType"/> to the strategy that implements it.
/// </summary>
public sealed class PowerUpCatalog
{
    private readonly Dictionary<PowerUpType, IPowerUpEffect> _effects = new();

    /// <summary>Health restored by a health pack in the default catalog.</summary>
    public const float DefaultHealAmount = 35f;

    /// <summary>Duration of the timed power-ups in the default catalog.</summary>
    public const float DefaultEffectDuration = 7f;

    /// <summary>Creates the catalog with the game's standard power-ups.</summary>
    public static PowerUpCatalog CreateDefault()
    {
        var catalog = new PowerUpCatalog();
        catalog.Register(new HealthPackEffect(DefaultHealAmount));
        catalog.Register(new TimedPowerUpEffect(PowerUpType.Speed, DefaultEffectDuration, "SPEED UP"));
        catalog.Register(new TimedPowerUpEffect(PowerUpType.Shield, DefaultEffectDuration - 2f, "SHIELD"));
        catalog.Register(new TimedPowerUpEffect(PowerUpType.RapidFire, DefaultEffectDuration, "RAPID FIRE"));
        return catalog;
    }

    /// <summary>Adds or replaces the effect for its power-up type.</summary>
    public void Register(IPowerUpEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects[effect.Type] = effect;
    }

    /// <summary>Returns the effect for a type.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no effect has been registered for the type.</exception>
    public IPowerUpEffect Get(PowerUpType type)
    {
        if (!_effects.TryGetValue(type, out IPowerUpEffect? effect))
        {
            throw new InvalidOperationException($"No power-up effect is registered for '{type}'.");
        }

        return effect;
    }

    /// <summary>True when an effect is registered for the type.</summary>
    public bool Contains(PowerUpType type) => _effects.ContainsKey(type);
}
