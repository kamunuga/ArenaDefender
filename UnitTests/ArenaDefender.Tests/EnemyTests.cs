using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;

namespace ArenaDefender.Tests;

/// <summary>Tests for the three enemy types and the enemy factory.</summary>
[TestFixture]
public class EnemyTests
{
    private static readonly ArenaBounds Bounds = new(5000f, 5000f);
    private const float Dt = 1f / 60f;

    [Test]
    public void Grunt_MovesStraightTowardsThePlayer()
    {
        var grunt = new GruntEnemy(new Vector2(1000, 1000));
        var player = new Vector2(1000, 2000);

        grunt.Update(0.5f, player, Bounds);

        Assert.Multiple(() =>
        {
            Assert.That(grunt.Position.X, Is.EqualTo(1000f).Within(1e-3f));
            Assert.That(grunt.Position.Y, Is.EqualTo(1000f + GruntEnemy.BaseStats.Speed * 0.5f).Within(1e-2f));
        });
    }

    [Test]
    public void Runner_TurnsNoFasterThanItsTurnRate()
    {
        var runner = new RunnerEnemy(new Vector2(1000, 1000));
        runner.FaceTowards(new Vector2(2000, 1000)); // facing +X
        Vector2 before = runner.Facing;

        // Player directly "below" (+Y): a 90° turn is needed, far more than one frame allows.
        runner.Update(Dt, new Vector2(1000, 2000), Bounds);

        float turned = GameMath.AngleBetween(before, runner.Facing);
        Assert.Multiple(() =>
        {
            Assert.That(turned, Is.EqualTo(RunnerEnemy.TurnRate * Dt).Within(1e-3f));
            Assert.That(runner.LastTurnDirection, Is.EqualTo(1), "Cross product should say turn towards +Y");
        });
    }

    [Test]
    public void Runner_IsFasterButWeakerThanGrunt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RunnerEnemy.BaseStats.Speed, Is.GreaterThan(GruntEnemy.BaseStats.Speed));
            Assert.That(RunnerEnemy.BaseStats.MaxHealth, Is.LessThan(GruntEnemy.BaseStats.MaxHealth));
        });
    }

    [Test]
    public void Brute_HitOnArmouredFront_TakesReducedDamage()
    {
        var brute = new BruteEnemy(new Vector2(1000, 1000));
        brute.FaceTowards(new Vector2(2000, 1000)); // facing +X

        // A bullet travelling in −X comes from the +X side: straight into the armour.
        float applied = brute.ApplyHit(10f, new Vector2(-1, 0));

        Assert.That(applied, Is.EqualTo(10f * BruteEnemy.ArmouredDamageFactor).Within(1e-4f));
    }

    [Test]
    public void Brute_HitFromBehind_TakesFullDamage()
    {
        var brute = new BruteEnemy(new Vector2(1000, 1000));
        brute.FaceTowards(new Vector2(2000, 1000)); // facing +X

        // A bullet travelling in +X comes from behind.
        float applied = brute.ApplyHit(10f, new Vector2(1, 0));

        Assert.That(applied, Is.EqualTo(10f));
    }

    [Test]
    public void Brute_HitFromTheSide_IsOutsideArmourArc()
    {
        var brute = new BruteEnemy(new Vector2(1000, 1000));
        brute.FaceTowards(new Vector2(2000, 1000));

        // Bullet travelling in +Y hits the side (90° from facing; armour covers ±55°).
        Assert.That(brute.IsArmouredHit(new Vector2(0, 1)), Is.False);
    }

    [Test]
    public void Enemy_HealthMultiplier_ScalesMaximumHealth()
    {
        var grunt = new GruntEnemy(Vector2.Zero, healthMultiplier: 1.5f);

        Assert.That(grunt.Health.Max, Is.EqualTo(GruntEnemy.BaseStats.MaxHealth * 1.5f).Within(1e-3f));
    }

    [Test]
    public void Enemy_ReachingZeroHealth_IsDeactivated()
    {
        var runner = new RunnerEnemy(Vector2.Zero);

        runner.ApplyHit(RunnerEnemy.BaseStats.MaxHealth, Vector2.UnitX);

        Assert.Multiple(() =>
        {
            Assert.That(runner.IsDead, Is.True);
            Assert.That(runner.IsActive, Is.False);
        });
    }

    [Test]
    public void Brute_ResistsKnockbackMoreThanGrunt()
    {
        var grunt = new GruntEnemy(Vector2.Zero);
        var brute = new BruteEnemy(Vector2.Zero);

        grunt.ApplyKnockback(new Vector2(100, 0));
        brute.ApplyKnockback(new Vector2(100, 0));

        Assert.That(brute.KnockbackVelocity.Length(), Is.LessThan(grunt.KnockbackVelocity.Length()));
    }

    [Test]
    public void Enemy_InvalidMultiplier_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new GruntEnemy(Vector2.Zero, healthMultiplier: 0f));
    }

    [TestCase(EnemyType.Grunt, typeof(GruntEnemy))]
    [TestCase(EnemyType.Runner, typeof(RunnerEnemy))]
    [TestCase(EnemyType.Brute, typeof(BruteEnemy))]
    public void EnemyFactory_CreatesTheRequestedType(EnemyType type, Type expectedClass)
    {
        Enemy enemy = new EnemyFactory().Create(type, new Vector2(5, 5), 1f, 1f, Vector2.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(enemy.GetType(), Is.EqualTo(expectedClass));
            Assert.That(enemy.Type, Is.EqualTo(type));
        });
    }

    [Test]
    public void EnemyFactory_UnknownType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EnemyFactory().Create((EnemyType)99, Vector2.Zero, 1f, 1f, Vector2.Zero));
    }
}
