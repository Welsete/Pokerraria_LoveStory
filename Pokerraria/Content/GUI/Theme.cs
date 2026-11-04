using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Pokerraria.Content.GUI;

/// <summary>
///     The Pokerraria look for the mod's screens, kept light: the blue-indigo panels inherited from Terramon are drawn in
///     the green leather of the journal (same brightness and shade), with dark leather borders. Only panels that belong to
///     the mod's own screens change; Terraria's and other mods' panels stay as they are. The UI textures got the same
///     recoloring once (tools/art/theme_gui.py).
/// </summary>
public class Theme : ModSystem
{
    private const float LeatherHue = 145f / 360f;
    private static readonly Color BorderLeather = new(16, 34, 24);

    public override void Load()
    {
        if (!Main.dedServ)
            On_UIPanel.DrawSelf += DrawThemed;
    }

    private static void DrawThemed(On_UIPanel.orig_DrawSelf orig, UIPanel self, SpriteBatch spriteBatch)
    {
        if (!IsOurs(self))
        {
            orig(self, spriteBatch);
            return;
        }

        var background = self.BackgroundColor;
        var border = self.BorderColor;
        self.BackgroundColor = Leather(background);
        self.BorderColor = border.R < 12 && border.G < 12 && border.B < 12 ? BorderLeather * (border.A / 255f) : Leather(border);
        try
        {
            orig(self, spriteBatch);
        }
        finally
        {
            self.BackgroundColor = background;
            self.BorderColor = border;
        }
    }

    /// <summary>
    ///     Whether the element is part of one of the mod's screens (it or something it's inside is the mod's).
    /// </summary>
    private static bool IsOurs(UIElement element)
    {
        for (var e = element; e != null; e = e.Parent)
        {
            if (e.GetType().Namespace?.StartsWith("Pokerraria") == true)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Blue-violet tones become leather green, a little darker and less saturated; anything else (gold, red, gray,
    ///     the types' colors) stays. Same math as tools/art/theme_gui.py.
    /// </summary>
    public static Color Leather(Color color)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        if (max <= 0f || delta / max < 0.12f || max != b && max != r)
            return color;
        float h;
        if (max == r)
            h = (g - b) / delta % 6f;
        else
            h = (r - g) / delta + 4f;
        h = (h / 6f + 1f) % 1f;
        if (h < 200f / 360f || h > 275f / 360f)
            return color;

        var s = delta / max * 0.7f;
        var v = max * 0.78f;
        var hh = LeatherHue * 6f;
        var i = (int)hh;
        var f = hh - i;
        var p = v * (1 - s);
        var q = v * (1 - s * f);
        var t = v * (1 - s * (1 - f));
        var (nr, ng, nb) = i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
        return new Color(nr, ng, nb, color.A / 255f);
    }
}
