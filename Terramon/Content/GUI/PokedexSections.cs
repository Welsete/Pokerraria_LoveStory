using System.Text.RegularExpressions;
using Terramon.Content.GUI.Common;
using Terramon.Content.NPCs;
using Terramon.ID;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

/// <summary>
///     A box in the Pokédex overview in the style of the Pokédex concept art: a navy panel with a gold frame and rivets
///     and a red title bar. Subclasses draw what goes inside.
/// </summary>
internal abstract class PokedexSection : UIElement
{
    public const float SectionWidth = 307f;
    protected const float TitleHeight = 24f;
    protected const float Padding = 8f;
    protected const float TextScale = 0.8f;

    public static readonly Color Navy = new(27, 35, 70);
    public static readonly Color NavyLight = new(48, 62, 116);
    public static readonly Color Gold = new(236, 190, 72);
    public static readonly Color GoldDark = new(140, 104, 34);
    public static readonly Color HeaderRed = new(150, 36, 46);
    public static readonly Color LabelColor = new(140, 200, 255);
    public static readonly Color ValueColor = new(255, 232, 150);
    public static readonly Color HintColor = new(160, 168, 196);

    private readonly string _titleKey;

    protected PokedexSection(string titleKey)
    {
        _titleKey = titleKey;
        Width.Set(SectionWidth, 0f);
        Left.Set(25f, 0f);
        MarginTop = 14f;
    }

    /// <summary>The height of one line of text at <see cref="TextScale" />.</summary>
    protected static float Line => FontAssets.MouseText.Value.LineSpacing * TextScale;

    protected static float InnerWidth => SectionWidth - Padding * 2f;

    protected static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.GUI.Pokedex." + key, args);

    /// <summary>Sets the height from <paramref name="contentHeight" /> (the space the content takes).</summary>
    protected void SetContentHeight(float contentHeight)
        => Height.Set(TitleHeight + contentHeight + Padding * 2f, 0f);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var rect = GetDimensions().ToRectangle();
        DrawFrame(spriteBatch, rect, Language.GetTextValue(_titleKey));
        DrawContent(spriteBatch, new Vector2(rect.X + Padding, rect.Y + TitleHeight + Padding));
    }

    protected abstract void DrawContent(SpriteBatch spriteBatch, Vector2 topLeft);

    /// <summary>Draws the navy box with its gold frame, rivets in the corners and a red title bar.</summary>
    public static void DrawFrame(SpriteBatch spriteBatch, Rectangle rect, string title)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, rect, GoldDark);
        var inner = rect;
        inner.Inflate(-2, -2);
        spriteBatch.Draw(pixel, inner, Navy);

        if (!string.IsNullOrEmpty(title))
        {
            var bar = new Rectangle(inner.X, inner.Y, inner.Width, (int)TitleHeight - 2);
            spriteBatch.Draw(pixel, bar, HeaderRed);
            spriteBatch.Draw(pixel, new Rectangle(bar.X, bar.Bottom, bar.Width, 2), GoldDark);
            Utils.DrawBorderString(spriteBatch, title, new Vector2(bar.X + 10, bar.Y + 3), Gold, TextScale);
        }

        DrawRivet(spriteBatch, rect.X + 1, rect.Y + 1);
        DrawRivet(spriteBatch, rect.Right - 5, rect.Y + 1);
        DrawRivet(spriteBatch, rect.X + 1, rect.Bottom - 5);
        DrawRivet(spriteBatch, rect.Right - 5, rect.Bottom - 5);
    }

    private static void DrawRivet(SpriteBatch spriteBatch, int x, int y)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, new Rectangle(x, y, 4, 4), Gold);
        spriteBatch.Draw(pixel, new Rectangle(x, y, 2, 2), Color.White * 0.8f);
    }

    /// <summary>Draws a type as a colored badge with its name in the middle.</summary>
    public static void DrawTypeBadge(SpriteBatch spriteBatch, PokemonType type, Rectangle rect, float scale = 0.6f)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var color = type.GetColor();
        spriteBatch.Draw(pixel, rect, Color.Lerp(color, Color.Black, 0.45f));
        var inner = rect;
        inner.Inflate(-1, -1);
        spriteBatch.Draw(pixel, inner, color);
        var name = Language.GetTextValue($"Mods.Terramon.Types.{type}");
        var size = FontAssets.MouseText.Value.MeasureString(name) * scale;
        if (size.X > rect.Width - 4)
        {
            scale *= (rect.Width - 4) / size.X;
            size = FontAssets.MouseText.Value.MeasureString(name) * scale;
        }

        Utils.DrawBorderString(spriteBatch, name,
            new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f + 3f * scale),
            Color.White, scale);
    }

    /// <summary>Draws <paramref name="text" /> shrunk so it's at most <paramref name="maxWidth" /> wide.</summary>
    protected static void DrawFitted(SpriteBatch spriteBatch, string text, Vector2 position, Color color,
        float maxWidth, float scale = TextScale)
    {
        var width = FontAssets.MouseText.Value.MeasureString(text).X * scale;
        if (width > maxWidth)
            scale *= maxWidth / width;
        Utils.DrawBorderString(spriteBatch, text, position, color, scale);
    }
}

