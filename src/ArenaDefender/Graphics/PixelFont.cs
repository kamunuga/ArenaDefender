using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Graphics;

/// <summary>How text is positioned relative to the given point.</summary>
public enum TextAlign
{
    /// <summary>The point is the left edge.</summary>
    Left,

    /// <summary>The point is the horizontal centre.</summary>
    Centre,

    /// <summary>The point is the right edge.</summary>
    Right,
}

/// <summary>
/// A 5×7 pixel bitmap font defined in code. All glyphs are packed into one texture atlas at
/// start-up and drawn scaled up with point sampling for a crisp retro look. Defining the font
/// in code removes the need for the MonoGame content pipeline and font files.
/// </summary>
public sealed class PixelFont : IDisposable
{
    /// <summary>Glyph width in font pixels.</summary>
    public const int GlyphWidth = 5;

    /// <summary>Glyph height in font pixels.</summary>
    public const int GlyphHeight = 7;

    /// <summary>Horizontal space between characters in font pixels.</summary>
    public const int Spacing = 1;

    /// <summary>Vertical space between lines in font pixels.</summary>
    public const int LineSpacing = 3;

    // Each glyph: 7 rows of 5 characters, '#' = lit pixel.
    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['A'] = ".###.#...##...#######...##...##...#",
        ['B'] = "####.#...##...#####.#...##...#####.",
        ['C'] = ".###.#...##....#....#....#...#.###.",
        ['D'] = "####.#...##...##...##...##...#####.",
        ['E'] = "######....#....####.#....#....#####",
        ['F'] = "######....#....####.#....#....#....",
        ['G'] = ".###.#...##....#.####...##...#.####",
        ['H'] = "#...##...##...#######...##...##...#",
        ['I'] = ".###...#....#....#....#....#...###.",
        ['J'] = "..###...#....#....#....#.#..#..##..",
        ['K'] = "#...##..#.#.#..##...#.#..#..#.#...#",
        ['L'] = "#....#....#....#....#....#....#####",
        ['M'] = "#...###.###.#.##.#.##...##...##...#",
        ['N'] = "#...##...###..##.#.##..###...##...#",
        ['O'] = ".###.#...##...##...##...##...#.###.",
        ['P'] = "####.#...##...#####.#....#....#....",
        ['Q'] = ".###.#...##...##...##.#.##..#..##.#",
        ['R'] = "####.#...##...#####.#.#..#..#.#...#",
        ['S'] = ".#####....#.....###.....#....#####.",
        ['T'] = "#####..#....#....#....#....#....#..",
        ['U'] = "#...##...##...##...##...##...#.###.",
        ['V'] = "#...##...##...##...##...#.#.#...#..",
        ['W'] = "#...##...##...##.#.##.#.##.#.#.#.#.",
        ['X'] = "#...##...#.#.#...#...#.#.#...##...#",
        ['Y'] = "#...##...#.#.#...#....#....#....#..",
        ['Z'] = "#####....#...#...#...#...#....#####",
        ['0'] = ".###.#...##..###.#.###..##...#.###.",
        ['1'] = "..#...##....#....#....#....#...###.",
        ['2'] = ".###.#...#....#...#...#...#...#####",
        ['3'] = "####.....#....#.###.....#....#####.",
        ['4'] = "...#...##..#.#.#..#.#####...#....#.",
        ['5'] = "######....####.....#....##...#.###.",
        ['6'] = "..##..#...#....####.#...##...#.###.",
        ['7'] = "#####....#...#...#...#....#....#...",
        ['8'] = ".###.#...##...#.###.#...##...#.###.",
        ['9'] = ".###.#...##...#.####....#...#..##..",
        [' '] = "...................................",
        ['.'] = "..........................##...##..",
        [','] = ".....................##....#...#...",
        [':'] = "......##...##........##...##.......",
        ['!'] = "..#....#....#....#....#.........#..",
        ['?'] = ".###.#...#....#...#...#.........#..",
        ['-'] = "...............#####...............",
        ['+'] = ".......#....#..#####..#....#.......",
        ['/'] = "....#...#....#...#...#....#...#....",
        ['%'] = "##..###..#...#...#...#...#..###..##",
        ['('] = "...#...#...#....#....#.....#.....#.",
        [')'] = ".#.....#.....#....#....#...#...#...",
        ['\''] = "..#....#...#.......................",
        ['>'] = ".#.....#.....#.....#...#...#...#...",
        ['<'] = "...#...#...#...#.....#.....#.....#.",
        ['='] = "..........#####.....#####..........",
        ['*'] = ".....#.#.#.###.#####.###.#.#.#.....",
        ['#'] = ".#.#..#.#.#####.#.#.#####.#.#..#.#.",
        ['_'] = "..............................#####",
        ['['] = ".###..#....#....#....#....#....###.",
        [']'] = ".###....#....#....#....#....#..###.",
    };

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _atlas;
    private readonly Dictionary<char, Rectangle> _sourceRectangles = new();

    /// <summary>Builds the glyph atlas texture.</summary>
    /// <exception cref="InvalidOperationException">Thrown if a glyph definition has the wrong length.</exception>
    public PixelFont(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));

        int cellWidth = GlyphWidth + 1; // one empty column between glyphs avoids bleeding
        int atlasWidth = Glyphs.Count * cellWidth;
        var data = new Color[atlasWidth * GlyphHeight];

        int index = 0;
        foreach ((char character, string pattern) in Glyphs)
        {
            if (pattern.Length != GlyphWidth * GlyphHeight)
            {
                throw new InvalidOperationException(
                    $"Glyph '{character}' has {pattern.Length} pixels; expected {GlyphWidth * GlyphHeight}.");
            }

            int originX = index * cellWidth;
            for (int y = 0; y < GlyphHeight; y++)
            {
                for (int x = 0; x < GlyphWidth; x++)
                {
                    if (pattern[y * GlyphWidth + x] == '#')
                    {
                        data[y * atlasWidth + originX + x] = Color.White;
                    }
                }
            }

            _sourceRectangles[character] = new Rectangle(originX, 0, GlyphWidth, GlyphHeight);
            index++;
        }

        _atlas = new Texture2D(graphicsDevice, atlasWidth, GlyphHeight);
        _atlas.SetData(data);
    }

    /// <summary>Size of the text in screen pixels at the given scale.</summary>
    public Vector2 Measure(string text, float scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        string[] lines = text.Split('\n');
        int longest = lines.Max(l => l.Length);
        float width = (longest * (GlyphWidth + Spacing) - Spacing) * scale;
        float height = (lines.Length * (GlyphHeight + LineSpacing) - LineSpacing) * scale;
        return new Vector2(MathF.Max(0f, width), height);
    }

    /// <summary>Draws text. Must be called between SpriteBatch.Begin and End (use point sampling).</summary>
    /// <param name="text">Text to draw; lower-case letters are drawn as capitals, unknown characters as '?'.</param>
    /// <param name="position">Anchor position (top of the text).</param>
    /// <param name="color">Text colour.</param>
    /// <param name="scale">Size of one font pixel in screen pixels.</param>
    /// <param name="align">Horizontal alignment relative to <paramref name="position"/>.</param>
    public void Draw(string text, Vector2 position, Color color, float scale = 2f, TextAlign align = TextAlign.Left)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float y = position.Y;
        foreach (string line in text.Split('\n'))
        {
            float width = Measure(line, scale).X;
            float x = align switch
            {
                TextAlign.Centre => position.X - width / 2f,
                TextAlign.Right => position.X - width,
                _ => position.X,
            };

            // Snap to whole pixels so the glyphs stay crisp.
            x = MathF.Round(x);
            float lineY = MathF.Round(y);

            foreach (char raw in line)
            {
                char c = char.ToUpperInvariant(raw);
                if (!_sourceRectangles.TryGetValue(c, out Rectangle source))
                {
                    source = _sourceRectangles['?'];
                }

                if (c != ' ')
                {
                    _spriteBatch.Draw(_atlas, new Vector2(x, lineY), source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }

                x += (GlyphWidth + Spacing) * scale;
            }

            y += (GlyphHeight + LineSpacing) * scale;
        }
    }

    /// <summary>Draws text with a dark drop shadow for readability over busy backgrounds.</summary>
    public void DrawShadowed(string text, Vector2 position, Color color, float scale = 2f, TextAlign align = TextAlign.Left)
    {
        float offset = MathF.Max(1f, scale * 0.5f);
        Draw(text, position + new Vector2(offset, offset), Color.Black * (color.A / 255f) * 0.7f, scale, align);
        Draw(text, position, color, scale, align);
    }

    /// <inheritdoc />
    public void Dispose() => _atlas.Dispose();
}
