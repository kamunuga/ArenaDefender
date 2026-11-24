using System.Numerics;
using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.PowerUps;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.Tests;

/// <summary>Tests for power-up effects, timers and pickups.</summary>
[TestFixture]
public class PowerUpTests
{
    [Test]
    public void HealthPack_RestoresHealth()
    {
        var player = new Player(Vector2.Zero);
        player.TakeHit(50f);

        new HealthPackEffect(35f).Apply(player);

        Assert.That(player.Health.Current, Is.EqualTo(85f));
    }

    [Test]
    public void TimedEffect_IsActiveUntilItsDurationRunsOut()
    {
        var effects = new PowerUpEffects();
        effects.Activate(PowerUpType.Speed, 3f);

        effects.Update(2.9f);
        bool activeBefore = effects.IsActive(PowerUpType.Speed);
        effects.Update(0.2f);

        Assert.Multiple(() =>
        {
            Assert.That(activeBefore, Is.True);
            Assert.That(effects.IsActive(PowerUpType.Speed), Is.False);
            Assert.That(effects.ActiveCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void CollectingSameEffectAgain_RefreshesInsteadOfStacking()
    {
        var effects = new PowerUpEffects();
        effects.Activate(PowerUpType.Shield, 5f);
        effects.Update(4f); // 1 s left

        effects.Activate(PowerUpType.Shield, 5f);

        Assert.That(effects.GetRemaining(PowerUpType.Shield), Is.EqualTo(5f).Within(1e-4f));
    }

    [Test]
    public void Expired_EventReportsWhichEffectEnded()
    {
        var effects = new PowerUpEffects();
        var expired = new List<PowerUpType>();
        effects.Expired += expired.Add;
        effects.Activate(PowerUpType.RapidFire, 1f);
        effects.Activate(PowerUpType.Speed, 10f);

        effects.Update(2f);

        Assert.That(expired, Is.EqualTo(new[] { PowerUpType.RapidFire }));
    }

    [Test]
    public void DefaultCatalog_HasAnEffectForEveryPowerUpType()
    {
        PowerUpCatalog catalog = PowerUpCatalog.CreateDefault();

        foreach (PowerUpType type in Enum.GetValues<PowerUpType>())
        {
            Assert.That(catalog.Contains(type), Is.True, $"Missing effect for {type}");
        }
    }

    [Test]
    public void Catalog_UnregisteredType_ThrowsHelpfulError()
    {
        var catalog = new PowerUpCatalog();

        var ex = Assert.Throws<InvalidOperationException>(() => catalog.Get(PowerUpType.Speed));
        Assert.That(ex!.Message, Does.Contain("Speed"));
    }

    [Test]
    public void PowerUp_InsideMagnetRange_IsPulledTowardsPlayer()
    {
        var powerUp = new PowerUp(PowerUpType.Health, new Vector2(100, 0));
        float before = GameMath.Distance(powerUp.Position, Vector2.Zero);

        powerUp.Update(0.1f, Vector2.Zero);

        Assert.That(GameMath.Distance(powerUp.Position, Vector2.Zero), Is.LessThan(before));
    }

    [Test]
    public void PowerUp_OutsideMagnetRange_DoesNotMove()
    {
        var start = new Vector2(PowerUp.MagnetRadius + 50f, 0);
        var powerUp = new PowerUp(PowerUpType.Health, start);

        powerUp.Update(0.1f, Vector2.Zero);

        Assert.That(powerUp.Position, Is.EqualTo(start));
    }

    [Test]
    public void PowerUp_DisappearsAfterLifetime()
    {
        var powerUp = new PowerUp(PowerUpType.Speed, new Vector2(1000, 1000));

        Simulate.For(PowerUp.Lifetime + 0.1f, 0.1f, dt => powerUp.Update(dt, Vector2.Zero));

        Assert.That(powerUp.IsActive, Is.False);
    }

    [TestCase(0.9f, 0.10f, PowerUpType.Health)] // healthy player: health is first 15/80 of the range
    [TestCase(0.9f, 0.30f, PowerUpType.Speed)]
    [TestCase(0.2f, 0.30f, PowerUpType.Health)] // hurt player: health weight rises to 45/110
    public void ChoosePowerUpType_FavoursHealthWhenPlayerIsHurt(float healthFraction, float roll, PowerUpType expected)
    {
        Assert.That(Core.World.GameWorld.ChoosePowerUpType(roll, healthFraction), Is.EqualTo(expected));
    }
}

/// <summary>Tests for aim assist, the nova blast and collision helpers.</summary>
[TestFixture]
public class CombatTests
{
    [Test]
    public void AimAssist_EnemyInsideCone_BendsShotTowardsIt()
    {
        // Enemy 5° off the aim line, 300 units away.
        Vector2 enemyPosition = GameMath.FromAngle(GameMath.DegreesToRadians(5f)) * 300f;
        var enemies = new List<Enemy> { new StationaryEnemy(enemyPosition) };

        Vector2 shot = AimAssist.Adjust(Vector2.Zero, Vector2.UnitX, enemies);

        float angleToEnemyBefore = GameMath.AngleBetween(Vector2.UnitX, enemyPosition);
        float angleToEnemyAfter = GameMath.AngleBetween(shot, enemyPosition);
        Assert.That(angleToEnemyAfter, Is.LessThan(angleToEnemyBefore));
    }

    [Test]
    public void AimAssist_EnemyOutsideCone_LeavesAimUnchanged()
    {
        Vector2 enemyPosition = GameMath.FromAngle(GameMath.DegreesToRadians(40f)) * 300f;
        var enemies = new List<Enemy> { new StationaryEnemy(enemyPosition) };

        Vector2 shot = AimAssist.Adjust(Vector2.Zero, Vector2.UnitX, enemies);

        Assert.That(shot, Is.EqualTo(Vector2.UnitX));
    }

    [Test]
    public void AimAssist_EnemyOutOfRange_LeavesAimUnchanged()
    {
        var enemies = new List<Enemy> { new StationaryEnemy(new Vector2(AimAssist.Range + 100f, 5f)) };

        Vector2 shot = AimAssist.Adjust(Vector2.Zero, Vector2.UnitX, enemies);

        Assert.That(shot, Is.EqualTo(Vector2.UnitX));
    }

    [TestCase(0f, 60f)]       // centre: full damage
    [TestCase(105f, 40.5f)]   // halfway: lerp(1, 0.35, 0.5) = 0.675 → 40.5
    [TestCase(210f, 21f)]     // edge: 35 %
    [TestCase(211f, 0f)]      // outside
    public void NovaBlast_DamageFallsOffLinearlyWithDistance(float distance, float expected)
    {
        Assert.That(NovaBlast.CalculateDamage(distance), Is.EqualTo(expected).Within(1e-3f));
    }

    [Test]
    public void NovaBlast_HitsOnlyEnemiesInRange_AndReportsKills()
    {
        var near = new StationaryEnemy(new Vector2(50, 0));                    // 50 hp, takes ~53 → dies
        var far = new StationaryEnemy(new Vector2(NovaBlast.Radius + 200f, 0)); // out of range
        var killed = new List<Enemy>();

        int hits = NovaBlast.Apply(Vector2.Zero, new List<Enemy> { near, far }, killed.Add);

        Assert.Multiple(() =>
        {
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(killed, Is.EqualTo(new[] { near }));
            Assert.That(far.Health.Current, Is.EqualTo(far.Health.Max));
        });
    }

    [Test]
    public void SeparateEnemies_PushesOverlappingEnemiesApart()
    {
        var a = new StationaryEnemy(new Vector2(100, 100));
        var b = new StationaryEnemy(new Vector2(110, 100)); // radius 15 each → overlapping by 20

        CollisionSystem.SeparateEnemies(new List<Enemy> { a, b });

        Assert.That(GameMath.Distance(a.Position, b.Position), Is.EqualTo(a.Radius + b.Radius).Within(1e-3f));
    }

    [Test]
    public void SeparateEnemies_ExactlyOnTopOfEachOther_DoesNotProduceNaN()
    {
        var a = new StationaryEnemy(new Vector2(100, 100));
        var b = new StationaryEnemy(new Vector2(100, 100));

        CollisionSystem.SeparateEnemies(new List<Enemy> { a, b });

        Assert.Multiple(() =>
        {
            Assert.That(GameMath.IsFinite(a.Position) && GameMath.IsFinite(b.Position), Is.True);
            Assert.That(GameMath.Distance(a.Position, b.Position), Is.GreaterThan(0f));
        });
    }

    [Test]
    public void ProjectilePool_ReusesReturnedProjectiles()
    {
        var pool = new ProjectilePool();
        Projectile first = pool.Rent();
        pool.Return(first);

        Projectile second = pool.Rent();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.CreatedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Projectile_LaunchWithZeroDirection_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Projectile().Launch(Vector2.Zero, Vector2.Zero));
    }
}
