using System.Numerics;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.PowerUps;
using ArenaDefender.Core.Systems;

namespace ArenaDefender.Core.World;

/// <summary>
/// The complete simulation of one game of Arena Defender. It owns every entity and system and
/// advances them in a fixed order each frame. It knows nothing about drawing, sound or input
/// devices: the front-end feeds it a <see cref="PlayerInput"/> and reacts to its events
/// (Observer pattern). This separation is what lets the whole game loop be unit tested.
/// </summary>
public sealed class GameWorld
{
    /// <summary>Default arena width (larger than the screen, so the camera scrolls).</summary>
    public const float DefaultArenaWidth = 2000f;

    /// <summary>Default arena height.</summary>
    public const float DefaultArenaHeight = 1400f;

    /// <summary>Longest frame the simulation will accept, to stop huge jumps after a hitch or breakpoint.</summary>
    public const float MaxDeltaTime = 0.1f;

    /// <summary>Seconds between random power-up spawns.</summary>
    public const float PowerUpSpawnInterval = 14f;

    /// <summary>Maximum power-ups on the ground at once.</summary>
    public const int MaxPowerUps = 6;

    /// <summary>Speed at which enemies bounce off the player after touching them.</summary>
    public const float ContactKnockback = 420f;

    /// <summary>Radius of the shockwave that clears space when the player loses a life.</summary>
    public const float RespawnClearRadius = 320f;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly List<PowerUp> _powerUps = new();
    private readonly ProjectilePool _projectilePool = new();
    private readonly IRandomProvider _random;
    private readonly EnemySpawner _spawner;
    private float _powerUpTimer = PowerUpSpawnInterval * 0.5f;

    /// <summary>Creates a new game. All dependencies are optional so tests can replace just what they need.</summary>
    /// <param name="random">Random source (defaults to <see cref="SystemRandomProvider"/>).</param>
    /// <param name="enemyFactory">Enemy factory (defaults to <see cref="EnemyFactory"/>).</param>
    /// <param name="catalog">Power-up catalog (defaults to <see cref="PowerUpCatalog.CreateDefault"/>).</param>
    /// <param name="bounds">Arena size (defaults to 2000 × 1400).</param>
    public GameWorld(
        IRandomProvider? random = null,
        IEnemyFactory? enemyFactory = null,
        PowerUpCatalog? catalog = null,
        ArenaBounds? bounds = null)
    {
        _random = random ?? new SystemRandomProvider();
        Catalog = catalog ?? PowerUpCatalog.CreateDefault();
        Bounds = bounds ?? new ArenaBounds(DefaultArenaWidth, DefaultArenaHeight);

        Player = new Player(Bounds.Centre);
        Score = new ScoreManager();
        Difficulty = new DifficultyManager();
        Difficulty.WaveStarted += wave => WaveStarted?.Invoke(this, wave);
        _spawner = new EnemySpawner(enemyFactory ?? new EnemyFactory(), _random, Difficulty);
    }

    /// <summary>Raised when an enemy is destroyed.</summary>
    public event EventHandler<EnemyKilledEventArgs>? EnemyKilled;

    /// <summary>Raised when an enemy takes damage.</summary>
    public event EventHandler<EnemyHitEventArgs>? EnemyHit;

    /// <summary>Raised when the player is hit (not raised for blocked hits).</summary>
    public event EventHandler<PlayerHitEventArgs>? PlayerHit;

    /// <summary>Raised when the shield absorbs a hit.</summary>
    public event EventHandler<Vector2>? ShieldBlocked;

    /// <summary>Raised when a power-up is collected.</summary>
    public event EventHandler<PowerUpCollectedEventArgs>? PowerUpCollected;

    /// <summary>Raised when the player fires.</summary>
    public event EventHandler<Projectile>? ProjectileFired;

    /// <summary>Raised when the nova blast goes off.</summary>
    public event EventHandler<NovaEventArgs>? NovaTriggered;

    /// <summary>Raised when a new wave begins.</summary>
    public event EventHandler<int>? WaveStarted;

    /// <summary>Raised once when the player runs out of lives.</summary>
    public event EventHandler? GameOver;

    /// <summary>The arena.</summary>
    public ArenaBounds Bounds { get; }

    /// <summary>The player.</summary>
    public Player Player { get; }

    /// <summary>Score and combo.</summary>
    public ScoreManager Score { get; }

    /// <summary>Waves and difficulty scaling.</summary>
    public DifficultyManager Difficulty { get; }

    /// <summary>The power-up effects available.</summary>
    public PowerUpCatalog Catalog { get; }

    /// <summary>Living enemies (read-only view).</summary>
    public IReadOnlyList<Enemy> Enemies => _enemies;

    /// <summary>Projectiles in flight (read-only view).</summary>
    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    /// <summary>Power-ups on the ground (read-only view).</summary>
    public IReadOnlyList<PowerUp> PowerUps => _powerUps;

    /// <summary>Seconds survived.</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>Enemies destroyed this game.</summary>
    public int EnemiesDefeated { get; private set; }

