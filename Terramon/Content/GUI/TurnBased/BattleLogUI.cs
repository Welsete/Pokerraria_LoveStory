using Showdown.NET.Definitions;
using Terramon.Core.Battling;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.TurnBased;

/// <summary>
///     A window with the history of the latest battle and details of both Pokémon in it.
///     Opens with its keybind (L by default) or with the button shown during battles.
/// </summary>
public sealed class BattleLogUI : SmartUIState
{
    private const float WindowWidth = 430f;
    private const float WindowHeight = 420f;
    private const float CardHeight = 136f;

    private static bool _open;

    private UIPanel _window;
    private UITextPanel<LocalizedText> _openButton;
    private UIList _list;
    private UIText _title;
    private int _shownVersion = -1;
    private bool _dragging;
    private Vector2 _dragOffset;

    private static LocalizedText Label(string key) => Language.GetText("Mods.Terramon.GUI.BattleLog." + key);

    public override bool Visible => _open || TestBattleUI.Instance is { Visible: true };

    public static void Toggle()
    {
        _open = !_open;
        SoundEngine.PlaySound(_open ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
        => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

    public override void OnInitialize()
    {
        // Small button to open the window during battles, in the empty corner next to the battle menu
        _openButton = new UITextPanel<LocalizedText>(Label("OpenButton"), 0.85f)
        {
            HAlign = 1f,
            VAlign = 0.9f
        };
        _openButton.Left.Pixels = -24f;
        _openButton.OnLeftClick += (_, _) => Toggle();
        AddHover(_openButton);

        _window = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.95f
        };
        _window.Width.Pixels = WindowWidth;
        _window.Height.Pixels = WindowHeight;
        _window.HAlign = 1f;
        _window.Left.Pixels = -24f;
        _window.Top.Pixels = 250f;
        _window.OnLeftMouseDown += StartDrag;
        _window.OnLeftMouseUp += (_, _) => _dragging = false;

        _title = new UIText(Label("Title"), 0.9f);
        _window.Append(_title);

        var close = new UITextPanel<string>("X", 0.8f)
        {
            HAlign = 1f
        };
        close.Width.Pixels = 28f;
        close.Height.Pixels = 26f;
        close.Top.Pixels = -6f;
        close.OnLeftClick += (_, _) => Toggle();
        AddHover(close);
        _window.Append(close);

        var cardWidth = (WindowWidth - _window.PaddingLeft - _window.PaddingRight - 8f) / 2f;
        _window.Append(new PokemonCard(Label("YourPokemon"), () => TestBattleUI.PlayerPanel?.CurrentMon)
        {
            Top = { Pixels = 28f },
            Width = { Pixels = cardWidth },
            Height = { Pixels = CardHeight }
        });
        _window.Append(new PokemonCard(Label("Opponent"), () => TestBattleUI.FoePanel?.CurrentMon)
        {
            Top = { Pixels = 28f },
            Left = { Pixels = cardWidth + 8f },
            Width = { Pixels = cardWidth },
            Height = { Pixels = CardHeight }
        });

        var listTop = 28f + CardHeight + 8f;
        _list = new UIList
        {
            ListPadding = 2f
        };
        _list.Top.Pixels = listTop;
        _list.Width.Set(-26f, 1f);
        _list.Height.Set(-listTop, 1f);
        _window.Append(_list);

        var scrollbar = new UIScrollbar
        {
            HAlign = 1f
        };
        scrollbar.Top.Pixels = listTop + 6f;
        scrollbar.Height.Set(-listTop - 12f, 1f);
        _list.SetScrollbar(scrollbar);
        _window.Append(scrollbar);
    }

    private static void AddHover(UIPanel panel)
    {
        panel.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
        panel.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = new Color(63, 82, 151) * 0.7f;
        panel.OnLeftClick += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private void StartDrag(UIMouseEvent evt, UIElement listeningElement)
    {
        // Only drag from the window itself (title area), not from the list or buttons
        if (evt.Target != _window && evt.Target != _title)
            return;

        _dragging = true;
        _dragOffset = evt.MousePosition - _window.GetDimensions().Position();
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        var inBattle = TestBattleUI.Instance is { Visible: true };
        Show(_openButton, inBattle && !_open);
        Show(_window, _open);

        if (_open)
        {
            RefreshList();

            if (_dragging)
            {
                var pos = Main.MouseScreen - _dragOffset;
                _window.HAlign = 0f;
                _window.Left.Pixels = MathHelper.Clamp(pos.X, 0f, Main.screenWidth / Main.UIScale - WindowWidth);
                _window.Top.Pixels = MathHelper.Clamp(pos.Y, 0f, Main.screenHeight / Main.UIScale - WindowHeight);
                _window.Recalculate();
            }

            if (_window.ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;
        }

        if (_openButton.Parent != null && _openButton.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
    }

    private void Show(UIElement element, bool show)
    {
        if (show && element.Parent == null)
        {
            Append(element);
            element.Recalculate();
        }
        else if (!show && element.Parent != null)
        {
            element.Remove();
        }
    }

    private void RefreshList()
    {
        if (_shownVersion == BattleLog.HistoryVersion)
            return;

        (string Text, Color Color)[] lines;
        lock (BattleLog.History)
        {
            lines = BattleLog.History.ToArray();
            _shownVersion = BattleLog.HistoryVersion;
        }

        _list.Clear();
        if (lines.Length == 0)
            _list.Add(new UIText(Label("Empty"), 0.8f) { TextColor = Color.Gray });

        foreach (var (text, color) in lines)
            _list.Add(new LogLine(text, color));

        // Keep the newest line in view
        _list.Recalculate();
        _list.ViewPosition = _list.GetTotalHeight();
    }
}

/// <summary>
///     One line of the battle history. Supports chat color tags and wraps to the list's width.
/// </summary>
public sealed class LogLine : UIElement
{
    private const float TextScale = 0.8f;
    private readonly string _text;
    private readonly Color _color;

    public LogLine(string text, Color color)
    {
        _text = text;
        _color = color;
        Width.Percent = 1f;
        Height.Pixels = 20f;
    }

    public override void Recalculate()
    {
        base.Recalculate();
        var width = GetInnerDimensions().Width;
        if (width <= 0f)
            return;

        var size = ChatManager.GetStringSize(FontAssets.MouseText.Value, _text, new Vector2(TextScale), width);
        var height = MathF.Max(20f, size.Y + 2f);
        if (Math.Abs(height - Height.Pixels) > 0.5f)
        {
            Height.Pixels = height;
            base.Recalculate();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dims = GetInnerDimensions();
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, _text, dims.Position(),
            _color, 0f, Vector2.Zero, new Vector2(TextScale), dims.Width);
    }
}

/// <summary>
///     Shows a Pokémon's name, level, types, HP, status and stat changes.
/// </summary>
public sealed class PokemonCard(LocalizedText title, Func<PokemonData> source) : UIPanel
{
    private const float TextScale = 0.78f;
    private const float LineHeight = 16f;

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.GUI.BattleLog." + key, args);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var dims = GetInnerDimensions();
        var pos = new Vector2(dims.X, dims.Y - 2f);
        foreach (var line in Lines(source(), dims.Width))
        {
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, line, pos,
                Color.White, 0f, Vector2.Zero, new Vector2(TextScale));
            pos.Y += LineHeight;
        }
    }

    private static string Colored(string hex, string text) => $"[c/{hex}:{text}]";

    private IEnumerable<string> Lines(PokemonData mon, float maxWidth)
    {
        yield return Colored("a0a8c8", title.Value);
        if (mon == null)
        {
            yield return Colored("808080", "---");
            yield break;
        }

        yield return $"{mon.DisplayName}  {Text("Level", mon.Level)}";

        var types = mon.Schema.Types.Select(t => Colored(t.GetHexColor(), Language.GetTextValue($"Mods.Terramon.Types.{t}")));
        yield return string.Join(" / ", types);

        var ratio = mon.MaxHP == 0 ? 0f : mon.HP / (float)mon.MaxHP;
        var hpColor = ratio > 0.5f ? "6ee06e" : ratio > 0.2f ? "f0d050" : "f05050";
        yield return Colored(hpColor, Text("HP", mon.HP, mon.MaxHP));

        var statusKey = "Mods.Terramon.GUI.BattleLog.Status." + mon.Status;
        var status = mon.HP == 0 ? Text("Status.Fnt") :
            Language.Exists(statusKey) ? Language.GetTextValue(statusKey) : Text("Status.None");
        yield return Text("StatusLabel", status);

        var changes = new List<string>();
        foreach (var stat in new[] { StatID.Atk, StatID.Def, StatID.SpA, StatID.SpD, StatID.Spe })
        {
            var stage = mon.StatStages[stat];
            if (stage == 0)
                continue;
            var name = Text("ShortStats." + stat);
            changes.Add(Colored(stage > 0 ? "6ee06e" : "f05050", $"{name} {stage:+0;-0}"));
        }

        if (changes.Count == 0)
        {
            yield return Colored("a0a0a0", Text("NoStatChanges"));
            yield break;
        }

        // Fill a line with as many changes as fit, then continue on the next one
        var current = string.Empty;
        foreach (var change in changes)
        {
            var attempt = current.Length == 0 ? change : current + "  " + change;
            if (current.Length != 0 && Measure(attempt) > maxWidth)
            {
                yield return current;
                attempt = change;
            }

            current = attempt;
        }

        yield return current;
    }

    private static float Measure(string text)
        => ChatManager.GetStringSize(FontAssets.MouseText.Value, text, new Vector2(TextScale)).X;
}
