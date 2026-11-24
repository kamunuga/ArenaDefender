using System.Numerics;

namespace ArenaDefender.Core.Mathematics;

/// <summary>
/// Pure, stateless maths helpers used throughout the game.
/// Every gameplay calculation that involves vectors goes through this class so that
/// the maths is written once, documented once and unit tested once.
/// </summary>
/// <remarks>
/// The game uses screen coordinates: +X is right and +Y is DOWN.
/// </remarks>
public static class GameMath
{
    /// <summary>Lengths smaller than this are treated as zero to avoid dividing by ~0.</summary>
    public const float Epsilon = 1e-5f;

    // ------------------------------------------------------------------
    // Distance
    // ------------------------------------------------------------------

    /// <summary>
    /// Euclidean distance between two points: √((x₂−x₁)² + (y₂−y₁)²).
    /// Used for pickup magnet range, nova blast radius, spawn safety distance and aim-assist range.
    /// </summary>
    public static float Distance(Vector2 a, Vector2 b) => MathF.Sqrt(DistanceSquared(a, b));

    /// <summary>
    /// Squared distance. Cheaper than <see cref="Distance"/> (no square root) and
    /// enough when we only need to compare distances, e.g. in collision checks.
    /// </summary>
    public static float DistanceSquared(Vector2 a, Vector2 b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return dx * dx + dy * dy;
    }

    // ------------------------------------------------------------------
    // Direction and vectors
    // ------------------------------------------------------------------

    /// <summary>Length (magnitude) of a vector.</summary>
    public static float Length(Vector2 v) => MathF.Sqrt(v.X * v.X + v.Y * v.Y);

    /// <summary>
    /// Returns a unit-length vector pointing the same way as <paramref name="v"/>,
    /// or <see cref="Vector2.Zero"/> when the vector is (almost) zero or contains NaN.
    /// This "safe" version prevents NaN values spreading through the simulation.
    /// </summary>
    public static Vector2 SafeNormalize(Vector2 v)
    {
        float length = Length(v);
        if (float.IsNaN(length) || float.IsInfinity(length) || length < Epsilon)
        {
            return Vector2.Zero;
        }

        return v / length;
    }

    /// <summary>
    /// Unit direction from <paramref name="from"/> towards <paramref name="to"/>
    /// (normalised difference vector). Used by enemies to chase the player and by
    /// the player to aim at the mouse.
    /// </summary>
    public static Vector2 DirectionTo(Vector2 from, Vector2 to) => SafeNormalize(to - from);

    /// <summary>Converts an angle in radians into a unit vector (cos θ, sin θ).</summary>
    public static Vector2 FromAngle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>Converts a vector into its angle in radians (atan2). Used to rotate sprites.</summary>
    public static float ToAngle(Vector2 v) => MathF.Atan2(v.Y, v.X);

