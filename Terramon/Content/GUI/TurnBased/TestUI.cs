using System.Runtime.InteropServices;
using EasyPacketsLib;
using ReLogic.Content;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Content.GUI.Common;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.NPCs;
using Terramon.Core.Loaders.UILoading;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI.TurnBased;

public sealed class TestBattleUI : SmartUIState
{
    private readonly static UIElement _optionsPanel;
    private readonly static UIText _promptText;
    private readonly static UIElement _optionButtons;
    private readonly static UIText _skipHint;
    private readonly static UIElement _movesPanel;
    private readonly static UIElement _pokemonPanel;
    private readonly static UIElement _bagPanel;
    private readonly static UIElement _bagTargetPanel;
    private static DynamicPixelRatioElement _mainPanel;

    private static Point16 _screenDimensions;
    private static bool _opened;

    /// <summary>
    ///     Interface layers that remain active while the battle UI is open/visible.
    ///     All other layers (except the <see cref="BattleUI" /> layer) are deactivated during battle.
    /// </summary>
    private static readonly string[] PreservedInterfaceLayers =
    [
        "Vanilla: Cursor",
        "Vanilla: Entity Health Bars",
        "Vanilla: Hotbar",
        "Vanilla: Interface Logic 1",
        "Vanilla: Interface Logic 2",
        "Vanilla: Interface Logic 3",
        "Vanilla: Interface Logic 4",
        "Vanilla: Mouse Over",
        "Vanilla: Mouse Text",
        "Vanilla: Player Chat",
        "Vanilla: Resource Bars"
    ];

    static TestBattleUI()
    {
        // Create options panel: a prompt on the left half, the four option buttons on the right half
        _optionsPanel = new UIElement();
        _optionsPanel.Width.Percent = _optionsPanel.Height.Percent = 1f;

        _promptText = new UIText(string.Empty, 0.6f, true)
        {
            IsWrapped = true,
            VAlign = 0.5f
        };
        _promptText.Width.Percent = 0.47f;
        _optionsPanel.Append(_promptText);

        var optionButtons = new UIElement();
        optionButtons.Left.Percent = optionButtons.Width.Percent = 0.5f;
        optionButtons.Height.Percent = 1f;
        _optionsPanel.Append(optionButtons);
        _optionButtons = optionButtons;

        // Shown in the corner while a turn is being shown
        _skipHint = new UIText(Terramon.Instance.GetLocalization("Battle.SkipTurnHint"), 0.8f)
        {
            HAlign = 1f,
            VAlign = 1f,
            TextColor = new Color(170, 170, 190)
        };

        for (ButtonType i = ButtonType.Fight; i <= ButtonType.Run; i++)
        {
            int xFactor = (int)i % 2;
            int yFactor = (int)i / 2;
            DynamicPixelRatioElement button = new($"Terramon/Assets/GUI/TurnBased/{i}Button", buttonLike: true);
            button.Left.Percent = xFactor * 0.5f;
            button.Top.Percent = yFactor * 0.5f;
            button.Width.Percent = button.Height.Percent = 0.5f;
            button.OnLeftClick += (_, _) => SoundEngine.PlaySound(in TerramonSoundID.BattleDecide);
            button.OnLeftClick += i switch
            {
                ButtonType.Fight => FightButton,
                ButtonType.Bag => BagButton,
                ButtonType.Pokemon => PokemonButton,
                ButtonType.Run => RunButton,
                _ => null,
            };
            UIText label = new(Terramon.Instance.GetLocalization($"GUI.TurnBased.ButtonLabels.{i}"), 0.75f, true);
            label.HAlign = label.VAlign = 0.5f;
            button.Append(label);
            optionButtons.Append(button);
        }

        // Create moves panel (blank)
        _movesPanel = new();
        _movesPanel.Width.Percent = _movesPanel.Height.Percent = 1f;
        for (int i = 0; i < 4; i++)
        {
            float xFactor = i % 2 * 0.5f;
            float yFactor = i / 2 * 0.5f;
            DynamicPixelRatioElement button = new(null, null, PixelRatioForUI, buttonLike: true);
            button.Left.Percent = xFactor;
            button.Top.Percent = yFactor;
            button.Width.Percent = button.Height.Percent = 0.5f;
            UIText label = new(string.Empty, 0.75f, true);
            label.HAlign = 0.5f;
            label.VAlign = 0.3f;
            button.Append(label);
            // Always-visible details under the name, like the move info screen in the games
            MoveInfoText typeLine = new(string.Empty, 0.38f, true) { HAlign = 0.5f, VAlign = 0.68f };
            MoveInfoText statsLine = new(string.Empty, 0.38f, true) { HAlign = 0.5f, VAlign = 0.88f, Stats = true };
            button.Append(typeLine);
            button.Append(statsLine);
            button.Append(new MoveReference());
            button.OnLeftClick += ClickMoveButton;
            _movesPanel.Append(button);
        }

        // Create Pokémon panel (blank)
        _pokemonPanel = new();
        _pokemonPanel.Width.Percent = _pokemonPanel.Height.Percent = 1f;
        for (int i = 0; i < 6; i++)
        {
            float xFactor = i % 3 / 3f;
            float yFactor = i / 3 * 0.5f;
            DynamicPixelRatioElement button = new("Terramon/Assets/GUI/TurnBased/Simple", buttonLike: true);
            button.Left.Percent = xFactor;
            button.Top.Percent = yFactor;
            button.Width.Percent = 1f / 3f;
            button.Height.Percent = 0.5f;
            UIText label = new(string.Empty, 0.75f, true) { VAlign = 0.5f };
            BesideIcon(label, 0.75f, true);
            UIImage image = new(TextureAssets.Npc[0])
            {
                VAlign = 0.5f
            };
            button.Append(label);
            button.Append(image);
            button.Append(new PokemonReference());
            button.OnLeftClick += ClickPokemonButton;
            _pokemonPanel.Append(button);
        }

        // Create Bag panel (blank): one button per recovery item the player carries
        _bagPanel = new();
        _bagPanel.Width.Percent = _bagPanel.Height.Percent = 1f;
        for (int i = 0; i < 6; i++)
        {
            DynamicPixelRatioElement button = MakeGridButton(i, _bagPanel);
            button.Append(new BagItemReference());
            button.OnLeftClick += ClickBagItemButton;
        }

        // Create Bag target panel (blank): which party Pokémon to use the picked item on
        _bagTargetPanel = new();
        _bagTargetPanel.Width.Percent = _bagTargetPanel.Height.Percent = 1f;
        for (int i = 0; i < 6; i++)
        {
            DynamicPixelRatioElement button = MakeGridButton(i, _bagTargetPanel);
            button.Append(new PokemonReference());
            button.OnLeftClick += ClickBagTargetButton;
        }
    }

