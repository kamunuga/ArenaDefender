namespace ArenaDefender.Core;

/// <summary>
/// The high-level state of the application. The front-end shows a different screen
/// for each state, so the enum replaces fragile boolean flags such as
/// <c>isPaused</c>, <c>isInMenu</c> and <c>isDead</c> that could contradict each other.
/// </summary>
public enum GameState
{
    /// <summary>Title screen with the Play button.</summary>
    MainMenu,

    /// <summary>The simulation is running.</summary>
    Playing,

    /// <summary>The simulation is frozen and the pause overlay is shown.</summary>
    Paused,

    /// <summary>The player has run out of lives.</summary>
    GameOver,
}

/// <summary>Identifies each kind of enemy. Used by the factory, the spawner weights and the renderer.</summary>
public enum EnemyType
{
    /// <summary>Standard enemy: walks straight at the player.</summary>
    Grunt,

    /// <summary>Fast but weak enemy that turns with a limited turn rate (cross product steering).</summary>
    Runner,

    /// <summary>Slow, strong enemy with a frontal armour plate (dot product check).</summary>
    Brute,
}

/// <summary>Every kind of collectible power-up.</summary>
public enum PowerUpType
{
    /// <summary>Restores health instantly.</summary>
    Health,

    /// <summary>Temporarily increases movement speed.</summary>
    Speed,

    /// <summary>Temporarily blocks all incoming damage.</summary>
    Shield,

    /// <summary>Temporarily increases the fire rate.</summary>
    RapidFire,
}

/// <summary>What happened to the player when they were hit. Lets the UI react correctly.</summary>
public enum HitResult
{
    /// <summary>The hit was ignored (invulnerable, shielded or already game over).</summary>
    Blocked,

    /// <summary>Health was reduced but the player is still alive.</summary>
    Damaged,

    /// <summary>Health reached zero and a life was lost; the player respawned.</summary>
    LifeLost,

    /// <summary>Health reached zero on the last life: game over.</summary>
    GameOver,
}