/// <summary>
///     Abilities, catch rate, growth rate, gender ratio, habitat and evolution of a registered Pokémon.
/// </summary>
internal sealed class PokedexInfoSection() : PokedexSection("Mods.Terramon.GUI.Pokedex.Info")
{
    private const float RowGap = 3f;
    private readonly List<(string Label, string[] Lines)> _rows = [];
    private float _labelWidth;

    // The abilities row (-1 if none) and what hovering it shows: every ability with its description
    private int _abilityRow = -1;
    private string _abilityTooltip = string.Empty;

    /// <summary>"SolarPower" -> "Solar Power". Ability names stay in English, like move names.</summary>
    public static string AbilityName(AbilityID ability)
        => Regex.Replace(ability.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    /// <summary>
    ///     Fills the rows for <paramref name="pokemon" />. <paramref name="known" /> says whether the player may see the
    ///     name of another Pokémon (evolutions of ones they haven't seen yet stay "???").
    /// </summary>
    public void SetPokemon(ushort pokemon, Func<ushort, bool> known)
    {
        _rows.Clear();
        _abilityRow = -1;
        _abilityTooltip = string.Empty;
        var schema = Terramon.DatabaseV2.GetPokemon(pokemon);
        if (schema == null)
            return;

        var rows = new List<(string, string)>();

        // Abilities
        var abilities = new List<string>();
        if (schema.Abilities.Ability1 != AbilityID.None)
            abilities.Add(AbilityName(schema.Abilities.Ability1));
        if (schema.Abilities.Ability2 != AbilityID.None)
            abilities.Add(AbilityName(schema.Abilities.Ability2));
        if (schema.Abilities.Hidden != AbilityID.None)
            abilities.Add(Text("HiddenAbility", AbilityName(schema.Abilities.Hidden)));
        if (abilities.Count > 0)
        {
            _abilityRow = rows.Count;
            _abilityTooltip = AbilityTooltip.Build([
                (schema.Abilities.Ability1, false), (schema.Abilities.Ability2, false),
                (schema.Abilities.Hidden, true)
            ]);
            rows.Add((Text("Abilities"), string.Join(" / ", abilities)));
        }

        rows.Add((Text("CatchRate"), $"{schema.CatchRate}/255"));
        rows.Add((Text("GrowthRate"), Text($"GrowthRates.{schema.GrowthRate}")));

        // Gender: the rate is how many eighths are female, -1 for genderless
        if (schema.GenderRatio < 0)
        {
            rows.Add((Text("Gender"), Text("Genderless")));
        }
        else
        {
            var female = schema.GenderRatio * 12.5f;
            rows.Add((Text("Gender"), Text("GenderRatio", $"{100f - female:0.#}", $"{female:0.#}")));
        }

        // Where it spawns (the simple type-based spawning of NPCSpawnController)
        var biomes = NPCSpawnController.GetHabitatBiomes(schema.Types);
        if (biomes.Count > 0)
            rows.Add((Text("Habitat"), string.Join(", ", biomes.Select(b => Text($"Biomes.{b}")))));

        // Evolution by level, both ways
        var evolution = new List<string>();
        foreach (var (id, other) in Terramon.DatabaseV2.Pokemon)
        {
            if (other.Evolution is { AtLevel: > 0 } evo && evo.ID == pokemon && id != pokemon)
                evolution.Add(Text("EvolvesFrom", NameIfKnown(id, known), evo.AtLevel));
        }

        if (schema.Evolution is { AtLevel: > 0 } next && next.ID != pokemon &&
            Terramon.DatabaseV2.GetPokemon(next.ID) != null)
            evolution.Add(Text("EvolvesInto", NameIfKnown(next.ID, known), next.AtLevel));
        if (evolution.Count == 0)
            evolution.Add(Text("NoEvolution"));
        rows.Add((Text("Evolution"), string.Join("\n", evolution)));

        // Lay out: labels in a column, values wrapped next to them
        var font = FontAssets.MouseText.Value;
        _labelWidth = Math.Min(rows.Max(r => font.MeasureString(r.Item1).X * TextScale) + 10f, 110f);
        var valueWidth = InnerWidth - _labelWidth;
        foreach (var (label, value) in rows)
            _rows.Add((label, MoveTooltip.Wrap(value, valueWidth, TextScale).Split('\n')));

        SetContentHeight(_rows.Sum(r => r.Lines.Length * Line) + RowGap * (_rows.Count - 1));
    }

    private static string NameIfKnown(ushort id, Func<ushort, bool> known)
        => known(id) ? Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(id) : "???";

    protected override void DrawContent(SpriteBatch spriteBatch, Vector2 topLeft)
    {
        var y = topLeft.Y;
        for (var i = 0; i < _rows.Count; i++)
        {
            var (label, lines) = _rows[i];

            // Hovering the abilities shows what each one does
            if (i == _abilityRow && _abilityTooltip.Length > 0)
            {
                var row = new Rectangle((int)topLeft.X - 3, (int)y - 1, (int)InnerWidth + 6,
                    (int)(lines.Length * Line) + 1);
                if (IsMouseHovering && row.Contains(Main.MouseScreen.ToPoint()))
                {
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, row, NavyLight);
                    MoveTooltip.Show(_abilityTooltip);
                }
            }

            DrawFitted(spriteBatch, label, new Vector2(topLeft.X, y), LabelColor, _labelWidth - 6f);
            foreach (var line in lines)
            {
                Utils.DrawBorderString(spriteBatch, line, new Vector2(topLeft.X + _labelWidth, y), ValueColor,
                    TextScale);
                y += Line;
            }

            y += RowGap;
        }
    }
}

