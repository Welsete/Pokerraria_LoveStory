using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.NPCs;
using Terramon.Content.NPCs.Trainers;
using Terramon.Content.WorldGeneration;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

/// <summary>
///     A panel of shortcuts for testing the mod (healing, levels, items, trainers...). Only opens with Debug Mode on.
/// </summary>
public sealed class AdminPanelUI : SmartUIState
{
    private const int Columns = 3;
    private const float ButtonWidth = 190f;
    private const float ButtonHeight = 36f;
    private const float Gap = 8f;
    private const float GridTop = 76f;

    private DragPanel _panel;

    /// <summary>
    ///     Every trainer in League order, for the trainer spawn button.
    /// </summary>
    private static readonly string[] AllTrainers = [..League.GymLeaders, ..League.EliteFourAndChampion];

    private static int _trainerIndex;
    private UITextPanel<LocalizedText> _trainerButton;

    // Where the player dragged the panel to; null until it's moved (then it opens centered)
    private static Vector2? _position;
    private static int _tab;
    private readonly List<UIElement> _buttons = [];
    private readonly List<UITextPanel<LocalizedText>> _tabButtons = [];

    private void UpdateTrainerButton()
        => _trainerButton?.SetText(Label("SpawnTrainer")
            .WithFormatArgs(Lang.GetNPCNameValue(League.TrainerType(AllTrainers[_trainerIndex]))));

