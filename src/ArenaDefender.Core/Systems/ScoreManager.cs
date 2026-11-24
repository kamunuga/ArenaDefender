using ArenaDefender.Core.Mathematics;

namespace ArenaDefender.Core.Systems;

/// <summary>
/// Keeps the score and the kill combo.
/// <list type="bullet">
/// <item>Kills: points = round(baseValue × multiplier).</item>
/// <item>Combo: every kill within <see cref="ComboWindow"/> seconds of the previous one adds
/// <see cref="ComboStep"/> to the multiplier, up to <see cref="MaxMultiplier"/>.</item>
/// <item>Survival: <see cref="SurvivalPointsPerSecond"/> points for every second alive.</item>
/// <item>Pickups: a flat bonus for each power-up collected.</item>
/// </list>
/// </summary>
public sealed class ScoreManager
{
    /// <summary>Seconds allowed between kills to keep the combo going.</summary>
    public const float ComboWindow = 2.5f;

    /// <summary>Multiplier added per chained kill.</summary>
    public const float ComboStep = 0.25f;

    /// <summary>The highest multiplier a combo can reach.</summary>
    public const float MaxMultiplier = 3f;

    /// <summary>Points earned per second survived.</summary>
    public const float SurvivalPointsPerSecond = 5f;

    /// <summary>Points for collecting a power-up.</summary>
    public const int PickupPoints = 50;

    private float _survivalRemainder;

    /// <summary>Current score.</summary>
    public int Score { get; private set; }

    /// <summary>Number of kills chained in the current combo.</summary>
    public int ComboCount { get; private set; }

    /// <summary>Seconds left before the combo resets.</summary>
    public float ComboTimeRemaining { get; private set; }

    /// <summary>
    /// Current score multiplier: 1 + ComboStep × ComboCount, clamped to <see cref="MaxMultiplier"/>.
    /// </summary>
    public float Multiplier => CalculateMultiplier(ComboCount);

    /// <summary>The multiplier for a given combo count (pure algebra, easy to test).</summary>
    public static float CalculateMultiplier(int comboCount) =>
        GameMath.Clamp(1f + ComboStep * MathF.Max(0, comboCount), 1f, MaxMultiplier);

    /// <summary>Points for a kill at a given multiplier, rounded to the nearest whole number.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a negative base value.</exception>
    public static int CalculateKillPoints(int baseValue, float multiplier)
    {
        if (baseValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, "Score values cannot be negative.");
        }

        return (int)MathF.Round(baseValue * MathF.Max(1f, multiplier), MidpointRounding.AwayFromZero);
    }

    /// <summary>Counts down the combo timer and the survival points.</summary>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (ComboTimeRemaining > 0f)
        {
            ComboTimeRemaining -= deltaTime;
            if (ComboTimeRemaining <= 0f)
            {
                ComboTimeRemaining = 0f;
                ComboCount = 0;
            }
        }

        // Accumulate fractional survival points so none are lost to rounding at high frame rates.
        _survivalRemainder += deltaTime * SurvivalPointsPerSecond;
        int whole = (int)_survivalRemainder;
        if (whole > 0)
        {
            Score += whole;
            _survivalRemainder -= whole;
        }
    }

    /// <summary>Records a kill and returns the points awarded (with the multiplier applied).</summary>
    public int RegisterKill(int baseValue)
    {
        int points = CalculateKillPoints(baseValue, Multiplier);
        Score += points;
        ComboCount++;
        ComboTimeRemaining = ComboWindow;
        return points;
    }

    /// <summary>Records a power-up pickup and returns the points awarded.</summary>
    public int RegisterPickup()
    {
        Score += PickupPoints;
        return PickupPoints;
    }

    /// <summary>Clears the score for a new game.</summary>
    public void Reset()
    {
        Score = 0;
        ComboCount = 0;
        ComboTimeRemaining = 0f;
        _survivalRemainder = 0f;
    }
}
