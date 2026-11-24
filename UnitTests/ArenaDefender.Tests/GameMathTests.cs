using System.Numerics;
using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Tests;

/// <summary>Tests for the maths helpers: distance, vectors, dot, cross and lerp.</summary>
[TestFixture]
public class GameMathTests
{
    private const float Tolerance = 1e-4f;

    // ---------------- Distance ----------------

    [Test]
    public void Distance_Between3_4_5Triangle_Returns5()
    {
        float distance = GameMath.Distance(new Vector2(0, 0), new Vector2(3, 4));

        Assert.That(distance, Is.EqualTo(5f).Within(Tolerance));
    }

    [Test]
    public void DistanceSquared_Between3_4_5Triangle_Returns25()
    {
        Assert.That(GameMath.DistanceSquared(new Vector2(1, 1), new Vector2(4, 5)), Is.EqualTo(25f).Within(Tolerance));
    }

    [Test]
    public void Distance_IsSymmetric()
    {
        var a = new Vector2(-7, 12);
        var b = new Vector2(30, -4);

        Assert.That(GameMath.Distance(a, b), Is.EqualTo(GameMath.Distance(b, a)).Within(Tolerance));
    }

    // ---------------- Direction / normalisation ----------------

    [Test]
    public void DirectionTo_ReturnsUnitVectorPointingAtTarget()
    {
        Vector2 direction = GameMath.DirectionTo(new Vector2(10, 10), new Vector2(10, 50));

        Assert.Multiple(() =>
        {
            Assert.That(GameMath.Length(direction), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(direction.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(direction.Y, Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void SafeNormalize_ZeroVector_ReturnsZeroInsteadOfNaN()
    {
        Vector2 result = GameMath.SafeNormalize(Vector2.Zero);

        Assert.That(result, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void SafeNormalize_NaNVector_ReturnsZero()
    {
        Vector2 result = GameMath.SafeNormalize(new Vector2(float.NaN, 1f));

        Assert.That(result, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Rotate_By90Degrees_TurnsUnitXIntoUnitY()
    {
        Vector2 rotated = GameMath.Rotate(Vector2.UnitX, MathF.PI / 2f);

        Assert.Multiple(() =>
        {
            Assert.That(rotated.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(rotated.Y, Is.EqualTo(1f).Within(Tolerance));
        });
    }

    // ---------------- Dot product ----------------

    [TestCase(1f, 0f, 1f, 0f, 1f)]   // same direction
    [TestCase(1f, 0f, 0f, 1f, 0f)]   // perpendicular
    [TestCase(1f, 0f, -1f, 0f, -1f)] // opposite
    [TestCase(2f, 3f, 4f, 5f, 23f)]  // general case: 2·4 + 3·5
    public void Dot_ReturnsExpectedValue(float ax, float ay, float bx, float by, float expected)
    {
        Assert.That(GameMath.Dot(new Vector2(ax, ay), new Vector2(bx, by)), Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void IsInFieldOfView_TargetStraightAhead_IsVisible()
    {
        Assert.That(GameMath.IsInFieldOfView(Vector2.UnitX, new Vector2(10, 1), 90f), Is.True);
    }

    [Test]
    public void IsInFieldOfView_TargetBehind_IsNotVisible()
    {
        Assert.That(GameMath.IsInFieldOfView(Vector2.UnitX, new Vector2(-10, 0), 90f), Is.False);
    }

    [Test]
    public void IsInFieldOfView_TargetJustOutsideHalfAngle_IsNotVisible()
    {
        // 90° FOV → 45° either side. A target at 50° must be outside.
        Vector2 target = GameMath.FromAngle(GameMath.DegreesToRadians(50f));

        Assert.That(GameMath.IsInFieldOfView(Vector2.UnitX, target, 90f), Is.False);
    }

    // ---------------- Cross product ----------------

    [TestCase(1f, 0f, 0f, 1f, 1)]   // target rotated +90° → turn positive
    [TestCase(1f, 0f, 0f, -1f, -1)] // target rotated −90° → turn negative
    [TestCase(1f, 0f, 5f, 0f, 0)]   // already facing it → no turn
    public void TurnDirection_UsesCrossProductSign(float cx, float cy, float dx, float dy, int expected)
    {
        Assert.That(GameMath.TurnDirection(new Vector2(cx, cy), new Vector2(dx, dy)), Is.EqualTo(expected));
    }

    [Test]
    public void Cross_IsAntiSymmetric()
    {
        var a = new Vector2(2, 7);
        var b = new Vector2(-3, 4);

        Assert.That(GameMath.Cross(a, b), Is.EqualTo(-GameMath.Cross(b, a)).Within(Tolerance));
    }

    [Test]
    public void TurnDirection_OppositeDirections_StillChoosesATurn()
    {
        // Cross product is 0 for opposite vectors, but the enemy must still turn around.
        Assert.That(GameMath.TurnDirection(Vector2.UnitX, -Vector2.UnitX), Is.Not.EqualTo(0));
    }

    [Test]
    public void SteerTowards_LargeAngle_TurnsOnlyByMaximumAmount()
    {
        const float maxTurn = 0.1f;

        Vector2 result = GameMath.SteerTowards(Vector2.UnitX, Vector2.UnitY, maxTurn);

        Assert.Multiple(() =>
        {
            Assert.That(GameMath.AngleBetween(Vector2.UnitX, result), Is.EqualTo(maxTurn).Within(1e-3f));
            Assert.That(GameMath.Cross(Vector2.UnitX, result), Is.GreaterThan(0f), "Should have turned towards +Y");
        });
    }

    [Test]
    public void SteerTowards_SmallAngle_SnapsExactlyToDesiredDirection()
    {
        Vector2 desired = GameMath.FromAngle(0.05f);

        Vector2 result = GameMath.SteerTowards(Vector2.UnitX, desired, 0.2f);

        Assert.That(GameMath.AngleBetween(result, desired), Is.EqualTo(0f).Within(1e-3f));
    }

    // ---------------- Lerp ----------------

    [TestCase(0f, 10f, 0f, 0f)]
    [TestCase(0f, 10f, 0.25f, 2.5f)]
    [TestCase(0f, 10f, 0.5f, 5f)]
    [TestCase(0f, 10f, 1f, 10f)]
    [TestCase(-4f, 4f, 0.5f, 0f)]
    public void Lerp_ReturnsValueAlongTheLine(float a, float b, float t, float expected)
    {
        Assert.That(GameMath.Lerp(a, b, t), Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void Lerp_Vector_HalfwayIsMidpoint()
    {
        Vector2 mid = GameMath.Lerp(new Vector2(0, 0), new Vector2(10, -20), 0.5f);

        Assert.That(mid, Is.EqualTo(new Vector2(5, -10)));
    }

    [Test]
    public void InverseLerp_UndoesLerp()
    {
        float value = GameMath.Lerp(20f, 80f, 0.3f);

        Assert.That(GameMath.InverseLerp(20f, 80f, value), Is.EqualTo(0.3f).Within(Tolerance));
    }

    [Test]
    public void SmoothingFactor_TwoHalfStepsEqualOneFullStep()
    {
        // Frame-rate independence: smoothing at 120 FPS must land where 60 FPS lands.
        const float sharpness = 8f;
        const float dt = 1f / 60f;

        float oneStep = GameMath.Lerp(0f, 100f, GameMath.SmoothingFactor(sharpness, dt));

        float half = GameMath.SmoothingFactor(sharpness, dt / 2f);
        float twoSteps = GameMath.Lerp(0f, 100f, half);
        twoSteps = GameMath.Lerp(twoSteps, 100f, half);

        Assert.That(twoSteps, Is.EqualTo(oneStep).Within(1e-3f));
    }

    [Test]
    public void SmoothingFactor_ZeroDeltaTime_ReturnsZero()
    {
        Assert.That(GameMath.SmoothingFactor(10f, 0f), Is.EqualTo(0f));
    }

    // ---------------- Circle overlap ----------------

    [TestCase(0f, 0f, 10f, 15f, 0f, 5f, true)]   // exactly touching
    [TestCase(0f, 0f, 10f, 14f, 0f, 5f, true)]   // overlapping
    [TestCase(0f, 0f, 10f, 16f, 0f, 5f, false)]  // just apart
    public void CirclesOverlap_DetectsContact(float ax, float ay, float ar, float bx, float by, float br, bool expected)
    {
        Assert.That(GameMath.CirclesOverlap(new Vector2(ax, ay), ar, new Vector2(bx, by), br), Is.EqualTo(expected));
    }
}
