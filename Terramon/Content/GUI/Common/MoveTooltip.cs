using System.Text;
using Terramon.ID;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.GUI.Common;

/// <summary>
///     The hover text of a move: its name in its type's color, type and category, power, accuracy and PP, and its
///     description. Shared by the battle move buttons and the Pokédex so moves look the same everywhere.
/// </summary>
public static class MoveTooltip
{
    /// <summary>How wide the description may get before it wraps, in pixels at normal text size.</summary>
    private const float DescriptionWidth = 380f;

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
    ///     The full hover text of a move, for <c>Main.instance.MouseText</c>.
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
