using System.Numerics;

namespace ArenaDefender.Core.Enemies;

/// <summary>
/// Creates enemies. The spawner depends on this interface, not on the concrete enemy classes,
/// so tests can supply a fake factory and new enemy types only require changing the factory
/// (Factory pattern + Dependency Inversion).
/// </summary>
public interface IEnemyFactory
{
    /// <summary>Creates an enemy of the given type.</summary>
    /// <param name="type">Kind of enemy.</param>
    /// <param name="position">Spawn position.</param>
    /// <param name="healthMultiplier">Wave health multiplier.</param>
    /// <param name="speedMultiplier">Wave speed multiplier.</param>
    /// <param name="target">Where the enemy should initially face (usually the player).</param>
    Enemy Create(EnemyType type, Vector2 position, float healthMultiplier, float speedMultiplier, Vector2 target);
}

/// <summary>The game's standard enemy factory.</summary>
public sealed class EnemyFactory : IEnemyFactory
{
    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown <see cref="EnemyType"/>.</exception>
    public Enemy Create(EnemyType type, Vector2 position, float healthMultiplier, float speedMultiplier, Vector2 target)
    {
        switch (type)
        {
            case EnemyType.Grunt:
                return new GruntEnemy(position, healthMultiplier, speedMultiplier);

            case EnemyType.Runner:
                var runner = new RunnerEnemy(position, healthMultiplier, speedMultiplier);
                runner.FaceTowards(target);
                return runner;

            case EnemyType.Brute:
                var brute = new BruteEnemy(position, healthMultiplier, speedMultiplier);
                brute.FaceTowards(target);
                return brute;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown enemy type.");
        }
    }
}