    /// <summary>
    ///     A button in a 3x2 grid with an icon, a name and a smaller second line, like the Pokémon panel's buttons.
    /// </summary>
    private static DynamicPixelRatioElement MakeGridButton(int i, UIElement panel)
    {
        DynamicPixelRatioElement button = new("Terramon/Assets/GUI/TurnBased/Simple", buttonLike: true);
        button.Left.Percent = i % 3 / 3f;
        button.Top.Percent = i / 3 * 0.5f;
        button.Width.Percent = 1f / 3f;
        button.Height.Percent = 0.5f;
        UIText label = new(string.Empty, 0.6f, true) { VAlign = 0.35f };
        UIText subLabel = new(string.Empty, 0.85f) { VAlign = 0.75f };
        BesideIcon(label, 0.6f, true);
        BesideIcon(subLabel, 0.85f, false);
        UIImage image = new(TextureAssets.Npc[0])
        {
            VAlign = 0.5f
        };
        image.Left.Pixels = 4f;
        button.Append(label);
        button.Append(subLabel);
        button.Append(image);
        panel.Append(button);
        return button;
    }

    private const float LabelLeft = 0.3f;
    private const float LabelWidth = 0.68f;

    // Labels next to an icon, with their normal size, shrunk every frame when their text doesn't fit
    private sealed class FittedLabel(UIText label, float scale, bool large)
    {
        public readonly UIText Label = label;
        public readonly float Scale = scale;
        public readonly bool Large = large;
        public float Current = scale;
    }

    private static readonly List<FittedLabel> FittedLabels = [];

    /// <summary>
    ///     Centers a label in the part of a button to the right of its icon; <see cref="FitLabels" /> shrinks long
    ///     names so they stay inside the button.
    /// </summary>
    private static void BesideIcon(UIText label, float scale, bool large)
    {
        label.Left.Percent = LabelLeft;
        label.Width.Percent = LabelWidth;
        label.TextOriginX = 0.5f;
        FittedLabels.Add(new FittedLabel(label, scale, large));
    }

    private static void FitLabels()
    {
        foreach (var fitted in FittedLabels)
        {
            var (label, scale, large) = (fitted.Label, fitted.Scale, fitted.Large);
            if (label.Parent?.Parent == null || string.IsNullOrEmpty(label.Text))
                continue;
            var room = label.Parent.GetInnerDimensions().Width * LabelWidth;
            if (room <= 0f)
                continue;
            var font = large ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;
            var width = font.MeasureString(label.Text).X * scale;
            var wanted = width > room ? scale * room / width : scale;
            if (Math.Abs(fitted.Current - wanted) < 0.005f)
                continue;
            fitted.Current = wanted;
            label.SetText(label.Text, wanted, large);
            label.Recalculate();
        }
    }

    public TestBattleUI()
    {
        Instance = this;
    }

    public static TestBattleUI Instance { get; private set; }
    public static ParticipantPanel PlayerPanel { get; private set; }
    public static ParticipantPanel FoePanel { get; private set; }
    public override bool Visible => _opened;
    private static float PixelRatioForUI() => 1f;

    private static readonly PokemonData[] FoeTeamBuffer = new PokemonData[6];

