using System.Numerics;

namespace ArenaDefender.Core.World;

/// <summary>
/// A snapshot of what the player wants to do this frame, expressed in game terms rather than
/// keys and mouse buttons. The MonoGame layer translates keyboard/mouse state into this struct,
/// so the game logic never depends on MonoGame input classes and can be driven by unit tests.
/// </summary>
/// <param name="Move">Desired movement direction (any length; it is normalised by the player).</param>
/// <param name="Aim">Direction the player is aiming in (any length).</param>
/// <param name="Fire">True while the primary attack button is held.</param>
/// <param name="Nova">True on the frame the nova blast button is pressed.</param>
public readonly record struct PlayerInput(Vector2 Move, Vector2 Aim, bool Fire, bool Nova)
{
    /// <summary>An input with nothing pressed.</summary>
    public static PlayerInput None => new(Vector2.Zero, Vector2.Zero, false, false);
}
