using System.Numerics;
using ArenaDefender.Core.Enemies;

namespace ArenaDefender.Core.World;

/// <summary>Data for <see cref="GameWorld.EnemyKilled"/>.</summary>
public sealed class EnemyKilledEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public EnemyKilledEventArgs(Enemy enemy, int points, float multiplier)
    {
        Enemy = enemy;
        Points = points;
        Multiplier = multiplier;
    }

    /// <summary>The enemy that died.</summary>
    public Enemy Enemy { get; }

    /// <summary>Points awarded.</summary>
    public int Points { get; }

    /// <summary>The combo multiplier that was applied.</summary>
    public float Multiplier { get; }
}

/// <summary>Data for <see cref="GameWorld.EnemyHit"/>.</summary>
public sealed class EnemyHitEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public EnemyHitEventArgs(Enemy enemy, float damage, bool armoured, Vector2 position)
    {
        Enemy = enemy;
        Damage = damage;
        Armoured = armoured;
        Position = position;
    }

    /// <summary>The enemy that was hit.</summary>
    public Enemy Enemy { get; }

    /// <summary>Damage actually dealt.</summary>
    public float Damage { get; }

    /// <summary>True when the hit struck a brute's armour.</summary>
    public bool Armoured { get; }

    /// <summary>Where the hit happened.</summary>
    public Vector2 Position { get; }
}

/// <summary>Data for <see cref="GameWorld.PlayerHit"/>.</summary>
public sealed class PlayerHitEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public PlayerHitEventArgs(HitResult result, float damage, Vector2 position)
    {
        Result = result;
        Damage = damage;
        Position = position;
    }

    /// <summary>What the hit did.</summary>
    public HitResult Result { get; }

    /// <summary>Damage of the attack.</summary>
    public float Damage { get; }

    /// <summary>Player position at the time.</summary>
    public Vector2 Position { get; }
}

/// <summary>Data for <see cref="GameWorld.PowerUpCollected"/>.</summary>
public sealed class PowerUpCollectedEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public PowerUpCollectedEventArgs(PowerUpType type, string displayName, Vector2 position)
    {
        Type = type;
        DisplayName = displayName;
        Position = position;
    }

    /// <summary>The power-up collected.</summary>
    public PowerUpType Type { get; }

    /// <summary>Text to show on screen.</summary>
    public string DisplayName { get; }

    /// <summary>Where it was collected.</summary>
    public Vector2 Position { get; }
}

/// <summary>Data for <see cref="GameWorld.NovaTriggered"/>.</summary>
public sealed class NovaEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public NovaEventArgs(Vector2 position, float radius, int enemiesHit)
    {
        Position = position;
        Radius = radius;
        EnemiesHit = enemiesHit;
    }

    /// <summary>Centre of the blast.</summary>
    public Vector2 Position { get; }

    /// <summary>Blast radius.</summary>
    public float Radius { get; }

    /// <summary>How many enemies were hit.</summary>
    public int EnemiesHit { get; }
}
