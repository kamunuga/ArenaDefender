using ArenaDefender.Core.World;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ArenaDefender.Input;

/// <summary>
/// Wraps MonoGame's keyboard and mouse state and remembers the previous frame, so we can
/// tell "is held down" apart from "was just pressed this frame".
/// </summary>
public sealed class InputState
{
    private KeyboardState _currentKeys;
    private KeyboardState _previousKeys;
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private bool _isActive;

    /// <summary>Mouse position in screen pixels.</summary>
    public Vector2 MousePosition => new(_currentMouse.X, _currentMouse.Y);

    /// <summary>True if the mouse moved since the last frame.</summary>
    public bool MouseMoved => _currentMouse.X != _previousMouse.X || _currentMouse.Y != _previousMouse.Y;

    /// <summary>Reads the devices. When the window is not focused, all input is ignored.</summary>
    public void Update(bool windowIsActive)
    {
        _previousKeys = _currentKeys;
        _previousMouse = _currentMouse;
        _isActive = windowIsActive;

        if (windowIsActive)
        {
            _currentKeys = Keyboard.GetState();
            _currentMouse = Mouse.GetState();
        }
        else
        {
            _currentKeys = default;
            _currentMouse = new MouseState(_previousMouse.X, _previousMouse.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        }
    }

    /// <summary>True while the key is held.</summary>
    public bool IsKeyDown(Keys key) => _isActive && _currentKeys.IsKeyDown(key);

    /// <summary>True only on the frame the key goes down.</summary>
    public bool IsKeyPressed(Keys key) => _isActive && _currentKeys.IsKeyDown(key) && !_previousKeys.IsKeyDown(key);

    /// <summary>True if any of the keys was just pressed.</summary>
    public bool IsAnyKeyPressed(params Keys[] keys) => keys.Any(IsKeyPressed);

    /// <summary>True while any of the keys is held.</summary>
    public bool IsAnyKeyDown(params Keys[] keys) => keys.Any(IsKeyDown);

    /// <summary>True while the left mouse button is held.</summary>
    public bool IsLeftMouseDown => _isActive && _currentMouse.LeftButton == ButtonState.Pressed;

    /// <summary>True on the frame the left mouse button is clicked.</summary>
    public bool IsLeftMouseClicked =>
        _isActive && _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>True on the frame the right mouse button is clicked.</summary>
    public bool IsRightMouseClicked =>
        _isActive && _currentMouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;

    /// <summary>"Back" action: Escape or P.</summary>
    public bool IsPausePressed => IsAnyKeyPressed(Keys.Escape, Keys.P);

    /// <summary>"Confirm" action: Enter or Space.</summary>
    public bool IsConfirmPressed => IsAnyKeyPressed(Keys.Enter, Keys.Space);

    /// <summary>
    /// Translates keyboard and mouse state into a <see cref="PlayerInput"/> for the game logic.
    /// Movement: WASD or arrow keys. Aim: mouse. Fire: left mouse or Space. Nova: right mouse, E or Shift.
    /// </summary>
    /// <param name="playerWorldPosition">Where the player is in the world.</param>
    /// <param name="camera">The camera, used to convert the mouse position into world coordinates.</param>
    public PlayerInput ToPlayerInput(Vector2 playerWorldPosition, Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        Vector2 move = Vector2.Zero;
        if (IsAnyKeyDown(Keys.W, Keys.Up)) move.Y -= 1f;
        if (IsAnyKeyDown(Keys.S, Keys.Down)) move.Y += 1f;
        if (IsAnyKeyDown(Keys.A, Keys.Left)) move.X -= 1f;
        if (IsAnyKeyDown(Keys.D, Keys.Right)) move.X += 1f;

        Vector2 mouseWorld = camera.ScreenToWorld(MousePosition);
        Vector2 aim = mouseWorld - playerWorldPosition;

        bool fire = IsLeftMouseDown || IsKeyDown(Keys.Space);
        bool nova = IsRightMouseClicked || IsAnyKeyPressed(Keys.E, Keys.LeftShift, Keys.RightShift);

        return new PlayerInput(move.ToCore(), aim.ToCore(), fire, nova);
    }
}
