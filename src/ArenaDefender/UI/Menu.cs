using ArenaDefender.Core.Mathematics;
using ArenaDefender.Graphics;
using ArenaDefender.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ArenaDefender.UI;

/// <summary>A clickable button with an animated hover effect.</summary>
public sealed class Button
{
    private float _highlight;

    /// <summary>Creates a button.</summary>
    public Button(string label, Rectangle bounds, Action onClick)
    {
        Label = label;
        Bounds = bounds;
        OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
    }

    /// <summary>Text on the button.</summary>
    public string Label { get; }

    /// <summary>Screen rectangle.</summary>
    public Rectangle Bounds { get; }

    /// <summary>What happens when the button is activated.</summary>
    public Action OnClick { get; }

    /// <summary>
    /// Animates the highlight. The highlight amount is LERPed towards 1 when selected and 0 when not,
    /// which gives a smooth fade instead of an instant colour change.
    /// </summary>
    public void Update(float deltaTime, bool selected)
    {
        _highlight = GameMath.Lerp(_highlight, selected ? 1f : 0f, GameMath.SmoothingFactor(14f, deltaTime));
    }

    /// <summary>True if the point lies over the button.</summary>
    public bool Contains(Vector2 point) => Bounds.Contains((int)point.X, (int)point.Y);

    /// <summary>Draws the button.</summary>
    public void Draw(ShapeRenderer shapes, PixelFont font, float opacity)
    {
        // Grow slightly when highlighted.
        int grow = (int)MathF.Round(GameMath.Lerp(0f, 6f, _highlight));
        Rectangle rect = new(Bounds.X - grow, Bounds.Y - grow / 2, Bounds.Width + grow * 2, Bounds.Height + grow);

        Color fill = ColorUtil.Lerp(Palette.Panel, new Color(30, 70, 110), _highlight);
        Color border = ColorUtil.Lerp(new Color(50, 70, 110), Palette.Accent, _highlight);
        Color text = ColorUtil.Lerp(Palette.TextDim, Color.White, _highlight);

        shapes.FillRectangle(rect, ColorUtil.Fade(fill, opacity));
        shapes.DrawRectangle(rect, ColorUtil.Fade(border, opacity), 2);
        float scale = 3f;
        Vector2 textSize = font.Measure(Label, scale);
        font.Draw(Label, new Vector2(rect.Center.X, rect.Center.Y - textSize.Y / 2f), ColorUtil.Fade(text, opacity), scale, TextAlign.Centre);
    }
}

/// <summary>
/// A vertical list of buttons usable with the mouse (hover + click) or the keyboard
/// (W/S or arrows to move, Enter or Space to choose).
/// </summary>
public sealed class Menu
{
    private readonly List<Button> _buttons = new();

    /// <summary>Index of the highlighted button.</summary>
    public int SelectedIndex { get; private set; }

    /// <summary>The buttons in order.</summary>
    public IReadOnlyList<Button> Buttons => _buttons;

    /// <summary>
    /// Creates buttons stacked vertically and centred on <paramref name="centreX"/>.
    /// </summary>
    public static Menu CreateVertical(float centreX, float top, int width, int height, int gap, params (string Label, Action OnClick)[] items)
    {
        var menu = new Menu();
        for (int i = 0; i < items.Length; i++)
        {
            var rect = new Rectangle((int)(centreX - width / 2f), (int)top + i * (height + gap), width, height);
            menu._buttons.Add(new Button(items[i].Label, rect, items[i].OnClick));
        }

        return menu;
    }

    /// <summary>Handles input and animation. Invokes a button's action when it is chosen.</summary>
    public void Update(float deltaTime, InputState input)
    {
        if (_buttons.Count == 0)
        {
            return;
        }

        if (input.IsAnyKeyPressed(Keys.Down, Keys.S, Keys.Tab))
        {
            SelectedIndex = (SelectedIndex + 1) % _buttons.Count;
        }
        else if (input.IsAnyKeyPressed(Keys.Up, Keys.W))
        {
            SelectedIndex = (SelectedIndex - 1 + _buttons.Count) % _buttons.Count;
        }

        int hovered = _buttons.FindIndex(b => b.Contains(input.MousePosition));
        if (hovered >= 0 && input.MouseMoved)
        {
            SelectedIndex = hovered;
        }

        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].Update(deltaTime, i == SelectedIndex);
        }

        if (hovered >= 0 && input.IsLeftMouseClicked)
        {
            SelectedIndex = hovered;
            _buttons[hovered].OnClick();
        }
        else if (input.IsConfirmPressed)
        {
            _buttons[SelectedIndex].OnClick();
        }
    }

    /// <summary>Draws all buttons.</summary>
    public void Draw(ShapeRenderer shapes, PixelFont font, float opacity = 1f)
    {
        foreach (Button button in _buttons)
        {
            button.Draw(shapes, font, opacity);
        }
    }
}