    /// <summary>Shots fired this game.</summary>
    public int ShotsFired { get; private set; }

    /// <summary>Shots that hit an enemy.</summary>
    public int ShotsHit { get; private set; }

    /// <summary>Hit percentage (0..100).</summary>
    public float Accuracy => ShotsFired == 0 ? 0f : 100f * ShotsHit / ShotsFired;

    /// <summary>True once the player has no lives left. The world stops updating.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// Advances the simulation by one frame.
    /// </summary>
    /// <param name="deltaTime">Elapsed seconds. Invalid values are ignored; large values are clamped.</param>
    /// <param name="input">What the player is doing this frame.</param>
    public void Update(float deltaTime, PlayerInput input)
    {
        if (IsGameOver || !float.IsFinite(deltaTime) || deltaTime <= 0f)
        {
            return;
        }

        deltaTime = MathF.Min(deltaTime, MaxDeltaTime);
        input = Sanitise(input);

        ElapsedTime += deltaTime;
        Difficulty.Update(deltaTime);
        Score.Update(deltaTime);
        Player.Update(deltaTime, input, Bounds);

        HandleShooting(input);
        HandleNova(input);
        SpawnEnemies(deltaTime);
        SpawnTimedPowerUps(deltaTime);

        foreach (Enemy enemy in _enemies)
        {
            enemy.Update(deltaTime, Player.Position, Bounds);
        }

        foreach (Projectile projectile in _projectiles)
        {
            projectile.Update(deltaTime, Bounds);
        }

        foreach (PowerUp powerUp in _powerUps)
        {
            powerUp.Update(deltaTime, Player.Position);
        }

        HandleProjectileEnemyCollisions();
        HandlePlayerEnemyCollisions();
        HandlePlayerPowerUpCollisions();
        CollisionSystem.SeparateEnemies(_enemies);

        RemoveInactiveEntities();
    }

