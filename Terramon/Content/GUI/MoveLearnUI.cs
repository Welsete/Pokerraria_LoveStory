using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

/// <summary>
///     Asks the player which move to forget when a Pokémon with four moves wants to learn a new one.
/// </summary>
public sealed class MoveLearnUI : SmartUIState
{
    private const float WindowWidth = 480f;
    private const float ButtonHeight = 48f;

    private static PokemonData _pokemon;
    private static MoveID _move;
    private static bool _dirty;

    private UIPanel _window;
    private UIText _description;

    public override bool Visible => _pokemon != null;

    public static bool IsOpen => _pokemon != null;

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.MoveLearn." + key, args);

    public static string MoveName(MoveID move)
        => Language.GetTextValue($"Mods.Terramon.Moves.{move}.DisplayName");

    /// <summary>
    ///     "Fire · Special · Power 90 · Acc. 100 · PP 15/15", like on the battle move buttons.
    /// </summary>
    private static string MoveDetails(MoveID move, int? pp = null)
    {
        var schema = Terramon.DatabaseV2.GetMove(move);
        if (schema == null)
            return string.Empty;
        return Language.GetTextValue($"Mods.Terramon.Types.{schema.Type}") + "  ·  " +
               Language.GetTextValue($"Mods.Terramon.Battle.MoveInfo.{schema.Category}") + "  ·  " +
               Language.GetTextValue("Mods.Terramon.Battle.MoveInfo.Stats",
                   schema.Power is > 1 ? schema.Power.ToString() : "—",
                   schema.Accuracy is { } acc and <= 100 ? acc.ToString() : "—",
                   pp ?? schema.PP, schema.PP);
    }

    private static string Description(MoveID move)
        => Language.GetTextValue($"Mods.Terramon.Moves.{move}.FlavorText");

    public static void Open(PokemonData pokemon, MoveID move)
    {
        _pokemon = pokemon;
        _move = move;
        _dirty = true;
        SoundEngine.PlaySound(SoundID.MenuOpen);
        Main.NewText(Text("Wants", pokemon.DisplayName, MoveName(move)), new Color(120, 200, 255));
    }

    public static void Close()
    {
        _pokemon = null;
        _dirty = true;
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
        => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

    public override void OnInitialize()
    {
        _window = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.95f,
            HAlign = 0.5f,
            VAlign = 0.4f
        };
        _window.Width.Pixels = WindowWidth;
    }

    private void Rebuild()
    {
        _dirty = false;
        _window.RemoveAllChildren();
        if (_window.Parent != null)
            _window.Remove();

        var pokemon = _pokemon;
        if (pokemon == null)
            return;

        var title = new UIText(Text("Prompt", pokemon.DisplayName, MoveName(_move)), 0.85f)
        {
            IsWrapped = true
        };
        title.Width.Set(0f, 1f);
        title.Height.Pixels = 44f;
        _window.Append(title);

        var top = 50f;

        // The new move, highlighted
        AddButton(MoveName(_move), MoveDetails(_move), top, null, _move, new Color(46, 120, 86) * 0.9f);
        top += ButtonHeight + 10f;

        var forgetLabel = new UIText(Text("ForgetWhich"), 0.8f);
        forgetLabel.Top.Pixels = top;
        _window.Append(forgetLabel);
        top += 24f;

        for (var i = 0; i < 4; i++)
        {
            var current = pokemon.Moves[i];
            var slot = i;
            if (current.ID == MoveID.None)
                AddButton("---", string.Empty, top, () => Replace(slot), MoveID.None);
            else
                AddButton(MoveName(current.ID), MoveDetails(current.ID, current.PP), top, () => Replace(slot), current.ID);
            top += ButtonHeight + 4f;
        }

        // Description of the move under the mouse (the new move's until then)
        top += 4f;
        _description = new UIText(Description(_move), 0.8f) { IsWrapped = true };
        _description.Width.Set(0f, 1f);
        _description.Top.Pixels = top;
        _description.Height.Pixels = 60f;
        _window.Append(_description);
        top += 64f;

        var decline = new UITextPanel<string>(Text("DontLearn"), 0.85f)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.7f
        };
        decline.Width.Set(0f, 1f);
        decline.Height.Pixels = 34f;
        decline.Top.Pixels = top;
        decline.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
        decline.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = new Color(63, 82, 151) * 0.7f;
        decline.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Decline();
        };
        _window.Append(decline);
        top += 34f;

        _window.Height.Pixels = top + _window.PaddingTop + _window.PaddingBottom;
        Append(_window);
        Recalculate();
    }

    /// <summary>
    ///     A move row: its name, its details below, and its description shown while hovered. Without a click action
    ///     it's only information (the new move).
    /// </summary>
    private void AddButton(string name, string details, float top, Action onClick, MoveID move, Color? color = null)
    {
        var normal = color ?? new Color(63, 82, 151) * 0.7f;
        var button = new UIPanel { BackgroundColor = normal };
        button.Width.Set(0f, 1f);
        button.Height.Pixels = ButtonHeight;
        button.Top.Pixels = top;
        button.SetPadding(6f);
        button.Append(new UIText(name, 0.9f) { HAlign = 0.5f, IgnoresMouseInteraction = true });
        button.Append(new UIText(details, 0.75f)
        {
            HAlign = 0.5f, VAlign = 1f, TextColor = new Color(200, 210, 235), IgnoresMouseInteraction = true
        });
        button.OnMouseOver += (_, el) =>
        {
            if (onClick != null)
                ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
            if (move != MoveID.None)
                _description?.SetText(Description(move));
        };
        button.OnMouseOut += (_, el) =>
        {
            ((UIPanel)el).BackgroundColor = normal;
            _description?.SetText(Description(_move));
        };
        if (onClick != null)
        {
            button.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                onClick();
            };
        }

        _window.Append(button);
    }

    private static void Replace(int slot)
    {
        var pokemon = _pokemon;
        if (pokemon == null)
            return;

        var old = pokemon.Moves[slot].ID;
        pokemon.SetMove(slot, _move);
        pokemon.PendingMoves.Remove(_move);
        Main.NewText(old == MoveID.None
                ? Text("Learned", pokemon.DisplayName, MoveName(_move))
                : Text("Forgot", pokemon.DisplayName, MoveName(old), MoveName(_move)),
            new Color(50, 255, 130));
        SoundEngine.PlaySound(SoundID.Item4);
        Close();
    }

    private static void Decline()
    {
        var pokemon = _pokemon;
        if (pokemon == null)
            return;

        pokemon.PendingMoves.Remove(_move);
        Main.NewText(Text("DidNotLearn", pokemon.DisplayName, MoveName(_move)), Color.LightGray);
        Close();
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (_dirty)
            Rebuild();

        if (_window.Parent != null && _window.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }
}