    /// <summary>
    ///     The team of the trainer or player the local player is battling, for the Poké Ball slots under the foe's
    ///     panel. Null against a wild Pokémon or outside battle.
    /// </summary>
    private static IReadOnlyList<PokemonData> FoeTeam()
    {
        var client = BattleClient.LocalClient;
        if (client.Foe is null or PokemonNPC || client.Battle == null || client.SideIndex == 0)
            return null;
        var foeSide = client.Battle[client.SideIndex == 1 ? 2 : 1];
        if (foeSide == null)
            return null;
        for (var i = 0; i < FoeTeamBuffer.Length; i++)
            FoeTeamBuffer[i] = foeSide.Team[i].Data;
        return FoeTeamBuffer;
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
    {
        if (_opened)
        {
            var battleUILayerName = UILoader.GetLayerName(UILoader.GetUIState<BattleUI>());
            var battleLogLayerName = UILoader.GetLayerName(UILoader.GetUIState<BattleLogUI>());

            foreach (var layer in CollectionsMarshal.AsSpan(layers))
            {
                var name = layer.Name;
                if (PreservedInterfaceLayers.Contains(name) || name == battleUILayerName || name == battleLogLayerName)
                    continue;

                layer.Active = false;
            }
        }

        return layers.FindIndex(layer => layer.Name.Equals("Vanilla: Radial Hotbars"));
    }

    public static void GoBack()
    {
        // From picking a target, go back to picking an item
        if (_bagTargetPanel.Parent != null)
        {
            SoundEngine.PlaySound(in TerramonSoundID.BattleCancel);
            ChangePanel(_bagPanel);
            return;
        }

        // A replacement for a fainted Pokémon has to be picked first
        if (_optionsPanel.Parent != null || BattleClient.LocalClient.CurrentRequest == ShowdownRequest.ForcedSwitch)
        {
            SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
            return;
        }

        SoundEngine.PlaySound(in TerramonSoundID.BattleCancel);
        ChangePanel(_optionsPanel);
    }

    public override void OnInitialize()
    {
        // Forest ??= Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/Forest_NineSlice");

        var parallelogram = Terramon.Instance.Assets.Request<Texture2D>("Assets/GUI/TurnBased/PlayerPanel_Simple");
        PlayerPanel = new(PixelRatioForUI);
        FoePanel = new(PixelRatioForUI);
        PlayerPanel.Width.Pixels = FoePanel.Width.Pixels = 450f;
        PlayerPanel.Height.Pixels = 128f;
        FoePanel.Height.Pixels = 110f;
        PlayerPanel.Top.Pixels = 256f;
        FoePanel.Top.Pixels = 128f;
        FoePanel.HAlign = 1f;
        FoePanel.IsFoe = true;
        PlayerPanel.DrawEXPBar = true;
        PlayerPanel.DrawBallSlots = true;
        // Trainers (and other players) show their team's Poké Balls too; wild Pokémon have none
        FoePanel.DrawBallSlots = true;
        FoePanel.BallParty = FoeTeam;
        Append(PlayerPanel);
        Append(FoePanel);

        DynamicPixelRatioElement p = new("Terramon/Assets/GUI/TurnBased/Simple")
        {
            BlockExternalInput = true,
            HAlign = 0.5f,
            VAlign = 0.9f,
        };
        p.Width.Percent = 0.5f;
        p.Height.Percent = 0.3f;
        p.SetPadding(32f);

        p.Append(_optionsPanel);

        Append(p);

        _mainPanel = p;
    }

    public static void Open()
    {
        PlayerPanel.ResetAnimation();
        FoePanel.ResetAnimation();

        // Options shown again; hidden again on the next update if a turn is still being shown
        _showingTurn = false;
        _skipHint.Remove();
        if (_optionButtons.Parent == null)
            _optionsPanel.Append(_optionButtons);
        _promptText.Width.Percent = 0.47f;

        UpdatePrompt();
        _bagPending = false;
        Instance.Recalculate();
        _screenDimensions = new(Main.screenWidth, Main.screenHeight);
        if (Main.playerInventory)
            Main.LocalPlayer.ToggleInv();

        _opened = true;
    }

    public static void Close()
    {
        _opened = false;
    }

    public static void ChangePanel(UIElement to)
    {
        _mainPanel.RemoveAllChildren();
        _mainPanel.Append(to);
        if (to == _optionsPanel)
            UpdatePrompt();
        else if (to == _movesPanel)
            UpdateMoves();
        else if (to == _pokemonPanel)
            UpdatePokemon();
        else if (to == _bagPanel)
            UpdateBag();
        else if (to == _bagTargetPanel)
            UpdateBagTargets();
        _mainPanel.RecalculateChildren();
    }

    /// <summary>
    ///     Shows "What will (active Pokémon) do?" next to the option buttons.
    /// </summary>
    public static void UpdatePrompt()
    {
        var active = TerramonPlayer.LocalPlayer.GetActivePokemon();
        _promptText.SetText(active == null
            ? string.Empty
            : Language.GetTextValue("Mods.Terramon.Battle.WhatWillDo", active.DisplayName));
    }

    public static void UpdateMoves(PokemonData data = null)
    {
        data ??= TerramonPlayer.LocalPlayer.GetActivePokemon();
        if (data is null)
            return;
        int cur = 0;
        foreach (var moveButton in _movesPanel.Children)
        {
            var actual = (DynamicPixelRatioElement)moveButton;
            var label = (UIText)actual.Children.First(e => e is UIText and not MoveInfoText);
            var typeLine = actual.Children.OfType<MoveInfoText>().First(e => !e.Stats);
            var statsLine = actual.Children.OfType<MoveInfoText>().First(e => e.Stats);
            var moveRef = (MoveReference)actual.Children.First(e => e is MoveReference);

            var move = data.Moves[cur];
            if (move.ID == MoveID.None)
            {
                actual.UpdateTextures(null);
                label.SetText(string.Empty);
                typeLine.SetText(string.Empty);
                statsLine.SetText(string.Empty);
                actual.BlockInteractions = true;
                cur++;
                continue;
            }

            actual.BlockInteractions = false;
            actual.Color = move.Schema.Type.GetColor();
            actual.UpdateTextures("Terramon/Assets/GUI/TurnBased/MoveButton_Normal");
            label.SetText(Language.GetText($"Mods.Terramon.Moves.{move.ID}.DisplayName"));
            var schema = move.Schema;
            typeLine.SetText(Language.GetTextValue($"Mods.Terramon.Types.{schema.Type}") + "  ·  " +
                             Language.GetTextValue($"Mods.Terramon.Battle.MoveInfo.{schema.Category}"));
            var maxPP = schema.PP + schema.PP * move.PPUp / 5;
            statsLine.SetText(Language.GetTextValue("Mods.Terramon.Battle.MoveInfo.Stats",
                schema.Power is > 1 ? schema.Power.ToString() : "—",
                schema.Accuracy is { } acc and <= 100 ? acc.ToString() : "—",
                move.PP, maxPP));
            moveRef.DataRef = data;
            moveRef.Move = cur;

            cur++;
        }
    }

    /// <summary>
    ///     While the moves are shown, hovering a move button shows what the move does (its description, type,
    ///     category, power, accuracy and PP), like hovering an item.
    /// </summary>
    private static void DrawMoveTooltip()
    {
        if (_movesPanel.Parent == null)
            return;
        foreach (var child in _movesPanel.Children)
        {
            if (child is not DynamicPixelRatioElement { BlockInteractions: false } button ||
                !button.ContainsPoint(Main.MouseScreen))
                continue;
            var moveRef = button.Children.OfType<MoveReference>().FirstOrDefault();
            if (moveRef?.DataRef == null)
                return;
            var move = moveRef.DataRef.Moves[moveRef.Move];
            var schema = move.Schema;
            if (move.ID == MoveID.None || schema == null)
                return;
            var maxPP = schema.PP + schema.PP * move.PPUp / 5;
            MoveTooltip.Show(MoveTooltip.Build(move.ID, move.PP, maxPP));
            return;
        }
    }

    private static void ClickMoveButton(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(in TerramonSoundID.BattleDecide);
        var move = (MoveReference)listeningElement.Children.First(e => e is MoveReference);

        if (BattleClient.LocalClient.MakeChoice(BattleChoice.Move, move.Move))
        {
            ChangePanel(_optionsPanel);
        }
    }

    public static void UpdatePokemon()
    {
        var team = TerramonPlayer.LocalPlayer.Party;
        for (int i = 0; i < team.Length; i++)
        {
            var pokemonButton = (DynamicPixelRatioElement)_pokemonPanel.Children.ElementAt(i);
            UIText label = (UIText)pokemonButton.Children.First(e => e is UIText);
            UIImage image = (UIImage)pokemonButton.Children.First(e => e is UIImage);
            PokemonReference poke = (PokemonReference)pokemonButton.Children.First(e => e is PokemonReference);
            var pokemon = team[i];
            if (pokemon is null)
            {
                pokemonButton.UpdateTextures(null);
                pokemonButton.BlockInteractions = true;
                label.SetText(string.Empty);
                continue;
            }

            bool fainted = pokemon.HP <= 0;
            pokemonButton.UpdateTextures("Terramon/Assets/GUI/TurnBased/Simple");
            label.SetText(DatabaseV2.GetLocalizedPokemonName(pokemon.Schema));
            image.SetImage(pokemon.GetMiniSprite());
            if (fainted)
            {
                pokemonButton.BlockInteractions = true;
                image.Color = pokemonButton.Color = Color.Gray;
            }
            else
            {
                pokemonButton.BlockInteractions = false;
                image.Color = pokemonButton.Color = Color.White;
            }

            poke.DataRef = pokemon;
            poke.PartySlot = i;
        }
    }

    private static void ClickPokemonButton(UIMouseEvent evt, UIElement listeningElement)
    {
        var pokeRef = (PokemonReference)listeningElement.Children.First(e => e is PokemonReference);
        if (pokeRef is null)
            return;
        var data = pokeRef.DataRef;

        if (BattleClient.LocalClient.MakeChoice(BattleChoice.Switch, pokeRef.PartySlot))
        {
            Open();
            ChangePanel(_optionsPanel);
        }
    }

    private static void FightButton(UIMouseEvent evt, UIElement listeningElement)
    {
        // for quick asset testing
        /*
        string path = Terramon.Instance.SourceFolder + """\Assets\GUI\TurnBased\Forest_NineSlice.png""";
        // Main.NewText(Path.Exists(path));
        using Stream stream = File.OpenRead(path);
        Forest = Terramon.Instance.Assets.CreateUntracked<Texture2D>(stream, path);
        */
        if (!BattleClient.LocalClient.CanMakeChoice(BattleChoice.Move))
            return;
        ChangePanel(_movesPanel);
    }

    private static void BagButton(UIMouseEvent evt, UIElement listeningElement)
    {
        if (!BattleClient.LocalClient.CanMakeChoice(BattleChoice.Move) ||
            BattleClient.LocalClient.CurrentRequest != ShowdownRequest.Any)
            return;

        if (!AvailableBagItems.Any())
        {
            _promptText.SetText(Language.GetTextValue("Mods.Terramon.Bag.Empty"));
            _promptText.Recalculate();
            return;
        }

        _bagPage = 0;
        ChangePanel(_bagPanel);
    }

    #region Bag

    /// <summary>
    ///     The items that can be used from the Bag in battle, in the order they're listed.
    /// </summary>
    private static int[] BagItems =>
    [
        ModContent.ItemType<Potion>(),
        ModContent.ItemType<SuperPotion>(),
        ModContent.ItemType<HyperPotion>(),
        ModContent.ItemType<MaxPotion>(),
        ModContent.ItemType<Revive>(),
        ModContent.ItemType<MaxRevive>()
    ];

    /// <summary>
    ///     Poké Balls listed first in the Bag, only in battles against wild Pokémon.
    /// </summary>
    private static int[] BagBalls =>
    [
        ModContent.ItemType<PokeBallItem>(),
        ModContent.ItemType<GreatBallItem>(),
        ModContent.ItemType<UltraBallItem>(),
        ModContent.ItemType<MasterBallItem>(),
        ModContent.ItemType<PremierBallItem>(),
        ModContent.ItemType<AetherBallItem>(),
        ModContent.ItemType<CherishBallItem>()
    ];

    private static bool AgainstWildPokemon => BattleClient.LocalClient.Foe is PokemonNPC;

    private static IEnumerable<int> AvailableBagItems =>
        (AgainstWildPokemon ? BagBalls.Concat(BagItems) : BagItems).Where(t => Main.LocalPlayer.CountItem(t, 1) > 0);

    // With more than 6 kinds of items, the last button turns pages
    private const int BagSlots = 6;
    private static int _bagPage;

    private static int _bagSelectedItem;

    // Waiting for the server to accept a Bag item (multiplayer only)
    private static bool _bagPending;

    private static (UIText Label, UIText SubLabel, UIImage Image) GridParts(UIElement button)
        => ((UIText)button.Children.First(e => e is UIText),
            (UIText)button.Children.Where(e => e is UIText).ElementAt(1),
            (UIImage)button.Children.First(e => e is UIImage));

    public static void UpdateBag()
    {
        var all = AvailableBagItems.Select(t => (Type: t, Count: Main.LocalPlayer.CountItem(t, 9999))).ToArray();
        var paged = all.Length > BagSlots;
        var perPage = paged ? BagSlots - 1 : BagSlots;
        var pages = Math.Max(1, (all.Length + perPage - 1) / perPage);
        _bagPage %= pages;
        var owned = all.Skip(_bagPage * perPage).Take(perPage).ToArray();

        for (int i = 0; i < _bagPanel.Children.Count(); i++)
        {
            var button = (DynamicPixelRatioElement)_bagPanel.Children.ElementAt(i);
            var (label, subLabel, image) = GridParts(button);
            var itemRef = (BagItemReference)button.Children.First(e => e is BagItemReference);

            if (paged && i == BagSlots - 1)
            {
                button.UpdateTextures("Terramon/Assets/GUI/TurnBased/Simple");
                button.BlockInteractions = false;
                button.Color = Color.White;
                label.SetText(Language.GetTextValue("Mods.Terramon.Bag.MorePages"));
                subLabel.SetText($"{_bagPage + 1}/{pages}");
                image.Color = Color.Transparent;
                itemRef.ItemType = -1;
                continue;
            }

            if (i >= owned.Length)
            {
                button.UpdateTextures(null);
                button.BlockInteractions = true;
                label.SetText(string.Empty);
                subLabel.SetText(string.Empty);
                image.Color = Color.Transparent;
                itemRef.ItemType = 0;
                continue;
            }

            var (type, count) = owned[i];
            Main.instance.LoadItem(type);
            button.UpdateTextures("Terramon/Assets/GUI/TurnBased/Simple");
            button.BlockInteractions = false;
            button.Color = Color.White;
            label.SetText(Lang.GetItemNameValue(type));
            subLabel.SetText(Language.GetTextValue("Mods.Terramon.Bag.Count", count));
            image.SetImage(TextureAssets.Item[type]);
            image.Color = Color.White;
            itemRef.ItemType = type;
        }
    }

    private static void ClickBagItemButton(UIMouseEvent evt, UIElement listeningElement)
    {
        var itemRef = (BagItemReference)listeningElement.Children.First(e => e is BagItemReference);
        if (itemRef.ItemType == 0)
            return;

        if (itemRef.ItemType == -1)
        {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            _bagPage++;
            UpdateBag();
            return;
        }

        if (ModContent.GetModItem(itemRef.ItemType) is BasePkballItem)
        {
            ThrowBallFromBag(itemRef.ItemType);
            return;
        }

        SoundEngine.PlaySound(in TerramonSoundID.BattleDecide);
        _bagSelectedItem = itemRef.ItemType;
        ChangePanel(_bagTargetPanel);
    }

    /// <summary>
    ///     Throws a Poké Ball from the Bag at the wild Pokémon. The ball flies there like a thrown one, and hitting the
    ///     Pokémon takes the turn (see <see cref="BasePkballProjectile" />).
    /// </summary>
    private static void ThrowBallFromBag(int ballType)
    {
        var client = BattleClient.LocalClient;
        var player = Main.LocalPlayer;
        if (client.Foe is not PokemonNPC { PlasmaState: false } wild || !client.CanMakeChoice(BattleChoice.Move) ||
            client.CurrentRequest != ShowdownRequest.Any || player.CountItem(ballType, 1) <= 0)
        {
            SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
            return;
        }

        var from = player.MountedCenter;
        var to = wild.NPC.Center;
        player.ChangeDir(to.X > from.X ? 1 : -1);

        // The ball flies straight for 10 ticks, then falls by 0.25 per tick. Aim so it lands on the Pokémon after
        // `time` ticks; the ball also gets 75% of the player's velocity when it spawns.
        var time = Math.Clamp(Vector2.Distance(from, to) / 7f, 20f, 50f);
        var fall = time > 10 ? 0.125f * (time - 10) * (time - 9) : 0f;
        var velocity = new Vector2((to.X - from.X) / time, (to.Y - from.Y - fall) / time) - player.velocity * 0.75f;

        SoundEngine.PlaySound(TerramonSoundID.PkballThrow, from);
        Projectile.NewProjectile(player.GetSource_ItemUse(ContentSamples.ItemsByType[ballType]), from, velocity,
            ContentSamples.ItemsByType[ballType].shoot, 0, 0f, player.whoAmI);
        player.ConsumeItem(ballType);
        ChangePanel(_optionsPanel);
    }

    private static bool CanUseOn(int itemType, PokemonData data)
        => data != null && ModContent.GetModItem(itemType) is RecoveryItem item && item.AffectedByPokemonDirectUse(data);

    public static void UpdateBagTargets()
    {
        var team = TerramonPlayer.LocalPlayer.Party;
        for (int i = 0; i < team.Length; i++)
        {
            var button = (DynamicPixelRatioElement)_bagTargetPanel.Children.ElementAt(i);
            var (label, subLabel, image) = GridParts(button);
            var poke = (PokemonReference)button.Children.First(e => e is PokemonReference);
            var pokemon = team[i];
            if (pokemon is null)
            {
                button.UpdateTextures(null);
                button.BlockInteractions = true;
                label.SetText(string.Empty);
                subLabel.SetText(string.Empty);
                image.Color = Color.Transparent;
                poke.DataRef = null;
                continue;
            }

            button.UpdateTextures("Terramon/Assets/GUI/TurnBased/Simple");
            label.SetText(pokemon.DisplayName);
            subLabel.SetText(pokemon.HP <= 0
                ? Language.GetTextValue("Mods.Terramon.Bag.Fainted")
                : Language.GetTextValue("Mods.Terramon.Bag.HP", pokemon.HP, pokemon.MaxHP));
            image.SetImage(pokemon.GetMiniSprite());

            // Potions only work on hurt Pokémon still standing, Revives only on fainted ones
            var usable = CanUseOn(_bagSelectedItem, pokemon);
            button.BlockInteractions = !usable;
            image.Color = button.Color = usable ? Color.White : Color.Gray;
            poke.DataRef = pokemon;
            poke.PartySlot = i;
        }
    }

    private static void ClickBagTargetButton(UIMouseEvent evt, UIElement listeningElement)
    {
        var pokeRef = (PokemonReference)listeningElement.Children.First(e => e is PokemonReference);
        var client = BattleClient.LocalClient;
        var type = _bagSelectedItem;
        if (pokeRef.DataRef is null || !CanUseOn(type, pokeRef.DataRef) || _bagPending ||
            !client.CanMakeChoice(BattleChoice.Move) || client.CurrentRequest != ShowdownRequest.Any ||
            Main.LocalPlayer.CountItem(type, 1) <= 0)
        {
            SoundEngine.PlaySound(in TerramonSoundID.ButtonLocked);
            return;
        }

        SoundEngine.PlaySound(in TerramonSoundID.BattleDecide);
        var slot = (byte)pokeRef.PartySlot;
        var name = pokeRef.DataRef.DisplayName;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // The battle runs on the server; the item is only spent once it says the item was used
            _bagPending = true;
            client.CurrentRequest = ShowdownRequest.Wait;
            Terramon.Instance.SendPacket(new BagItemRpc(type, slot, name));
        }
        else if (BattleManager.Instance.UseItem(client, type, slot, name))
        {
            // The battle itself sets the exact new HP here
            ApplyBagItem(type, slot, applyHP: false);
        }

        ChangePanel(_optionsPanel);
    }

