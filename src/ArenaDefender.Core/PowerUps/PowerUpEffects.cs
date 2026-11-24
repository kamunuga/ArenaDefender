namespace ArenaDefender.Core.PowerUps;

/// <summary>
/// Tracks the timed power-ups currently affecting the player and how long each has left.
/// </summary>
/// <remarks>
/// A <see cref="Dictionary{TKey,TValue}"/> keyed by <see cref="PowerUpType"/> is used because the
/// most common operation is "is effect X active?", which a dictionary answers in O(1), and because
/// each type can only be active once (collecting it again refreshes the timer instead of stacking).
/// </remarks>
public sealed class PowerUpEffects
{
    private readonly Dictionary<PowerUpType, float> _remaining = new();
    private readonly List<PowerUpType> _keyBuffer = new();

    /// <summary>Raised when a timed effect runs out.</summary>
    public event Action<PowerUpType>? Expired;

    /// <summary>The number of effects currently active.</summary>
    public int ActiveCount => _remaining.Count;

    /// <summary>
    /// Starts an effect, or refreshes it if it is already active. The timer is set to whichever is
    /// longer: the time left or the new duration, so picking up a second one never shortens it.
    /// </summary>
    /// <param name="type">Effect to activate.</param>
    /// <param name="duration">Duration in seconds; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a non-positive duration.</exception>
    public void Activate(PowerUpType type, float duration)
    {
        if (!float.IsFinite(duration) || duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Effect duration must be positive.");
        }

        _remaining[type] = _remaining.TryGetValue(type, out float current) ? MathF.Max(current, duration) : duration;
    }

    /// <summary>True while the given effect has time remaining.</summary>
    public bool IsActive(PowerUpType type) => _remaining.TryGetValue(type, out float time) && time > 0f;

    /// <summary>Seconds left on the effect, or 0 if it is not active.</summary>
    public float GetRemaining(PowerUpType type) => _remaining.TryGetValue(type, out float time) ? time : 0f;

    /// <summary>A snapshot of active effects and their remaining time (for the HUD).</summary>
    public IReadOnlyList<(PowerUpType Type, float Remaining)> Snapshot()
    {
        var list = new List<(PowerUpType, float)>(_remaining.Count);
        foreach (KeyValuePair<PowerUpType, float> pair in _remaining)
        {
            list.Add((pair.Key, pair.Value));
        }

        return list;
    }

    /// <summary>Counts all timers down and removes the ones that have finished.</summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    public void Update(float deltaTime)
    {
        // Copy keys first: changing a dictionary while enumerating it is not allowed.
        _keyBuffer.Clear();
        _keyBuffer.AddRange(_remaining.Keys);

        foreach (PowerUpType type in _keyBuffer)
        {
            float left = _remaining[type] - deltaTime;
            if (left <= 0f)
            {
                _remaining.Remove(type);
                Expired?.Invoke(type);
            }
            else
            {
                _remaining[type] = left;
            }
        }
    }

    /// <summary>Removes every active effect.</summary>
    public void Clear() => _remaining.Clear();
}
