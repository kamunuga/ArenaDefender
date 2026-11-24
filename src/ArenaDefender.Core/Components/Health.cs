namespace ArenaDefender.Core.Components;

/// <summary>
/// Reusable health component shared by the player and every enemy (composition over
/// inheritance). The current value is encapsulated: it can only change through
/// <see cref="TakeDamage"/>, <see cref="Heal"/> and <see cref="Reset"/>, which guarantee it
/// always stays between 0 and <see cref="Max"/>.
/// </summary>
public sealed class Health
{
    /// <summary>Creates a health component that starts full.</summary>
    /// <param name="max">Maximum health. Must be a positive, finite number.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is not positive or finite.</exception>
    public Health(float max)
    {
        if (!float.IsFinite(max) || max <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "Maximum health must be a positive number.");
        }

        Max = max;
        Current = max;
    }

    /// <summary>Raised once when health first reaches zero.</summary>
    public event Action? Died;

    /// <summary>The maximum health.</summary>
    public float Max { get; }

    /// <summary>The current health, always in the range [0, Max].</summary>
    public float Current { get; private set; }

    /// <summary>True when health is zero.</summary>
    public bool IsDead => Current <= 0f;

    /// <summary>Current / Max as a 0..1 value, used by health bars.</summary>
    public float Fraction => Current / Max;

    /// <summary>
    /// Reduces health. Damage never takes health below zero.
    /// </summary>
    /// <param name="amount">Damage to apply. Must be zero or positive.</param>
    /// <returns>The damage that was actually applied (may be less than requested).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for negative or non-finite damage.</exception>
    public float TakeDamage(float amount)
    {
        ValidateAmount(amount, nameof(amount));
        if (IsDead || amount == 0f)
        {
            return 0f;
        }

        float applied = MathF.Min(amount, Current);
        Current -= applied;

        if (IsDead)
        {
            Current = 0f;
            Died?.Invoke();
        }

        return applied;
    }

    /// <summary>
    /// Restores health, capped at <see cref="Max"/>. Dead entities cannot be healed.
    /// </summary>
    /// <param name="amount">Health to restore. Must be zero or positive.</param>
    /// <returns>The amount actually restored.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for negative or non-finite amounts.</exception>
    public float Heal(float amount)
    {
        ValidateAmount(amount, nameof(amount));
        if (IsDead)
        {
            return 0f;
        }

        float restored = MathF.Min(amount, Max - Current);
        Current += restored;
        return restored;
    }

    /// <summary>Refills health to maximum (used when the player respawns).</summary>
    public void Reset() => Current = Max;

    private static void ValidateAmount(float amount, string paramName)
    {
        if (!float.IsFinite(amount) || amount < 0f)
        {
            throw new ArgumentOutOfRangeException(paramName, amount, "Amount must be a finite, non-negative number.");
        }
    }
}