    /// <summary>
    ///     Called on the client that used a Bag item once the server has answered.
    /// </summary>
    public static void BagItemResult(bool accepted, int itemType, int partySlot)
    {
        _bagPending = false;
        if (accepted)
        {
            ApplyBagItem(itemType, partySlot, applyHP: true);
            return;
        }

        // Nothing happened, so the turn can still be used
        var client = BattleClient.LocalClient;
        if (client.BattleOngoing && client.CurrentRequest == ShowdownRequest.Wait)
            client.CurrentRequest = ShowdownRequest.Any;
        Main.NewText(Language.GetTextValue("Mods.Terramon.Bag.Failed"), Color.Orange);
    }

    /// <summary>
    ///     Spends the item and applies its effect to the local copy of the Pokémon.
    ///     In multiplayer the battle only sends HP as a percentage, so the exact HP is applied here too.
    /// </summary>
    private static void ApplyBagItem(int itemType, int partySlot, bool applyHP)
    {
        var player = Main.LocalPlayer;
        player.ConsumeItem(itemType);

        var data = TerramonPlayer.LocalPlayer.Party[partySlot];
        switch (ModContent.GetModItem(itemType))
        {
            case BasePotionItem potion when data != null:
                if (applyHP)
                    data.Heal(potion.BattleHealAmount);
                SoundEngine.PlaySound(in SoundID.Item13);
                break;
            case BaseReviveItem revive when data != null:
                if (applyHP)
                    data.HP = (ushort)Math.Max(1, Math.Ceiling(data.MaxHP * revive.BattleRestorationPercentage));
                // Clear the fainted status in the battle too, so the Pokémon can be switched back in
                var side = BattleSide.LocalSide;
                if (side != null)
                    side[partySlot].Status = NonVolatileStatus.None;
                data.Status = NonVolatileStatus.None;
                SoundEngine.PlaySound(in SoundID.Item29);
                break;
        }
    }

