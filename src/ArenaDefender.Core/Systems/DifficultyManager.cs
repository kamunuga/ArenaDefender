namespace ArenaDefender.Core.Systems;

/// <summary>
/// Turns elapsed time into a wave number and wave number into difficulty settings.
/// All formulas are simple algebra so they can be explained and unit tested:
/// <list type="bullet">
/// <item>wave = 1 + ⌊elapsed ÷ WaveDuration⌋</item>
/// <item>spawn interval = max(MinSpawnInterval, BaseSpawnInterval × 0.88^(wave − 1)) – exponential decay</item>
/// <item>health multiplier = 1 + 0.15 × (wave − 1) – linear growth</item>
/// <item>speed multiplier = min(1.6, 1 + 0.05 × (wave − 1)) – linear growth with a cap</item>
/// <item>max enemies = min(60, 10 + 4 × (wave − 1))</item>
/// </list>
/// </summary>
public sealed class DifficultyManager
{
    /// <summary>Seconds per wave.</summary>
    public const float WaveDuration = 25f;

    /// <summary>Seconds between spawns in wave 1.</summary>
    public const float BaseSpawnInterval = 1.5f;

    /// <summary>The fastest enemies can ever spawn.</summary>
    public const float MinSpawnInterval = 0.3f;

    /// <summary>How much the spawn interval shrinks each wave (0.88 = 12 % faster).</summary>
    public const float SpawnDecay = 0.88f;

    /// <summary>Extra enemy health per wave.</summary>
    public const float HealthGrowthPerWave = 0.15f;

    /// <summary>Extra enemy speed per wave.</summary>
    public const float SpeedGrowthPerWave = 0.05f;

    /// <summary>Highest speed multiplier, so enemies never become impossible to outrun.</summary>
    public const float MaxSpeedMultiplier = 1.6f;

    /// <summary>Wave at which runners start appearing.</summary>
    public const int RunnerUnlockWave = 2;

    /// <summary>Wave at which brutes start appearing.</summary>
    public const int BruteUnlockWave = 3;

    /// <summary>Raised when a new wave begins, with the new wave number.</summary>
    public event Action<int>? WaveStarted;

    /// <summary>Seconds since the game started.</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>The current wave (starts at 1).</summary>
    public int Wave { get; private set; } = 1;

    /// <summary>Seconds left in the current wave.</summary>
    public float TimeUntilNextWave => WaveDuration - (ElapsedTime % WaveDuration);

    /// <summary>Spawn interval for the current wave.</summary>
    public float CurrentSpawnInterval => GetSpawnInterval(Wave);

    /// <summary>Health multiplier for the current wave.</summary>
    public float CurrentHealthMultiplier => GetHealthMultiplier(Wave);

    /// <summary>Speed multiplier for the current wave.</summary>
    public float CurrentSpeedMultiplier => GetSpeedMultiplier(Wave);

    /// <summary>Enemy cap for the current wave.</summary>
    public int CurrentMaxEnemies => GetMaxEnemies(Wave);

    /// <summary>Advances time and raises <see cref="WaveStarted"/> when the wave changes.</summary>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        ElapsedTime += deltaTime;
        int newWave = GetWaveForTime(ElapsedTime);
        if (newWave != Wave)
        {
            Wave = newWave;
            WaveStarted?.Invoke(Wave);
        }
    }

    /// <summary>wave = 1 + ⌊elapsed ÷ WaveDuration⌋.</summary>
    public static int GetWaveForTime(float elapsedSeconds) =>
        1 + (int)MathF.Floor(MathF.Max(0f, elapsedSeconds) / WaveDuration);

    /// <summary>Seconds between spawns for a wave (exponential decay, clamped).</summary>
    public static float GetSpawnInterval(int wave) =>
        MathF.Max(MinSpawnInterval, BaseSpawnInterval * MathF.Pow(SpawnDecay, ClampWave(wave) - 1));

    /// <summary>Enemy health multiplier for a wave.</summary>
    public static float GetHealthMultiplier(int wave) => 1f + HealthGrowthPerWave * (ClampWave(wave) - 1);

    /// <summary>Enemy speed multiplier for a wave, capped at <see cref="MaxSpeedMultiplier"/>.</summary>
    public static float GetSpeedMultiplier(int wave) =>
        MathF.Min(MaxSpeedMultiplier, 1f + SpeedGrowthPerWave * (ClampWave(wave) - 1));

    /// <summary>Maximum number of enemies alive at once in a wave.</summary>
    public static int GetMaxEnemies(int wave) => Math.Min(60, 10 + 4 * (ClampWave(wave) - 1));

    /// <summary>
    /// Relative spawn weights for each enemy type in a wave. Higher weight = more likely.
    /// New enemy types unlock as waves progress and become more common over time.
    /// </summary>
    public static IReadOnlyList<(EnemyType Type, float Weight)> GetSpawnWeights(int wave)
    {
        wave = ClampWave(wave);
        var weights = new List<(EnemyType, float)> { (EnemyType.Grunt, 10f) };

        if (wave >= RunnerUnlockWave)
        {
            weights.Add((EnemyType.Runner, 3f + wave));
        }

        if (wave >= BruteUnlockWave)
        {
            weights.Add((EnemyType.Brute, 1f + wave * 0.5f));
        }

        return weights;
    }

    private static int ClampWave(int wave) => Math.Max(1, wave);
}
