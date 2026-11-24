namespace ArenaDefender.Core.Components;

/// <summary>
/// A simple countdown timer used for weapon fire rate, the nova blast and invulnerability.
/// </summary>
public sealed class Cooldown
{
    /// <summary>Creates a cooldown that is ready immediately.</summary>
    /// <param name="duration">Default length in seconds. Must be zero or positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative or non-finite duration.</exception>
    public Cooldown(float duration)
    {
        if (!float.IsFinite(duration) || duration < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be zero or positive.");
        }

        Duration = duration;
    }

    /// <summary>The default duration in seconds.</summary>
    public float Duration { get; }

    /// <summary>Seconds left until the cooldown is ready.</summary>
    public float Remaining { get; private set; }

    /// <summary>The length the cooldown was last started with.</summary>
    public float LastDuration { get; private set; }

    /// <summary>True when the cooldown has finished.</summary>
    public bool IsReady => Remaining <= 0f;

    /// <summary>0 when just triggered, 1 when ready. Used for UI cooldown bars.</summary>
    public float Progress => LastDuration <= 0f ? 1f : 1f - (Remaining / LastDuration);

    /// <summary>Counts the timer down.</summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    public void Update(float deltaTime)
    {
        if (Remaining > 0f)
        {
            Remaining = MathF.Max(0f, Remaining - deltaTime);
        }
    }

    /// <summary>Starts the cooldown with its default duration.</summary>
    public void Trigger() => Trigger(Duration);

    /// <summary>Starts the cooldown with a custom duration (e.g. a faster fire rate).</summary>
    /// <param name="duration">Length in seconds; negative values are treated as zero.</param>
    public void Trigger(float duration)
    {
        LastDuration = MathF.Max(0f, duration);
        Remaining = LastDuration;
    }

    /// <summary>Makes the cooldown ready immediately.</summary>
    public void Reset() => Remaining = 0f;
}
