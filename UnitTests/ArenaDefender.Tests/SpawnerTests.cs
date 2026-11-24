using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Systems;
using ArenaDefender.Core.World;

namespace ArenaDefender.Tests;

/// <summary>Tests for enemy spawning and weighted random selection.</summary>
[TestFixture]
public class SpawnerTests
{
    private static readonly ArenaBounds Bounds = new(2000f, 1400f);

    private static readonly IReadOnlyList<(EnemyType Type, float Weight)> Weights = new List<(EnemyType, float)>
    {
        (EnemyType.Grunt, 6f),  // 0.0 – 0.6
        (EnemyType.Runner, 3f), // 0.6 – 0.9
        (EnemyType.Brute, 1f),  // 0.9 – 1.0
    };

    [TestCase(0.00f, EnemyType.Grunt)]
    [TestCase(0.59f, EnemyType.Grunt)]
    [TestCase(0.61f, EnemyType.Runner)]
    [TestCase(0.89f, EnemyType.Runner)]
    [TestCase(0.95f, EnemyType.Brute)]
    [TestCase(1.00f, EnemyType.Brute)] // out-of-range roll is clamped
    public void ChooseEnemyType_PicksTheSegmentTheRollLandsIn(float roll, EnemyType expected)
    {
        Assert.That(EnemySpawner.ChooseEnemyType(Weights, roll), Is.EqualTo(expected));
    }

    [Test]
    public void ChooseEnemyType_ZeroWeightTypeIsNeverChosen()
    {
        var weights = new List<(EnemyType, float)> { (EnemyType.Grunt, 0f), (EnemyType.Brute, 5f) };

        for (float roll = 0f; roll < 1f; roll += 0.05f)
        {
            Assert.That(EnemySpawner.ChooseEnemyType(weights, roll), Is.EqualTo(EnemyType.Brute));
        }
    }

    [Test]
    public void ChooseEnemyType_AllWeightsZero_Throws()
    {
        var weights = new List<(EnemyType, float)> { (EnemyType.Grunt, 0f) };

        Assert.Throws<ArgumentException>(() => EnemySpawner.ChooseEnemyType(weights, 0.5f));
    }

    [Test]
    public void ChooseSpawnPosition_IsAlwaysFarFromPlayerAndInsideArena()
    {
        var spawner = new EnemySpawner(new EnemyFactory(), new SystemRandomProvider(seed: 1234), new DifficultyManager());
        var player = Bounds.Centre;

        for (int i = 0; i < 500; i++)
        {
            Vector2 position = spawner.ChooseSpawnPosition(player, Bounds);

            Assert.That(GameMath.Distance(position, player), Is.GreaterThanOrEqualTo(EnemySpawner.MinSpawnDistance));
            Assert.That(Bounds.Contains(position), Is.True);
        }
    }

    [Test]
    public void ChooseSpawnPosition_PlayerInCorner_FallsBackToFarCorner()
    {
        // Every random edge point (FakeRandom always returns 0 → top-left corner area) is too close to a
        // player standing in the top-left corner, so the spawner must use the farthest corner instead.
        var spawner = new EnemySpawner(new EnemyFactory(), new FakeRandom(0f), new DifficultyManager());
        var player = new Vector2(40, 40);

        Vector2 position = spawner.ChooseSpawnPosition(player, Bounds);

        Assert.That(position, Is.EqualTo(new Vector2(Bounds.Width - EnemySpawner.EdgeInset, Bounds.Height - EnemySpawner.EdgeInset)));
    }

    [Test]
    public void Update_SpawnsAfterTheInitialDelay()
    {
        var factory = new RecordingEnemyFactory();
        var spawner = new EnemySpawner(factory, new FakeRandom(0.3f), new DifficultyManager());

        Enemy? tooEarly = spawner.Update(0.5f, Bounds.Centre, 0, Bounds);
        Enemy? onTime = spawner.Update(0.6f, Bounds.Centre, 0, Bounds);

        Assert.Multiple(() =>
        {
            Assert.That(tooEarly, Is.Null);
            Assert.That(onTime, Is.Not.Null);
            Assert.That(factory.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Update_DoesNotSpawnWhenEnemyCapReached()
    {
        var difficulty = new DifficultyManager();
        var spawner = new EnemySpawner(new RecordingEnemyFactory(), new FakeRandom(0.3f), difficulty);

        Enemy? spawned = spawner.Update(5f, Bounds.Centre, difficulty.CurrentMaxEnemies, Bounds);

        Assert.That(spawned, Is.Null);
    }

    [Test]
    public void Update_AppliesTheCurrentWaveHealthMultiplier()
    {
        var difficulty = new DifficultyManager();
        // Reach wave 4. Run slightly past the boundary: adding 0.1f 750 times gives 74.99…, not exactly 75,
        // because 0.1 cannot be stored exactly as a float (this test originally failed for that reason).
        Simulate.For(DifficultyManager.WaveDuration * 3 + 1f, 0.1f, difficulty.Update);
        Assert.That(difficulty.Wave, Is.EqualTo(4), "Precondition: should be in wave 4");
        var factory = new RecordingEnemyFactory();
        var spawner = new EnemySpawner(factory, new FakeRandom(0.3f), difficulty);

        spawner.Update(2f, Bounds.Centre, 0, Bounds);

        Assert.That(factory.Requests[0].HealthMultiplier, Is.EqualTo(DifficultyManager.GetHealthMultiplier(4)).Within(1e-4f));
    }

    [Test]
    public void Constructor_NullDependency_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new EnemySpawner(null!, new FakeRandom(), new DifficultyManager()));
    }
}
