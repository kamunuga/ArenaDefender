namespace ArenaDefender.Core.Systems;

/// <summary>
/// Source of random numbers. Game systems ask this interface for randomness instead of
/// creating <see cref="Random"/> themselves, so unit tests can pass in predictable values
/// (Dependency Inversion – it is what makes the spawner testable).
/// </summary>
public interface IRandomProvider
{
    /// <summary>Returns a float in the range [0, 1).</summary>
    float NextFloat();

    /// <summary>Returns an integer in the range [minInclusive, maxExclusive).</summary>
    int NextInt(int minInclusive, int maxExclusive);
}

/// <summary>Production implementation wrapping <see cref="Random"/>.</summary>
public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random;

    /// <summary>Creates a provider. Pass a seed to get a repeatable sequence.</summary>
    public SystemRandomProvider(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc />
    public float NextFloat() => _random.NextSingle();

    /// <inheritdoc />
    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
