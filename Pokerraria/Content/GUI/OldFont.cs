using System.Text;
using ReLogic.Content;
using Terraria.UI;

namespace Pokerraria.Content.GUI;

/// <summary>
///     The "old book" lettering of the journal's pages (IM Fell English, OFL), drawn from a sheet of letters made by
///     tools/lovestory/make_old_font.py, in ink colors over the paper.
/// </summary>
public sealed class OldFont
{
    private readonly record struct Glyph(Rectangle Source, Vector2 Offset, int Advance);

    private static OldFont _text, _title;

    /// <summary>Body text of the pages.</summary>
    public static OldFont Text => _text ??= new OldFont("OldFont");

    /// <summary>Page titles, in italics.</summary>
    public static OldFont Title => _title ??= new OldFont("OldFontTitle");

    private readonly Asset<Texture2D> _sheet;
    private readonly Dictionary<char, Glyph> _glyphs = [];

    public int LineHeight { get; }

    private OldFont(string name)
    {
        var path = "Assets/GUI/Journal/" + name;
        _sheet = ModContent.Request<Texture2D>("Pokerraria/" + path, AssetRequestMode.ImmediateLoad);
        var lines = Encoding.UTF8.GetString(Pokerraria.Instance.GetFileBytes(path + ".txt")).Split('\n');
        LineHeight = int.Parse(lines[0].Trim());
        foreach (var line in lines.Skip(1))
        {
            var p = line.Trim().Split(' ');
            if (p.Length < 8)
                continue;
            var n = p.Select(int.Parse).ToArray();
            _glyphs[(char)n[0]] = new Glyph(new Rectangle(n[1], n[2], n[3], n[4]), new Vector2(n[5], n[6]), n[7]);
        }
    }

    private Glyph? Get(char c)
    {
        if (_glyphs.TryGetValue(c, out var g))
            return g;
        // Letters the font doesn't have: a close one (dashes, quotes), or a question mark
        return c switch
        {
            '—' or '−' => _glyphs.GetValueOrDefault('–'),
            ' ' => _glyphs.GetValueOrDefault(' '),
            _ => _glyphs.GetValueOrDefault('?')
        };
    }

    public float Measure(string s) => s.Sum(c => Get(c)?.Advance ?? 0);

    /// <summary>
    ///     Breaks the text into lines that fit <paramref name="width" />; line breaks in the text are kept (an empty line
    ///     stays empty).
    /// </summary>
    public List<string> Wrap(string text, float width)
    {
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r", "").Split('\n'))
        {
            var line = "";
            foreach (var word in paragraph.Split(' '))
            {
                var attempt = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Measure(attempt) > width)
                {
                    result.Add(line);
                    line = word;
                }
                else
                    line = attempt;
            }

            result.Add(line);
        }

        return result;
    }

    public void Draw(SpriteBatch spriteBatch, string s, Vector2 position, Color color)
    {
        var x = position.X;
        foreach (var c in s)
        {
            if (Get(c) is not { } g)
                continue;
            spriteBatch.Draw(_sheet.Value, new Vector2(x + g.Offset.X, position.Y + g.Offset.Y).Floor(), g.Source, color);
            x += g.Advance;
        }
    }
}

/// <summary>
///     Text written in ink with an <see cref="OldFont" />, wrapped to a width (its height follows the lines).
/// </summary>
public sealed class InkText : UIElement
{
    private readonly OldFont _font;
    private readonly List<string> _lines;
    private readonly Color _color;
    private readonly bool _centered;

    public InkText(string text, OldFont font, float width, Color color, bool centered = false)
    {
        _font = font;
        _color = color;
        _centered = centered;
        _lines = font.Wrap(text, width);
        Width.Pixels = width;
        Height.Pixels = _lines.Count * font.LineHeight;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var box = GetDimensions();
        for (var i = 0; i < _lines.Count; i++)
        {
            var x = box.X + (_centered ? (box.Width - _font.Measure(_lines[i])) / 2f : 0f);
            _font.Draw(spriteBatch, _lines[i], new Vector2(x, box.Y + i * _font.LineHeight), _color);
        }
    }
}