    #endregion

    private static void PokemonButton(UIMouseEvent evt, UIElement listeningElement)
    {
        ChangePanel(_pokemonPanel);
    }

    private static void RunButton(UIMouseEvent evt, UIElement listeningElement)
    {
        // Not while the last turn is still being shown
        if (BattlePlayback.LocalBusy)
            return;
        SoundEngine.PlaySound(in TerramonSoundID.BattleRun);

        var forfeit = new ForfeitOrder
        {
            Sender = TerramonPlayer.LocalPlayer
        };
        forfeit.Send();
    }

    // Whether the options are hidden because a turn is being shown
    private static bool _showingTurn;

    // The message being written out in the battle box, and since when
    private static string _typedLine = string.Empty;
    private static uint _typedSince;

    public override void SafeUpdate(GameTime gameTime)
    {
        FitLabels();
        UpdateTurnPlayback();

        // When the active Pokémon faints, go straight to picking another one (once its faint has been shown)
        if (_opened && !_showingTurn && _pokemonPanel.Parent == null &&
            BattleClient.LocalClient.CurrentRequest == ShowdownRequest.ForcedSwitch)
        {
            ChangePanel(_pokemonPanel);
        }
    }

    /// <summary>
    ///     While a turn is being shown (see <see cref="BattlePlayback" />), the options are hidden and the latest battle
    ///     message is shown in their place, like in the main games. Clicking the panel shows the rest of the turn at once.
    /// </summary>
    private static void UpdateTurnPlayback()
    {
        var busy = _opened && BattlePlayback.LocalBusy;
        if (busy != _showingTurn)
        {
            _showingTurn = busy;
            if (busy)
            {
                // Back to the main options (e.g. from looking at the team while waiting for the foe)
                if (_optionsPanel.Parent == null && BattleClient.LocalClient.CurrentRequest != ShowdownRequest.ForcedSwitch)
                    ChangePanel(_optionsPanel);
                _optionButtons.Remove();
                if (_skipHint.Parent == null)
                    _optionsPanel.Append(_skipHint);
                _promptText.Width.Percent = 1f;
                _promptText.SetText(string.Empty);
            }
            else
            {
                _skipHint.Remove();
                if (_optionButtons.Parent == null)
                    _optionsPanel.Append(_optionButtons);
                _promptText.Width.Percent = 0.47f;
                UpdatePrompt();
            }

            _optionsPanel.Recalculate();
        }

        if (!busy)
            return;

        // Written out letter by letter, like the main games
        var line = BattlePlayback.LocalLine ?? string.Empty;
        if (line != _typedLine)
        {
            _typedLine = line;
            _typedSince = Main.GameUpdateCount;
        }

        var letters = (int)((Main.GameUpdateCount - _typedSince) * BattlePlayback.LettersPerTick) + 1;
        var typed = letters >= line.Length ? line : line[..letters];
        if (_promptText.Text != typed)
        {
            _promptText.SetText(typed);
            _promptText.Recalculate();
        }

        // A click on the panel skips to the end of the turn
        if (Main.mouseLeft && Main.mouseLeftRelease && _mainPanel?.ContainsPoint(Main.MouseScreen) == true)
        {
            Main.mouseLeftRelease = false;
            BattlePlayback.SkipLocal();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        // After the buttons, so the move's tooltip panel covers them instead of mixing with them
        DrawMoveTooltip();
        MoveTooltip.DrawPending(spriteBatch);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Point16 newDim = new(Main.screenWidth, Main.screenHeight);
        if (newDim != _screenDimensions)
        {
            Recalculate();
            _screenDimensions = newDim;
        }

        /*
        RemoveAllChildren();
        OnInitialize();
        Recalculate();
        */
    }
}

public sealed class MoveInfoText(string text, float scale, bool large) : UIText(text, scale, large)
{
    public bool Stats;
}

public sealed class MoveReference : UIElement
{
    public PokemonData DataRef;
    public int Move;
}

public sealed class BagItemReference : UIElement
{
    public int ItemType;
}

public sealed class PokemonReference : UIElement
{
    public PokemonData DataRef;
    public int PartySlot;
}

public sealed class DynamicPixelRatioElement : UIElement
{
    private bool _buttonLike;
    private Asset<Texture2D> _corners;
    private Func<float> _getPixelRatio;
    private bool _hovered;
    private float _hoverTime;
    private Asset<Texture2D> _nineSlice;
    private bool _noStretching;
    public bool BlockExternalInput;
    public bool BlockInteractions;