    private static string Text(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.GUI.AdminPanel." + key, args);

    // LocalizedText (not string) so the labels follow the game language
    private static LocalizedText Label(string key) => Language.GetText("Mods.Terramon.GUI.AdminPanel." + key);

    public static void Toggle()
    {
        var ui = UILoader.GetUIState<AdminPanelUI>();
        if (!ui.Visible && !GameplayConfig.Instance.DebugMode)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Commands.RequiresDebugMode"), TerramonCommand.ChatColorRed);
            return;
        }

        ui.Visible = !ui.Visible;
        SoundEngine.PlaySound(ui.Visible ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
        => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

    /// <summary>
    ///     The buttons of each tab: Pokémon, League and Terraria.
    /// </summary>
    private static (string Tab, (string Key, Action Action)[] Actions)[] Tabs =>
    [
        ("TabPokemon", [
            ("HealParty", HealParty),
            ("LevelUp1", () => LevelUpActive(1)),
            ("LevelUp5", () => LevelUpActive(5)),
            ("LevelUp10", () => LevelUpActive(10)),
            ("EvolveActive", EvolveActive),
            ("FaintActive", FaintActive),
            ("PerfectIVs", PerfectIVs),
            ("ToggleShiny", ToggleShiny),
            ("TeachSpriteMove", TeachSpriteMove),
            ("GiveItems", GiveItems),
            ("GiveHeldItems", GiveHeldItems),
            ("GiveStones", GiveEvolutionStones),
            ("SpawnWild", SpawnWild),
            ("SpawnOnix", SpawnOnix),
            ("SpawnLucario", SpawnLucario),
            ("GiveLucario", GiveLucario),
            ("ClearWild", ClearWild),
            ("ClearBattles", ClearBattles)
        ]),
        ("TabLeague", [
            ("SpawnTrainer", SpawnSelectedTrainer),
            ("NextBoss", AdvanceBossProgress),
            ("GiveBadges", GiveAllBadges),
            ("LeagueItems", GiveLeagueItems),
            ("ResetBadges", ResetBadges),
            ("TeleportBrockGym", TeleportToBrockGym),
            ("BuildBrockGym", BuildBrockGymHere),
            ("CallOak", CallProfessorOak)
        ]),
        ("TabTerraria", [
            ("ToggleTime", ToggleTime),
            ("ToggleRain", ToggleRain),
            ("GodMode", ToggleGodMode),
            ("HealPlayer", HealPlayer),
            ("TeleportSpawn", TeleportToSpawn),
            ("TeleportCursor", TeleportToCursor),
            ("KillHostiles", KillHostiles),
            ("GiveMoney", GiveMoney),
            ("GiveGear", GiveGear),
            ("GiveBossItems", GiveBossItems),
            ("RevealMap", RevealMap)
        ]),
        ("TabItems", []) // A grid of every item of the mod, see BuildItemGrid
    ];

    public override void OnInitialize()
    {
        var tabs = Tabs;
        var rows = tabs.Max(t => (t.Actions.Length + 1 + Columns - 1) / Columns); // +1 for Close
        _panel = new DragPanel();
        _panel.Width.Pixels = Columns * (ButtonWidth + Gap) + Gap + _panel.PaddingLeft + _panel.PaddingRight;
        _panel.Height.Pixels = GridTop + rows * (ButtonHeight + Gap) + _panel.PaddingTop + _panel.PaddingBottom;
        Append(_panel);

        _panel.Append(new UIText(Label("Title"), 0.8f)
        {
            HAlign = 0.5f,
            IgnoresMouseInteraction = true // Dragging works from the title too
        });

        var tabWidth = (Columns * (ButtonWidth + Gap) - Gap - (tabs.Length - 1) * Gap) / tabs.Length;
        for (var i = 0; i < tabs.Length; i++)
        {
            var tabIndex = i;
            var tab = new UITextPanel<LocalizedText>(Label(tabs[i].Tab), 0.85f);
            tab.Width.Pixels = tabWidth;
            tab.Height.Pixels = ButtonHeight - 4f;
            tab.Left.Pixels = Gap + i * (tabWidth + Gap);
            tab.Top.Pixels = 30f;
            tab.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _tab = tabIndex;
                ShowTab();
            };
            _tabButtons.Add(tab);
            _panel.Append(tab);
        }

        ShowTab();
    }

    private void ShowTab()
    {
        foreach (var button in _buttons)
            button.Remove();
        _buttons.Clear();
        _trainerButton = null;

        for (var i = 0; i < _tabButtons.Count; i++)
        {
            _tabButtons[i].BackgroundColor = i == _tab ? new Color(90, 115, 200) : new Color(44, 57, 105) * 0.9f;
            _tabButtons[i].BorderColor = i == _tab ? Color.Gold : Color.Black;
        }

        if (Tabs[_tab].Tab == "TabItems")
        {
            BuildItemGrid();
            _panel.Recalculate();
            return;
        }

        (string Key, Action Action)[] actions = [..Tabs[_tab].Actions, ("Close", (Action)Toggle)];
        for (var i = 0; i < actions.Length; i++)
        {
            var (key, action) = actions[i];
            var button = new UITextPanel<LocalizedText>(Label(key), 0.8f);
            if (key == "SpawnTrainer")
            {
                // Right click picks which trainer the button spawns
                _trainerButton = button;
                UpdateTrainerButton();
                button.OnRightClick += (_, _) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _trainerIndex = (_trainerIndex + 1) % AllTrainers.Length;
                    UpdateTrainerButton();
                };
            }

            button.Width.Pixels = ButtonWidth;
            button.Height.Pixels = ButtonHeight;
            button.Left.Pixels = Gap + i % Columns * (ButtonWidth + Gap);
            button.Top.Pixels = GridTop + i / Columns * (ButtonHeight + Gap);
            button.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
            button.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = new Color(63, 82, 151) * 0.7f;
            button.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                action();
            };
            _buttons.Add(button);
            _panel.Append(button);
        }

