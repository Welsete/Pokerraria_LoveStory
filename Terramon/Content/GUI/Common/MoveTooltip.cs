using System.Text;
using Terramon.ID;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.Common;

/// <summary>
///     The hover text of a move: its name in its type's color, type and category, power, accuracy and PP, and its
///     description. Shared by the battle move buttons and the Pokédex so moves look the same everywhere.
/// </summary>
public static class MoveTooltip
{
    /// <summary>How wide the description may get before it wraps, in pixels at normal text size.</summary>
    private const float DescriptionWidth = 380f;

    /// <summary>The background of the tooltip panel: the mod's navy, like the other tooltips.</summary>
    private static readonly Color PanelColor = new Color(36, 33, 96) * 0.97f;

    private static string _pending;

    /// <summary>
    ///     Shows <paramref name="text" /> (chat tags allowed) in a tooltip panel next to the mouse. The UI that calls
    ///     this draws it with <see cref="DrawPending" /> once everything else of that UI is drawn, so the panel is on top
    ///     of its buttons instead of under them.
    /// </summary>
    public static void Show(string text)
    {
        if (!string.IsNullOrEmpty(text))
            _pending = text;
    }

    /// <summary>
    ///     Draws the tooltip given to <see cref="Show" /> this frame, if any, and clears it.
    /// </summary>
    public static void DrawPending(SpriteBatch spriteBatch)
    {
        if (_pending == null)
            return;
        var text = _pending;
        _pending = null;
        DrawPanel(spriteBatch, text, Main.MouseScreen);
    }

    /// <summary>
    ///     Draws <paramref name="text" /> on a solid navy panel next to <paramref name="mouse" /> (UI coordinates),
    ///     moved so it always stays on screen at any UI scale.
    /// </summary>
    public static void DrawPanel(SpriteBatch spriteBatch, string text, Vector2 mouse)
    {
        const int padX = 10;
        const int padY = 8;
        const float margin = 6f;
        var font = FontAssets.MouseText.Value;
        var size = ChatManager.GetStringSize(font, text, Vector2.One);
        var screen = new Vector2(Main.screenWidth, Main.screenHeight) / Main.UIScale;

        // Below and to the right of the cursor, or to its left / above it near the screen edges
        var pos = mouse + new Vector2(18f, 18f);
        if (pos.X + size.X + padX + margin > screen.X)
            pos.X = mouse.X - size.X - padX - 12f;
        if (pos.Y + size.Y + padY + margin > screen.Y)
            pos.Y = screen.Y - size.Y - padY - margin;
        pos.X = Math.Max(pos.X, padX + margin);
        pos.Y = Math.Max(pos.Y, padY + margin);
        pos = pos.Floor();

        var panel = new Rectangle((int)pos.X - padX, (int)pos.Y - padY, (int)size.X + padX * 2,
            (int)size.Y + padY * 2 - 4);
        Utils.DrawInvBG(spriteBatch, panel, PanelColor);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, pos, Color.White, 0f, Vector2.Zero,
            Vector2.One);
    }

    /// <summary>The move's name ("Thunderbolt").</summary>
    public static string Name(MoveID move)
        => Language.GetTextValue($"Mods.Terramon.Moves.{move}.DisplayName");

    /// <summary>The move's description, or an empty string if it has none.</summary>
    public static string Description(MoveID move)
    {
        var key = $"Mods.Terramon.Moves.{move}.FlavorText";
        return Language.Exists(key) ? Language.GetTextValue(key) : string.Empty;
    }

    /// <summary>
    ///     The full hover text of a move, for <see cref="Show" />.
    ///     With <paramref name="pp" /> (in battle) it shows the PP left out of <paramref name="maxPP" />, otherwise the
    ///     move's PP.
    /// </summary>
    public static string Build(MoveID move, int? pp = null, int? maxPP = null)
    {
        var schema = Terramon.DatabaseV2.GetMove(move);
        if (schema == null)
            return Name(move);

        var power = schema.Power is > 1 ? schema.Power.ToString() : "—";
        var accuracy = schema.Accuracy is { } acc and <= 100 ? acc.ToString() : "—";
        var text = new StringBuilder();
        text.Append($"[c/{schema.Type.GetHexColor()}:{Name(move)}]\n");
        text.Append(Language.GetTextValue($"Mods.Terramon.Types.{schema.Type}"))
            .Append("  ·  ")
            .Append(Language.GetTextValue($"Mods.Terramon.Battle.MoveInfo.{schema.Category}"))
            .Append('\n');
        text.Append(pp.HasValue
            ? Language.GetTextValue("Mods.Terramon.Battle.MoveInfo.Stats", power, accuracy, pp.Value,
                maxPP ?? schema.PP)
            : Language.GetTextValue("Mods.Terramon.GUI.Pokedex.MoveStats", power, accuracy, schema.PP));

        var description = Description(move);
        if (description.Length > 0)
            text.Append('\n').Append(Wrap(description, DescriptionWidth));
        return text.ToString();
    }

    /// <summary>
    ///     Breaks <paramref name="text" /> into lines no wider than <paramref name="maxWidth" /> pixels when drawn with
    ///     the mouse text font at <paramref name="scale" />.
    /// </summary>
    public static string Wrap(string text, float maxWidth, float scale = 1f)
    {
        var font = FontAssets.MouseText.Value;
        var result = new StringBuilder();
        foreach (var paragraph in text.Split('\n'))
        {
            if (result.Length > 0)
                result.Append('\n');
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(candidate).X * scale > maxWidth)
                {
                    result.Append(line).Append('\n');
                    line.Clear().Append(word);
                }
                else
                {
                    line.Clear().Append(candidate);
                }
            }

            result.Append(line);
        }

        return result.ToString();
    }
}
