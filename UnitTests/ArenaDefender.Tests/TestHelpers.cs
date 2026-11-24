using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.Tests;

/// <summary>
/// A predictable random number source. Returns the given values in order and then repeats them,
/// so tests that involve randomness (spawning, drops) give the same result on every run.
/// </summary>
internal sealed class FakeRandom : IRandomProvider
{
    private readonly float[] _values;
    private int _index;

    public FakeRandom(params float[] values)
    {
        _values = values.Length == 0 ? new[] { 0f } : values;
    }

    public float NextFloat()
    {
        float value = _values[_index % _values.Length];
        _index++;
        return value;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        int range = maxExclusive - minInclusive;
        return minInclusive + Math.Min(range - 1, (int)(NextFloat() * range));
    }
}

/// <summary>An enemy that never moves. Lets tests control positions exactly.</summary>
internal sealed class StationaryEnemy : Enemy
{
    public static readonly EnemyStats DefaultStats = new(
        MaxHealth: 50f, Speed: 0f, ContactDamage: 20f, ScoreValue: 100, Radius: 15f,
        KnockbackResistance: 1f, PowerUpDropChance: 0f);

    public StationaryEnemy(Vector2 position, EnemyStats? stats = null)
        : base(EnemyType.Grunt, stats ?? DefaultStats, position, 1f, 1f)
    {
    }

    protected override void Move(float deltaTime, Vector2 playerPosition)
    {
    }
}

/// <summary>Factory that records requests, so spawner tests can check what was asked for.</summary>
internal sealed class RecordingEnemyFactory : IEnemyFactory
{
    public List<(EnemyType Type, Vector2 Position, float HealthMultiplier)> Requests { get; } = new();

    public Enemy Create(EnemyType type, Vector2 position, float healthMultiplier, float speedMultiplier, Vector2 target)
    {
        Requests.Add((type, position, healthMultiplier));
        return new StationaryEnemy(position);
    }
}

internal static class Simulate
{
    /// <summary>Calls an update action repeatedly with a fixed time step.</summary>
    public static void For(float seconds, float step, Action<float> update)
    {
        int frames = (int)MathF.Round(seconds / step);
        for (int i = 0; i < frames; i++)
        {
            update(step);
        }
    }
}