/// <summary>
///     The base stats of a registered Pokémon as bars, with their total and the EVs it gives when defeated.
/// </summary>
internal sealed class PokedexStatsSection() : PokedexSection("Mods.Terramon.GUI.Pokedex.BaseStats")
{
    private const float RowHeight = 20f;
    private const float LabelWidth = 86f;
    private const float NumberWidth = 34f;

    private static readonly string[] StatKeys = ["HP", "Attack", "Defense", "SpAtk", "SpDef", "Speed"];

    private readonly byte[] _stats = new byte[6];
    private string _evYield = string.Empty;

    public void SetPokemon(ushort pokemon)
    {
        var stats = Terramon.DatabaseV2.GetPokemon(pokemon)?.BaseStats;
        var yields = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            _stats[i] = stats != null && stats.Count > i ? stats[i] : (byte)0;
            var effort = stats != null && stats.Count > i + 6 ? stats[i + 6] : 0;
            if (effort > 0)
                yields.Add($"+{effort} {Text($"Stats.{StatKeys[i]}")}");
        }

        _evYield = yields.Count > 0 ? $"{Text("EVYield")}: {string.Join(", ", yields)}" : string.Empty;
        SetContentHeight(RowHeight * 7 + (_evYield.Length > 0 ? Line + 4f : 0f));
    }

    /// <summary>Red for low stats up to cyan for very high ones, like the stat bars in the games.</summary>
    private static Color StatColor(int value) => value switch
    {
        < 50 => new Color(240, 90, 80),
        < 80 => new Color(245, 160, 60),
        < 100 => new Color(240, 215, 70),
        < 130 => new Color(130, 210, 90),
        _ => new Color(80, 205, 225)
    };

    protected override void DrawContent(SpriteBatch spriteBatch, Vector2 topLeft)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var barX = topLeft.X + LabelWidth + NumberWidth;
        var barWidth = InnerWidth - LabelWidth - NumberWidth;
        var y = topLeft.Y;
        var total = 0;
        for (var i = 0; i < 6; i++)
        {
            int value = _stats[i];
            total += value;
            DrawFitted(spriteBatch, Text($"Stats.{StatKeys[i]}"), new Vector2(topLeft.X, y), LabelColor,
                LabelWidth - 4f);
            Utils.DrawBorderString(spriteBatch, value.ToString(), new Vector2(topLeft.X + LabelWidth, y), Color.White,
                TextScale);

            // Bar: dark track, then the stat out of 200 (only a few Pokémon go above that)
            var track = new Rectangle((int)barX, (int)y + 4, (int)barWidth, 10);
            spriteBatch.Draw(pixel, track, Color.Black * 0.45f);
            var fill = track;
            fill.Width = (int)(barWidth * Math.Clamp(value / 200f, 0.02f, 1f));
            spriteBatch.Draw(pixel, fill, StatColor(value));
            spriteBatch.Draw(pixel, new Rectangle(fill.X, fill.Y, fill.Width, 2), Color.White * 0.3f);
            y += RowHeight;
        }

        DrawFitted(spriteBatch, Text("Total"), new Vector2(topLeft.X, y), Gold, LabelWidth - 4f);
        Utils.DrawBorderString(spriteBatch, total.ToString(), new Vector2(topLeft.X + LabelWidth, y), Gold,
            TextScale);
        y += RowHeight;

        if (_evYield.Length > 0)
            DrawFitted(spriteBatch, _evYield, new Vector2(topLeft.X, y + 4f), HintColor, InnerWidth);
    }
}