    /// <summary>Adds an enemy directly (used by tests and debug tools).</summary>
    public void AddEnemy(Enemy enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);
        _enemies.Add(enemy);
    }

    /// <summary>Adds a power-up directly (used by tests and debug tools).</summary>
    public void AddPowerUp(PowerUp powerUp)
    {
        ArgumentNullException.ThrowIfNull(powerUp);
        _powerUps.Add(powerUp);
    }

    // ------------------------------------------------------------------
    // Player actions
    // ------------------------------------------------------------------

    private void HandleShooting(PlayerInput input)
    {
        if (!input.Fire || !Player.TryFire())
        {
            return;
        }

        Vector2 direction = AimAssist.Adjust(Player.Position, Player.Facing, _enemies);
        if (direction == Vector2.Zero)
        {
            direction = Player.Facing;
        }

        Projectile projectile = _projectilePool.Rent();
        projectile.Launch(Player.Position + direction * (Player.Radius + 4f), direction);
        _projectiles.Add(projectile);
        ShotsFired++;
        ProjectileFired?.Invoke(this, projectile);
    }

    private void HandleNova(PlayerInput input)
    {
        if (!input.Nova || !Player.TryNova())
        {
            return;
        }

        int hits = NovaBlast.Apply(Player.Position, _enemies, OnEnemyKilled);
        NovaTriggered?.Invoke(this, new NovaEventArgs(Player.Position, NovaBlast.Radius, hits));
    }

    // ------------------------------------------------------------------
    // Spawning
    // ------------------------------------------------------------------

    private void SpawnEnemies(float deltaTime)
    {
        Enemy? spawned = _spawner.Update(deltaTime, Player.Position, _enemies.Count, Bounds);
        if (spawned is not null)
        {
            _enemies.Add(spawned);
        }
    }

    private void SpawnTimedPowerUps(float deltaTime)
    {
        _powerUpTimer -= deltaTime;
        if (_powerUpTimer > 0f)
        {
            return;
        }

        _powerUpTimer = PowerUpSpawnInterval;
        if (_powerUps.Count >= MaxPowerUps)
        {
            return;
        }

        // Somewhere random inside the arena, but not right on top of the player.
        const float inset = 80f;
        Vector2 position = Player.Position;
        for (int attempt = 0; attempt < 10 && GameMath.Distance(position, Player.Position) < 250f; attempt++)
        {
            position = new Vector2(
                GameMath.Lerp(inset, Bounds.Width - inset, _random.NextFloat()),
                GameMath.Lerp(inset, Bounds.Height - inset, _random.NextFloat()));
        }

        _powerUps.Add(new PowerUp(ChoosePowerUpType(_random.NextFloat(), Player.Health.Fraction), position));
    }

    /// <summary>
    /// Chooses a power-up type. Health packs become three times as likely when the player is
    /// below half health, which quietly helps struggling players.
    /// </summary>
    /// <param name="roll">Random number in [0, 1).</param>
    /// <param name="playerHealthFraction">Player health as 0..1.</param>
    public static PowerUpType ChoosePowerUpType(float roll, float playerHealthFraction)
    {
        float healthWeight = playerHealthFraction < 0.5f ? 45f : 15f;
        var options = new List<(PowerUpType, float)>
        {
            (PowerUpType.Health, healthWeight),
            (PowerUpType.Speed, 20f),
            (PowerUpType.Shield, 20f),
            (PowerUpType.RapidFire, 25f),
        };
        return WeightedRandom.Choose(options, roll);
    }

    // ------------------------------------------------------------------
    // Collisions
    // ------------------------------------------------------------------

    private void HandleProjectileEnemyCollisions()
    {
        foreach (Projectile projectile in _projectiles)
        {
            if (!projectile.IsActive)
            {
                continue;
            }

            foreach (Enemy enemy in _enemies)
            {
                if (!enemy.IsActive || !CollisionSystem.Intersects(projectile, enemy))
                {
                    continue;
                }

                bool armoured = enemy is BruteEnemy brute && brute.IsArmouredHit(projectile.Direction);
                float applied = enemy.ApplyHit(projectile.Damage, projectile.Direction);
                enemy.ApplyKnockback(projectile.Direction * (armoured ? 20f : 90f));
                projectile.Deactivate();
                ShotsHit++;

                EnemyHit?.Invoke(this, new EnemyHitEventArgs(enemy, applied, armoured, projectile.Position));
                if (enemy.IsDead)
                {
                    OnEnemyKilled(enemy);
                }

                break; // a projectile can only hit one enemy
            }
        }
    }

    private void HandlePlayerEnemyCollisions()
    {
        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsActive || !CollisionSystem.Intersects(Player, enemy))
            {
                continue;
            }

            // Push the enemy out of the player and bounce it away, hit or not.
            Vector2 away = GameMath.DirectionTo(Player.Position, enemy.Position);
            if (away == Vector2.Zero)
            {
                away = -Player.Facing;
            }

            enemy.Position = Player.Position + away * (Player.Radius + enemy.Radius + 1f);
            enemy.ApplyKnockback(away * ContactKnockback);

            bool shielded = Player.IsShielded;
            HitResult result = Player.TakeHit(enemy.ContactDamage);
            if (result == HitResult.Blocked)
            {
                if (shielded)
                {
                    ShieldBlocked?.Invoke(this, Player.Position);
                }

                continue;
            }

            PlayerHit?.Invoke(this, new PlayerHitEventArgs(result, enemy.ContactDamage, Player.Position));

            if (result == HitResult.LifeLost)
            {
                ClearSpaceAroundPlayer();
            }
            else if (result == HitResult.GameOver)
            {
                IsGameOver = true;
                GameOver?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }

    private void HandlePlayerPowerUpCollisions()
    {
        foreach (PowerUp powerUp in _powerUps)
        {
            if (!powerUp.IsActive || !CollisionSystem.Intersects(Player, powerUp))
            {
                continue;
            }

            IPowerUpEffect effect = Catalog.Get(powerUp.Type);
            effect.Apply(Player);
            Score.RegisterPickup();
            powerUp.Deactivate();
            PowerUpCollected?.Invoke(this, new PowerUpCollectedEventArgs(powerUp.Type, effect.DisplayName, powerUp.Position));
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void OnEnemyKilled(Enemy enemy)
    {
        float multiplier = Score.Multiplier;
        int points = Score.RegisterKill(enemy.ScoreValue);
        EnemiesDefeated++;

        if (_powerUps.Count < MaxPowerUps && _random.NextFloat() < enemy.Stats.PowerUpDropChance)
        {
            PowerUpType type = ChoosePowerUpType(_random.NextFloat(), Player.Health.Fraction);
            _powerUps.Add(new PowerUp(type, Bounds.Clamp(enemy.Position, PowerUp.DefaultRadius)));
        }

        EnemyKilled?.Invoke(this, new EnemyKilledEventArgs(enemy, points, multiplier));
    }

    /// <summary>After losing a life, knock nearby enemies away so the player is not instantly hit again.</summary>
    private void ClearSpaceAroundPlayer()
    {
        foreach (Enemy enemy in _enemies)
        {
            float distance = GameMath.Distance(Player.Position, enemy.Position);
            if (distance < RespawnClearRadius)
            {
                Vector2 away = GameMath.DirectionTo(Player.Position, enemy.Position);
                float strength = GameMath.Lerp(900f, 300f, distance / RespawnClearRadius);
                enemy.ApplyKnockback(away * strength);
            }
        }
    }

    private void RemoveInactiveEntities()
    {
        _enemies.RemoveAll(e => !e.IsActive);
        _powerUps.RemoveAll(p => !p.IsActive);

        // Walk backwards so removing by index does not skip items; return spent projectiles to the pool.
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            if (!_projectiles[i].IsActive)
            {
                _projectilePool.Return(_projectiles[i]);
                _projectiles.RemoveAt(i);
            }
        }
    }

    private static PlayerInput Sanitise(PlayerInput input) => input with
    {
        Move = GameMath.IsFinite(input.Move) ? input.Move : Vector2.Zero,
        Aim = GameMath.IsFinite(input.Aim) ? input.Aim : Vector2.Zero,
    };
}
