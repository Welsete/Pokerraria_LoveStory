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
    private const float WindowWidth = 360f;
    private const float ButtonHeight = 34f;

    private static PokemonData _pokemon;
    private static MoveID _move;
    private static bool _dirty;

    private UIPanel _window;

    public override bool Visible => _pokemon != null;

    public static bool IsOpen => _pokemon != null;

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.MoveLearn." + key, args);

    public static string MoveName(MoveID move)
        => Language.GetTextValue($"Mods.Terramon.Moves.{move}.DisplayName");

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
        for (var i = 0; i < 4; i++)
        {
            var current = pokemon.Moves[i].ID;
            var label = current == MoveID.None ? "---" : MoveName(current);
            var slot = i;
            AddButton(label, top, () => Replace(slot));
            top += ButtonHeight + 4f;
        }

        top += 6f;
        AddButton(Text("DontLearn"), top, Decline);
        top += ButtonHeight;

        _window.Height.Pixels = top + _window.PaddingTop + _window.PaddingBottom;
        Append(_window);
        Recalculate();
    }

    private void AddButton(string label, float top, Action onClick)
    {
        var button = new UITextPanel<string>(label, 0.85f)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.7f
        };
        button.Width.Set(0f, 1f);
        button.Height.Pixels = ButtonHeight;
        button.Top.Pixels = top;
        button.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
        button.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = new Color(63, 82, 151) * 0.7f;
        button.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            onClick();
        };
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
