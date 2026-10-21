using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using Terramon.Content.GUI.Common;
using Terramon.Core.Battling;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace Terramon.Content.GUI;

/// <summary>
///     The Pokémon summary screen: a window with the Pokémon on a little scene and its basic info on the left, and tabs
///     (stats, IVs, EVs, moves, other) on the right. Opened from the party slots, the party sidebar and the PC with
///     middle click or the summary keybind; closed with Esc or the close button.
/// </summary>
public sealed class SummaryUI : SmartUIState
{
    /// <summary>
    ///     The tabs of the right side of the summary.
    /// </summary>
    public enum Tab : byte
    {
        Stats,
        IVs,
        EVs,
        Moves,
        Other
    }

    private static PokemonData[] _list = [];
    private static int _index;
    private static bool _playerInventoryOpen;

    private SummaryWindow _window;

    static SummaryUI()
    {
        // Don't run this on the server
        if (Main.dedServ) return;

        // Esc (the inventory key) closes the summary instead of toggling the inventory
        On_Player.ToggleInv += static (orig, self) =>
        {
            if (Active)
            {
                Close();
                return;
            }

            orig(self);
        };
    }

    /// <summary>
    ///     The tab being shown. Kept between openings, so the summary opens where the player left it.
    /// </summary>
    public static Tab CurrentTab { get; private set; } = Tab.Stats;

    /// <summary>
    ///     The animation shown on the scene (for Pokémon with a full animation set).
    /// </summary>
    internal static PokemonAnimState AnimState { get; set; }

    /// <summary>
    ///     The move whose description is shown in the Moves tab.
    /// </summary>
    internal static int SelectedMove { get; set; }

    /// <summary>
    ///     Goes from 0 to 1 right after the tab or Pokémon changes, to grow the radar charts in.
    /// </summary>
    internal static float Appear { get; private set; }

    /// <summary>
    ///     Whether the summary screen is open.
    /// </summary>
    public static bool Active => Main.inFancyUI && Main.InGameUI.CurrentState is SummaryUI;

    /// <summary>
    ///     The Pokémon being shown.
    /// </summary>
    public static PokemonData Current => _list.Length == 0 ? null : _list[Math.Clamp(_index, 0, _list.Length - 1)];

    internal static int Count => _list.Length;
    internal static int Index => _index;

    public override bool Visible => false; // Vanilla will update/draw this state through IngameFancyUI

    /// <summary>
    ///     Whether the summary can be opened right now (not in battle, not holding anything, no other menu open).
    /// </summary>
    public static bool CanOpen =>
        !Main.gameMenu
        && !Main.LocalPlayer.dead
        && !Main.inFancyUI
        && !HubUI.Active
        && !MoveLearnUI.IsOpen
        && !BattleClient.LocalBattleOngoing
        && Main.mouseItem.IsAir
        && TooltipOverlay.GetHeldPokemon(out _) == null;

    /// <summary>
    ///     The one-line hint shown in slot tooltips, e.g. "Middle click or I: Summary".
    /// </summary>
    public static string HintText()
    {
        var keys = KeybindSystem.OpenSummaryKeybind?.GetAssignedKeys();
        var key = keys is { Count: > 0 } ? keys[0] : "—";
        return Language.GetTextValue("Mods.Terramon.GUI.Summary.Hint", key);
    }

    /// <summary>
    ///     True on the frame the player asks to open the summary of the slot under the mouse (middle click or the
    ///     summary keybind).
    /// </summary>
    public static bool OpenRequested() =>
        KeybindSystem.OpenSummaryKeybind.JustPressed || (Main.mouseMiddle && Main.mouseMiddleRelease);

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    }

    /// <summary>
    ///     Opens the summary of <paramref name="selected" />. The arrows then go through the other Pokémon in
    ///     <paramref name="list" /> (the party, or a PC box); empty entries are skipped.
    /// </summary>
    public static void Open(IReadOnlyList<PokemonData> list, PokemonData selected)
    {
        if (list == null || selected == null)
            return;

        if (!CanOpen)
        {
            SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
            return;
        }

        _list = list.Where(d => d != null).ToArray();
        _index = Math.Max(0, Array.IndexOf(_list, selected));
        if (_list.Length == 0)
            _list = [selected];
        ResetView();

        _playerInventoryOpen = Main.playerInventory;
        var state = UILoader.GetUIState<SummaryUI>();
        IngameFancyUI.OpenUIState(state);
        SoundEngine.PlaySound(in SoundID.MenuOpen);
    }

    /// <summary>
    ///     Closes the summary and gives back the inventory as it was.
    /// </summary>
    public static void Close()
    {
        if (!Active)
            return;

        // Like IngameFancyUI.Close, but brings back the inventory only if it was open before
        Main.inFancyUI = false;
        Main.LocalPlayer.releaseInventory = false;
        Main.InGameUI.SetState(null);
        UILinkPointNavigator.Shortcuts.FANCYUI_SPECIAL_INSTRUCTIONS = 0;
        Main.playerInventory = _playerInventoryOpen;
        _list = [];
        SoundEngine.PlaySound(in SoundID.MenuClose);
    }

    /// <summary>
    ///     Shows the previous (-1) or next (+1) Pokémon of the list.
    /// </summary>
    internal static void Step(int direction)
    {
        if (_list.Length < 2)
            return;
        _index = (_index + direction + _list.Length) % _list.Length;
        ResetView();
        SoundEngine.PlaySound(in SoundID.MenuTick);
    }

    internal static void SetTab(Tab tab)
    {
        if (CurrentTab == tab)
            return;
        CurrentTab = tab;
        Appear = 0f;
        RelearnSlot = -1;
        SoundEngine.PlaySound(in SoundID.MenuTick);
    }

    private static void ResetView()
    {
        AnimState = PokemonAnimState.Idle;
        SelectedMove = 0;
        Appear = 0f;
        RelearnSlot = -1;
    }

    #region Move relearner

    /// <summary>
    ///     The move slot (0-3) the move relearner list is open for in the Moves tab, or -1 when it's closed.
    /// </summary>
    internal static int RelearnSlot { get; private set; } = -1;

    /// <summary>
    ///     How many rows the move relearner list is scrolled down.
    /// </summary>
    internal static int RelearnScroll { get; set; }

    /// <summary>
    ///     Whether the moves of <paramref name="data" /> can be changed from the summary right now: only Pokémon in
    ///     the local player's party, and never during a battle.
    /// </summary>
    public static bool CanRelearn(PokemonData data)
        => data != null && !data.Moves.IsDefault && !BattleClient.LocalBattleOngoing &&
           Array.IndexOf(TerramonPlayer.LocalPlayer.Party, data) >= 0;

    /// <summary>
    ///     The level-up moves of the Pokémon's species up to its current level (including the ones learned on
    ///     evolving) that it doesn't know right now, lowest level first. These are the moves it could have learned
    ///     already, e.g. ones the player declined when it leveled up.
    /// </summary>
    public static List<(byte Level, MoveID Move)> RelearnableMoves(PokemonData data)
    {
        var moves = new List<(byte, MoveID)>();
        var learnset = data?.Schema?.LevelUpLearnset;
        if (learnset == null || data.Moves.IsDefault)
            return moves;

        foreach (var entry in learnset.OrderBy(e => e.AtLevel))
        {
            var move = (MoveID)entry.ID;
            if (entry.AtLevel > data.Level || move == MoveID.None || !Enum.IsDefined(move) ||
                Terramon.DatabaseV2.GetMove(move) == null || data.KnowsMove(move) ||
                moves.Exists(m => m.Item2 == move))
                continue;
            moves.Add((entry.AtLevel, move));
        }

        return moves;
    }

    /// <summary>
    ///     Opens the move relearner list for <paramref name="slot" /> (a move to replace, or an empty slot to fill).
    /// </summary>
    internal static void OpenRelearn(int slot)
    {
        if (!CanRelearn(Current) || slot is < 0 or > 3)
            return;
        RelearnSlot = slot;
        RelearnScroll = 0;
        SoundEngine.PlaySound(in SoundID.MenuOpen);
    }

    /// <summary>
    ///     Closes the move relearner list without changing anything.
    /// </summary>
    internal static void CloseRelearn()
    {
        if (RelearnSlot < 0)
            return;
        RelearnSlot = -1;
        SoundEngine.PlaySound(in SoundID.MenuClose);
    }

    /// <summary>
    ///     Teaches <paramref name="move" /> to the shown Pokémon in the slot the relearner list is open for, like
    ///     learning it on level up: it replaces the move there (or fills the first empty slot) with full PP.
    ///     The party is saved with the player and the change reaches other players through the usual party sync.
    /// </summary>
    internal static void Relearn(MoveID move)
    {
        var data = Current;
        var slot = RelearnSlot;
        RelearnSlot = -1;
        if (!CanRelearn(data) || slot is < 0 or > 3 || !RelearnableMoves(data).Exists(m => m.Move == move))
        {
            SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
            return;
        }

        var old = data.Moves[slot].ID;
        if (old == MoveID.None)
        {
            if (!data.TryLearnMove(move))
                return;
        }
        else
        {
            data.SetMove(slot, move);
        }

        // It knows it now, so the level-up prompt shouldn't ask about it again
        data.PendingMoves?.Remove(move);

        for (var i = 0; i < 4; i++)
            if (data.Moves[i].ID == move)
                SelectedMove = i;

        Main.NewText(old == MoveID.None
                ? Language.GetTextValue("Mods.Terramon.MoveLearn.Learned", data.DisplayName, MoveLearnUI.MoveName(move))
                : Language.GetTextValue("Mods.Terramon.MoveLearn.Forgot", data.DisplayName,
                    MoveLearnUI.MoveName(old), MoveLearnUI.MoveName(move)),
            new Color(50, 255, 130));
        SoundEngine.PlaySound(SoundID.Item4);
    }

    #endregion

    public override void OnInitialize()
    {
        _window = new SummaryWindow
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _window.Width.Set(SummaryWindow.WindowWidth, 0f);
        _window.Height.Set(SummaryWindow.WindowHeight, 0f);
        Append(_window);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Active)
            return;

        // Never stay open over a battle (or a death)
        if (Current == null || BattleClient.LocalBattleOngoing || Main.LocalPlayer.dead)
        {
            Close();
            return;
        }

        Appear = Math.Min(1f, Appear + 1f / 14f);
        if (RelearnSlot >= 0 && (CurrentTab != Tab.Moves || !CanRelearn(Current)))
            RelearnSlot = -1;

        if (JustPressed(Keys.Left))
            Step(-1);
        else if (JustPressed(Keys.Right))
            Step(1);
    }

    private static bool JustPressed(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
}