/// <summary>
///     The moves a registered Pokémon learns by leveling up: level, name in its type's color, type, category and
///     power. Hovering a move shows its description.
/// </summary>
internal sealed class PokedexLearnsetSection() : PokedexSection("Mods.Terramon.GUI.Pokedex.Moves")
{
    private const float RowHeight = 22f;
    private const float HeaderHeight = 20f;

    // Column positions inside the box
    private const float NameX = 44f;
    private const float TypeX = 168f;
    private const float TypeWidth = 58f;
    private const float CategoryX = 230f;

    private readonly List<(byte Level, MoveID Move)> _moves = [];

    public void SetPokemon(ushort pokemon)
    {
        _moves.Clear();
        var learnset = Terramon.DatabaseV2.GetPokemon(pokemon)?.LevelUpLearnset;
        if (learnset != null)
        {
            foreach (var entry in learnset.OrderBy(e => e.AtLevel).ThenBy(e => e.ID))
            {
                var move = (MoveID)entry.ID;
                if (Terramon.DatabaseV2.GetMove(move) == null || !Enum.IsDefined(move))
                    continue;
                _moves.Add((entry.AtLevel, move));
            }
        }

        SetContentHeight(_moves.Count == 0
            ? Line
            : HeaderHeight + RowHeight * _moves.Count + 4f + Line);
    }

    private static string CategoryShort(MoveCategory category) => Text($"CategoryShort.{category}");