    /// <summary>Rotates a vector by <paramref name="radians"/> using the 2D rotation matrix.</summary>
    public static Vector2 Rotate(Vector2 v, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    // ------------------------------------------------------------------
    // Dot and cross products
    // ------------------------------------------------------------------

    /// <summary>
    /// Dot product a·b = aₓbₓ + aᵧbᵧ. For unit vectors this equals cos(angle between them):
    /// 1 = same direction, 0 = perpendicular, −1 = opposite.
    /// </summary>
    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>
    /// 2D cross product (the Z component of the 3D cross product): a × b = aₓbᵧ − aᵧbₓ.
    /// Its SIGN tells us which way to turn to get from <paramref name="a"/> to <paramref name="b"/>:
    /// positive = rotate by a positive angle (clockwise on screen, because +Y points down),
    /// negative = rotate by a negative angle (counter-clockwise on screen), zero = parallel.
    /// </summary>
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// Returns +1, −1 or 0 – which way <paramref name="current"/> must rotate to face <paramref name="desired"/>.
    /// Built on the cross product.
    /// </summary>
    public static int TurnDirection(Vector2 current, Vector2 desired)
    {
        float cross = Cross(current, desired);
        if (MathF.Abs(cross) < Epsilon)
        {
            // Parallel. If they point in opposite directions we still need to turn, pick +1.
            return Dot(current, desired) < 0f ? 1 : 0;
        }

        return cross > 0f ? 1 : -1;
    }

    /// <summary>Angle in radians (0..π) between two vectors, from the dot product.</summary>
    public static float AngleBetween(Vector2 a, Vector2 b)
    {
        Vector2 na = SafeNormalize(a);
        Vector2 nb = SafeNormalize(b);
        if (na == Vector2.Zero || nb == Vector2.Zero)
        {
            return 0f;
        }

        return MathF.Acos(Clamp(Dot(na, nb), -1f, 1f));
    }

    /// <summary>
    /// True when <paramref name="toTarget"/> lies inside a cone of <paramref name="fovDegrees"/>
    /// centred on <paramref name="facing"/>. Uses the dot product: the target is inside the cone when
    /// dot(facing, toTarget) ≥ cos(fov / 2). No expensive acos call is needed.
    /// </summary>
    public static bool IsInFieldOfView(Vector2 facing, Vector2 toTarget, float fovDegrees)
    {
        Vector2 f = SafeNormalize(facing);
        Vector2 t = SafeNormalize(toTarget);
        if (f == Vector2.Zero || t == Vector2.Zero)
        {
            return false;
        }

        float halfAngle = DegreesToRadians(fovDegrees) * 0.5f;
        return Dot(f, t) >= MathF.Cos(halfAngle);
    }

    /// <summary>
    /// Turns <paramref name="currentDirection"/> towards <paramref name="desiredDirection"/>
    /// by at most <paramref name="maxTurnRadians"/>. The cross product decides whether to turn
    /// left or right, the dot product (via <see cref="AngleBetween"/>) decides how far is left.
    /// Used by Runner and Brute enemies so they cannot instantly change direction.
    /// </summary>
    public static Vector2 SteerTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
    {
        Vector2 current = SafeNormalize(currentDirection);
        Vector2 desired = SafeNormalize(desiredDirection);
        if (desired == Vector2.Zero)
        {
            return current;
        }

        if (current == Vector2.Zero)
        {
            return desired;
        }

        float angle = AngleBetween(current, desired);
        if (angle <= maxTurnRadians)
        {
            return desired;
        }

        int turn = TurnDirection(current, desired);
        return SafeNormalize(Rotate(current, turn * maxTurnRadians));
    }

    // ------------------------------------------------------------------
    // Linear interpolation and algebra helpers
    // ------------------------------------------------------------------

    /// <summary>Linear interpolation: a + (b − a)·t. t = 0 gives a, t = 1 gives b.</summary>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>Component-wise linear interpolation between two vectors.</summary>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => new(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));

    /// <summary>
    /// Inverse of lerp: where does <paramref name="value"/> sit between a and b (0..1, unclamped)?
    /// </summary>
    public static float InverseLerp(float a, float b, float value)
    {
        if (MathF.Abs(b - a) < Epsilon)
        {
            return 0f;
        }

        return (value - a) / (b - a);
    }

    /// <summary>
    /// Converts a "sharpness" value into a frame-rate independent lerp factor:
    /// t = 1 − e^(−sharpness·dt). Using a fixed t (e.g. 0.1) every frame would make
    /// smoothing faster on fast computers; this formula gives the same feel at any FPS.
    /// </summary>
    public static float SmoothingFactor(float sharpness, float deltaTime)
    {
        if (sharpness <= 0f || deltaTime <= 0f)
        {
            return 0f;
        }

        return 1f - MathF.Exp(-sharpness * deltaTime);
    }

    /// <summary>Clamps a value into [min, max].</summary>
    public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

    /// <summary>Clamps a value into [0, 1].</summary>
    public static float Clamp01(float value) => Clamp(value, 0f, 1f);

    /// <summary>Degrees → radians.</summary>
    public static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);

    /// <summary>
    /// Circle-vs-circle overlap test: the circles touch when the distance between their
    /// centres is no more than the sum of their radii. Compared squared to avoid a square root.
    /// </summary>
    public static bool CirclesOverlap(Vector2 centreA, float radiusA, Vector2 centreB, float radiusB)
    {
        float radii = radiusA + radiusB;
        return DistanceSquared(centreA, centreB) <= radii * radii;
    }

    /// <summary>True if the vector contains no NaN or infinity values.</summary>
    public static bool IsFinite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
}
