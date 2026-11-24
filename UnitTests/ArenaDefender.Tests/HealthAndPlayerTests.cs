using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Components;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.World;

namespace ArenaDefender.Tests;

/// <summary>Tests for the health component and its edge cases.</summary>
[TestFixture]
public class HealthTests
{
    [Test]
    public void NewHealth_StartsAtMaximum()
    {
        var health = new Health(100f);

        Assert.Multiple(() =>
        {
            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.That(health.Fraction, Is.EqualTo(1f));
            Assert.That(health.IsDead, Is.False);
        });
    }

    [Test]
    public void TakeDamage_ReducesCurrentHealth()
    {
        var health = new Health(100f);

        health.TakeDamage(30f);

        Assert.That(health.Current, Is.EqualTo(70f));
    }

    [Test]
    public void TakeDamage_MoreThanRemaining_StopsAtZeroAndReportsActualDamage()
    {
        var health = new Health(100f);

        float applied = health.TakeDamage(150f);

        Assert.Multiple(() =>
        {
            Assert.That(health.Current, Is.EqualTo(0f));
            Assert.That(applied, Is.EqualTo(100f));
            Assert.That(health.IsDead, Is.True);
        });
    }

    [Test]
    public void TakeDamage_NegativeAmount_Throws()
    {
        var health = new Health(100f);

        Assert.Throws<ArgumentOutOfRangeException>(() => health.TakeDamage(-5f));
    }

    [Test]
    public void Heal_CannotExceedMaximum()
    {
        var health = new Health(100f);
        health.TakeDamage(10f);

        float restored = health.Heal(50f);

        Assert.Multiple(() =>
        {
            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.That(restored, Is.EqualTo(10f));
        });
    }

    [Test]
    public void Heal_WhenDead_HasNoEffect()
    {
        var health = new Health(50f);
        health.TakeDamage(50f);

        health.Heal(20f);

        Assert.That(health.Current, Is.EqualTo(0f));
    }

    [Test]
    public void Died_IsRaisedExactlyOnce_EvenWhenHitAgain()
    {
        var health = new Health(20f);
        int deaths = 0;
        health.Died += () => deaths++;

        health.TakeDamage(25f);
        health.TakeDamage(10f);

        Assert.That(deaths, Is.EqualTo(1));
    }

    [TestCase(0f)]
    [TestCase(-10f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void Constructor_InvalidMaximum_Throws(float max)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Health(max));
    }
}

/// <summary>Tests for player movement, damage, lives and power-up modifiers.</summary>
[TestFixture]
public class PlayerTests
{
    private static readonly ArenaBounds Bounds = new(1000f, 1000f);
    private Player _player = null!;

    [SetUp]
    public void SetUp() => _player = new Player(new Vector2(500, 500));

