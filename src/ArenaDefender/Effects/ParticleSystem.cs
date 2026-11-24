using ArenaDefender.Core.Mathematics;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Effects;

/// <summary>
/// Purely visual particles (sparks, explosions, trails). They live only in the front-end:
/// the game logic never knows they exist. Colour and size are LERPed from start to end values
/// over each particle's life.
/// </summary>
public sealed class ParticleSystem
{
    /// <summary>Hard limit so a big explosion chain can never slow the game down.</summary>
    public const int MaxParticles = 900;

    private readonly List<Particle> _particles = new(MaxParticles);
    private readonly Random _random = new();

    /// <summary>Number of live particles.</summary>
    public int Count => _particles.Count;

    /// <summary>Emits a burst of particles flying out in random directions.</summary>
    public void Burst(Vector2 position, Color startColor, Color endColor, int count, float minSpeed, float maxSpeed, float size, float life)
    {
        for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
        {
            float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
            float speed = GameMath.Lerp(minSpeed, maxSpeed, (float)_random.NextDouble());
            float lifetime = life * GameMath.Lerp(0.6f, 1.2f, (float)_random.NextDouble());
            Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _particles.Add(new Particle(position, velocity, lifetime, startColor, endColor, size, 0f));
        }
    }

    /// <summary>Emits particles in a cone around a direction (e.g. sparks bouncing off armour).</summary>
    public void Spray(Vector2 position, Vector2 direction, float spreadRadians, Color startColor, Color endColor, int count, float speed, float size, float life)
    {
        float baseAngle = MathF.Atan2(direction.Y, direction.X);
        for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
        {
            float angle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadRadians;
            float s = speed * GameMath.Lerp(0.5f, 1.2f, (float)_random.NextDouble());
            Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * s;
            _particles.Add(new Particle(position, velocity, life, startColor, endColor, size, 0f));
        }
    }

    /// <summary>Adds one particle that stays in place and fades (used for trails).</summary>
    public void Trail(Vector2 position, Color startColor, Color endColor, float size, float life)
    {
        if (_particles.Count < MaxParticles)
        {
            _particles.Add(new Particle(position, Vector2.Zero, life, startColor, endColor, size, 0f));
        }
    }

    /// <summary>Moves and ages all particles and removes the dead ones.</summary>
    public void Update(float deltaTime)
    {
        float damping = GameMath.SmoothingFactor(3f, deltaTime);
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle p = _particles[i];
            p.Age += deltaTime;
            if (p.Age >= p.Lifetime)
            {
                // Swap-remove: O(1) removal because particle order does not matter.
                _particles[i] = _particles[^1];
                _particles.RemoveAt(_particles.Count - 1);
                continue;
            }

            p.Position += p.Velocity * deltaTime;
            p.Velocity = GameMath.Lerp(p.Velocity.ToCore(), System.Numerics.Vector2.Zero, damping).ToXna();
        }
    }

    /// <summary>Draws all particles (inside an active SpriteBatch with the camera transform).</summary>
    public void Draw(ShapeRenderer shapes)
    {
        foreach (Particle p in _particles)
        {
            float t = p.Age / p.Lifetime;
            Color color = ColorUtil.Lerp(p.StartColor, p.EndColor, t);
            float size = GameMath.Lerp(p.StartSize, p.EndSize, t);
            shapes.FillCircle(p.Position, size, color);
        }
    }

    /// <summary>Removes every particle.</summary>
    public void Clear() => _particles.Clear();

    private sealed class Particle
    {
        public Particle(Vector2 position, Vector2 velocity, float lifetime, Color startColor, Color endColor, float startSize, float endSize)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = MathF.Max(0.01f, lifetime);
            StartColor = startColor;
            EndColor = endColor;
            StartSize = startSize;
            EndSize = endSize;
        }

        public Vector2 Position { get; set; }

        public Vector2 Velocity { get; set; }

        public float Age { get; set; }

        public float Lifetime { get; }

        public Color StartColor { get; }

        public Color EndColor { get; }

        public float StartSize { get; }

        public float EndSize { get; }
    }
}
