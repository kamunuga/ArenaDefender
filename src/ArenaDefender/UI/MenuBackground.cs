using ArenaDefender.Core;
using ArenaDefender.Graphics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.UI;

/// <summary>Animated decoration behind the menus: a grid and enemy shapes circling the centre.</summary>
public sealed class MenuBackground
{
    private readonly (float Radius, float Speed, float Phase, EnemyType Type, float Size)[] _orbiters;

    /// <summary>Creates the background with a fixed random layout.</summary>
    public MenuBackground()
    {
        var random = new Random(7);
        var types = new[] { EnemyType.Grunt, EnemyType.Runner, EnemyType.Brute };
        _orbiters = new (float, float, float, EnemyType, float)[18];
        for (int i = 0; i < _orbiters.Length; i++)
        {
            EnemyType type = types[i % types.Length];
            float size = type switch { EnemyType.Brute => 26f, EnemyType.Runner => 11f, _ => 15f };
            _orbiters[i] = (
                Radius: 220f + (float)random.NextDouble() * 380f,
                Speed: (0.08f + (float)random.NextDouble() * 0.15f) * (i % 2 == 0 ? 1 : -1),
                Phase: (float)(random.NextDouble() * MathHelper.TwoPi),
                Type: type,
                Size: size);
        }
    }

    /// <summary>Draws the background.</summary>
    public void Draw(ShapeRenderer shapes, Point screen, float time)
    {
        for (int x = 0; x < screen.X; x += 80)
        {
            shapes.FillRectangle(new Rectangle(x, 0, 1, screen.Y), Palette.Grid * 0.6f);
        }

        for (int y = 0; y < screen.Y; y += 80)
        {
            shapes.FillRectangle(new Rectangle(0, y, screen.X, 1), Palette.Grid * 0.6f);
        }

        Vector2 centre = new(screen.X / 2f, screen.Y / 2f);
        foreach (var orbiter in _orbiters)
        {
            float angle = orbiter.Phase + time * orbiter.Speed * MathHelper.TwoPi;
            Vector2 position = centre + new Vector2(MathF.Cos(angle) * orbiter.Radius * 1.4f, MathF.Sin(angle) * orbiter.Radius * 0.7f);
            Color color = Palette.ForEnemy(orbiter.Type) * 0.22f;

            if (orbiter.Type == EnemyType.Runner)
            {
                float heading = angle + (orbiter.Speed > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2);
                shapes.FillTriangle(position, orbiter.Size * 2.6f, heading, color);
            }
            else
            {
                shapes.FillCircle(position, orbiter.Size, color);
            }
        }

        // Darken the middle a little so the title and buttons stand out.
        shapes.FillRectangle(new Rectangle(0, 0, screen.X, screen.Y), Palette.Background * 0.35f);
    }
}
