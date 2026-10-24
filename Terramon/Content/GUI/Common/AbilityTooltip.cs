using System.Text;
using System.Text.RegularExpressions;
using Terramon.ID;
using Terraria.Localization;

namespace Terramon.Content.GUI.Common;

/// <summary>
///     Names and short descriptions of abilities, for the hover panels of the summary and the Pokédex.
/// </summary>
public static class AbilityTooltip
{
    /// <summary>How wide a description may get before it wraps, in pixels at normal text size.</summary>
    private const float DescriptionWidth = 340f;

    private const string NameHex = "ffdd6e";
    private const string HiddenHex = "a0a8c4";

    /// <summary>"SolarPower" -> "Solar Power". Ability names stay in English, like move names.</summary>
    public static string Name(AbilityID ability)
        => Regex.Replace(ability.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    /// <summary>What the ability does in one or two sentences, or an empty string if it has no description.</summary>
    public static string Description(AbilityID ability)
    {
        var key = $"Mods.Terramon.Abilities.{ability}.Description";
        return Language.Exists(key) ? Language.GetTextValue(key) : string.Empty;
    }

    /// <summary>
    ///     The hover text of one ability: its name (with "hidden ability" when <paramref name="hidden" />) and its
    ///     description, wrapped.
    /// </summary>
    public static string Build(AbilityID ability, bool hidden = false)
    {
        var text = new StringBuilder();
        Append(text, ability, hidden);
        return text.ToString();
    }

    /// <summary>
    ///     The hover text of several abilities, one after the other (e.g. all the abilities a species can have).
    /// </summary>
    public static string Build(IEnumerable<(AbilityID Ability, bool Hidden)> abilities)
    {
        var text = new StringBuilder();
        foreach (var (ability, hidden) in abilities)
        {
            if (ability == AbilityID.None)
                continue;
            if (text.Length > 0)
                text.Append('\n');
            Append(text, ability, hidden);
        }

        return text.ToString();
    }

    private static void Append(StringBuilder text, AbilityID ability, bool hidden)
    {
        text.Append($"[c/{NameHex}:{Name(ability)}]");
        if (hidden)
            text.Append($"  [c/{HiddenHex}:{Language.GetTextValue("Mods.Terramon.GUI.Summary.HiddenAbility")}]");
        var description = Description(ability);
        if (description.Length > 0)
            text.Append('\n').Append(MoveTooltip.Wrap(description, DescriptionWidth));
    }
}
