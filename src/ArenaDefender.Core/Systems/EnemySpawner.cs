using System.Numerics;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;

namespace ArenaDefender.Core.Systems;

/// <summary>
/// Spawns enemies on a timer. The timer interval, enemy cap, enemy mix and stat multipliers
/// all come from <see cref="DifficultyManager"/>, so the game gets harder as waves progress.
/// </summary>
public sealed class EnemySpawner
{
    /// <summary>Enemies never appear closer than this to the player.</summary>
    public const float MinSpawnDistance = 380f;

    /// <summary>How far inside the arena edge enemies appear.</summary>
    public const float EdgeInset = 40f;

    private const int MaxPositionAttempts = 12;

    private readonly IEnemyFactory _factory;
    private readonly IRandomProvider _random;
    private readonly DifficultyManager _difficulty;
    private float _timer;

    /// <summary>Creates a spawner.</summary>
    /// <exception cref="ArgumentNullException">Thrown when a dependency is null.</exception>
    public EnemySpawner(IEnemyFactory factory, IRandomProvider random, DifficultyManager difficulty)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        _timer = 1f; // short grace period at the start of the game
    }

    /// <summary>Total enemies spawned so far.</summary>
    public int TotalSpawned { get; private set; }

    /// <summary>
    /// Advances the spawn timer and returns a new enemy when it is time to spawn one and the cap
    /// has not been reached; otherwise returns null.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    /// <param name="playerPosition">Player position (enemies spawn away from it).</param>
    /// <param name="aliveEnemies">How many enemies are currently alive.</param>
    /// <param name="bounds">The arena.</param>
    public Enemy? Update(float deltaTime, Vector2 playerPosition, int aliveEnemies, ArenaBounds bounds)
    {
        _timer -= deltaTime;
        if (_timer > 0f)
        {
            return null;
        }

        _timer += _difficulty.CurrentSpawnInterval;
        if (_timer < 0f)
        {
            _timer = 0f; // after a long frame, don't spawn a burst to "catch up"
        }

        if (aliveEnemies >= _difficulty.CurrentMaxEnemies)
        {
            return null;
        }

        EnemyType type = ChooseEnemyType(DifficultyManager.GetSpawnWeights(_difficulty.Wave), _random.NextFloat());
        Vector2 position = ChooseSpawnPosition(playerPosition, bounds);

        TotalSpawned++;
        return _factory.Create(type, position, _difficulty.CurrentHealthMultiplier, _difficulty.CurrentSpeedMultiplier, playerPosition);
    }

    /// <summary>
    /// Chooses which enemy type to spawn using the wave's weights (see <see cref="WeightedRandom"/>).
    /// </summary>
    /// <param name="weights">Types and their relative weights.</param>
    /// <param name="roll">A random number in [0, 1).</param>
    /// <exception cref="ArgumentException">Thrown when the list is empty or all weights are zero.</exception>
    public static EnemyType ChooseEnemyType(IReadOnlyList<(EnemyType Type, float Weight)> weights, float roll) =>
        WeightedRandom.Choose(weights, roll);

    /// <summary>
    /// Picks a random point along the arena edge that is at least <see cref="MinSpawnDistance"/> from
    /// the player. If no such point is found after several tries, uses the corner farthest from the player.
    /// </summary>
    public Vector2 ChooseSpawnPosition(Vector2 playerPosition, ArenaBounds bounds)
    {
        for (int attempt = 0; attempt < MaxPositionAttempts; attempt++)
        {
            Vector2 candidate = RandomEdgePoint(bounds);
            if (GameMath.Distance(candidate, playerPosition) >= MinSpawnDistance)
            {
                return candidate;
            }
        }

        return FarthestCorner(playerPosition, bounds);
    }

    private Vector2 RandomEdgePoint(ArenaBounds bounds)
    {
        float t = _random.NextFloat();
        float minX = EdgeInset, maxX = bounds.Width - EdgeInset;
        float minY = EdgeInset, maxY = bounds.Height - EdgeInset;

        return _random.NextInt(0, 4) switch
        {
            0 => new Vector2(GameMath.Lerp(minX, maxX, t), minY), // top edge
            1 => new Vector2(GameMath.Lerp(minX, maxX, t), maxY), // bottom edge
            2 => new Vector2(minX, GameMath.Lerp(minY, maxY, t)), // left edge
            _ => new Vector2(maxX, GameMath.Lerp(minY, maxY, t)), // right edge
        };
    }

    private static Vector2 FarthestCorner(Vector2 playerPosition, ArenaBounds bounds)
    {
        Vector2[] corners =
        {
            new(EdgeInset, EdgeInset),
            new(bounds.Width - EdgeInset, EdgeInset),
            new(EdgeInset, bounds.Height - EdgeInset),
            new(bounds.Width - EdgeInset, bounds.Height - EdgeInset),
        };

        Vector2 best = corners[0];
        float bestDistance = -1f;
        foreach (Vector2 corner in corners)
        {
            float d = GameMath.DistanceSquared(corner, playerPosition);
            if (d > bestDistance)
            {
                bestDistance = d;
                best = corner;
            }
        }

        return best;
    }
}