    public DynamicPixelRatioElement(string basePath, Func<float> getPixelRatio = null, bool noStretching = false,
        bool buttonLike = false)
    {
        UpdateTextures(basePath, noStretching);
        SetupCommon(buttonLike, getPixelRatio);
    }

    public DynamicPixelRatioElement(Asset<Texture2D> nineSlice, Asset<Texture2D> corners,
        Func<float> getPixelRatio = null, bool noStretching = false, bool buttonLike = false)
    {
        UpdateTextures(nineSlice, corners, noStretching);
        SetupCommon(buttonLike, getPixelRatio);
    }

    public Color Color { get; set; } = Color.White;

    private static void Unhovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        if (dyn.BlockInteractions)
            return;
        dyn._hovered = false;
    }

    private static void Hovered(UIMouseEvent evt, UIElement listeningElement)
    {
        var dyn = (DynamicPixelRatioElement)listeningElement;
        if (dyn.BlockInteractions)
            return;
        dyn._hovered = true;
    }

    private void SetupCommon(bool buttonLike, Func<float> getPixelRatio)
    {
        _getPixelRatio = getPixelRatio;
        OverrideSamplerState = SamplerState.PointClamp;
        if (!buttonLike)
            return;
        _buttonLike = true;
        OnMouseOver += Hovered;
        OnMouseOut += Unhovered;
    }