        _panel.Recalculate();
    }

    /// <summary>
    ///     Every item of the mod in a scrollable grid: left click gives one, right click a full stack.
    /// </summary>
    private void BuildItemGrid()
    {
        var hint = new UIText(Label("ItemsHint"), 0.75f) { HAlign = 0.5f };
        hint.Top.Pixels = GridTop - 4f;
        _buttons.Add(hint);
        _panel.Append(hint);

        var grid = new UIGrid { ListPadding = 4f };
        grid.Left.Pixels = Gap;
        grid.Top.Pixels = GridTop + 20f;
        grid.Width.Pixels = Columns * (ButtonWidth + Gap) - Gap - 24f;
        grid.Height.Set(-(GridTop + 20f), 1f);

        var scrollbar = new UIScrollbar();
        scrollbar.Left.Pixels = Gap + grid.Width.Pixels + 6f;
        scrollbar.Top.Pixels = GridTop + 26f;
        scrollbar.Height.Set(-(GridTop + 32f), 1f);
        grid.SetScrollbar(scrollbar);

        foreach (var item in ModContent.GetContent<ModItem>()
                     .Where(i => i.Mod == Terramon.Instance)
                     .OrderBy(i => i.Type))
            grid.Add(new ItemCell(item.Type));

        _buttons.Add(grid);
        _buttons.Add(scrollbar);
        _panel.Append(grid);
        _panel.Append(scrollbar);
    }

    /// <summary>
    ///     One item of the item grid, drawn like an inventory slot, with the item's tooltip on hover.
    /// </summary>
    private sealed class ItemCell : UIElement
    {
        private const float Scale = 0.8f;
        private Item _item;

        public ItemCell(int type)
        {
            _item = new Item(type);
            Width.Pixels = Height.Pixels = 52f * Scale;
            OnLeftClick += (_, _) => Give(1);
            OnRightClick += (_, _) => Give(_item.maxStack);
        }

        private void Give(int amount)
        {
            SoundEngine.PlaySound(SoundID.Grab);
            var player = Main.LocalPlayer;
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), _item.type, Math.Max(1, amount));
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var oldScale = Main.inventoryScale;
            Main.inventoryScale = Scale;
            ItemSlot.Draw(spriteBatch, ref _item, ItemSlot.Context.ChestItem, GetDimensions().Position());
            Main.inventoryScale = oldScale;

            if (!IsMouseHovering)
                return;
            Main.HoverItem = _item.Clone();
            Main.hoverItemName = _item.Name;
        }
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Visible || _panel == null)
            return;

        // Open centered the first time, then wherever it was dragged to
        var screen = GetDimensions();
        var position = _position ?? new Vector2((screen.Width - _panel.Width.Pixels) / 2f,
            (screen.Height - _panel.Height.Pixels) / 2f);
        if (_panel.Dragging)
            position = _panel.DragPosition();
        position.X = Math.Clamp(position.X, 0f, Math.Max(0f, screen.Width - _panel.Width.Pixels));
        position.Y = Math.Clamp(position.Y, 0f, Math.Max(0f, screen.Height - _panel.Height.Pixels));
        if (_panel.Dragging || _position.HasValue)
            _position = position;
        if (_panel.Left.Pixels != position.X || _panel.Top.Pixels != position.Y)
        {
            _panel.Left.Pixels = position.X;
            _panel.Top.Pixels = position.Y;
            _panel.Recalculate();
        }

        // Don't use items or click the world through the panel
        if (_panel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        // Close if Debug Mode gets turned off
        if (!GameplayConfig.Instance.DebugMode)
            Visible = false;
    }

    /// <summary>
    ///     A panel that can be dragged around by clicking anywhere that isn't a button.
    /// </summary>
    private sealed class DragPanel : UIPanel
    {
        private Vector2 _grabOffset;
        public bool Dragging { get; private set; }

        public Vector2 DragPosition() => Main.MouseScreen - _grabOffset;

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            if (evt.Target != this)
                return;
            _grabOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
            Dragging = true;
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            Dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Dragging && !Main.mouseLeft)
                Dragging = false;
        }
    }

    private static void Say(string key, params object[] args)
        => Main.NewText(Text(key, args), TerramonCommand.ChatColorYellow);

    private static PokemonData GetActiveOrWarn()
    {
        var active = TerramonPlayer.LocalPlayer.GetActivePokemon();
        if (active == null)
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"), TerramonCommand.ChatColorRed);
        return active;
    }

    private static void HealParty()
    {
        var healed = 0;
        foreach (var data in TerramonPlayer.LocalPlayer.Party)
        {
            if (data == null)
                continue;
            data.HP = data.MaxHP;
            data.Status = NonVolatileStatus.None;
            data.RestorePP();
            healed++;
        }

        Main.NewText(Language.GetTextValue("Mods.Terramon.Commands.Heal.Success", healed), TerramonCommand.ChatColorYellow);
    }

    private static void LevelUpActive(int levels)
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        for (var i = 0; i < levels && data.LevelUp(); i++)
        {
        }

        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.RareCandyUse", data.DisplayName, data.Level),
            TerramonCommand.ChatColorYellow);
        // The party growth check then evolves it (if it reached its level) and teaches the new moves
    }

    /// <summary>
    ///     Evolves the active Pokémon into its next level-up evolution, whatever its level.
    /// </summary>
    private static void EvolveActive()
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        var evolution = data.Schema.Evolution;
        if (evolution == null || evolution.ID == data.ID)
        {
            Say("NoEvolution", data.DisplayName);
            return;
        }

        // The evolution sequence says what it evolved into once the new form is revealed
        data.EvolveInto(evolution.ID);
        TerramonPlayer.LocalPlayer.UpdatePokedex(evolution.ID, PokedexEntryStatus.Registered, shiny: data.IsShiny);
        SoundEngine.PlaySound(SoundID.Item4);
    }

    private static void PerfectIVs()
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        var oldMaxHP = data.MaxHP;
        data.IVs = new PokemonIVs(31, 31, 31, 31, 31, 31);
        data.HP = (ushort)Math.Clamp(data.HP + data.MaxHP - oldMaxHP, 0, data.MaxHP);
        Say("PerfectIVsDone", data.DisplayName);
    }

    private static int _spriteMoveIndex;

    /// <summary>
    ///     Puts a move that has its own drawn animation (Assets/Moves) in the active Pokémon's first slot, one after the
    ///     other on each click, so the animations can be tried without leveling up.
    /// </summary>
    private static void TeachSpriteMove()
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        var moves = Enum.GetValues<MoveID>()
            .Where(m => m != MoveID.None && ModContent.HasAsset($"Terramon/Assets/Moves/{m}"))
            .Distinct()
            .ToArray();
        if (moves.Length == 0)
            return;
        var move = moves[_spriteMoveIndex++ % moves.Length];
        data.SetMove(0, move);
        Say("TeachSpriteMoveDone", data.DisplayName, MoveLearnUI.MoveName(move));
    }

    private static void ToggleShiny()
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        data.IsShiny = !data.IsShiny;
        Say(data.IsShiny ? "ShinyOn" : "ShinyOff", data.DisplayName);
    }

    private static void GiveHeldItems()
    {
        var player = Main.LocalPlayer;
        foreach (var item in ModContent.GetContent<BattleHeldItem>())
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), item.Type);
        Say("HeldItemsGiven");
    }

    private static void GiveEvolutionStones()
    {
        var player = Main.LocalPlayer;
        foreach (var item in ModContent.GetContent<EvolutionaryItem>())
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), item.Type, 3);
        Say("StonesGiven");
    }

    #region Terraria

    private static void ToggleTime()
    {
        // Jump to 4:30 in the morning or 7:30 at night
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            Main.NewText(Text("SingleplayerOnly"), TerramonCommand.ChatColorRed);
            return;
        }

        Main.dayTime = !Main.dayTime;
        Main.time = 0;
        Say(Main.dayTime ? "NowDay" : "NowNight");
    }

    private static void ToggleRain()
    {
        if (Main.raining)
            Main.StopRain();
        else
            Main.StartRain();
        Say(Main.raining ? "RainOn" : "RainOff");
    }

    private static void ToggleGodMode()
    {
        var cheats = Main.LocalPlayer.GetModPlayer<AdminCheatPlayer>();
        cheats.GodMode = !cheats.GodMode;
        Say(cheats.GodMode ? "GodModeOn" : "GodModeOff");
    }

    private static void HealPlayer()
    {
        var player = Main.LocalPlayer;
        player.statLife = player.statLifeMax2;
        player.statMana = player.statManaMax2;
        for (var i = 0; i < Player.MaxBuffs; i++)
        {
            if (player.buffType[i] > 0 && Main.debuff[player.buffType[i]])
                player.DelBuff(i--);
        }

        player.HealEffect(player.statLifeMax2);
        Say("PlayerHealed");
    }

    private static void TeleportTo(Vector2 worldPosition)
    {
        var player = Main.LocalPlayer;
        var position = worldPosition - new Vector2(player.width / 2f, player.height);
        player.Teleport(position, TeleportationStyleID.RodOfDiscord);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, position.X, position.Y,
                TeleportationStyleID.RodOfDiscord);
    }

    private static void TeleportToSpawn()
    {
        var player = Main.LocalPlayer;
        var spawn = player.SpawnX >= 0 && player.SpawnY >= 0
            ? new Point(player.SpawnX, player.SpawnY)
            : new Point(Main.spawnTileX, Main.spawnTileY);
        TeleportTo(spawn.ToWorldCoordinates(8f, 0f));
    }

    /// <summary>
    ///     The next click in the world teleports there (see <see cref="AdminCheatPlayer" />).
    /// </summary>
    private static void TeleportToCursor()
    {
        Main.LocalPlayer.GetModPlayer<AdminCheatPlayer>().TeleportOnClick = true;
        Say("ClickToTeleport");
    }

    public static void TeleportPlayerTo(Vector2 worldPosition) => TeleportTo(worldPosition);

    private static void KillHostiles()
    {
        var killed = 0;
        foreach (var npc in Main.ActiveNPCs)
        {
            if (npc.friendly || npc.townNPC || npc.dontTakeDamage || npc.ModNPC is PokemonNPC or TrainerNPC ||
                npc.Distance(Main.LocalPlayer.Center) > 16f * 120f)
                continue;
            npc.SimpleStrikeNPC(npc.lifeMax * 10, 0, noPlayerInteraction: true);
            killed++;
        }

        Say("HostilesKilled", killed);
    }

    private static void GiveMoney()
    {
        var player = Main.LocalPlayer;
        player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, 10);
        Say("MoneyGiven");
    }

    private static void GiveGear()
    {
        var player = Main.LocalPlayer;
        var source = player.GetSource_GiftOrReward();
        foreach (var type in new[]
                 {
                     ItemID.Shellphone, ItemID.LunarHook, ItemID.TerrasparkBoots, ItemID.FishronWings,
                     ItemID.LaserDrill, ItemID.Torch
                 })
            player.QuickSpawnItem(source, type, type == ItemID.Torch ? 99 : 1);
        Say("GearGiven");
    }

    private static void GiveBossItems()
    {
        var player = Main.LocalPlayer;
        var source = player.GetSource_GiftOrReward();
        foreach (var type in new[]
                 {
                     ItemID.SlimeCrown, ItemID.SuspiciousLookingEye, ItemID.WormFood, ItemID.BloodySpine,
                     ItemID.Abeemination, ItemID.DeerThing, ItemID.GuideVoodooDoll, ItemID.QueenSlimeCrystal,
                     ItemID.MechanicalEye, ItemID.MechanicalWorm, ItemID.MechanicalSkull, ItemID.LihzahrdPowerCell,
                     ItemID.TruffleWorm, ItemID.CelestialSigil
                 })
            player.QuickSpawnItem(source, type);
        Say("BossItemsGiven");
    }

    private static void RevealMap()
    {
        for (var x = 0; x < Main.maxTilesX; x++)
        {
            for (var y = 0; y < Main.maxTilesY; y++)
            {
                if (WorldGen.InWorld(x, y))
                    Main.Map.Update(x, y, 255);
            }
        }

        Main.refreshMap = true;
        Say("MapRevealed");
    }

    #endregion

    private static void FaintActive()
    {
        var data = GetActiveOrWarn();
        if (data == null)
            return;
        data.HP = 0;
        data.Faint();
    }

    private static void GiveItems()
    {
        var player = Main.LocalPlayer;
        var source = player.GetSource_GiftOrReward();
        player.QuickSpawnItem(source, ModContent.ItemType<PokeBallItem>(), 10);
        player.QuickSpawnItem(source, ModContent.ItemType<UltraBallItem>(), 5);
        player.QuickSpawnItem(source, ModContent.ItemType<MasterBallItem>());
        player.QuickSpawnItem(source, ModContent.ItemType<Potion>(), 5);
        player.QuickSpawnItem(source, ModContent.ItemType<Revive>(), 5);
        player.QuickSpawnItem(source, ModContent.ItemType<RareCandy>(), 10);
        Say("ItemsGiven");
    }

    private static Vector2 InFrontOfPlayer()
    {
        var player = Main.LocalPlayer;
        return player.Bottom + new Vector2(player.direction * 64f, 0f);
    }

    private static void SpawnWild()
    {
        var types = PokemonEntityLoader.IDToNPCType.Values.ToArray();
        if (types.Length == 0)
            return;
        var pos = InFrontOfPlayer();
        NPC.NewNPC(Main.LocalPlayer.GetSource_FromThis(), (int)pos.X, (int)pos.Y, Main.rand.Next(types));
    }

    private static void SpawnOnix()
    {
        if (!PokemonEntityLoader.IDToNPCType.TryGetValue(NationalDexID.Onix, out var type))
            return;
        var pos = InFrontOfPlayer();
        NPC.NewNPC(Main.LocalPlayer.GetSource_FromThis(), (int)pos.X, (int)pos.Y, type);
    }

    private static void SpawnLucario()
    {
        if (!PokemonEntityLoader.IDToNPCType.TryGetValue(NationalDexID.Lucario, out var type))
            return;
        var pos = InFrontOfPlayer();
        NPC.NewNPC(Main.LocalPlayer.GetSource_FromThis(), (int)pos.X, (int)pos.Y, type);
    }

    private static void GiveLucario()
    {
        var player = TerramonPlayer.LocalPlayer;
        var data = PokemonData.Create(NationalDexID.Lucario, 30).CaughtBy(player.Player).Build();
        Say(player.AddPartyPokemon(data, out _) ? "LucarioGiven" : "PartyFull");
    }

    private static void ClearWild()
    {
        foreach (var npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is PokemonNPC { BattleClient.Foe: null })
                npc.active = false;
        }
    }

    private static void SpawnSelectedTrainer()
    {
        // Goes through the server in multiplayer, like the challenge items
        League.RequestSpawn(Main.LocalPlayer, League.TrainerType(AllTrainers[_trainerIndex]));
    }

    /// <summary>
    ///     The bosses that unlock each step of the League, in order. Only changes this game's flags (meant for
    ///     singleplayer testing).
    /// </summary>
    private static readonly (string Key, Func<bool> IsDowned, Action Down)[] BossSteps =
    [
        ("EyeOfCthulhu", () => NPC.downedBoss1, () => NPC.downedBoss1 = true),
        ("EvilBoss", () => NPC.downedBoss2, () => NPC.downedBoss2 = true),
        ("Skeletron", () => NPC.downedBoss3, () => NPC.downedBoss3 = true),
        ("WallOfFlesh", () => Main.hardMode, () => Main.hardMode = true),
        ("AnyMech", () => NPC.downedMechBossAny, () => NPC.downedMechBoss1 = NPC.downedMechBossAny = true),
        ("AllMechs", () => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
            () => NPC.downedMechBoss1 = NPC.downedMechBoss2 = NPC.downedMechBoss3 = NPC.downedMechBossAny = true),
        ("Plantera", () => NPC.downedPlantBoss, () => NPC.downedPlantBoss = true),
        ("Golem", () => NPC.downedGolemBoss, () => NPC.downedGolemBoss = true),
        ("Cultist", () => NPC.downedAncientCultist, () => NPC.downedAncientCultist = true),
        ("MoonLord", () => NPC.downedMoonlord, () => NPC.downedMoonlord = true)
    ];

    private static void AdvanceBossProgress()
    {
        foreach (var (key, isDowned, down) in BossSteps)
        {
            if (isDowned())
                continue;
            down();
            Say("BossDowned", Text("Boss." + key));
            return;
        }

        // Everything was already defeated: start over
        NPC.downedBoss1 = NPC.downedBoss2 = NPC.downedBoss3 = Main.hardMode = false;
        NPC.downedMechBoss1 = NPC.downedMechBoss2 = NPC.downedMechBoss3 = NPC.downedMechBossAny = false;
        NPC.downedPlantBoss = NPC.downedGolemBoss = NPC.downedAncientCultist = NPC.downedMoonlord = false;
        Say("BossesReset");
    }

    private static void GiveAllBadges()
    {
        var player = Main.LocalPlayer;
        var badges = player.GetModPlayer<GymPlayer>().Badges;
        foreach (var name in League.GymLeaders)
        {
            badges.Add(name);

            // Hand over the badge items too, so the Badge Case can be checked
            var badgeType = BadgeTypeOf(name);
            if (badgeType > 0 && !player.HasItem(badgeType))
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), badgeType);
        }

        Say("BadgesGiven");
    }

    private static int BadgeTypeOf(string leader) => leader switch
    {
        "Brock" => ModContent.ItemType<BoulderBadge>(),
        "Misty" => ModContent.ItemType<CascadeBadge>(),
        "LtSurge" => ModContent.ItemType<ThunderBadge>(),
        "Erika" => ModContent.ItemType<RainbowBadge>(),
        "Koga" => ModContent.ItemType<SoulBadge>(),
        "Sabrina" => ModContent.ItemType<MarshBadge>(),
        "Blaine" => ModContent.ItemType<VolcanoBadge>(),
        "Giovanni" => ModContent.ItemType<EarthBadge>(),
        _ => 0
    };

    private static void GiveLeagueItems()
    {
        var player = Main.LocalPlayer;
        foreach (var item in ModContent.GetContent<ChallengeItem>())
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), item.Type);
        Say("LeagueItemsGiven");
    }

    private static void ResetBadges()
    {
        Main.LocalPlayer.GetModPlayer<GymPlayer>().ResetLeague();
        Say("BadgesReset");
    }

    private static void ClearBattles()
    {
        new ResetEverythingStatement().Send();
        Say("BattlesCleared");
    }

    /// <summary>
    ///     Forgets that the player met Professor Oak and calls him over, to test his welcome.
    /// </summary>
    private static void CallProfessorOak()
    {
        Main.LocalPlayer.GetModPlayer<OakPlayer>().MetOak = false;
        ProfessorOak.RequestVisit(Main.LocalPlayer, test: true);
        Say("OakCalled");
    }

    private static void TeleportToBrockGym()
    {
        if (!BrockGymSystem.Exists)
        {
            Main.NewText(Text("NoBrockGym"), TerramonCommand.ChatColorRed);
            return;
        }

        var player = Main.LocalPlayer;
        var position = BrockGymSystem.EntranceSpot - new Vector2(player.width / 2f, player.height);
        player.Teleport(position, TeleportationStyleID.RodOfDiscord);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, position.X, position.Y,
                TeleportationStyleID.RodOfDiscord);
    }

    /// <summary>
    ///     Builds Brock's Gym around the player, for worlds made before it existed. Singleplayer only, since the
    ///     tiles it changes aren't sent to other players.
    /// </summary>
    private static void BuildBrockGymHere()
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
        {
            Main.NewText(Text("SingleplayerOnly"), TerramonCommand.ChatColorRed);
            return;
        }

        var player = Main.LocalPlayer;
        var feet = player.Bottom.ToTileCoordinates();
        var left = Math.Clamp(feet.X - 8, 60, Main.maxTilesX - BrockGymBuilder.Width - 60);
        var top = Math.Clamp(feet.Y - BrockGymBuilder.Height + 4, 60, Main.maxTilesY - BrockGymBuilder.Height - 60);
        BrockGymBuilder.Build(left, top);
        TeleportToBrockGym();
        Say("BrockGymBuilt");
    }
}