/// <summary>
///     Draws the summary window. Everything is drawn by hand (panels, radar charts, badges) in the style of the
///     Terramon Brasil concept art: navy panels with gold rivets, a red title bar and Terraria's own font.
/// </summary>
internal sealed class SummaryWindow : UIElement
{
    public const int WindowWidth = 780;
    public const int WindowHeight = 470;

    // Palette
    private static readonly Color WindowBg = new(24, 33, 66);
    private static readonly Color PanelBg = new(31, 44, 88);
    private static readonly Color PanelDarkBg = new(22, 30, 60);
    private static readonly Color PanelBorder = new(78, 104, 176);
    private static readonly Color OutlineDark = new(9, 13, 30);
    private static readonly Color Gold = new(232, 190, 84);
    private static readonly Color GoldDark = new(150, 104, 36);
    private static readonly Color GoldLight = new(255, 240, 170);
    private static readonly Color GoldText = new(255, 221, 110);
    private static readonly Color LabelText = new(120, 210, 245);
    private static readonly Color ValueText = new(240, 232, 160);
    private static readonly Color DimText = new(150, 165, 210);
    private static readonly Color HeaderRed = new(160, 34, 46);
    private static readonly Color HeaderDark = new(100, 18, 28);
    private static readonly Color HeaderLight = new(210, 70, 78);
    private static readonly Color PlusColor = new(255, 120, 110);
    private static readonly Color MinusColor = new(120, 170, 255);
    private static readonly Color RadarBg = new(16, 22, 48);
    private static readonly Color RadarRing = new(58, 74, 130);
    private static readonly Color RadarSpoke = new(40, 54, 100);

    private static readonly Rectangle PixelSource = new(0, 0, 1, 1);
    private static readonly List<float> Crossings = [];
    private static readonly Dictionary<(ushort, bool), Vector2> SpriteSizes = [];

    private static readonly Asset<Texture2D> MaleIconTexture;
    private static readonly Asset<Texture2D> FemaleIconTexture;

    // Stat order used everywhere here: HP, Atk, Def, SpA, SpD, Spe
    private static readonly string[] StatKeys = ["HP", "Atk", "Def", "SpA", "SpD", "Spe"];

    // Radar axes clockwise from the top: HP, Atk, Def, Spe, SpD, SpA
    private static readonly int[] RadarOrder = [0, 1, 2, 5, 4, 3];

    // Natures go in groups of five over Atk, Def, Spe, SpA, SpD (Hardy, Lonely, Brave...)
    private static readonly int[] NatureStat = [1, 2, 5, 3, 4];

    private static readonly string[] FieldAbilityKeys = ["Ride", "Fly", "Surf", "Dig"];

    private static readonly Color[] FieldAbilityColors =
        [new(214, 140, 70), new(120, 190, 240), new(70, 130, 220), new(160, 110, 70)];

    private readonly Rectangle[] _moveRects = new Rectangle[4];
    private readonly Rectangle[] _relearnButtons = new Rectangle[4];
    private readonly List<(Rectangle Rect, MoveID Move)> _relearnRows = [];
    private Rectangle _relearnCancel;
    private readonly Rectangle[] _tabRects = new Rectangle[5];
    private Rectangle _animButton;
    private Rectangle _closeButton;
    private Rectangle _nextButton;
    private Rectangle _prevButton;
    private Rectangle _scene;
    private Point _mouse;

    static SummaryWindow()
    {
        if (Main.dedServ) return;
        MaleIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/IconMale");
        FemaleIconTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Party/IconFemale");
    }

    private static Texture2D Pixel => TextureAssets.MagicPixel.Value;
    private static DynamicSpriteFont Font => FontAssets.MouseText.Value;

    private static string T(string key, params object[] args) =>
        Language.GetTextValue("Mods.Terramon.GUI.Summary." + key, args);

    #region Input

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        var data = SummaryUI.Current;
        if (data == null)
            return;

        var p = evt.MousePosition.ToPoint();
        if (_closeButton.Contains(p))
        {
            SummaryUI.Close();
            return;
        }

        if (SummaryUI.Count > 1 && _prevButton.Contains(p))
        {
            SummaryUI.Step(-1);
            return;
        }

        if (SummaryUI.Count > 1 && _nextButton.Contains(p))
        {
            SummaryUI.Step(1);
            return;
        }

        for (var i = 0; i < _tabRects.Length; i++)
        {
            if (!_tabRects[i].Contains(p)) continue;
            SummaryUI.SetTab((SummaryUI.Tab)i);
            return;
        }

        if (_animButton != Rectangle.Empty && _animButton.Contains(p))
        {
            NextAnimation(data);
            return;
        }

        if (_scene.Contains(p))
        {
            SoundEngine.PlaySound(data.GetCry());
            return;
        }

        if (SummaryUI.CurrentTab != SummaryUI.Tab.Moves) return;

        // Move relearner list: pick a move, or cancel
        if (SummaryUI.RelearnSlot >= 0)
        {
            if (_relearnCancel.Contains(p))
            {
                SummaryUI.CloseRelearn();
                return;
            }

            foreach (var (rect, move) in _relearnRows)
            {
                if (!rect.Contains(p)) continue;
                SummaryUI.Relearn(move);
                return;
            }

            return;
        }

        for (var i = 0; i < _relearnButtons.Length; i++)
        {
            if (_relearnButtons[i] == Rectangle.Empty || !_relearnButtons[i].Contains(p)) continue;
            SummaryUI.OpenRelearn(i);
            return;
        }