    public DynamicPixelRatioElement UpdateTextures(string newBasePath, bool noStretching = false)
    {
        if (ModContent.RequestIfExists(newBasePath + "_NineSlice", out _nineSlice))
            _noStretching = noStretching;
        else if (ModContent.RequestIfExists(newBasePath + "_NineSlice_Stretch", out _nineSlice))
            _noStretching = false;
        ModContent.RequestIfExists(newBasePath + "_Corners", out _corners);
        return this;
    }

    public DynamicPixelRatioElement UpdateTextures(Asset<Texture2D> nineSlice, Asset<Texture2D> corners,
        bool noStretching = false)
    {
        _nineSlice = nineSlice;
        _corners = corners;
        _noStretching = noStretching;
        return this;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (!BlockInteractions)
            base.LeftClick(evt);
    }

    public override void Update(GameTime gameTime)
    {
        if (_hovered && _hoverTime < 1f)
            _hoverTime = Math.Min(_hoverTime + 0.1f, 1f);
        else if (!_hovered && _hoverTime > 0f)
            _hoverTime = Math.Max(_hoverTime - 0.1f, 0f);
        base.Update(gameTime);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (BlockExternalInput && ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        Rectangle bounds = GetDimensions().ToRectangle();
        Color drawColor = Color;
        float zoom = _getPixelRatio?.Invoke() ?? 1f;
        if (_buttonLike)
        {
            int infl = (int)(_hoverTime * 2f);
            bounds.Inflate(infl, infl);
            drawColor.A = (byte)((1f - _hoverTime) * byte.MaxValue);
        }

        if (_nineSlice != null)
            DrawAdjustableBox(spriteBatch, _nineSlice.Value, bounds, drawColor, zoom, _noStretching);
        if (_corners != null)
            DrawAdjustableCorners(spriteBatch, _corners.Value, bounds, drawColor, zoom);
    }

    public static void DrawAdjustableCorners(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col,
        float scale)
    {
        Rectangle frame = tex.Frame(2);
        spriteBatch.Draw(tex, rect.TopLeft(), frame, col, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        Vector2 og = new(tex.Width * 0.5f, tex.Height);
        spriteBatch.Draw(tex, rect.BottomRight(), frame, col, 0f, og, scale, SpriteEffects.None, 0f);
    }

    public static void DrawAdjustableBar(SpriteBatch spriteBatch, Texture2D tex, Vector2 pos, float width, Color col,
        float pxScale, Effect effect = null)
    {
        if (width <= 0f)
            return;

        SpriteBatchData data = default;
        if (effect != null)
            data = spriteBatch.Restart(effect);

        Vector2 drawPos = pos;
        float quadWidth = tex.Width / 3f * pxScale;
        var xScale = (width - quadWidth * 2f) / quadWidth;
        Rectangle frame = tex.Frame(3);
        int oldWidth = frame.Width;
        if (width < quadWidth)
            frame.Width = (int)(frame.Width * (width / quadWidth));
        spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, pxScale, SpriteEffects.None, 0f);
        frame.Width = oldWidth;
        frame.X += frame.Width;
        drawPos.X += quadWidth;
        if (xScale > 0f)
        {
            if (effect == null)
                data = spriteBatch.Restart(SamplerState.PointClamp);
            else
            {
                spriteBatch.End();
                data.SamplerState = SamplerState.PointClamp;
                spriteBatch.Begin(in data);
            }

            spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, new Vector2(xScale * pxScale, pxScale),
                SpriteEffects.None, 0f);

            spriteBatch.End();
            data.SamplerState = SamplerState.LinearClamp;
            spriteBatch.Begin(in data);

            var pxs = quadWidth * xScale;
            drawPos.X += (int)pxs;
        }
        else
            drawPos.X = pos.X + quadWidth;

        if (width > quadWidth)
        {
            frame.X += frame.Width;
            if (width < quadWidth * 2f)
                frame.Width = (int)(frame.Width * ((width - quadWidth) / quadWidth));
            spriteBatch.Draw(tex, drawPos, frame, col, 0f, Vector2.Zero, pxScale, SpriteEffects.None, 0f);
        }

        if (effect != null)
            spriteBatch.Restart(newEffect: null);
    }

    public static void DrawAdjustableBox(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col,
        float pxScale, bool noStretching = false, SkipDrawBoxSegment flags = SkipDrawBoxSegment.None)
    {
        Vector2 quadSize = new Vector2(tex.Width / 3f, tex.Height / 3f) * pxScale;
        var xScale = (rect.Width - quadSize.X * 2f) / quadSize.X;
        var yScale = (rect.Height - quadSize.Y * 2f) / quadSize.Y;

        void DrawSegment(in Vector2 position, in Rectangle frame, float xSize = 1f, float ySize = 1f)
        {
            spriteBatch.Draw(tex, position, frame, col, 0f, Vector2.Zero, new Vector2(xSize * pxScale, ySize * pxScale),
                SpriteEffects.None, 0f);
        }

        void DrawWrapped(Vector2 position, Rectangle frame, float scale, bool down)
        {
            for (float currentScale = 0f; currentScale < scale; currentScale++)
            {
                if (currentScale != 0f)
                {
                    if (down)
                        position.Y += quadSize.Y;
                    else
                        position.X += quadSize.X;
                }

                float overshoot = currentScale + 1f;
                if (overshoot > scale)
                {
                    if (down)
                        frame.Height = (int)MathF.Ceiling(frame.Height * (scale - currentScale));
                    else
                        frame.Width = (int)MathF.Ceiling(frame.Width * (scale - currentScale));
                }

                DrawSegment(in position, in frame);
            }
        }

        // Draw center
        if ((flags & SkipDrawBoxSegment.CenterCenter) == 0)
        {
            Rectangle centerFrame = tex.Frame(3, 3, 1, 1);
            DrawSegment(new Vector2(rect.X + quadSize.X, rect.Y + quadSize.Y), centerFrame, xScale, yScale);
        }

        // Draw sides
        if ((flags & SkipDrawBoxSegment.TopCenter) == 0)
        {
            Rectangle topSideFrame = tex.Frame(3, 3, 1);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y);
            if (noStretching)
                DrawWrapped(pos, topSideFrame, xScale, false);
            else
                DrawSegment(pos, topSideFrame, xScale);
        }

