using ArenaDefender.Core;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.Tests;

/// <summary>Tests for scoring, the combo multiplier and survival points.</summary>
[TestFixture]
public class ScoreManagerTests
{
    private ScoreManager _score = null!;

    [SetUp]
    public void SetUp() => _score = new ScoreManager();

    [TestCase(0, 1f)]
    [TestCase(1, 1.25f)]
    [TestCase(4, 2f)]
    [TestCase(8, 3f)]
    [TestCase(100, 3f)] // capped
    [TestCase(-3, 1f)]  // defensive: never below 1
    public void CalculateMultiplier_FollowsFormulaAndIsCapped(int combo, float expected)
    {
        Assert.That(ScoreManager.CalculateMultiplier(combo), Is.EqualTo(expected).Within(1e-4f));
    }

    [Test]
    public void RegisterKill_FirstKill_AwardsBaseValue()
    {
        int points = _score.RegisterKill(100);

        Assert.Multiple(() =>
        {
            Assert.That(points, Is.EqualTo(100));
            Assert.That(_score.Score, Is.EqualTo(100));
        });
    }

    [Test]
    public void RegisterKill_ChainedKills_EarnIncreasingPoints()
    {
        int first = _score.RegisterKill(100);
        int second = _score.RegisterKill(100);
        int third = _score.RegisterKill(100);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { first, second, third }, Is.EqualTo(new[] { 100, 125, 150 }));
            Assert.That(_score.Score, Is.EqualTo(375));
        });
    }

    [Test]
    public void Combo_ResetsWhenComboWindowExpires()
    {
        _score.RegisterKill(100);
        _score.RegisterKill(100);

        _score.Update(ScoreManager.ComboWindow + 0.01f);

        Assert.Multiple(() =>
        {
            Assert.That(_score.ComboCount, Is.EqualTo(0));
            Assert.That(_score.Multiplier, Is.EqualTo(1f));
        });
    }

    [Test]
    public void Survival_AwardsPointsForEachSecondAlive()
    {
        _score.Update(2f);

        Assert.That(_score.Score, Is.EqualTo((int)(2 * ScoreManager.SurvivalPointsPerSecond)));
    }

    [Test]
    public void Survival_SmallFramesStillAddUp()
    {
        // 60 frames of 1/60 s = 1 second; no points must be lost to rounding.
        Simulate.For(1f, 1f / 60f, dt => _score.Update(dt));

        Assert.That(_score.Score, Is.EqualTo((int)ScoreManager.SurvivalPointsPerSecond).Within(1));
    }

    [Test]
    public void CalculateKillPoints_RoundsToNearestWholePoint()
    {
        Assert.That(ScoreManager.CalculateKillPoints(150, 1.25f), Is.EqualTo(188)); // 187.5 → 188
    }

    [Test]
    public void CalculateKillPoints_NegativeBaseValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoreManager.CalculateKillPoints(-1, 1f));
    }
}

/// <summary>Tests for wave progression and difficulty scaling formulas.</summary>
[TestFixture]
public class DifficultyManagerTests
{
    [TestCase(0f, 1)]
    [TestCase(24.9f, 1)]
    [TestCase(25f, 2)]
    [TestCase(60f, 3)]
    [TestCase(-5f, 1)] // invalid input is treated as the start
    public void GetWaveForTime_ReturnsExpectedWave(float seconds, int expectedWave)
    {
        Assert.That(DifficultyManager.GetWaveForTime(seconds), Is.EqualTo(expectedWave));
    }

    [Test]
    public void SpawnInterval_ShrinksEveryWave()
    {
        for (int wave = 1; wave < 10; wave++)
        {
            Assert.That(
                DifficultyManager.GetSpawnInterval(wave + 1),
                Is.LessThan(DifficultyManager.GetSpawnInterval(wave)),
                $"Wave {wave + 1} should spawn faster than wave {wave}");
        }
    }

    [Test]
    public void SpawnInterval_NeverGoesBelowMinimum()
    {
        Assert.That(DifficultyManager.GetSpawnInterval(500), Is.EqualTo(DifficultyManager.MinSpawnInterval));
    }

    [TestCase(1, 1f)]
    [TestCase(5, 1.6f)]
    [TestCase(11, 2.5f)]
    public void HealthMultiplier_GrowsLinearly(int wave, float expected)
    {
        Assert.That(DifficultyManager.GetHealthMultiplier(wave), Is.EqualTo(expected).Within(1e-4f));
    }

    [Test]
    public void SpeedMultiplier_IsCapped()
    {
        Assert.That(DifficultyManager.GetSpeedMultiplier(1000), Is.EqualTo(DifficultyManager.MaxSpeedMultiplier));
    }

    [Test]
    public void SpawnWeights_UnlockRunnersThenBrutes()
    {
        var wave1 = DifficultyManager.GetSpawnWeights(1).Select(w => w.Type).ToList();
        var wave2 = DifficultyManager.GetSpawnWeights(2).Select(w => w.Type).ToList();
        var wave3 = DifficultyManager.GetSpawnWeights(3).Select(w => w.Type).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(wave1, Is.EqualTo(new[] { EnemyType.Grunt }));
            Assert.That(wave2, Does.Contain(EnemyType.Runner).And.Not.Contain(EnemyType.Brute));
            Assert.That(wave3, Does.Contain(EnemyType.Brute));
        });
    }

    [Test]
    public void Update_RaisesWaveStartedWhenWaveChanges()
    {
        var difficulty = new DifficultyManager();
        var started = new List<int>();
        difficulty.WaveStarted += started.Add;

        Simulate.For(DifficultyManager.WaveDuration * 2 + 1f, 0.1f, difficulty.Update);

        Assert.That(started, Is.EqualTo(new[] { 2, 3 }));
    }
}