        for (var i = 0; i < _moveRects.Length; i++)
        {
            if (!_moveRects[i].Contains(p) || data.Moves.IsDefault || data.Moves[i].ID == MoveID.None) continue;
            SummaryUI.SelectedMove = i;
            SoundEngine.PlaySound(in SoundID.MenuTick);
            return;
        }
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);
        // Scrolls the move relearner list (clamped when it's drawn)
        if (SummaryUI.RelearnSlot >= 0 && SummaryUI.CurrentTab == SummaryUI.Tab.Moves)
            SummaryUI.RelearnScroll -= Math.Sign(evt.ScrollWheelValue);
    }

    /// <summary>
    ///     Goes to the next animation the Pokémon has (Idle, Walk, Attack, Sleep...).
    /// </summary>
    private static void NextAnimation(PokemonData data)
    {
        var set = PokemonAnimations.Get(data.Schema?.Identifier);
        if (set == null)
            return;
        var states = Enum.GetValues<PokemonAnimState>().Where(s => set.Anims.ContainsKey(s)).ToList();
        if (states.Count == 0)
            return;
        var i = states.IndexOf(SummaryUI.AnimState);
        SummaryUI.AnimState = states[(i + 1) % states.Count];
        SoundEngine.PlaySound(in SoundID.MenuTick);
    }

    private bool Hovering(Rectangle r) => r.Contains(_mouse);

    #endregion

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var data = SummaryUI.Current;
        if (data?.Schema == null)
            return;

        _mouse = Main.MouseScreen.ToPoint();
        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        var dims = GetDimensions();
        var o = new Point((int)dims.X, (int)dims.Y);

        // Dim the world behind the window
        Fill(spriteBatch,
            new Rectangle(-10, -10, (int)(Main.screenWidth / Main.UIScale) + 20,
                (int)(Main.screenHeight / Main.UIScale) + 20), Color.Black * 0.45f);

        // Window frame
        var window = new Rectangle(o.X, o.Y, WindowWidth, WindowHeight);
        Fill(spriteBatch, window, OutlineDark);
        Fill(spriteBatch, Inset(window, 2), PanelBorder);
        Fill(spriteBatch, Inset(window, 4), WindowBg);
        Rivets(spriteBatch, Inset(window, 2));

        DrawHeader(spriteBatch, o);
        DrawLeftColumn(spriteBatch, o, data);
        DrawRightSide(spriteBatch, o, data);

        // Hover panels (abilities, moves) last, on top of the whole window
        MoveTooltip.DrawPending(spriteBatch);
    }

    #region Header

    private void DrawHeader(SpriteBatch sb, Point o)
    {
        var bar = new Rectangle(o.X + 10, o.Y + 10, WindowWidth - 20, 30);
        Fill(sb, bar, OutlineDark);
        var inner = Inset(bar, 1);
        Fill(sb, inner, HeaderDark);
        Fill(sb, new Rectangle(inner.X, inner.Y, inner.Width, inner.Height - 3), HeaderRed);
        Fill(sb, new Rectangle(inner.X, inner.Y, inner.Width, 2), HeaderLight);
        Rivets(sb, bar);

        var ball = BallAssets.GetBallIcon(BallID.PokeBall);
        var titleX = bar.X + 12f;
        if (ball != null)
        {
            var tex = ball.Value;
            sb.Draw(tex, new Vector2(bar.X + 12, bar.Center.Y - tex.Height / 2), Color.White);
            titleX += tex.Width + 8;
        }

        Text(sb, Upper(T("Title")), titleX, bar.Center.Y, Color.White, 0.85f, 0f, 0.5f);

        // Close button
        _closeButton = new Rectangle(bar.Right - 28, bar.Y + 4, 22, 22);
        var closeHover = Hovering(_closeButton);
        Fill(sb, _closeButton, OutlineDark);
        Fill(sb, Inset(_closeButton, 1), closeHover ? HeaderLight : HeaderDark);
        var c = _closeButton.Center.ToVector2();
        Line(sb, c + new Vector2(-5, -5), c + new Vector2(5, 5), Color.White, 2f);
        Line(sb, c + new Vector2(-5, 5), c + new Vector2(5, -5), Color.White, 2f);
        if (closeHover)
            Main.instance.MouseText(T("Close"));

        // Previous/next Pokémon
        if (SummaryUI.Count > 1)
        {
            _nextButton = new Rectangle(_closeButton.X - 34, _closeButton.Y, 22, 22);
            var counter = new Rectangle(_nextButton.X - 52, _closeButton.Y, 52, 22);
            _prevButton = new Rectangle(counter.X - 22, _closeButton.Y, 22, 22);
            ArrowButton(sb, _prevButton, -1);
            ArrowButton(sb, _nextButton, 1);
            TextCentered(sb, $"{SummaryUI.Index + 1}/{SummaryUI.Count}", counter, GoldText, 0.8f);
            if (Hovering(_prevButton))
                Main.instance.MouseText(T("Previous"));
            else if (Hovering(_nextButton))
                Main.instance.MouseText(T("Next"));
        }
        else
        {
            _prevButton = _nextButton = Rectangle.Empty;
        }
    }

    private void ArrowButton(SpriteBatch sb, Rectangle r, int direction)
    {
        Fill(sb, r, OutlineDark);
        Fill(sb, Inset(r, 1), Hovering(r) ? HeaderLight : HeaderDark);
        var c = r.Center.ToVector2();
        FillPolygon(sb,
        [
            c + new Vector2(4 * direction, 0), c + new Vector2(-3 * direction, -6), c + new Vector2(-3 * direction, 6)
        ], Color.White);
    }

    #endregion

    #region Left column

    private void DrawLeftColumn(SpriteBatch sb, Point o, PokemonData data)
    {
        const int x = 14;
        const int w = 262;

        // Name plate: "#448 Lucario ♂  Lv. 42"
        var plate = new Rectangle(o.X + x, o.Y + 46, w, 30);
        Panel(sb, plate, PanelBg);
        var number = $"#{data.ID:D3}";
        Text(sb, number, plate.X + 12, plate.Center.Y, DimText, 0.8f, 0f, 0.5f);
        var level = Language.GetTextValue("Mods.Terramon.GUI.Party.LevelDisplay", data.Level);
        Text(sb, level, plate.Right - 12, plate.Center.Y, ValueText, 0.8f, 1f, 0.5f);
        var nameRight = plate.Right - 12 - Measure(level, 0.8f).X - 8;
        if (data.Gender is Gender.Male or Gender.Female)
        {
            var icon = (data.Gender == Gender.Male ? MaleIconTexture : FemaleIconTexture).Value;
            nameRight -= icon.Width;
            sb.Draw(icon, new Vector2(nameRight, plate.Center.Y - icon.Height / 2f), Color.White);
            nameRight -= 6;
        }

        var nameX = plate.X + 12 + Measure(number, 0.8f).X + 8;
        TextFit(sb, data.DisplayName, nameX, plate.Center.Y, nameRight - nameX,
            data.IsShiny ? GoldLight : GoldText, 0.9f, 0.5f);

        // Scene with the animated Pokémon
        var frame = new Rectangle(o.X + x, o.Y + 80, w, 150);
        DrawScene(sb, frame, data);

        // Type badges
        var types = data.Schema.Types;
        for (var i = 0; i < types.Count; i++)
            TypeBadge(sb, new Rectangle(o.X + x + i * 98, o.Y + 236, 92, 22), types[i], 0.75f);

        // Nature, ability, held item, original trainer
        var info = new Rectangle(o.X + x, o.Y + 264, w, 112);
        Panel(sb, info, PanelBg);
        var rowY = info.Y + 20;
        var valueX = info.X + 104;
        var valueWidth = info.Right - 12 - valueX;
        InfoRow(sb, T("Nature"), NatureText(data), info.X + 12, valueX, rowY, valueWidth);
        DrawAbilityRow(sb, data, info, valueX, rowY + 24, valueWidth);

        var item = data.HeldItem;
        if (item == null || item.IsAir)
        {
            InfoRow(sb, T("Item"), T("NoItem"), info.X + 12, valueX, rowY + 48, valueWidth);
        }
        else
        {
            ItemSlot.DrawItemIcon(item, ItemSlot.Context.MouseItem, sb, new Vector2(valueX + 8, rowY + 48), 1f, 18f,
                Color.White);
            InfoRow(sb, T("Item"), Lang.GetItemNameValue(item.type), info.X + 12, valueX + 22, rowY + 48,
                valueWidth - 22);
        }

        InfoRow(sb, T("Trainer"), string.IsNullOrEmpty(data.OT) ? T("Unknown") : data.OT, info.X + 12, valueX,
            rowY + 72, valueWidth);

        // HP and EXP
        var bars = new Rectangle(o.X + x, o.Y + 380, w, 78);
        Panel(sb, bars, PanelBg);
        var hpPercent = data.MaxHP == 0 ? 0f : (float)data.HP / data.MaxHP;
        Text(sb, T("HP"), bars.X + 12, bars.Y + 17, LabelText, 0.75f, 0f, 0.5f);
        Bar(sb, new Rectangle(bars.X + 52, bars.Y + 12, 120, 10), hpPercent, HPColor(hpPercent));
        Text(sb, $"{data.HP}/{data.MaxHP}", bars.Right - 12, bars.Y + 17, ValueText, 0.75f, 1f, 0.5f);

        Text(sb, T("EXP"), bars.X + 12, bars.Y + 39, LabelText, 0.75f, 0f, 0.5f);
        string expNote;
        float expPercent;
        if (data.Level < Terramon.MaxPokemonLevel)
        {
            var growth = data.Schema.GrowthRate;
            var minExp = ExperienceLookupTable.GetLevelTotalExp(data.Level, growth);
            var nextExp = ExperienceLookupTable.GetLevelTotalExp(data.Level + 1, growth);
            expPercent = nextExp > minExp ? Math.Clamp((float)(data.TotalEXP - minExp) / (nextExp - minExp), 0f, 1f) : 1f;
            expNote = T("ToNextLevel", Math.Max(0, nextExp - data.TotalEXP), data.Level + 1);
        }
        else
        {
            expPercent = 1f;
            expNote = T("MaxLevel");
        }

        Bar(sb, new Rectangle(bars.X + 52, bars.Y + 34, 120, 10), expPercent, new Color(80, 170, 255));
        Text(sb, $"{(int)(expPercent * 100)}%", bars.Right - 12, bars.Y + 39, ValueText, 0.75f, 1f, 0.5f);
        TextFit(sb, expNote, bars.X + 12, bars.Y + 60, bars.Width - 24, DimText, 0.7f, 0.5f);
    }

    /// <summary>
    ///     The ability row of the info panel, with a little "?" badge: hovering it shows what the ability does.
    /// </summary>
    private void DrawAbilityRow(SpriteBatch sb, PokemonData data, Rectangle info, int valueX, int y, int valueWidth)
    {
        var ability = data.Ability;
        var row = new Rectangle(info.X + 6, y - 11, info.Width - 12, 22);
        var hover = ability != AbilityID.None && Hovering(row);
        if (hover)
            Fill(sb, row, new Color(42, 57, 110));

        var name = AbilityName(ability);
        const int badgeSpace = 20;
        InfoRow(sb, T("Ability"), name, info.X + 12, valueX, y, valueWidth - badgeSpace);
        if (ability == AbilityID.None)
            return;

        var nameWidth = Math.Min(Measure(name, 0.78f).X, valueWidth - badgeSpace);
        HelpBadge(sb, (int)(valueX + nameWidth + 6), y, hover);
        if (hover)
            MoveTooltip.Show(AbilityTooltip.Build(ability, data.Schema.Abilities?.Hidden == ability));
    }

    /// <summary>
    ///     A small "?" square that tells the player they can hover something to read more about it.
    /// </summary>
    private static void HelpBadge(SpriteBatch sb, int x, int centerY, bool active)
    {
        var r = new Rectangle(x, centerY - 7, 13, 13);
        Fill(sb, r, OutlineDark);
        Fill(sb, Inset(r, 1), active ? GoldDark : PanelBorder);
        TextCentered(sb, "?", r, active ? GoldLight : Color.White, 0.6f);
    }

    private static void InfoRow(SpriteBatch sb, string label, string value, float labelX, float valueX, float y,
        float valueWidth)
    {
        Text(sb, Upper(label), labelX, y, LabelText, 0.72f, 0f, 0.5f);
        TextFit(sb, value, valueX, y, valueWidth, ValueText, 0.78f, 0.5f);
    }

    /// <summary>
    ///     A little landscape (day or night, like the world) with the Pokémon standing on the grass.
    /// </summary>
    private void DrawScene(SpriteBatch sb, Rectangle frame, PokemonData data)
    {
        Fill(sb, frame, OutlineDark);
        Fill(sb, Inset(frame, 2), PanelBorder);
        var scene = Inset(frame, 4);
        _scene = scene;

        var typeColor = data.Schema.Types.Count > 0 ? data.Schema.Types[0].GetColor() : Color.White;
        var day = Main.dayTime;
        var skyTop = day ? Color.Lerp(new Color(70, 110, 190), typeColor, 0.2f) : new Color(14, 20, 48);
        var skyBottom = day ? Color.Lerp(new Color(160, 205, 240), typeColor, 0.15f) : new Color(48, 62, 112);
        var groundY = scene.Bottom - 22;

        // Sky in 4 px bands, for a pixel-art gradient
        for (var y = scene.Y; y < groundY; y += 4)
        {
            var t = (float)(y - scene.Y) / Math.Max(1, groundY - scene.Y);
            Fill(sb, new Rectangle(scene.X, y, scene.Width, Math.Min(4, groundY - y)), Color.Lerp(skyTop, skyBottom, t));
        }

        if (day)
        {
            Cloud(sb, scene.X + 30, scene.Y + 22);
            Cloud(sb, scene.X + 170, scene.Y + 38);
        }
        else
        {
            int[] starX = [18, 52, 95, 140, 188, 230, 70, 205, 160];
            int[] starY = [14, 34, 10, 26, 12, 40, 52, 60, 48];
            for (var i = 0; i < starX.Length; i++)
            {
                var twinkle = (Main.GameUpdateCount / 20 + i) % 7 == 0 ? 0.4f : 1f;
                Fill(sb, new Rectangle(scene.X + starX[i], scene.Y + starY[i], 2, 2), Color.White * twinkle);
            }
        }

        // Far hills
        var hills = new List<Vector2> { new(scene.X, groundY) };
        for (var hx = 0; hx <= scene.Width; hx += 6)
            hills.Add(new Vector2(scene.X + hx, groundY - 26 - 10 * MathF.Sin(hx * 0.045f + 1.2f)));
        hills.Add(new Vector2(scene.Right, groundY));
        FillPolygon(sb, hills, day ? new Color(74, 128, 104) : new Color(30, 48, 66));

        // Pine trees, away from the middle where the Pokémon stands
        var treeColor = day ? new Color(46, 96, 74) : new Color(22, 38, 50);
        int[] treeX = [16, 44, 200, 232];
        int[] treeH = [34, 46, 40, 30];
        for (var i = 0; i < treeX.Length; i++)
            Tree(sb, scene.X + treeX[i], groundY, treeH[i], treeColor);

        // Grass and dirt
        Fill(sb, new Rectangle(scene.X, groundY, scene.Width, scene.Bottom - groundY), new Color(110, 74, 48));
        Fill(sb, new Rectangle(scene.X, groundY, scene.Width, 6), day ? new Color(96, 170, 70) : new Color(54, 102, 52));
        Fill(sb, new Rectangle(scene.X, groundY, scene.Width, 2), day ? new Color(140, 210, 96) : new Color(76, 132, 70));
        for (var dx = 6; dx < scene.Width - 4; dx += 14)
            Fill(sb, new Rectangle(scene.X + dx, groundY + 10 + dx % 3 * 3, 3, 2), new Color(84, 56, 36));

        // Shadow
        var feet = new Vector2(scene.Center.X, groundY + 3);
        Fill(sb, new Rectangle((int)feet.X - 22, (int)feet.Y - 2, 44, 4), Color.Black * 0.22f);
        Fill(sb, new Rectangle((int)feet.X - 14, (int)feet.Y - 3, 28, 6), Color.Black * 0.15f);

        // The Pokémon, crisp (point sampling)
        var size = SpriteSize(data);
        var fit = Math.Min((scene.Width - 40) / size.X, (scene.Height - 30) / size.Y);
        var scale = fit >= 1f ? MathF.Floor(Math.Min(fit, 3f)) : fit;
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        PokemonAnimations.DrawInUI(sb, data.ID, data.IsShiny, feet, scale, SummaryUI.AnimState);
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        // Ball it was caught in, shiny sparkle, species if nicknamed
        var ball = BallAssets.GetBallIcon(data.Ball);
        if (ball != null)
        {
            sb.Draw(ball.Value, new Vector2(scene.X + 5, scene.Y + 5), Color.White);
            if (Hovering(new Rectangle(scene.X + 5, scene.Y + 5, ball.Value.Width, ball.Value.Height)))
                Main.instance.MouseText(BallName(data.Ball));
        }

        if (data.IsShiny)
            Sparkle(sb, scene.Right - 14, scene.Y + 12);

        if (!string.IsNullOrEmpty(data.Nickname))
            Text(sb, data.LocalizedName, scene.Center.X, scene.Y + 12, Color.White * 0.9f, 0.7f, 0.5f, 0.5f);

        // Animation preview button (only Pokémon with a Mystery Dungeon animation set have more than one pose)
        if (PokemonAnimations.Get(data.Schema.Identifier) != null)
        {
            _animButton = new Rectangle(scene.Right - 84, scene.Bottom - 17, 80, 14);
            var hover = Hovering(_animButton);
            Fill(sb, _animButton, Color.Black * (hover ? 0.7f : 0.5f));
            var arrow = new Vector2(_animButton.X + 6, _animButton.Center.Y);
            FillPolygon(sb, [arrow + new Vector2(0, -4), arrow + new Vector2(5, 0), arrow + new Vector2(0, 4)],
                hover ? GoldText : Color.White);
            TextFit(sb, T("Anim." + SummaryUI.AnimState), _animButton.X + 14, _animButton.Center.Y,
                _animButton.Width - 18, hover ? GoldText : Color.White, 0.62f, 0.5f);
            if (hover)
                Main.instance.MouseText(T("AnimHint"));
        }
        else
        {
            _animButton = Rectangle.Empty;
            if (Hovering(scene))
                Main.instance.MouseText(T("CryHint"));
        }

        Rivets(sb, frame);
    }

    private static void Cloud(SpriteBatch sb, int x, int y)
    {
        var c = Color.White * 0.75f;
        Fill(sb, new Rectangle(x, y, 30, 6), c);
        Fill(sb, new Rectangle(x + 6, y - 4, 14, 4), c);
        Fill(sb, new Rectangle(x + 12, y - 7, 8, 3), c);
    }

    private static void Tree(SpriteBatch sb, int x, int groundY, int height, Color color)
    {
        Fill(sb, new Rectangle(x - 1, groundY - 6, 3, 6), new Color(70, 48, 32));
        for (var i = 0; i < 3; i++)
        {
            var top = groundY - height + i * height / 4;
            var halfWidth = 5 + i * 3;
            FillPolygon(sb,
            [
                new Vector2(x + 0.5f, top), new Vector2(x + 0.5f + halfWidth, top + height / 2f - 2),
                new Vector2(x + 0.5f - halfWidth, top + height / 2f - 2)
            ], color);
        }
    }

    private static void Sparkle(SpriteBatch sb, int x, int y)
    {
        var pulse = 0.75f + 0.25f * MathF.Sin(Main.GameUpdateCount * 0.12f);
        var c = GoldLight * pulse;
        Fill(sb, new Rectangle(x - 1, y - 6, 2, 12), c);
        Fill(sb, new Rectangle(x - 6, y - 1, 12, 2), c);
        Fill(sb, new Rectangle(x - 2, y - 2, 4, 4), Color.White * pulse);
    }

    /// <summary>
    ///     Size of the Pokémon's idle pose as <see cref="PokemonAnimations.DrawInUI" /> draws it at scale 1.
    /// </summary>
    private static Vector2 SpriteSize(PokemonData data)
    {
        if (SpriteSizes.TryGetValue((data.ID, data.IsShiny), out var size))
            return size;

        size = new Vector2(64f);
        var identifier = data.Schema.Identifier;
        var set = PokemonAnimations.Get(identifier);
        if (set != null && set.Anims.TryGetValue(PokemonAnimState.Idle, out var idle))
        {
            size = new Vector2(idle.FrameWidth, idle.FrameHeight) * set.Scale;
        }
        else if (ModContent.RequestIfExists<Texture2D>(
                     $"Terramon/Assets/Pokemon/{identifier}{(data.IsShiny ? "_S" : string.Empty)}", out var sheet,
                     AssetRequestMode.ImmediateLoad))
        {
            var frames = PokemonEntityLoader.IDToNPCType.TryGetValue(data.ID, out var npcType)
                ? Math.Max(1, Main.npcFrameCount[npcType])
                : 1;
            size = new Vector2(sheet.Value.Width, sheet.Value.Height / (float)frames);
        }

        size = Vector2.Max(size, Vector2.One);
        SpriteSizes[(data.ID, data.IsShiny)] = size;
        return size;
    }

    #endregion

    #region Right side

    private void DrawRightSide(SpriteBatch sb, Point o, PokemonData data)
    {
        var content = new Rectangle(o.X + 288, o.Y + 46, 478, 370);
        Panel(sb, content, PanelBg);
        var tabBar = new Rectangle(o.X + 288, o.Y + 420, 478, 38);
        Panel(sb, tabBar, PanelDarkBg);

        // Tabs
        var tabWidth = (tabBar.Width - 16) / _tabRects.Length;
        for (var i = 0; i < _tabRects.Length; i++)
        {
            var r = new Rectangle(tabBar.X + 8 + i * tabWidth, tabBar.Y + 5, tabWidth, 28);
            _tabRects[i] = r;
            var selected = (int)SummaryUI.CurrentTab == i;
            var hover = Hovering(r);
            if (selected)
            {
                Fill(sb, r, new Color(52, 70, 130));
                Fill(sb, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), Gold);
                var tip = new Vector2(r.Center.X, r.Y + 7);
                FillPolygon(sb, [tip + new Vector2(-5, -5), tip + new Vector2(5, -5), tip], Gold);
            }
            else if (hover)
            {
                Fill(sb, r, new Color(40, 54, 104));
            }

            TextCentered(sb, T("Tabs." + (SummaryUI.Tab)i), r with { Y = r.Y + 2 },
                selected ? GoldText : hover ? Color.White : DimText, 0.8f);
        }

        var c = new Rectangle(content.X + 14, content.Y + 12, content.Width - 28, content.Height - 24);
        switch (SummaryUI.CurrentTab)
        {
            case SummaryUI.Tab.Stats:
                DrawStatsTab(sb, c, data);
                break;
            case SummaryUI.Tab.IVs:
                DrawIVsTab(sb, c, data);
                break;
            case SummaryUI.Tab.EVs:
                DrawEVsTab(sb, c, data);
                break;
            case SummaryUI.Tab.Moves:
                DrawMovesTab(sb, c, data);
                break;
            case SummaryUI.Tab.Other:
                DrawOtherTab(sb, c, data);
                break;
        }
    }

    private static void TabTitle(SpriteBatch sb, Rectangle c, string title, string right = null)
    {
        Text(sb, Upper(title), c.X, c.Y + 9, GoldText, 0.85f, 0f, 0.5f);
        if (right != null)
            Text(sb, right, c.Right, c.Y + 9, ValueText, 0.8f, 1f, 0.5f);
        Fill(sb, new Rectangle(c.X, c.Y + 22, c.Width, 1), PanelBorder);
    }

    private void DrawStatsTab(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        TabTitle(sb, c, T("Tabs.Stats"));
        var stats = CalcStats(data);
        NatureEffect(data.Nature, out var up, out var down);

        // Reference: a base 150 stat with perfect IVs and EVs at this level fills the chart
        var level = data.Level;
        var refHP = (2 * 150 + 31 + 63) * level / 100 + level + 10;
        var refOther = (2 * 150 + 31 + 63) * level / 100 + 5;
        var values = new float[6];
        var labels = new string[6];
        var colors = new Color[6];
        for (var i = 0; i < 6; i++)
        {
            values[i] = Math.Clamp(stats[i] / (float)(i == 0 ? refHP : refOther), 0f, 1f);
            labels[i] = stats[i].ToString();
            colors[i] = i == up ? PlusColor : i == down ? MinusColor : Color.White;
        }

        Radar(sb, new Vector2(c.X + 152, c.Y + 186), 92f, values, labels, colors, null,
            new Color(77, 139, 255), new Color(156, 196, 255));

        // Base stats of the species
        var colX = c.X + 314;
        var colW = c.Right - colX;
        var baseStats = data.Schema.BaseStats;
        int[] bases = [baseStats.HP, baseStats.Attack, baseStats.Defense, baseStats.SpAtk, baseStats.SpDef, baseStats.Speed];
        Text(sb, Upper(T("BaseStats")), colX, c.Y + 42, LabelText, 0.72f, 0f, 0.5f);
        for (var i = 0; i < 6; i++)
        {
            var y = c.Y + 64 + i * 20;
            Text(sb, T("Stat." + StatKeys[i]), colX, y, DimText, 0.68f, 0f, 0.5f);
            var bar = new Rectangle(colX + 44, y - 4, colW - 78, 8);
            Bar(sb, bar, bases[i] / 200f, BaseStatColor(bases[i]));
            Text(sb, bases[i].ToString(), c.Right, y, ValueText, 0.68f, 1f, 0.5f);
        }

        Text(sb, T("BaseTotal", bases.Sum()), colX, c.Y + 190, DimText, 0.68f, 0f, 0.5f);

        // Nature box
        var box = new Rectangle(colX - 4, c.Y + 210, colW + 4, 74);
        Fill(sb, box, PanelDarkBg);
        Outline(sb, box, PanelBorder);
        Text(sb, Upper(T("Nature")), box.X + 8, box.Y + 12, LabelText, 0.68f, 0f, 0.5f);
        TextFit(sb, NatureName(data.Nature), box.X + 8, box.Y + 32, box.Width - 16, GoldText, 0.82f, 0.5f);
        if (up < 0)
        {
            Text(sb, T("NatureNeutral"), box.X + 8, box.Y + 54, DimText, 0.68f, 0f, 0.5f);
        }
        else
        {
            TextFit(sb, "+ " + T("StatLong." + StatKeys[up]), box.X + 8, box.Y + 50, box.Width - 16, PlusColor,
                0.68f, 0.5f);
            TextFit(sb, "- " + T("StatLong." + StatKeys[down]), box.X + 8, box.Y + 64, box.Width - 16, MinusColor,
                0.68f, 0.5f);
        }

        Text(sb, T("StatsHint"), c.X, c.Bottom - 6, DimText, 0.62f, 0f, 0.5f);
    }

    private void DrawIVsTab(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        var ivs = data.IVs;
        int[] v = [ivs.HP, ivs.Attack, ivs.Defense, ivs.SpAtk, ivs.SpDef, ivs.Speed];
        var total = v.Sum();
        TabTitle(sb, c, T("Tabs.IVs"));

        // A smaller radar on the left, so there's room for a plain-words rating of every stat
        var values = v.Select(x => x / 31f).ToArray();
        var labels = v.Select(x => x.ToString()).ToArray();
        var maxed = v.Select(x => x == 31).ToArray();
        var colors = maxed.Select(m => m ? GoldText : Color.White).ToArray();
        Radar(sb, new Vector2(c.X + 118, c.Y + 142), 70f, values, labels, colors, maxed,
            new Color(80, 200, 140), new Color(160, 240, 190));

        // Each stat: name, value and what that value means ("31 Perfect", "18 Good"...)
        var colX = c.X + 250;
        var colW = c.Right - colX;
        for (var i = 0; i < 6; i++)
        {
            var y = c.Y + 44 + i * 21;
            var rating = IVRating(v[i]);
            if (i % 2 == 0)
                Fill(sb, new Rectangle(colX - 4, y - 10, colW + 4, 20), PanelDarkBg);
            TextFit(sb, T("StatLong." + StatKeys[i]), colX, y, 96, DimText, 0.68f, 0.5f);
            Text(sb, v[i].ToString(), colX + 120, y, v[i] == 31 ? GoldText : ValueText, 0.78f, 1f, 0.5f);
            TextFit(sb, T("IVRating." + rating), colX + 130, y, colW - 132, IVRatingColor(rating), 0.68f, 0.5f);
        }

        // Total and the overall judgement
        var totalY = c.Y + 186;
        Text(sb, Upper(T("Total")), colX, totalY, LabelText, 0.72f, 0f, 0.5f);
        Text(sb, $"{total}/186", c.Right, totalY, GoldText, 0.85f, 1f, 0.5f);
        Bar(sb, new Rectangle(colX, totalY + 12, colW, 8), total / 186f, new Color(80, 200, 140));
        var judge = total switch
        {
            >= 186 => "Perfect",
            > 150 => "Outstanding",
            > 120 => "RelativelySuperior",
            > 90 => "AboveAverage",
            _ => "Decent"
        };
        TextFit(sb, T("Judge." + judge), colX, totalY + 36, colW, ValueText, 0.74f, 0.5f);

        // Legend: gold dots are perfect IVs
        Fill(sb, new Rectangle(colX, totalY + 53, 6, 6), GoldText);
        TextFit(sb, T("PerfectLegend"), colX + 12, totalY + 56, colW - 12, DimText, 0.62f, 0.5f);

        // What IVs are, in a few plain words
        var box = new Rectangle(c.X, c.Y + 256, c.Width, c.Bottom - (c.Y + 256));
        Fill(sb, box, OutlineDark);
        Fill(sb, Inset(box, 1), new Color(16, 22, 48));
        Text(sb, Upper(T("IVsWhat")), box.X + 10, box.Y + 11, GoldText, 0.68f, 0f, 0.5f);
        Paragraph(sb, T("IVsInfo"), box.X + 10, box.Y + 21, box.Width - 20, 0.62f, 4, Color.White);
    }

    /// <summary>
    ///     A word for how good one IV is, so beginners can read the numbers at a glance.
    /// </summary>
    private static string IVRating(int iv) => iv switch
    {
        >= 31 => "Perfect",
        >= 26 => "Great",
        >= 16 => "Good",
        >= 1 => "Fair",
        _ => "None"
    };

    private static Color IVRatingColor(string rating) => rating switch
    {
        "Perfect" => GoldText,
        "Great" => new Color(90, 210, 110),
        "Good" => new Color(170, 215, 80),
        "Fair" => new Color(240, 170, 70),
        _ => new Color(230, 90, 80)
    };

    private void DrawEVsTab(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        var evs = data.EVs;
        int[] v = [evs.HP, evs.Attack, evs.Defense, evs.SpAttack, evs.SpDefense, evs.Speed];
        var total = (int)evs.Sum;
        TabTitle(sb, c, T("Tabs.EVs"));

        var values = v.Select(x => x / 252f).ToArray();
        var labels = v.Select(x => x.ToString()).ToArray();
        var maxed = v.Select(x => x >= 252).ToArray();
        var colors = maxed.Select(m => m ? GoldText : Color.White).ToArray();
        Radar(sb, new Vector2(c.X + 152, c.Y + 186), 92f, values, labels, colors, maxed,
            new Color(255, 160, 80), new Color(255, 210, 150));

        var colX = c.X + 314;
        var colW = c.Right - colX;
        Text(sb, Upper(T("Total")), colX, c.Y + 42, LabelText, 0.72f, 0f, 0.5f);
        Text(sb, $"{total}/510", colX, c.Y + 64, GoldText, 0.95f, 0f, 0.5f);
        Bar(sb, new Rectangle(colX, c.Y + 80, colW, 8), total / 510f, new Color(255, 160, 80));
        TextFit(sb, total >= 510 ? T("EVsFull") : T("EVsLeft", 510 - total), colX, c.Y + 104, colW, ValueText,
            0.72f, 0.5f);

        Fill(sb, new Rectangle(colX, c.Y + 126, 6, 6), GoldText);
        TextFit(sb, T("MaxedLegend"), colX + 12, c.Y + 129, colW - 12, DimText, 0.65f, 0.5f);

        Paragraph(sb, T("EVsInfo"), colX, c.Y + 146, colW, 0.65f, 8, DimText);
    }

    private void DrawMovesTab(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        _relearnRows.Clear();
        _relearnCancel = Rectangle.Empty;
        Array.Fill(_relearnButtons, Rectangle.Empty);
        if (SummaryUI.RelearnSlot >= 0)
        {
            DrawRelearnList(sb, c, data);
            return;
        }

        TabTitle(sb, c, T("Tabs.Moves"));
        var canRelearn = SummaryUI.CanRelearn(data) && SummaryUI.RelearnableMoves(data).Count > 0;
        var hovered = -1;
        for (var i = 0; i < 4; i++)
        {
            var r = new Rectangle(c.X, c.Y + 30 + i * 58, c.Width, 52);
            _moveRects[i] = r;
            var move = data.Moves.IsDefault ? default : data.Moves[i];
            var schema = move.ID == MoveID.None ? null : move.Schema;
            var hover = Hovering(r) && schema != null;
            if (hover)
                hovered = i;
            var selected = SummaryUI.SelectedMove == i && schema != null;

            Fill(sb, r, OutlineDark);
            Fill(sb, Inset(r, 1), selected ? new Color(50, 68, 128) : hover ? new Color(42, 57, 110) : PanelDarkBg);
            if (selected)
                Outline(sb, r, Gold);

            if (schema == null)
            {
                Text(sb, "—", r.X + 16, r.Center.Y, DimText, 0.85f, 0f, 0.5f);
                if (canRelearn)
                    RelearnButton(sb, i, new Rectangle(r.Right - 110, r.Center.Y - 10, 100, 20), T("Relearn.Learn"));
                continue;
            }

            var typeColor = schema.Type.GetColor();
            Fill(sb, new Rectangle(r.X + 1, r.Y + 1, 6, r.Height - 2), typeColor);

            // Name, then category and power/accuracy
            Text(sb, MoveLearnUI.MoveName(move.ID), r.X + 16, r.Y + 15, Color.White, 0.88f, 0f, 0.5f);
            var category = Language.GetTextValue($"Mods.Terramon.Battle.MoveInfo.{schema.Category}");
            var catWidth = (int)Measure(category, 0.62f).X + 10;
            var catRect = new Rectangle(r.X + 16, r.Y + 30, catWidth, 15);
            Fill(sb, catRect, CategoryColor(schema.Category));
            TextCentered(sb, category, catRect, Color.White, 0.62f);
            var power = schema.Power is > 1 ? schema.Power.ToString() : "—";
            var accuracy = schema.Accuracy is { } acc and <= 100 ? acc.ToString() : "—";
            Text(sb, T("PowerAccuracy", power, accuracy), catRect.Right + 8, catRect.Center.Y, DimText, 0.68f, 0f,
                0.5f);

            // Type and PP on the right
            TypeBadge(sb, new Rectangle(r.Right - 96, r.Y + 7, 88, 18), schema.Type, 0.65f);
            var maxPP = schema.PP + schema.PP * move.PPUp / 5;
            var ppColor = move.PP == 0 ? PlusColor : move.PP * 4 <= maxPP ? GoldText : ValueText;
            Text(sb, T("PP", move.PP, maxPP), r.Right - 10, r.Y + 38, ppColor, 0.72f, 1f, 0.5f);

            if (canRelearn)
                RelearnButton(sb, i, new Rectangle(r.Right - 186, r.Y + 7, 82, 18), T("Relearn.Change"));
        }

        // Description of the hovered (or selected) move
        var shown = hovered >= 0 ? hovered : SummaryUI.SelectedMove;
        var box = new Rectangle(c.X, c.Y + 264, c.Width, c.Bottom - (c.Y + 264));
        Fill(sb, box, OutlineDark);
        Fill(sb, Inset(box, 1), new Color(16, 22, 48));
        var shownMove = data.Moves.IsDefault ? default : data.Moves[Math.Clamp(shown, 0, 3)];
        if (shownMove.ID == MoveID.None)
        {
            Text(sb, T("NoMoveSelected"), box.X + 10, box.Center.Y, DimText, 0.7f, 0f, 0.5f);
            return;
        }

        Text(sb, MoveLearnUI.MoveName(shownMove.ID), box.X + 10, box.Y + 12, GoldText, 0.72f, 0f, 0.5f);
        Paragraph(sb, Language.GetTextValue($"Mods.Terramon.Moves.{shownMove.ID}.FlavorText"), box.X + 10,
            box.Y + 22, box.Width - 20, 0.68f, 3, Color.White);
    }

    /// <summary>
    ///     The "Change move" / "Learn a move" button of a move card, which opens the move relearner list.
    /// </summary>
    private void RelearnButton(SpriteBatch sb, int slot, Rectangle r, string label)
    {
        _relearnButtons[slot] = r;
        var hover = Hovering(r);
        Fill(sb, r, OutlineDark);
        Fill(sb, Inset(r, 1), hover ? HeaderLight : HeaderRed);
        Fill(sb, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, 1), HeaderLight);
        TextCentered(sb, label, r, Color.White, 0.62f);
        if (hover)
            MoveTooltip.Show(T("Relearn.ButtonHint"));
    }

    /// <summary>
    ///     The move relearner: every level-up move the Pokémon could already know, to put in the chosen slot.
    /// </summary>
    private void DrawRelearnList(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        var slot = SummaryUI.RelearnSlot;
        var old = data.Moves.IsDefault ? MoveID.None : data.Moves[slot].ID;
        TabTitle(sb, c, old == MoveID.None ? T("Relearn.TitleEmpty") : T("Relearn.TitleReplace", MoveLearnUI.MoveName(old)));
        TextFit(sb, T("Relearn.Hint"), c.X, c.Y + 34, c.Width, DimText, 0.64f, 0.5f);

        const int rowHeight = 24;
        var listTop = c.Y + 48;
        var listBottom = c.Bottom - 32;
        var visible = Math.Max(1, (listBottom - listTop) / rowHeight);
        var moves = SummaryUI.RelearnableMoves(data);
        SummaryUI.RelearnScroll = Math.Clamp(SummaryUI.RelearnScroll, 0, Math.Max(0, moves.Count - visible));
        var scroll = SummaryUI.RelearnScroll;
        var listWidth = c.Width - (moves.Count > visible ? 10 : 0);

        if (moves.Count == 0)
            Text(sb, T("Relearn.NoMoves"), c.X + 4, listTop + 12, DimText, 0.72f, 0f, 0.5f);

        for (var i = scroll; i < moves.Count && i < scroll + visible; i++)
        {
            var (level, move) = moves[i];
            var schema = Terramon.DatabaseV2.GetMove(move);
            var r = new Rectangle(c.X, listTop + (i - scroll) * rowHeight, listWidth, rowHeight - 2);
            _relearnRows.Add((r, move));
            var hover = Hovering(r);
            Fill(sb, r, hover ? new Color(50, 68, 128) : i % 2 == 0 ? PanelDarkBg : new Color(27, 38, 76));
            if (hover)
            {
                Outline(sb, r, Gold);
                MoveTooltip.Show(MoveTooltip.Build(move));
            }

            Fill(sb, new Rectangle(r.X + 1, r.Y + 1, 4, r.Height - 2), schema.Type.GetColor());
            var levelText = level == 0 ? T("Relearn.Evolution") : T("Relearn.Level", level);
            TextFit(sb, levelText, r.X + 10, r.Center.Y, 50, DimText, 0.62f, 0.5f);
            TextFit(sb, MoveLearnUI.MoveName(move), r.X + 66, r.Center.Y, 150, Color.White, 0.76f, 0.5f);
            TypeBadge(sb, new Rectangle(r.X + 222, r.Y + 3, 76, r.Height - 6), schema.Type, 0.58f);
            var category = Language.GetTextValue($"Mods.Terramon.Battle.MoveInfo.{schema.Category}");
            var catRect = new Rectangle(r.X + 304, r.Y + 4, 64, r.Height - 8);
            Fill(sb, catRect, CategoryColor(schema.Category));
            TextCentered(sb, category, catRect, Color.White, 0.56f);
            var power = schema.Power is > 1 ? schema.Power.ToString() : "—";
            Text(sb, power, r.Right - 8, r.Center.Y, ValueText, 0.72f, 1f, 0.5f);
        }

        // Scroll bar when the list is longer than the box
        if (moves.Count > visible)
        {
            var track = new Rectangle(c.Right - 6, listTop, 6, visible * rowHeight - 2);
            Fill(sb, track, OutlineDark);
            var thumbHeight = Math.Max(12, track.Height * visible / moves.Count);
            var thumbY = track.Y + (track.Height - thumbHeight) * scroll / Math.Max(1, moves.Count - visible);
            Fill(sb, new Rectangle(track.X + 1, thumbY, 4, thumbHeight), Gold);
        }

        // Cancel
        _relearnCancel = new Rectangle(c.Right - 120, c.Bottom - 24, 120, 22);
        var cancelHover = Hovering(_relearnCancel);
        Fill(sb, _relearnCancel, OutlineDark);
        Fill(sb, Inset(_relearnCancel, 1), cancelHover ? new Color(73, 94, 171) : new Color(52, 70, 130));
        TextCentered(sb, T("Relearn.Cancel"), _relearnCancel, Color.White, 0.7f);
    }

    private void DrawOtherTab(SpriteBatch sb, Rectangle c, PokemonData data)
    {
        TabTitle(sb, c, T("Tabs.Other"));

        // Left: how and where it was met
        var leftX = c.X;
        var colW = 205;
        Text(sb, Upper(T("MetSection")), leftX, c.Y + 40, LabelText, 0.72f, 0f, 0.5f);

        var y = c.Y + 60;
        var ball = BallAssets.GetBallIcon(data.Ball);
        var ballX = leftX;
        if (ball != null)
        {
            sb.Draw(ball.Value, new Vector2(leftX, y + 8 - ball.Value.Height / 2f), Color.White);
            ballX += ball.Value.Width + 6;
        }

        LabelValue(sb, T("CaughtIn"), BallName(data.Ball), ballX, y, colW - (ballX - leftX));
        y += 36;
        LabelValue(sb, T("OT"), string.IsNullOrEmpty(data.OT) ? T("Unknown") : data.OT, leftX, y, colW);
        y += 36;
        var metDate = data.MetDate.HasValue
            ? data.MetDate.Value.ToString("d", Language.ActiveCulture.CultureInfo)
            : T("Unknown");
        LabelValue(sb, T("Met"), T("MetValue", data.MetLevel, metDate), leftX, y, colW);
        y += 36;
        LabelValue(sb, T("World"), string.IsNullOrEmpty(data.MetWorld) ? T("Unknown") : data.MetWorld, leftX, y,
            colW);

        // Right: friendship and characteristic
        var rightX = c.X + 235;
        var rightW = c.Right - rightX;
        Text(sb, Upper(T("Friendship")), rightX, c.Y + 40, LabelText, 0.72f, 0f, 0.5f);
        var hearts = (int)MathF.Round(data.Happiness / 51f);
        for (var i = 0; i < 5; i++)
            Heart(sb, rightX + i * 18, c.Y + 56, i < hearts);
        Text(sb, $"{data.Happiness}/255", rightX + 96, c.Y + 63, ValueText, 0.7f, 0f, 0.5f);
        TextFit(sb, T("FriendshipLevel." + Math.Min(hearts, 5)), rightX, c.Y + 86, rightW, DimText, 0.65f, 0.5f);

        Text(sb, Upper(T("Characteristic")), rightX, c.Y + 116, LabelText, 0.72f, 0f, 0.5f);
        Paragraph(sb, Characteristic(data), rightX, c.Y + 126, rightW, 0.75f, 3, ValueText);

        DrawFieldAbilities(sb, new Rectangle(c.X, c.Y + 214, c.Width, c.Bottom - (c.Y + 214)), data);
    }

    /// <summary>
    ///     The "Field abilities" section of the Other tab (Ride, Fly, Surf, Dig badges).
    /// </summary>
    private void DrawFieldAbilities(SpriteBatch sb, Rectangle area, PokemonData data)
    {
        Fill(sb, area, OutlineDark);
        Fill(sb, Inset(area, 1), new Color(16, 22, 48));
        Text(sb, Upper(T("FieldAbilities")), area.X + 10, area.Y + 14, LabelText, 0.72f, 0f, 0.5f);

        var abilities = GetFieldAbilities(data);
        var badgeW = (area.Width - 20 - 3 * 8) / 4;
        for (var i = 0; i < FieldAbilityKeys.Length; i++)
        {
            var r = new Rectangle(area.X + 10 + i * (badgeW + 8), area.Y + 28, badgeW, 40);
            var has = abilities != null && abilities[i];
            var color = has ? FieldAbilityColors[i] : new Color(60, 66, 90);
            Fill(sb, r, OutlineDark);
            Fill(sb, Inset(r, 1), Color.Lerp(color, Color.Black, 0.25f));
            Fill(sb, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, 2), Color.Lerp(color, Color.White, 0.3f));
            TextCentered(sb, T("Field." + FieldAbilityKeys[i]), r, has ? Color.White : DimText, 0.75f);
        }

        var note = abilities == null ? T("FieldSoon") : abilities.Any(a => a) ? T("FieldHint") : T("FieldNone");
        Paragraph(sb, note, area.X + 10, area.Y + 72, area.Width - 20, 0.62f, 2, DimText);
    }

    /// <summary>
    ///     Extension point for field abilities: which of Ride, Fly, Surf and Dig (in <see cref="FieldAbilityKeys" />
    ///     order) <paramref name="data" /> can use outside battle, or null while this build has no field-ability system
    ///     (the section then shows grey "coming soon" badges). Point this at <c>Terramon.Core.FieldAbilities</c> once
    ///     it exists.
    /// </summary>
    private static bool[] GetFieldAbilities(PokemonData data) =>
    [
        FieldAbilities.CanRide(data), FieldAbilities.CanFly(data), FieldAbilities.CanSurf(data),
        FieldAbilities.CanDig(data)
    ];

    private static void LabelValue(SpriteBatch sb, string label, string value, float x, float y, float width)
    {
        Text(sb, label, x, y, DimText, 0.62f, 0f, 0.5f);
        TextFit(sb, value, x, y + 17, width, ValueText, 0.78f, 0.5f);
    }

    private static void Heart(SpriteBatch sb, int x, int y, bool full)
    {
        string[] rows = [".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..."];
        var color = full ? new Color(235, 72, 96) : new Color(64, 46, 72);
        for (var row = 0; row < rows.Length; row++)
        for (var col = 0; col < rows[row].Length; col++)
        {
            if (rows[row][col] == 'X')
                Fill(sb, new Rectangle(x + col * 2, y + row * 2, 2, 2), color);
        }

        if (full)
            Fill(sb, new Rectangle(x + 2, y + 2, 2, 2), new Color(255, 170, 180));
    }

    #endregion

    #region Radar chart

    /// <summary>
    ///     A hexagon radar chart: background, rings at 25/50/75/100 %, spokes, the filled value shape and labels.
    ///     Arrays are in the HP, Atk, Def, SpA, SpD, Spe order; <paramref name="values" /> go from 0 to 1.
    /// </summary>
    private static void Radar(SpriteBatch sb, Vector2 center, float radius, float[] values, string[] valueLabels,
        Color[] nameColors, bool[] maxed, Color fill, Color line)
    {
        FillPolygon(sb, Hexagon(center, radius + 8), RadarBg);
        PolyLine(sb, Hexagon(center, radius + 8), RadarRing, 1f);
        for (var ring = 1; ring <= 4; ring++)
            PolyLine(sb, Hexagon(center, radius * ring / 4f), ring == 4 ? PanelBorder : RadarRing, 1f);

        var outer = Hexagon(center, radius);
        foreach (var vertex in outer)
            Line(sb, center, vertex, RadarSpoke, 1f);

        // Grow in when the tab opens (ease out)
        var t = 1f - MathF.Pow(1f - SummaryUI.Appear, 3f);
        var shape = new Vector2[6];
        for (var axis = 0; axis < 6; axis++)
        {
            var stat = RadarOrder[axis];
            var dir = Direction(axis);
            shape[axis] = center + dir * Math.Max(2f, radius * values[stat] * t);
        }

        FillPolygon(sb, shape, fill * 0.6f);
        PolyLine(sb, shape, line, 2f);
        for (var axis = 0; axis < 6; axis++)
        {
            var stat = RadarOrder[axis];
            var p = shape[axis];
            var dot = maxed != null && maxed[stat] ? GoldText : line;
            Fill(sb, new Rectangle((int)p.X - 2, (int)p.Y - 2, 5, 5), OutlineDark);
            Fill(sb, new Rectangle((int)p.X - 1, (int)p.Y - 1, 3, 3), dot);
        }

        // Labels: stat name, value under it
        for (var axis = 0; axis < 6; axis++)
        {
            var stat = RadarOrder[axis];
            var dir = Direction(axis);
            var p = center + dir * (radius + 16);
            var anchorX = MathF.Abs(dir.X) < 0.1f ? 0.5f : dir.X > 0 ? 0f : 1f;
            float nameY;
            if (dir.Y < -0.9f)
                nameY = p.Y - 30;
            else if (dir.Y > 0.9f)
                nameY = p.Y + 2;
            else
                nameY = p.Y - 15;

            Text(sb, T("Stat." + StatKeys[stat]), p.X, nameY, nameColors[stat], 0.7f, anchorX);
            Text(sb, valueLabels[stat], p.X, nameY + 14, GoldText, 0.82f, anchorX);
        }
    }

    private static Vector2 Direction(int axis)
    {
        var angle = MathHelper.ToRadians(-90f + axis * 60f);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    private static Vector2[] Hexagon(Vector2 center, float radius)
    {
        var points = new Vector2[6];
        for (var i = 0; i < 6; i++)
            points[i] = center + Direction(i) * radius;
        return points;
    }

    #endregion

    #region Pokémon data helpers

    /// <summary>
    ///     Current stats in the HP, Atk, Def, SpA, SpD, Spe order, with the nature applied.
    /// </summary>
    private static int[] CalcStats(PokemonData data)
    {
        var b = data.Schema.BaseStats;
        var iv = data.IVs;
        var ev = data.EVs;
        int level = data.Level;
        NatureEffect(data.Nature, out var up, out var down);

        int Stat(int index, int baseStat, int ivValue, int evValue)
        {
            var raw = (2 * baseStat + ivValue + evValue / 4) * level / 100 + 5;
            if (index == up) return raw * 110 / 100;
            if (index == down) return raw * 90 / 100;
            return raw;
        }

        return
        [
            data.MaxHP,
            Stat(1, b.Attack, iv.Attack, ev.Attack),
            Stat(2, b.Defense, iv.Defense, ev.Defense),
            Stat(3, b.SpAtk, iv.SpAtk, ev.SpAttack),
            Stat(4, b.SpDef, iv.SpDef, ev.SpDefense),
            Stat(5, b.Speed, iv.Speed, ev.Speed)
        ];
    }

    /// <summary>
    ///     Which stat (HP, Atk, Def, SpA, SpD, Spe index) the nature raises and lowers; both -1 for neutral natures.
    /// </summary>
    private static void NatureEffect(NatureID nature, out int up, out int down)
    {
        var n = (int)nature;
        up = NatureStat[n / 5];
        down = NatureStat[n % 5];
        if (up != down) return;
        up = down = -1;
    }

    private static string NatureName(NatureID nature) => Language.GetTextValue("Mods.Terramon.Natures." + nature);

    private static string NatureText(PokemonData data)
    {
        NatureEffect(data.Nature, out var up, out _);
        var name = NatureName(data.Nature);
        return up < 0 ? name : $"{name} (+{T("Stat." + StatKeys[up])})";
    }

    // Ability names stay in English, like move names ("SolarPower" -> "Solar Power")
    private static string AbilityName(AbilityID ability) =>
        Regex.Replace(ability.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    private static string BallName(BallID ball) => Language.GetTextValue($"Mods.Terramon.Items.{ball}Item.DisplayName");

    /// <summary>
    ///     The Pokémon's characteristic, from its highest IV (ties go to the first in HP, Atk, Def, Spe, SpA, SpD order)
    ///     and that IV's value modulo 5, like in the main games.
    /// </summary>
    private static string Characteristic(PokemonData data)
    {
        var ivs = data.IVs;
        (string Key, int Value)[] order =
        [
            ("HP", ivs.HP), ("Atk", ivs.Attack), ("Def", ivs.Defense), ("Spe", ivs.Speed), ("SpA", ivs.SpAtk),
            ("SpD", ivs.SpDef)
        ];
        var best = order[0];
        foreach (var entry in order)
        {
            if (entry.Value > best.Value)
                best = entry;
        }

        return T($"Characteristics.{best.Key}{best.Value % 5}");
    }

    private static Color HPColor(float percent) => percent switch
    {
        > 0.5f => new Color(90, 210, 110),
        > 0.2f => new Color(240, 200, 70),
        _ => new Color(230, 80, 70)
    };

    private static Color BaseStatColor(int value) => value switch
    {
        >= 150 => new Color(80, 220, 200),
        >= 120 => new Color(90, 210, 110),
        >= 90 => new Color(170, 215, 80),
        >= 60 => new Color(240, 200, 70),
        _ => new Color(240, 130, 70)
    };

    private static Color CategoryColor(MoveCategory category) => category switch
    {
        MoveCategory.Physical => new Color(200, 92, 60),
        MoveCategory.Special => new Color(72, 116, 210),
        _ => new Color(120, 124, 140)
    };

    #endregion

    #region Drawing primitives

    private static Rectangle Inset(Rectangle r, int amount) =>
        new(r.X + amount, r.Y + amount, r.Width - amount * 2, r.Height - amount * 2);

    private static void Fill(SpriteBatch sb, Rectangle r, Color color) => sb.Draw(Pixel, r, PixelSource, color);

    private static void Outline(SpriteBatch sb, Rectangle r, Color color)
    {
        Fill(sb, new Rectangle(r.X, r.Y, r.Width, 1), color);
        Fill(sb, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), color);
        Fill(sb, new Rectangle(r.X, r.Y, 1, r.Height), color);
        Fill(sb, new Rectangle(r.Right - 1, r.Y, 1, r.Height), color);
    }

    private static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness)
    {
        var d = b - a;
        var length = d.Length();
        if (length < 0.01f)
            return;
        sb.Draw(Pixel, a, PixelSource, color, MathF.Atan2(d.Y, d.X), new Vector2(0f, 0.5f),
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private static void PolyLine(SpriteBatch sb, IReadOnlyList<Vector2> points, Color color, float thickness)
    {
        for (var i = 0; i < points.Count; i++)
            Line(sb, points[i], points[(i + 1) % points.Count], color, thickness);
    }

    /// <summary>
    ///     Fills any simple polygon (convex or not) one pixel row at a time, so edges stay crisp like pixel art.
    /// </summary>
    private static void FillPolygon(SpriteBatch sb, IReadOnlyList<Vector2> points, Color color)
    {
        if (points.Count < 3)
            return;

        var minY = float.MaxValue;
        var maxY = float.MinValue;
        foreach (var p in points)
        {
            minY = Math.Min(minY, p.Y);
            maxY = Math.Max(maxY, p.Y);
        }

        for (var y = (int)MathF.Floor(minY); y < (int)MathF.Ceiling(maxY); y++)
        {
            var yc = y + 0.5f;
            Crossings.Clear();
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                var a = points[i];
                var b = points[j];
                if ((a.Y <= yc && b.Y > yc) || (b.Y <= yc && a.Y > yc))
                    Crossings.Add(a.X + (yc - a.Y) * (b.X - a.X) / (b.Y - a.Y));
            }

            Crossings.Sort();
            for (var k = 0; k + 1 < Crossings.Count; k += 2)
            {
                var x0 = (int)MathF.Round(Crossings[k]);
                var x1 = (int)MathF.Round(Crossings[k + 1]);
                if (x1 > x0)
                    Fill(sb, new Rectangle(x0, y, x1 - x0, 1), color);
            }
        }
    }

    /// <summary>
    ///     A navy panel with a dark outline, a light inner border and gold rivets in the corners.
    /// </summary>
    private static void Panel(SpriteBatch sb, Rectangle r, Color background)
    {
        Fill(sb, r, OutlineDark);
        Fill(sb, Inset(r, 1), PanelBorder);
        Fill(sb, Inset(r, 2), background);
        Rivets(sb, r);
    }

    private static void Rivets(SpriteBatch sb, Rectangle r)
    {
        Rivet(sb, r.X + 3, r.Y + 3);
        Rivet(sb, r.Right - 7, r.Y + 3);
        Rivet(sb, r.X + 3, r.Bottom - 7);
        Rivet(sb, r.Right - 7, r.Bottom - 7);
    }

    private static void Rivet(SpriteBatch sb, int x, int y)
    {
        Fill(sb, new Rectangle(x, y, 4, 4), GoldDark);
        Fill(sb, new Rectangle(x, y, 3, 3), Gold);
        Fill(sb, new Rectangle(x, y, 1, 1), GoldLight);
    }

    private static void Bar(SpriteBatch sb, Rectangle r, float percent, Color color)
    {
        Fill(sb, r, OutlineDark);
        var inner = Inset(r, 1);
        Fill(sb, inner, new Color(12, 16, 34));
        var width = (int)(inner.Width * Math.Clamp(percent, 0f, 1f));
        if (width <= 0)
            return;
        Fill(sb, inner with { Width = width }, color);
        Fill(sb, new Rectangle(inner.X, inner.Y, width, Math.Max(1, inner.Height / 3)), Color.Lerp(color, Color.White, 0.35f));
    }

    private static void TypeBadge(SpriteBatch sb, Rectangle r, PokemonType type, float scale)
    {
        var color = type.GetColor();
        Fill(sb, r, Color.Lerp(color, Color.Black, 0.6f));
        var inner = Inset(r, 1);
        Fill(sb, inner, Color.Lerp(color, Color.Black, 0.2f));
        Fill(sb, new Rectangle(inner.X, inner.Y, inner.Width, 2), Color.Lerp(color, Color.White, 0.3f));
        TextCentered(sb, Upper(Language.GetTextValue($"Mods.Terramon.Types.{type}")), r, Color.White, scale);
    }

    #endregion

    #region Text

    private static string Upper(string text) => text.ToUpper(Language.ActiveCulture.CultureInfo);

    private static Vector2 Measure(string text, float scale) => Font.MeasureString(text) * scale;

    /// <summary>
    ///     Terraria's outlined text. <paramref name="anchorY" /> 0.5 centres the line on <paramref name="y" />.
    /// </summary>
    private static void Text(SpriteBatch sb, string text, float x, float y, Color color, float scale,
        float anchorX = 0f, float anchorY = 0f)
    {
        if (string.IsNullOrEmpty(text))
            return;
        Utils.DrawBorderString(sb, text, new Vector2(x, y + (anchorY > 0f ? 2f * scale : 0f)), color, scale, anchorX,
            anchorY);
    }

    /// <summary>
    ///     Like <see cref="Text" />, shrinking the text so it fits in <paramref name="maxWidth" />.
    /// </summary>
    private static void TextFit(SpriteBatch sb, string text, float x, float y, float maxWidth, Color color,
        float scale, float anchorY = 0f)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            return;
        var width = Measure(text, scale).X;
        if (width > maxWidth)
            scale *= maxWidth / width;
        Text(sb, text, x, y, color, scale, 0f, anchorY);
    }

    private static void TextCentered(SpriteBatch sb, string text, Rectangle r, Color color, float scale)
    {
        var width = Measure(text, scale).X;
        if (width > r.Width - 6)
            scale *= (r.Width - 6) / width;
        Text(sb, text, r.Center.X, r.Center.Y, color, scale, 0.5f, 0.5f);
    }

    /// <summary>
    ///     Word-wrapped text, at most <paramref name="maxLines" /> lines.
    /// </summary>
    private static void Paragraph(SpriteBatch sb, string text, float x, float y, float width, float scale,
        int maxLines, Color color)
    {
        if (string.IsNullOrEmpty(text))
            return;
        var lines = Utils.WordwrapString(text, Font, (int)(width / scale), maxLines, out _);
        var lineHeight = 26f * scale;
        var drawn = 0;
        foreach (var line in lines)
        {
            if (line == null)
                continue;
            Text(sb, line.Trim(), x, y + drawn * lineHeight, color, scale);
            drawn++;
        }
    }

    #endregion
}