        if ((flags & SkipDrawBoxSegment.CenterLeft) == 0)
        {
            Rectangle leftSideFrame = tex.Frame(3, 3, 0, 1);
            Vector2 pos = new(rect.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, leftSideFrame, yScale, true);
            else
                DrawSegment(pos, leftSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.CenterRight) == 0)
        {
            Rectangle rightSideFrame = tex.Frame(3, 3, 2, 1);
            Vector2 pos = new(rect.X + rect.Width - quadSize.X, rect.Y + quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, rightSideFrame, yScale, true);
            else
                DrawSegment(pos, rightSideFrame, 1f, yScale);
        }

        if ((flags & SkipDrawBoxSegment.BottomCenter) == 0)
        {
            Rectangle bottomSideFrame = tex.Frame(3, 3, 1, 2);
            Vector2 pos = new(rect.X + quadSize.X, rect.Y + rect.Height - quadSize.Y);
            if (noStretching)
                DrawWrapped(pos, bottomSideFrame, xScale, false);
            else
                DrawSegment(pos, bottomSideFrame, xScale);
        }

        // Draw corners

        if ((flags & SkipDrawBoxSegment.TopLeft) == 0)
        {
            Rectangle topLeftCorner = tex.Frame(3, 3);
            DrawSegment(new Vector2(rect.X, rect.Y), topLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.TopRight) == 0)
        {
            Rectangle topRightCorner = tex.Frame(3, 3, 2);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y), topRightCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomLeft) == 0)
        {
            Rectangle bottomLeftCorner = tex.Frame(3, 3, 0, 2);
            DrawSegment(new Vector2(rect.X, rect.Y + rect.Height - quadSize.Y), bottomLeftCorner);
        }

        if ((flags & SkipDrawBoxSegment.BottomRight) == 0)
        {
            Rectangle bottomRightCorner = tex.Frame(3, 3, 2, 2);
            DrawSegment(new Vector2(rect.X + rect.Width - quadSize.X, rect.Y + rect.Height - quadSize.Y),
                bottomRightCorner);
        }
    }

    public static void DrawAdjustableParallelogram(SpriteBatch spriteBatch, Texture2D tex, in Rectangle rect, Color col,
        float pxScale)
    {
        Rectangle frame = tex.Frame(3, 3);
        Rectangle firstFrame = frame;
        float shiftClosest = frame.Height * 0.5f / pxScale;
        float shiftFurthest = frame.Height / pxScale;
        float quadWidth = frame.Width * pxScale;
        float overlapShiftFurthest = (frame.Height - rect.Height / 3f) * 1.5f;
        float overlapShiftClosest = overlapShiftFurthest * 0.5f;
        float areaWeNeedToCover = rect.Width - quadWidth * 2f - shiftFurthest + overlapShiftFurthest;
        Vector2 xScale = new(areaWeNeedToCover / frame.Width, pxScale);
        // tl
        Vector2 tlPos = new(rect.X + shiftFurthest - overlapShiftFurthest, rect.Y);
        spriteBatch.Draw(tex, tlPos, frame, col, 0f, Vector2.Zero, pxScale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        // tm
        Vector2 tmPos = new(rect.X + quadWidth + shiftFurthest - overlapShiftFurthest, rect.Y);
        spriteBatch.Draw(tex, tmPos, frame, col, 0f, Vector2.Zero, xScale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        // tr
        spriteBatch.Draw(tex, rect.TopRight(), frame, col, 0f, firstFrame.TopRight(), pxScale, SpriteEffects.None, 0f);
        frame.X = 0;
        frame.Y += frame.Height;
        if (frame.Height * pxScale * 2f < rect.Height)
        {
            // ml
            Vector2 mlPos = new(rect.X + shiftClosest - overlapShiftClosest, rect.Y + rect.Height * 0.5f);
            spriteBatch.Draw(tex, mlPos, frame, col, 0f, new Vector2(0f, firstFrame.Height * 0.5f), pxScale,
                SpriteEffects.None, 0f);
            frame.X += frame.Width;
            // mm
            Vector2 mmPos = rect.Center();
            spriteBatch.Draw(tex, mmPos, frame, col, 0f, firstFrame.Center(), xScale, SpriteEffects.None, 0f);
            frame.X += frame.Width;
            // mr
            Vector2 mrPos = new(rect.X + rect.Width - shiftClosest + overlapShiftClosest, rect.Y + rect.Height * 0.5f);
            spriteBatch.Draw(tex, mrPos, frame, col, 0f, new Vector2(firstFrame.Width, firstFrame.Height * 0.5f),
                pxScale, SpriteEffects.None, 0f);
            frame.X = 0;
        }

        frame.Y += frame.Height;
        // bl
        spriteBatch.Draw(tex, rect.BottomLeft(), frame, col, 0f, firstFrame.BottomLeft(), pxScale, SpriteEffects.None,
            0f);
        frame.X += frame.Width;
        // bm
        Vector2 bmPos = new(rect.X + quadWidth, rect.Y + rect.Height);
        spriteBatch.Draw(tex, bmPos, frame, col, 0f, firstFrame.BottomLeft(), xScale, SpriteEffects.None, 0f);
        frame.X += frame.Width;
        // br
        Vector2 brPos = new(rect.X + rect.Width - shiftFurthest + overlapShiftFurthest, rect.Y + rect.Height);
        spriteBatch.Draw(tex, brPos, frame, col, 0f, firstFrame.BottomRight(), pxScale, SpriteEffects.None, 0f);
    }
}

internal enum ButtonType
{
    Fight,
    Bag,
    Pokemon,
    Run,
}

[Flags]
public enum SkipDrawBoxSegment
{
    None = 0,
    TopLeft = 1,
    TopCenter = 2,
    TopRight = 4,
    CenterLeft = 8,
    CenterCenter = 16,
    CenterRight = 32,
    BottomLeft = 64,
    BottomCenter = 128,
    BottomRight = 256,
}