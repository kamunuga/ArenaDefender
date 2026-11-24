using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.World;

namespace ArenaDefender.Tests;

/// <summary>
/// Integration tests for <see cref="GameWorld"/>: they check that the systems work together
/// (collisions → damage → score → events) without any graphics.
/// </summary>
[TestFixture]
public class GameWorldTests
{
    private const float Dt = 1f / 60f;

    /// <summary>A world with a random source that never drops power-ups or spawns early.</summary>
    private static GameWorld CreateWorld() => new(new FakeRandom(0.99f));

    [Test]
    public void ProjectileHittingEnemy_KillsItAwardsScoreAndRaisesEvent()
    {
        GameWorld world = CreateWorld();
        var target = new StationaryEnemy(world.Player.Position + new Vector2(120, 0),
            StationaryEnemy.DefaultStats with { MaxHealth = 5f });
        world.AddEnemy(target);
        EnemyKilledEventArgs? killed = null;
        world.EnemyKilled += (_, e) => killed = e;

        var fireRight = new PlayerInput(Vector2.Zero, Vector2.UnitX, Fire: true, Nova: false);
        Simulate.For(0.5f, Dt, dt => world.Update(dt, fireRight));

        Assert.Multiple(() =>
        {
            Assert.That(killed, Is.Not.Null);
            Assert.That(killed!.Enemy, Is.SameAs(target));
            Assert.That(world.EnemiesDefeated, Is.EqualTo(1));
            Assert.That(world.Score.Score, Is.GreaterThanOrEqualTo(target.ScoreValue));
            Assert.That(world.Enemies, Does.Not.Contain(target));
        });
    }

    [Test]
    public void EnemyTouchingPlayer_DamagesPlayerAndRaisesEvent()
    {
        GameWorld world = CreateWorld();
        world.AddEnemy(new StationaryEnemy(world.Player.Position + new Vector2(10, 0)));
        PlayerHitEventArgs? hit = null;
        world.PlayerHit += (_, e) => hit = e;

        world.Update(Dt, PlayerInput.None);

        Assert.Multiple(() =>
        {
            Assert.That(hit, Is.Not.Null);
            Assert.That(hit!.Result, Is.EqualTo(HitResult.Damaged));
            Assert.That(world.Player.Health.Current, Is.EqualTo(Player.MaxHealth - StationaryEnemy.DefaultStats.ContactDamage));
        });
    }

    [Test]
    public void EnemyTouchingPlayer_IsPushedOutOfThePlayer()
    {
        GameWorld world = CreateWorld();
        var enemy = new StationaryEnemy(world.Player.Position + new Vector2(5, 0));
        world.AddEnemy(enemy);

        world.Update(Dt, PlayerInput.None);

        float distance = GameMath.Distance(world.Player.Position, enemy.Position);
        Assert.That(distance, Is.GreaterThanOrEqualTo(world.Player.Radius + enemy.Radius));
    }

    [Test]
    public void PlayerTouchingPowerUp_AppliesEffectAwardsPointsAndRemovesIt()
    {
        GameWorld world = CreateWorld();
        world.AddPowerUp(new PowerUp(PowerUpType.Shield, world.Player.Position));
        PowerUpCollectedEventArgs? collected = null;
        world.PowerUpCollected += (_, e) => collected = e;

        world.Update(Dt, PlayerInput.None);

        Assert.Multiple(() =>
        {
            Assert.That(collected?.Type, Is.EqualTo(PowerUpType.Shield));
            Assert.That(world.Player.IsShielded, Is.True);
            Assert.That(world.PowerUps, Is.Empty);
            Assert.That(world.Score.Score, Is.GreaterThanOrEqualTo(Core.Systems.ScoreManager.PickupPoints));
        });
    }

    [Test]
    public void LosingAllLives_EndsTheGameAndStopsTheSimulation()
    {
        GameWorld world = CreateWorld();
        var killer = new StationaryEnemy(world.Player.Position,
            StationaryEnemy.DefaultStats with { ContactDamage = 1000f, MaxHealth = 1_000_000f });
        world.AddEnemy(killer);
        int gameOverEvents = 0;
        world.GameOver += (_, _) => gameOverEvents++;

        // Keep the enemy glued to the player; each life ends as soon as invulnerability wears off.
        for (int frame = 0; frame < 60 * 20 && !world.IsGameOver; frame++)
        {
            killer.Position = world.Player.Position;
            world.Update(Dt, PlayerInput.None);
        }

        float timeAtGameOver = world.ElapsedTime;
        world.Update(Dt, PlayerInput.None);

        Assert.Multiple(() =>
        {
            Assert.That(world.IsGameOver, Is.True);
            Assert.That(world.Player.Lives, Is.EqualTo(0));
            Assert.That(gameOverEvents, Is.EqualTo(1));
            Assert.That(world.ElapsedTime, Is.EqualTo(timeAtGameOver), "World must not update after game over");
        });
    }

    [TestCase(float.NaN)]
    [TestCase(-1f)]
    [TestCase(0f)]
    [TestCase(float.PositiveInfinity)]
    public void Update_InvalidDeltaTime_IsIgnored(float badDeltaTime)
    {
        GameWorld world = CreateWorld();

        world.Update(badDeltaTime, PlayerInput.None);

        Assert.That(world.ElapsedTime, Is.EqualTo(0f));
    }

    [Test]
    public void Update_HugeDeltaTime_IsClampedToMaximumStep()
    {
        GameWorld world = CreateWorld();

        world.Update(5f, PlayerInput.None);

        Assert.That(world.ElapsedTime, Is.EqualTo(GameWorld.MaxDeltaTime).Within(1e-6f));
    }

    [Test]
    public void Update_NaNInput_DoesNotCorruptPlayerPosition()
    {
        GameWorld world = CreateWorld();
        var corrupt = new PlayerInput(new Vector2(float.NaN, 1f), new Vector2(float.PositiveInfinity, 0f), true, false);

        Simulate.For(0.5f, Dt, dt => world.Update(dt, corrupt));

        Assert.That(GameMath.IsFinite(world.Player.Position), Is.True);
    }

    [Test]
    public void LongSimulation_WithScriptedPlayer_RunsWithoutErrorsAndGetsHarder()
    {
        // A smoke test: 3 minutes of play with a simple "bot" that strafes in a circle, shoots the
        // nearest enemy and uses the nova when it can. Seeded randomness makes it repeatable.
        var world = new GameWorld(new Core.Systems.SystemRandomProvider(seed: 42));
        float time = 0f;

        Assert.DoesNotThrow(() =>
        {
            for (int frame = 0; frame < 60 * 180 && !world.IsGameOver; frame++)
            {
                time += Dt;
                Enemy? nearest = world.Enemies.OrderBy(e => GameMath.DistanceSquared(e.Position, world.Player.Position)).FirstOrDefault();
                Vector2 aim = nearest is null ? Vector2.UnitX : nearest.Position - world.Player.Position;
                Vector2 move = GameMath.FromAngle(time * 0.8f);
                world.Update(Dt, new PlayerInput(move, aim, Fire: true, Nova: nearest is not null));
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(world.ElapsedTime, Is.GreaterThan(30f), "The bot should survive at least one wave");
            Assert.That(world.Difficulty.Wave, Is.GreaterThan(1));
            Assert.That(world.EnemiesDefeated, Is.GreaterThan(0));
            Assert.That(world.Enemies.Count, Is.LessThanOrEqualTo(world.Difficulty.CurrentMaxEnemies));
            Assert.That(world.Enemies.All(e => GameMath.IsFinite(e.Position) && world.Bounds.Contains(e.Position)), Is.True);
        });
    }
}
