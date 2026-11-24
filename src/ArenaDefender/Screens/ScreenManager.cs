using ArenaDefender.Core;

namespace ArenaDefender.Screens;

/// <summary>
/// Abstract base for every screen (menu, gameplay, pause, game over).
/// Each screen decides how it updates and draws itself; the <see cref="ScreenManager"/>
/// only needs this abstraction (polymorphism).
/// </summary>
public abstract class GameScreen
{
    /// <summary>Stores the shared context.</summary>
    protected GameScreen(GameContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Shared services.</summary>
    protected GameContext Context { get; }

    /// <summary>Which high-level game state this screen represents.</summary>
    public abstract GameState State { get; }

    /// <summary>
    /// True if the screen only covers part of the view (like the pause menu), so the
    /// screen underneath should still be drawn.
    /// </summary>
    public virtual bool IsOverlay => false;

    /// <summary>Called when the screen becomes the top screen.</summary>
    public virtual void OnEnter()
    {
    }

    /// <summary>Called when the screen is removed.</summary>
    public virtual void OnExit()
    {
    }

    /// <summary>Updates the screen. Only the top screen is updated.</summary>
    public abstract void Update(float deltaTime);

    /// <summary>Draws the screen.</summary>
    public abstract void Draw();
}

/// <summary>
/// Manages the screens with a <see cref="Stack{T}"/>: the top screen receives input.
/// A stack fits naturally – the pause menu is "pushed" on top of gameplay and "popped" to resume,
/// which keeps the paused game exactly as it was underneath.
/// Changes requested during an update are queued and applied afterwards, so a screen can safely
/// replace itself from inside its own Update method.
/// </summary>
public sealed class ScreenManager
{
    private readonly Stack<GameScreen> _screens = new();
    private readonly Queue<Action> _pendingChanges = new();

    /// <summary>True when there are no screens (the game should close).</summary>
    public bool IsEmpty => _screens.Count == 0 && _pendingChanges.Count == 0;

    /// <summary>The screen currently on top, if any.</summary>
    public GameScreen? Current => _screens.Count > 0 ? _screens.Peek() : null;

    /// <summary>The game state of the top screen (main menu when there is none).</summary>
    public GameState CurrentState => Current?.State ?? GameState.MainMenu;

    /// <summary>Puts a screen on top of the current one.</summary>
    public void Push(GameScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _pendingChanges.Enqueue(() =>
        {
            _screens.Push(screen);
            screen.OnEnter();
        });
    }

    /// <summary>Removes the top screen.</summary>
    public void Pop()
    {
        _pendingChanges.Enqueue(() =>
        {
            if (_screens.Count > 0)
            {
                _screens.Pop().OnExit();
            }

            _screens.TryPeek(out GameScreen? top);
            top?.OnEnter();
        });
    }

    /// <summary>Removes all screens and shows the given one.</summary>
    public void Replace(GameScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _pendingChanges.Enqueue(() =>
        {
            while (_screens.Count > 0)
            {
                _screens.Pop().OnExit();
            }

            _screens.Push(screen);
            screen.OnEnter();
        });
    }

    /// <summary>
    /// Emergency reset used for error recovery: discards queued changes and every screen,
    /// then shows the given screen immediately.
    /// </summary>
    public void ResetTo(GameScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _pendingChanges.Clear();
        _screens.Clear();
        _screens.Push(screen);
        screen.OnEnter();
    }

    /// <summary>Updates the top screen, then applies any queued screen changes.</summary>
    public void Update(float deltaTime)
    {
        ApplyPendingChanges();
        Current?.Update(deltaTime);
        ApplyPendingChanges();
    }

    /// <summary>Draws from the lowest visible screen up to the top one.</summary>
    public void Draw()
    {
        if (_screens.Count == 0)
        {
            return;
        }

        // ToArray returns the top first; walk down until a full-screen (non-overlay) screen is found.
        GameScreen[] screens = _screens.ToArray();
        int start = 0;
        while (start < screens.Length - 1 && screens[start].IsOverlay)
        {
            start++;
        }

        for (int i = start; i >= 0; i--)
        {
            screens[i].Draw();
        }
    }

    /// <summary>Applies all queued Push/Pop/Replace operations in order.</summary>
    public void ApplyPendingChanges()
    {
        while (_pendingChanges.Count > 0)
        {
            _pendingChanges.Dequeue().Invoke();
        }
    }
}
