using ArenaDefender.Core.Mathematics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Graphics;

/// <summary>
/// A 2D camera that smoothly follows a target. Each frame its position is LERPed towards the
/// target with a frame-rate independent factor, so it trails slightly behind the player instead
/// of being rigidly attached. Also provides screen shake and screen ↔ world conversions.
/// </summary>
public sealed class Camera2D
{
    /// <summary>How quickly the camera catches up with its target.</summary>
    public const float FollowSharpness = 6f;

    private readonly Random _random = new();
    private float _shakeIntensity;
    private float _shakeTime;
    private float _shakeDuration;
    private Vector2 _shakeOffset;

    /// <summary>Creates a camera for a viewport of the given size.</summary>
    public Camera2D(Point viewportSize)
    {
        ViewportSize = viewportSize;
    }

    /// <summary>The world point at the centre of the screen.</summary>
    public Vector2 Position { get; set; }

    /// <summary>The size of the screen in pixels.</summary>
    public Point ViewportSize { get; }

    /// <summary>World → screen transformation matrix, used by SpriteBatch.Begin.</summary>
    public Matrix Transform =>
        Matrix.CreateTranslation(-MathF.Round(Position.X + _shakeOffset.X), -MathF.Round(Position.Y + _shakeOffset.Y), 0f) *
        Matrix.CreateTranslation(ViewportSize.X / 2f, ViewportSize.Y / 2f, 0f);

    /// <summary>Moves the camera towards the target, keeping the view inside the arena.</summary>
    /// <param name="deltaTime">Elapsed seconds.</param>
    /// <param name="target">World point to follow.</param>
    /// <param name="worldSize">Size of the arena (the camera will not show much beyond it).</param>
    public void Update(float deltaTime, Vector2 target, Vector2 worldSize)
    {
        Vector2 clampedTarget = ClampToWorld(target, worldSize);
        Position = GameMath.Lerp(Position.ToCore(), clampedTarget.ToCore(), GameMath.SmoothingFactor(FollowSharpness, deltaTime)).ToXna();
        UpdateShake(deltaTime);
    }

    /// <summary>Jumps straight to a point (used when a game starts).</summary>
    public void SnapTo(Vector2 target, Vector2 worldSize) => Position = ClampToWorld(target, worldSize);

    /// <summary>Starts a screen shake. A stronger shake replaces a weaker one.</summary>
    public void Shake(float intensity, float duration)
    {
        if (intensity >= _shakeIntensity * (_shakeDuration <= 0f ? 0f : _shakeTime / _shakeDuration))
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeTime = duration;
        }
    }

    /// <summary>Converts a screen position (e.g. the mouse) into world coordinates.</summary>
    public Vector2 ScreenToWorld(Vector2 screenPosition) => Vector2.Transform(screenPosition, Matrix.Invert(Transform));

    /// <summary>Converts a world position into screen coordinates.</summary>
    public Vector2 WorldToScreen(Vector2 worldPosition) => Vector2.Transform(worldPosition, Transform);

    private Vector2 ClampToWorld(Vector2 target, Vector2 worldSize)
    {
        const float margin = 60f; // allow a little of the outside to show, so the border is visible
        float halfW = ViewportSize.X / 2f;
        float halfH = ViewportSize.Y / 2f;

        float x = worldSize.X + 2 * margin <= ViewportSize.X
            ? worldSize.X / 2f
            : Math.Clamp(target.X, halfW - margin, worldSize.X - halfW + margin);
        float y = worldSize.Y + 2 * margin <= ViewportSize.Y
            ? worldSize.Y / 2f
            : Math.Clamp(target.Y, halfH - margin, worldSize.Y - halfH + margin);
        return new Vector2(x, y);
    }

    private void UpdateShake(float deltaTime)
    {
        if (_shakeTime <= 0f)
        {
            _shakeOffset = Vector2.Zero;
            return;
        }

        _shakeTime = MathF.Max(0f, _shakeTime - deltaTime);

        // Shake strength fades out linearly over the duration.
        float strength = _shakeIntensity * (_shakeDuration > 0f ? _shakeTime / _shakeDuration : 0f);
        _shakeOffset = new Vector2(
            (float)(_random.NextDouble() * 2 - 1) * strength,
            (float)(_random.NextDouble() * 2 - 1) * strength);
    }
}
