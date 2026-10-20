using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.NPCs;
using Terramon.Content.NPCs.Trainers;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
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

    private UIPanel _panel;

    /// <summary>
    ///     Every trainer in League order, for the trainer spawn button.
    /// </summary>
    private static readonly string[] AllTrainers = [..League.GymLeaders, ..League.EliteFourAndChampion];

    private static int _trainerIndex;
    private UITextPanel<LocalizedText> _trainerButton;

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

    public override void OnInitialize()
    {
        (string Key, Action Action)[] actions =
        [
            ("HealParty", HealParty),
            ("LevelUp1", () => LevelUpActive(1)),
            ("LevelUp5", () => LevelUpActive(5)),
            ("FaintActive", FaintActive),
            ("GiveItems", GiveItems),
            ("SpawnWild", SpawnWild),
            ("ClearWild", ClearWild),
            ("SpawnTrainer", SpawnSelectedTrainer),
            ("NextBoss", AdvanceBossProgress),
            ("GiveBadges", GiveAllBadges),
            ("LeagueItems", GiveLeagueItems),
            ("ResetBadges", ResetBadges),
            ("ClearBattles", ClearBattles),
            ("Close", Toggle)
        ];

        var rows = (actions.Length + Columns - 1) / Columns;
        _panel = new UIPanel
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _panel.Width.Pixels = Columns * (ButtonWidth + Gap) + Gap + _panel.PaddingLeft + _panel.PaddingRight;
        _panel.Height.Pixels = 40f + rows * (ButtonHeight + Gap) + _panel.PaddingTop + _panel.PaddingBottom;
        Append(_panel);

        _panel.Append(new UIText(Label("Title"), 0.9f)
        {
            HAlign = 0.5f
        });

        for (var i = 0; i < actions.Length; i++)
        {
            var (key, action) = actions[i];
            var button = new UITextPanel<LocalizedText>(Label(key), 0.85f);
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
            button.Top.Pixels = 40f + i / Columns * (ButtonHeight + Gap);
            button.OnMouseOver += (_, el) => ((UIPanel)el).BackgroundColor = new Color(73, 94, 171);
            button.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = new Color(63, 82, 151) * 0.7f;
            button.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                action();
            };
            _panel.Append(button);
        }
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Visible || _panel == null)
            return;

        // Don't use items or click the world through the panel
        if (_panel.ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;

        // Close if Debug Mode gets turned off
        if (!GameplayConfig.Instance.DebugMode)
            Visible = false;
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
    }

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
}
