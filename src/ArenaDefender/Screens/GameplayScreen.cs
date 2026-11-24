using ArenaDefender.Core;
using ArenaDefender.Core.Enemies;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Persistence;
using ArenaDefender.Core.World;
using ArenaDefender.Effects;
using ArenaDefender.Graphics;
using ArenaDefender.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Screens;

/// <summary>
/// The screen where the game is played. It connects the pure game logic (<see cref="GameWorld"/>)
/// to MonoGame:
/// <list type="number">
/// <item>reads the keyboard/mouse and turns it into a <see cref="PlayerInput"/>,</item>
/// <item>calls <see cref="GameWorld.Update"/>,</item>
/// <item>listens to the world's events to create particles, floating text, screen shake and HUD banners,</item>
/// <item>draws the world and the HUD.</item>
/// </list>
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private const float GameOverDelay = 1.8f;
    private const float NovaWaveDuration = 0.45f;

    private readonly GameWorld _world;
    private readonly Camera2D _camera;
    private readonly WorldRenderer _renderer;
    private readonly Hud _hud = new();
    private readonly ParticleSystem _particles = new();
    private readonly FloatingTextSystem _floatingText = new();
    private readonly List<(Vector2 Centre, float Age)> _novaWaves = new();
    private float _gameOverTimer;
    private float _trailTimer;
    private bool _finished;

    /// <summary>Starts a new game.</summary>
    public GameplayScreen(GameContext context)
        : base(context)
    {
        _world = new GameWorld();
        _camera = new Camera2D(Context.ScreenSize);
        _camera.SnapTo(_world.Player.Position.ToXna(), WorldSize);
        _renderer = new WorldRenderer(Context.Shapes, Context.Font);

        _world.EnemyKilled += OnEnemyKilled;
        _world.EnemyHit += OnEnemyHit;
        _world.PlayerHit += OnPlayerHit;
        _world.ShieldBlocked += OnShieldBlocked;
        _world.PowerUpCollected += OnPowerUpCollected;
        _world.ProjectileFired += OnProjectileFired;
        _world.NovaTriggered += OnNovaTriggered;
        _world.WaveStarted += OnWaveStarted;

        _hud.ShowBanner("WAVE 1", "WASD TO MOVE  -  MOUSE TO AIM  -  CLICK TO SHOOT");
    }

    private Vector2 WorldSize => new(_world.Bounds.Width, _world.Bounds.Height);

    /// <inheritdoc />
    public override GameState State => GameState.Playing;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!_world.IsGameOver && Context.Input.IsPausePressed)
        {
            Context.Screens.Push(new PauseScreen(Context));
            return;
        }

        if (!_world.IsGameOver)
        {
            PlayerInput input = Context.Input.ToPlayerInput(_world.Player.Position.ToXna(), _camera);
            _world.Update(deltaTime, input);
            EmitPlayerTrail(deltaTime);
        }
        else
        {
            _gameOverTimer += deltaTime;
            if (_gameOverTimer >= GameOverDelay && !_finished)
            {
                FinishGame();
            }
        }

        _camera.Update(deltaTime, _world.Player.Position.ToXna(), WorldSize);
        _particles.Update(deltaTime);
        _floatingText.Update(deltaTime);
        _hud.Update(deltaTime, _world);

        for (int i = _novaWaves.Count - 1; i >= 0; i--)
        {
            var wave = _novaWaves[i];
            wave.Age += deltaTime;
            if (wave.Age >= NovaWaveDuration)
            {
                _novaWaves.RemoveAt(i);
            }
            else
            {
                _novaWaves[i] = wave;
            }
        }
    }

    /// <inheritdoc />
    public override void Draw()
    {
        SpriteBatch batch = Context.SpriteBatch;
        float time = Context.TotalTime;

        // 1) The world, drawn through the camera transform with smooth (linear) sampling.
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, _camera.Transform);
        _renderer.DrawArena(_world.Bounds, time);
        _renderer.DrawPowerUps(_world.PowerUps, time);
        _renderer.DrawProjectiles(_world.Projectiles);
        _renderer.DrawEnemies(_world.Enemies, time);
        if (!_world.IsGameOver)
        {
            _renderer.DrawPlayer(_world.Player, time);
        }

        foreach ((Vector2 centre, float age) in _novaWaves)
        {
            float t = age / NovaWaveDuration;
            _renderer.DrawNovaWave(centre, GameMath.Lerp(20f, Core.Systems.NovaBlast.Radius, MathF.Sqrt(t)), 1f - t);
        }

        _particles.Draw(Context.Shapes);
        batch.End();

        // 2) Pixel-font text that lives in the world, with crisp point sampling.
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.Transform);
        _renderer.DrawPowerUpIcons(_world.PowerUps, time);
        batch.End();

        // 3) Screen-space UI.
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        _floatingText.Draw(Context.Font, _camera);
        _hud.Draw(Context.Shapes, Context.Font, _world, Context.ScreenSize, time);

        if (_world.IsGameOver)
        {
            float t = GameMath.Clamp01(_gameOverTimer / GameOverDelay);
            Context.Shapes.FillRectangle(new Rectangle(0, 0, Context.ScreenSize.X, Context.ScreenSize.Y), Color.Black * (0.6f * t));
            Context.Font.DrawShadowed("GAME OVER", Context.ScreenCentre - new Vector2(0, 40), ColorUtil.Fade(Palette.HealthLow, t), 10f, TextAlign.Centre);
        }
        else if (Context.Screens.Current == this)
        {
            DrawCrosshair(Context.Input.MousePosition);
        }

        batch.End();
    }

    private void DrawCrosshair(Vector2 position)
    {
        ShapeRenderer shapes = Context.Shapes;
        Color color = _world.Player.FireCooldown.IsReady ? Color.White : Palette.TextDim;
        shapes.DrawCircleOutline(position, 11f, color, 2f, 24);
        shapes.FillRectangle(new Rectangle((int)position.X - 1, (int)position.Y - 1, 3, 3), Palette.Gold);
        shapes.DrawLine(position + new Vector2(-18, 0), position + new Vector2(-8, 0), color, 2f);
        shapes.DrawLine(position + new Vector2(8, 0), position + new Vector2(18, 0), color, 2f);
        shapes.DrawLine(position + new Vector2(0, -18), position + new Vector2(0, -8), color, 2f);
        shapes.DrawLine(position + new Vector2(0, 8), position + new Vector2(0, 18), color, 2f);
    }

    private void EmitPlayerTrail(float deltaTime)
    {
        _trailTimer -= deltaTime;
        if (_trailTimer > 0f || _world.Player.Velocity.Length() < 60f)
        {
            return;
        }

        _trailTimer = 0.03f;
        bool fast = _world.Player.Effects.IsActive(PowerUpType.Speed);
        Color start = fast ? Palette.ForPowerUp(PowerUpType.Speed) * 0.6f : Palette.Player * 0.35f;
        _particles.Trail(_world.Player.Position.ToXna(), start, Color.Transparent, fast ? 12f : 8f, 0.35f);
    }

    private void FinishGame()
    {
        _finished = true;
        var entry = new HighScoreEntry(_world.Score.Score, _world.Difficulty.Wave, _world.ElapsedTime, DateTime.UtcNow);
        int rank = Context.SubmitHighScore(entry);
        var summary = new GameSummary(
            _world.Score.Score,
            _world.Difficulty.Wave,
            _world.ElapsedTime,
            _world.EnemiesDefeated,
            _world.Accuracy,
            rank);
        Context.Screens.Replace(new GameOverScreen(Context, summary));
    }

    // ------------------------------------------------------------------
    // World event handlers: purely visual reactions to what the game logic reports.
    // ------------------------------------------------------------------

    private void OnEnemyKilled(object? sender, EnemyKilledEventArgs e)
    {
        Vector2 position = e.Enemy.Position.ToXna();
        Color color = Palette.ForEnemy(e.Enemy.Type);
        bool brute = e.Enemy.Type == EnemyType.Brute;

        _particles.Burst(position, color, Color.Transparent, brute ? 40 : 16, 60f, brute ? 380f : 260f, brute ? 6f : 4f, 0.6f);
        _particles.Burst(position, Color.White, Color.Transparent, brute ? 10 : 4, 20f, 120f, 3f, 0.3f);

        string text = e.Multiplier > 1f ? $"+{e.Points} X{e.Multiplier:0.##}" : $"+{e.Points}";
        _floatingText.Add(text, position, e.Multiplier > 1f ? Palette.Gold : Color.White, brute ? 3f : 2f);
        _hud.PulseScore();
        _camera.Shake(brute ? 9f : 3f, brute ? 0.3f : 0.12f);
    }

    private void OnEnemyHit(object? sender, EnemyHitEventArgs e)
    {
        if (e.Enemy.IsDead)
        {
            return;
        }

        Vector2 position = e.Position.ToXna();
        if (e.Armoured)
        {
            // Sparks bounce back off the armour towards the shooter.
            Vector2 back = GameMath.DirectionTo(e.Enemy.Position, e.Position).ToXna();
            _particles.Spray(position, back, 1.4f, Palette.Armour, Color.Transparent, 6, 220f, 3f, 0.3f);
            if (Random.Shared.NextDouble() < 0.25)
            {
                _floatingText.Add("BLOCKED", position, Palette.Armour, 2f, 0.6f);
            }
        }
        else
        {
            _particles.Burst(position, Palette.ForEnemy(e.Enemy.Type), Color.Transparent, 5, 40f, 160f, 3f, 0.3f);
        }
    }

    private void OnPlayerHit(object? sender, PlayerHitEventArgs e)
    {
        Vector2 position = e.Position.ToXna();
        _hud.FlashDamage();

        switch (e.Result)
        {
            case HitResult.Damaged:
                _camera.Shake(8f, 0.25f);
                _particles.Burst(position, Palette.HealthLow, Color.Transparent, 12, 60f, 220f, 4f, 0.4f);
                _floatingText.Add($"-{e.Damage:0}", position, Palette.HealthLow, 2f);
                break;

            case HitResult.LifeLost:
                _camera.Shake(14f, 0.5f);
                _particles.Burst(position, Palette.Player, Color.Transparent, 50, 80f, 420f, 5f, 0.8f);
                _novaWaves.Add((position, 0f));
                int lives = _world.Player.Lives;
                _hud.ShowBanner("LIFE LOST", lives == 1 ? "LAST LIFE - BE CAREFUL!" : $"{lives} LIVES LEFT");
                break;

            case HitResult.GameOver:
                _camera.Shake(20f, 0.8f);
                _particles.Burst(position, Palette.Player, Color.Transparent, 90, 80f, 520f, 6f, 1.2f);
                _particles.Burst(position, Color.White, Color.Transparent, 30, 40f, 260f, 4f, 0.8f);
                break;
        }
    }

    private void OnShieldBlocked(object? sender, System.Numerics.Vector2 position)
    {
        _particles.Burst(position.ToXna(), Palette.ForPowerUp(PowerUpType.Shield), Color.Transparent, 6, 60f, 200f, 3f, 0.3f);
    }

    private void OnPowerUpCollected(object? sender, PowerUpCollectedEventArgs e)
    {
        Color color = Palette.ForPowerUp(e.Type);
        _particles.Burst(e.Position.ToXna(), color, Color.Transparent, 24, 40f, 240f, 4f, 0.5f);
        _floatingText.Add(e.DisplayName, e.Position.ToXna() - new Vector2(0, 20), color, 3f, 1.1f);
    }

    private void OnProjectileFired(object? sender, Projectile projectile)
    {
        _particles.Spray(projectile.Position.ToXna(), projectile.Direction.ToXna(), 0.6f, Palette.Projectile, Color.Transparent, 3, 180f, 2.5f, 0.12f);
    }

    private void OnNovaTriggered(object? sender, NovaEventArgs e)
    {
        _novaWaves.Add((e.Position.ToXna(), 0f));
        _camera.Shake(7f, 0.25f);
        _particles.Burst(e.Position.ToXna(), Palette.Accent, Color.Transparent, 40, 300f, 520f, 4f, 0.45f);
        if (e.EnemiesHit > 2)
        {
            _floatingText.Add($"NOVA X{e.EnemiesHit}", e.Position.ToXna() - new Vector2(0, 40), Palette.Accent, 3f);
        }
    }

    private void OnWaveStarted(object? sender, int wave)
    {
        string subtitle = wave switch
        {
            DifficultyManagerWaves.Runner => "RUNNERS INCOMING - SIDESTEP THEM!",
            DifficultyManagerWaves.Brute => "BRUTES INCOMING - SHOOT THEM FROM BEHIND!",
            _ => "ENEMIES GROW STRONGER",
        };
        _hud.ShowBanner($"WAVE {wave}", subtitle);
    }

    /// <summary>Wave numbers that introduce new enemies (kept in sync with the DifficultyManager constants).</summary>
    private static class DifficultyManagerWaves
    {
        public const int Runner = Core.Systems.DifficultyManager.RunnerUnlockWave;
        public const int Brute = Core.Systems.DifficultyManager.BruteUnlockWave;
    }
}

/// <summary>Results of a finished game, passed to the game-over screen.</summary>
/// <param name="Score">Final score.</param>
/// <param name="Wave">Wave reached.</param>
/// <param name="SurvivalSeconds">Time survived.</param>
/// <param name="EnemiesDefeated">Enemies destroyed.</param>
/// <param name="Accuracy">Percentage of shots that hit.</param>
/// <param name="Rank">Leaderboard rank achieved (0 = did not place).</param>
public sealed record GameSummary(int Score, int Wave, float SurvivalSeconds, int EnemiesDefeated, float Accuracy, int Rank);