    [Test]
    public void TakeHit_ReducesHealthAndGivesShortInvulnerability()
    {
        HitResult result = _player.TakeHit(25f);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(HitResult.Damaged));
            Assert.That(_player.Health.Current, Is.EqualTo(75f));
            Assert.That(_player.IsInvulnerable, Is.True);
        });
    }

    [Test]
    public void TakeHit_WhileInvulnerable_IsBlocked()
    {
        _player.TakeHit(25f);

        HitResult second = _player.TakeHit(25f);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(HitResult.Blocked));
            Assert.That(_player.Health.Current, Is.EqualTo(75f));
        });
    }

    [Test]
    public void TakeHit_WithShieldActive_IsBlocked()
    {
        _player.Effects.Activate(PowerUpType.Shield, 5f);

        HitResult result = _player.TakeHit(50f);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(HitResult.Blocked));
            Assert.That(_player.Health.Current, Is.EqualTo(Player.MaxHealth));
        });
    }

    [Test]
    public void LethalHit_WithLivesLeft_LosesALifeAndRespawnsAtFullHealth()
    {
        HitResult result = _player.TakeHit(500f);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(HitResult.LifeLost));
            Assert.That(_player.Lives, Is.EqualTo(Player.StartingLives - 1));
            Assert.That(_player.Health.Current, Is.EqualTo(Player.MaxHealth));
            Assert.That(_player.InvulnerableTime, Is.EqualTo(Player.RespawnInvulnerability));
        });
    }

    [Test]
    public void LethalHit_OnLastLife_IsGameOver()
    {
        var player = new Player(Vector2.Zero, lives: 1);

        HitResult result = player.TakeHit(500f);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(HitResult.GameOver));
            Assert.That(player.IsOutOfLives, Is.True);
        });
    }

    [Test]
    public void SpeedPowerUp_MultipliesMovementSpeed()
    {
        _player.Effects.Activate(PowerUpType.Speed, 5f);

        Assert.That(_player.CurrentSpeed, Is.EqualTo(Player.BaseSpeed * Player.SpeedBoostMultiplier).Within(1e-3f));
    }

    [Test]
    public void RapidFirePowerUp_DividesFireInterval()
    {
        _player.Effects.Activate(PowerUpType.RapidFire, 5f);

        Assert.That(_player.FireInterval, Is.EqualTo(Player.BaseFireInterval / Player.RapidFireMultiplier).Within(1e-5f));
    }

    [Test]
    public void Update_AcceleratesSmoothlyTowardsTopSpeed()
    {
        var input = new PlayerInput(new Vector2(1, 0), Vector2.Zero, false, false);

        _player.Update(1f / 60f, input, Bounds);
        float speedAfterOneFrame = _player.Velocity.Length();
        Simulate.For(1f, 1f / 60f, dt => _player.Update(dt, input, Bounds));

        Assert.Multiple(() =>
        {
            Assert.That(speedAfterOneFrame, Is.GreaterThan(0f));
            Assert.That(speedAfterOneFrame, Is.LessThan(Player.BaseSpeed * 0.5f), "Lerp should not reach full speed instantly");
            Assert.That(_player.Velocity.Length(), Is.EqualTo(Player.BaseSpeed).Within(1f));
        });
    }

    [Test]
    public void Update_DiagonalInput_IsNotFasterThanStraightInput()
    {
        var diagonal = new PlayerInput(new Vector2(1, 1), Vector2.Zero, false, false);

        Simulate.For(2f, 1f / 60f, dt => _player.Update(dt, diagonal, new ArenaBounds(100000f, 100000f)));

        Assert.That(_player.Velocity.Length(), Is.EqualTo(Player.BaseSpeed).Within(1f));
    }

    [Test]
    public void Update_PlayerCannotLeaveTheArena()
    {
        var left = new PlayerInput(new Vector2(-1, 0), Vector2.Zero, false, false);

        Simulate.For(5f, 1f / 60f, dt => _player.Update(dt, left, Bounds));

        Assert.That(_player.Position.X, Is.EqualTo(_player.Radius).Within(1e-3f));
    }

    [Test]
    public void TryFire_CannotFireAgainUntilCooldownEnds()
    {
        bool first = _player.TryFire();
        bool second = _player.TryFire();
        _player.Update(Player.BaseFireInterval + 0.01f, PlayerInput.None, Bounds);
        bool third = _player.TryFire();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(third, Is.True);
        });
    }

    [Test]
    public void Cooldown_ProgressGoesFromZeroToOne()
    {
        var cooldown = new Cooldown(2f);
        cooldown.Trigger();
        float atStart = cooldown.Progress;
        cooldown.Update(1f);
        float halfway = cooldown.Progress;
        cooldown.Update(5f);

        Assert.Multiple(() =>
        {
            Assert.That(atStart, Is.EqualTo(0f));
            Assert.That(halfway, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(cooldown.IsReady, Is.True);
            Assert.That(cooldown.Remaining, Is.EqualTo(0f), "Remaining time must never go negative");
        });
    }
}
