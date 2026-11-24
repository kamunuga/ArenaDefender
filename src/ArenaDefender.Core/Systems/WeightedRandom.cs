using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Systems;

/// <summary>Generic weighted random selection, shared by enemy and power-up spawning.</summary>
public static class WeightedRandom
{
    /// <summary>
    /// Picks one item. Imagine the weights laid end to end on a line of length ΣW;
    /// roll × ΣW lands inside exactly one segment and that segment's item is chosen.
    /// Negative weights are treated as zero.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="options">Items and their relative weights.</param>
    /// <param name="roll">A random number in [0, 1).</param>
    /// <exception cref="ArgumentException">Thrown when the list is empty or every weight is zero.</exception>
    public static T Choose<T>(IReadOnlyList<(T Item, float Weight)> options, float roll)
    {
        ArgumentNullException.ThrowIfNull(options);

        float total = 0f;
        foreach ((T _, float weight) in options)
        {
            total += MathF.Max(0f, weight);
        }

        if (options.Count == 0 || total <= 0f)
        {
            throw new ArgumentException("At least one option must have a positive weight.", nameof(options));
        }

        float target = GameMath.Clamp(roll, 0f, 0.999999f) * total;
        float cumulative = 0f;
        foreach ((T item, float weight) in options)
        {
            float w = MathF.Max(0f, weight);
            cumulative += w;
            if (w > 0f && target < cumulative)
            {
                return item;
            }
        }

        // Floating point safety net: return the last option with a positive weight.
        for (int i = options.Count - 1; i >= 0; i--)
        {
            if (options[i].Weight > 0f)
            {
                return options[i].Item;
            }
        }

        return options[^1].Item;
    }
}