    private static Color CategoryColor(MoveCategory category) => category switch
    {
        MoveCategory.Physical => new Color(255, 150, 90),
        MoveCategory.Special => new Color(120, 170, 255),
        _ => new Color(190, 190, 205)
    };

    protected override void DrawContent(SpriteBatch spriteBatch, Vector2 topLeft)
    {
        if (_moves.Count == 0)
        {
            Utils.DrawBorderString(spriteBatch, Text("NoMoves"), topLeft, HintColor, TextScale);
            return;
        }

        var pixel = TextureAssets.MagicPixel.Value;
        const float headerScale = 0.7f;
        var x = topLeft.X;
        var y = topLeft.Y;

        // Column titles
        Utils.DrawBorderString(spriteBatch, Text("Columns.Level"), new Vector2(x, y), Gold, headerScale);
        Utils.DrawBorderString(spriteBatch, Text("Columns.Move"), new Vector2(x + NameX, y), Gold, headerScale);
        Utils.DrawBorderString(spriteBatch, Text("Columns.Type"), new Vector2(x + TypeX, y), Gold, headerScale);
        Utils.DrawBorderString(spriteBatch, Text("Columns.Category"), new Vector2(x + CategoryX, y), Gold,
            headerScale);
        Utils.DrawBorderString(spriteBatch, Text("Columns.Power"), new Vector2(x + InnerWidth, y), Gold, headerScale,
            1f);
        y += HeaderHeight;

        string hover = null;
        for (var i = 0; i < _moves.Count; i++)
        {
            var (level, move) = _moves[i];
            var schema = Terramon.DatabaseV2.GetMove(move);
            var row = new Rectangle((int)x - 3, (int)y, (int)InnerWidth + 6, (int)RowHeight - 2);

            var hovered = IsMouseHovering && row.Contains(Main.MouseScreen.ToPoint());
            if (hovered)
                hover = MoveTooltip.Build(move);
            if (hovered || i % 2 == 0)
                spriteBatch.Draw(pixel, row, hovered ? NavyLight : Color.Black * 0.18f);

            var textY = y + 2f;
            var levelText = level == 0 ? Text("EvolutionMove") : level.ToString();
            Utils.DrawBorderString(spriteBatch, levelText, new Vector2(x + 2f, textY), Color.White, TextScale);
            DrawFitted(spriteBatch, MoveTooltip.Name(move), new Vector2(x + NameX, textY),
                Color.Lerp(schema.Type.GetColor(), Color.White, 0.25f), TypeX - NameX - 6f);
            DrawTypeBadge(spriteBatch, schema.Type, new Rectangle((int)(x + TypeX), (int)y + 2, (int)TypeWidth, 16));
            DrawFitted(spriteBatch, CategoryShort(schema.Category), new Vector2(x + CategoryX, textY),
                CategoryColor(schema.Category), 36f, 0.7f);
            var power = schema.Power is > 1 ? schema.Power.ToString() : "—";
            Utils.DrawBorderString(spriteBatch, power, new Vector2(x + InnerWidth, textY), Color.White, TextScale, 1f);
            y += RowHeight;
        }

        DrawFitted(spriteBatch, Text("MovesHint"), new Vector2(x, y + 4f), HintColor, InnerWidth, 0.7f);

        if (hover != null)
            MoveTooltip.Show(hover);
    }
}

/// <summary>
///     Shown instead of the data boxes for a Pokémon that was only seen: catching it shows the rest.
/// </summary>
internal sealed class PokedexHintSection() : PokedexSection("Mods.Terramon.GUI.Pokedex.Info")
{
    private string[] _lines = [];

    public void Refresh()
    {
        _lines = MoveTooltip.Wrap(Text("SeenHint"), InnerWidth, TextScale).Split('\n');
        SetContentHeight(_lines.Length * Line);
    }

    protected override void DrawContent(SpriteBatch spriteBatch, Vector2 topLeft)
    {
        foreach (var line in _lines)
        {
            Utils.DrawBorderString(spriteBatch, line, topLeft, HintColor, TextScale);
            topLeft.Y += Line;
        }
    }
}
