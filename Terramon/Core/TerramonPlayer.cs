using System.Reflection;
using System.Text;
using EasyPacketsLib;
using MonoMod.Cil;
using Showdown.NET.Definitions;
using Terramon.Content.Buffs;
using Terramon.Content.Commands;
using Terramon.Content.GUI;
using Terramon.Content.GUI.TurnBased;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.NPCs.Trainers;
using Terramon.Content.Projectiles;
using Terramon.Content.Tiles.Banners;
using Terramon.Content.Tiles.Interactive;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders;
using Terramon.Core.Loaders.UILoading;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Core;

public class TerramonPlayer : ModPlayer, IBattleProvider
{
    private static int _hoveredPlayer = -1;
    private readonly PCService _pc = new();
    private readonly PokedexService _pokedex = new();
    private readonly PokedexService _shinyDex = new();

    private int _activePCTileEntityID = -1;
    private int _activeSlot = -1;
    private int _lastActiveSlot = -1;

    // ReSharper disable once InconsistentNaming
    internal BattleClient _battleClient;
    private bool _lastPlayerInventory;

    private bool _locallyRequestedClient;
    private int _premierBonusCount;
    private bool _receivedShinyCharm;

    public Vector3 ColorPickerHSL;
    public bool ExpShareOn;
    public bool HasChosenStarter;
    public bool HasExpCharm;
    public bool HasPokeBanner;
    public bool HasShinyCharm;
    public ExpShareSettings NonParticipantSettings = new(0.5f);
    public ExpShareSettings ParticipantSettings = new();

    public int ActivePCTileEntityID
    {
        get => _activePCTileEntityID;
        set
        {
            _activePCTileEntityID = value;
            if (Main.myPlayer != Player.whoAmI) return; // Only the local player should handle this
            if (value != -1)
                PCInterface.OnOpen();
            else
                PCInterface.OnClose();
        }
    }

    public PokemonData[] Party { get; } = new PokemonData[6];

    public int ActiveSlot
    {
        get => _activeSlot;
        set
        {
            // Don't double execute logic here
            if (_activeSlot == value) return;

            // Toggle off dedicated pet slot
            if (_activeSlot == -1 && !Player.miscEquips[0].IsAir)
                Player.hideMisc[0] = true;

            // Cancel Pokémon cry sound in party display UI
            if (Player == Main.LocalPlayer)
                PartySidebarSlot.CrySoundSource?.Cancel();

            _activeSlot = value;
            if (value != -1)
                _lastActiveSlot = _activeSlot;
            else if (Party[_lastActiveSlot] == null)
                _lastActiveSlot = 0;

            var buffType = ModContent.BuffType<PokemonCompanion>();
            var hasBuff = Player.HasBuff(buffType);
            switch (value)
            {
                case >= 0 when !hasBuff:
                    Player.AddBuff(buffType, 2);
                    break;
                case -1 when hasBuff:
                    Player.ClearBuff(buffType);
                    break;
            }
        }
    }

    public PokemonPet ActivePetProjectile
    {
        get => _activeSlot >= 0 ? field : null;
        set;
    }

    public static TerramonPlayer LocalPlayer => Main.LocalPlayer.Terramon();

    /// <summary>
    ///     How close (in pixels) another player has to be to be challenged to a battle.
    /// </summary>
    public const float ChallengeRange = 50 * 16f;

    /// <summary>
    ///     How long a challenge to another player waits for an answer before it's taken back. Longer than the
    ///     popup's own timeout, so this only matters if the answer got lost (e.g. the other player left).
    /// </summary>
    private const int ChallengeGiveUpTicks = (ChallengePopupUI.TimeoutSeconds + 10) * 60;

    private ulong _challengeSentAt;

    private static string MultiplayerText(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.Multiplayer.Challenge." + key, args);

    public bool StartBattle(IBattleProvider other)
    {
        var c = other.BattleClient;
        var isLocal = Player.whoAmI == Main.myPlayer;

        // Challenging another player: they have to be nearby and free
        if (other is TerramonPlayer target && isLocal)
        {
            if (target.Player.Distance(Player.Center) > ChallengeRange)
            {
                Main.NewText(MultiplayerText("TooFar", target.BattleName), Color.Orange);
                return false;
            }

            if (c is null || c.State != ClientBattleState.None)
            {
                Main.NewText(MultiplayerText("Busy", target.BattleName), Color.Orange);
                return false;
            }

            if (GetActivePokemon() is null)
            {
                Main.NewText(MultiplayerText("NeedPokemon"), Color.Orange);
                return false;
            }
        }

        if (c is null || c.State != ClientBattleState.None)
            return false;

        // Fainted Pokémon can't battle: neither our own active one nor a knocked out wild one
        var fainted = GetActivePokemon() is { HP: 0 } active ? active
            : other is Content.NPCs.PokemonNPC { Data.HP: 0 } wild ? wild.Data : null;
        if (fainted != null)
        {
            if (Player.whoAmI == Main.myPlayer)
                Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.CantBattleFainted", fainted.DisplayName),
                    Color.Orange);
            return false;
        }

        // Starting a battle of our own turns down a challenge we haven't answered yet
        if (isLocal && ChallengePopupUI.HasPending)
            ChallengePopupUI.Answer(false);

        if (other is TerramonPlayer && isLocal)
        {
            Main.NewText(MultiplayerText("Sent", other.BattleName), Color.LightSkyBlue);
            _challengeSentAt = Main.GameUpdateCount;
        }

        var question = new ChallengeQuestion
        {
            Sender = this
        };
        question.Send(other);
        return true;
    }

    /// <summary>
    ///     Why this (local) player can't take on a challenge from <paramref name="challenger" /> right now,
    ///     or null if they can.
    /// </summary>
    public string ChallengeRefusalReason(IBattleProvider challenger)
    {
        if (_battleClient is null || _battleClient.State != ClientBattleState.None)
            return MultiplayerText("ReasonBusy");
        if (GetActivePokemon() is not { HP: > 0 })
            return MultiplayerText("ReasonNoPokemon");
        if (challenger?.SyncedEntity is Player other && other.Distance(Player.Center) > ChallengeRange)
            return MultiplayerText("ReasonTooFar");
        return null;
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;

        IL_Main.DrawMouseOver += static il =>
        {
            var c = new ILCursor(il);

            var playerIndex = 0;
            c.GotoNext(
                i => i.MatchLdsfld<Main>(nameof(Main.player)),
                i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
                i => i.MatchLdelemRef(),
                i => i.MatchLdcI4(0),
                i => i.MatchStfld<Player>(nameof(Player.cursorItemIconEnabled)),
                i => i.MatchLdsfld<Main>(nameof(Main.player)),
                i => i.MatchLdloc(out playerIndex));

            c.EmitLdloc(playerIndex);
            c.EmitStsfld(typeof(TerramonPlayer).GetField(nameof(_hoveredPlayer),
                BindingFlags.NonPublic | BindingFlags.Static)!);
        };
    }

    /// <summary>
    ///     Returns this player's currently active Pokémon, or null if there is none.
    /// </summary>
    public PokemonData GetActivePokemon()
    {
        return ActiveSlot >= 0 ? Party[ActiveSlot] : null;
    }

    public PokedexService GetPokedex(bool shiny = false)
    {
        return shiny ? _shinyDex : _pokedex;
    }

    public PCService GetPC()
    {
        return _pc;
    }

    public override void OnEnterWorld()
    {
        MoveLearnUI.Close();
        ChallengePopupUI.Close();

        // Request a full sync of the World Dex from the server when joining a host in multiplayer
        if (Main.netMode == NetmodeID.MultiplayerClient) Mod.SendPacket(new RequestWorldDexRpc());
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Clear active PC when player dies
        TurnOffUsedPC();
    }

    private void TurnOffUsedPC()
    {
        if (_activePCTileEntityID != int.MaxValue)
        {
            if (_activePCTileEntityID != -1 &&
                TileEntity.ByID.TryGetValue(_activePCTileEntityID, out var entity) && entity is PCTileEntity
                {
                    PoweredOn: true
                } pc)
                pc.ToggleOnOff();
        }
        else
        {
            ActivePCTileEntityID = -1;
        }
    }

    public override void OnRespawn()
    {
        if (Player.whoAmI != Main.myPlayer) return;

        // Reapply companion buff on player respawn
        if (_activeSlot >= 0 && !Player.HasBuff(ModContent.BuffType<PokemonCompanion>()))
            Player.AddBuff(ModContent.BuffType<PokemonCompanion>(), 2);
    }

    public override void SetControls()
    {
        if (TestBattleUI.Instance.Visible && Player.controlInv && Player.releaseInventory)
        {
            TestBattleUI.GoBack();
            Player.releaseInventory = false;
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        ProcessActiveMonTriggers();

        if (HasChosenStarter && KeybindSystem.HubKeybind.JustPressed)
            HubUI.ToggleActive();

        if (KeybindSystem.BattleLogKeybind.JustPressed)
            BattleLogUI.Toggle();
        if (KeybindSystem.AdminPanelKeybind.JustPressed)
            AdminPanelUI.Toggle();

        if (_battleClient != null && _hoveredPlayer != -1 && _hoveredPlayer != Main.myPlayer &&
            _battleClient.State == ClientBattleState.None)
        {
            if (Main.mouseRight && Main.mouseRightRelease)
                StartBattle(Main.player[_hoveredPlayer].Terramon());
            _hoveredPlayer = -1;
        }

        if (!KeybindSystem.TogglePartyKeybind.JustPressed) return;
        var inventoryParty = UILoader.GetUIState<InventoryParty>();
        if (inventoryParty.Visible) inventoryParty.SimulateToggleSlots();
    }

    private void ProcessActiveMonTriggers()
    {
        var targetSlot = -1;
    
        if (KeybindSystem.TogglePokemonKeybind.JustPressed)
        {
            targetSlot = _activeSlot != -1 ? _activeSlot : _lastActiveSlot;
        }
        else if (KeybindSystem.NextPokemonKeybind.JustPressed)
        {
            targetSlot = _activeSlot != -1 ? GetNextValidSlot(_activeSlot) : _lastActiveSlot;
        }
        else if (KeybindSystem.PrevPokemonKeybind.JustPressed)
        {
            targetSlot = _activeSlot != -1 ? GetPreviousValidSlot(_activeSlot) : _lastActiveSlot;
        }
    
        if (targetSlot != -1)
            PartyDisplay.SimulateLeftClickOnSlot(targetSlot);
    }

    private int GetNextValidSlot(int currentSlot)
    {
        for (var i = 1; i <= 6; i++)
        {
            var nextSlot = (currentSlot + i) % 6;
            if (Party[nextSlot] != null)
                return nextSlot;
        }
        return currentSlot; // No valid slots found, stay on current
    }

    private int GetPreviousValidSlot(int currentSlot)
    {
        for (var i = 1; i <= 6; i++)
        {
            var prevSlot = (currentSlot - i + 6) % 6;
            if (Party[prevSlot] != null)
                return prevSlot;
        }
        return currentSlot; // No valid slots found, stay on current
    }

    public override void ResetEffects()
    {
        HasShinyCharm = false;
        ExpShareOn = false;
    }

    public override void PostUpdateBuffs()
    {
        if (TooltipOverlay.IsHoldingPokemon())
            Player.controlUseItem = false;
    }

    public override void PreUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;

        ProcessPartyGrowth();

        if ((Player.chest != -1 || (!Main.playerInventory && !HubUI.Active)) && ActivePCTileEntityID != -1)
            TurnOffUsedPC();

        // Handle player removing companion buff manually (right-clicking the buff icon)
        if (!Player.HasBuff<PokemonCompanion>() && _activeSlot >= 0 && !Player.dead)
            ActiveSlot = -1;

        // A fainted Pokémon can't stay out, so send it back once the battle is over
        if (_activeSlot >= 0 && Party[_activeSlot] is { HP: 0 } && _battleClient?.Foe == null)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.ReturnedFainted", Party[_activeSlot].DisplayName),
                Color.Orange);
            ActiveSlot = -1;
        }

        // End the sidebar animation if the player opens their inventory to prevent visual bugs
        if (Main.playerInventory && !_lastPlayerInventory)
            PartyDisplay.Sidebar.ForceKillAnimation();

        _lastPlayerInventory = Main.playerInventory;

        if (_premierBonusCount <= 0 || Main.npcShop != 0) return;
        var premierBonus = _premierBonusCount / 10;
        if (premierBonus > 0)
        {
            Main.NewText(Language.GetTextValue(
                $"Mods.Terramon.GUI.NPCShop.PremierBonus{(premierBonus != 1 ? "Plural" : string.Empty)}",
                premierBonus));

            Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<PremierBallItem>(),
                premierBonus);
        }

        _premierBonusCount = 0;
    }
    
    public override void PostUpdateMiscEffects()
    {
        // Prevents NPCs from targeting the player during turn-based battles
        if (BattleClient.BattleOngoing)
            Player.aggro = -750;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI == Main.myPlayer && Main.netMode == NetmodeID.MultiplayerClient)
            UpdateChallenges();

        if (_battleClient is null)
        {
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    _battleClient = new(this);
                    break;
                case NetmodeID.MultiplayerClient:
                    if (Player.whoAmI == Main.myPlayer)
                        goto case NetmodeID.SinglePlayer;
                    if (_locallyRequestedClient)
                        break;
                    Mod.SendPacket(new RequestClientRpc(), Player.whoAmI, Main.myPlayer, true);
                    _locallyRequestedClient = true;
                    break;
                case NetmodeID.Server:
                    if (_locallyRequestedClient)
                        break;
                    Mod.SendPacket(new RequestClientRpc(), Player.whoAmI);
                    _locallyRequestedClient = true;
                    break;
            }
        }
    }

    private void UpdateChallenges()
    {
        ChallengePopupUI.Tick();

        // Our challenge to another player got no answer: take it back so we aren't stuck waiting
        if (_battleClient is { State: ClientBattleState.Requested, Foe: TerramonPlayer target } &&
            Main.GameUpdateCount - _challengeSentAt > ChallengeGiveUpTicks)
        {
            var takeback = new ChallengeTakeback
            {
                Sender = this
            };
            takeback.Send(target);
            Main.NewText(MultiplayerText("NoAnswer", target.BattleName), Color.Orange);
        }
    }

    public override void PlayerDisconnect()
    {
        // A player who leaves forfeits their battle (or drops their pending challenge)
        if (Main.netMode == NetmodeID.Server)
            BattleManager.Instance?.ProviderLeft(this);
    }

    public override void PreUpdateBuffs()
    {
        if (HasPokeBanner)
            Player.AddBuff(ModContent.BuffType<PokeBannerBuff>(), 2, quiet: false);
    }

    public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item)
    {
        //use CanSellItem rather than PostSellItem because PostSellItem doesn't return item data correctly
        if (item.type == ModContent.ItemType<PokeBallItem>())
            _premierBonusCount -= item.stack;

        return item.type != ModContent.ItemType<MasterBallItem>() && base.CanSellItem(vendor, shopInventory, item);
    }

    public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
    {
        if (item.type == ModContent.ItemType<PokeBallItem>())
            _premierBonusCount++;
    }

    public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
    {
        if (attempt.inLava || attempt.inHoney)
            return;

        // Console.WriteLine(ValuableItem.Pool.ToString(i => Lang.GetItemName(i).Value));

        var terramonChance = FishingRod.DefaultCatchTerramonChance;
        if (attempt.bobberType == PokeBallBobber.ProjectileType)
            terramonChance = ((FishingRod)attempt.playerFishingConditions.Pole.ModItem).CatchTerramonChance;

        if (Main.rand.NextDouble() > terramonChance)
            return;

        if (Main.rand.NextBool(3)) // pokemon are less likely
        {
            var dexID = FishingPool.Choose(attempt, Player);
            if (dexID == 0 || !PokemonEntityLoader.IDToNPCType.TryGetValue(dexID, out var npcType))
            {
                itemDrop = ValuableItem.Pool.Get();
                return;
            }

            itemDrop = -1;
            SoundEngine.PlaySound(TerramonSoundID.GetCry(dexID), sonarPosition);

            npcSpawn = npcType;
        }
        else // items are more likely
        {
            itemDrop = ValuableItem.Pool.Get();
        }
    }

    /// <summary>
    ///     Adds a Pokémon to the player's party. Returns false if their party is full; otherwise returns true.
    /// </summary>
    /// <param name="data">The Pokémon to add to the party.</param>
    /// <param name="justRegistered">Whether the Pokémon was just registered in the Pokédex.</param>
    public bool AddPartyPokemon(PokemonData data, out bool justRegistered)
    {
        justRegistered = UpdatePokedex(data.ID, PokedexEntryStatus.Registered, shiny: data.IsShiny);
        var nextIndex = NextFreePartyIndex();
        if (nextIndex == 6) return false;
        // Caught or given Pokémon without an original trainer become this player's
        data.ClaimIfUnowned(Player.name);
        Party[nextIndex] = data;

        return true;
    }

    public void SwapParty(int first, int second)
    {
        if (_activeSlot == first)
            ActiveSlot = second;
        else if (_activeSlot == second)
            ActiveSlot = first;
        (Party[first], Party[second]) = (Party[second], Party[first]);
    }

    public int NextFreePartyIndex()
    {
        for (var i = 0; i < Party.Length; i++)
            if (Party[i] == null)
                return i;

        return 6;
    }

    public bool UpdatePokedex(ushort id, PokedexEntryStatus status, bool force = false, bool shiny = false)
    {
        TerramonWorld.UpdateWorldDex(id, status, Player.name, force);
        var hasEntry = _pokedex.Entries.TryGetValue(id, out var entry);
        var entryUpdated = false;

        if (hasEntry)
        {
            if (entry.Status < status || force)
            {
                entry.Status = status;
                entryUpdated = true;
            }

            if (status == PokedexEntryStatus.Registered)
            {
                entry.CaughtCount++;
                HandleCatchMilestoneRewards(id, entry.CaughtCount);
            }
        }

        if (shiny)
        {
            var hasShinyEntry = _shinyDex.Entries.TryGetValue(id, out var shinyEntry);
            if (hasShinyEntry)
            {
                if (shinyEntry.Status < status || force)
                    shinyEntry.Status = status;

                if (shinyEntry.Status == PokedexEntryStatus.Registered)
                    shinyEntry.CaughtCount++;
            }
        }

        if (HubUI.Active) UILoader.GetUIState<HubUI>().RefreshPokedex(id);
        if (!force && status == PokedexEntryStatus.Registered && entryUpdated &&
            _pokedex.RegisteredCount == Terramon.LoadedPokemonCount && !_receivedShinyCharm)
            GiveShinyCharmReward();
        return force ? hasEntry : entryUpdated;
    }

    private void HandleCatchMilestoneRewards(ushort id, int caughtCount)
    {
        var milestone = GetMilestoneFromCaughtCount(caughtCount);
        if (milestone == null) return;

        TerramonWorld.QueueNewText(Language.GetTextValue($"Mods.Terramon.Misc.CatchMilestone{caughtCount}",
            Terramon.DatabaseV2.GetLocalizedPokemonName(id).Value), TerramonCommand.ChatColorYellow);

        GiveBannerReward(id, milestone.Value);
    }

    private static BannerTier? GetMilestoneFromCaughtCount(int caughtCount)
    {
        return caughtCount switch
        {
            3 => BannerTier.Tier1,
            6 => BannerTier.Tier2,
            9 => BannerTier.Tier3,
            12 => BannerTier.Tier4,
            _ => null
        };
    }

    private void GiveBannerReward(ushort id, BannerTier tier)
    {
        if (!PokemonEntityLoader.IDToBannerType.TryGetValue(id, out var bannerType)) return;

        var bannerItem = new Item();
        bannerItem.SetDefaults(bannerType);
        var modItem = bannerItem.ModItem as PokeBannerItem;
        modItem!.Tier = tier;
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), bannerItem);
    }

    private void GiveShinyCharmReward()
    {
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
            ModContent.ItemType<ShinyCharm>());
        _receivedShinyCharm = true;
    }

    public PCBox TransferPokemonToPC(PokemonData data)
    {
        data?.ClaimIfUnowned(Player.name);
        return _pc.StorePokemon(data);
    }

    public string GetDefaultNameForPCBox(PCBox box)
    {
        return Language.GetTextValue("Mods.Terramon.Misc.PCBoxDefaultName", _pc.Boxes.IndexOf(box) + 1);
    }
    
    public bool IsPartyFull()
    {
        return NextFreePartyIndex() == 6;
    }
    
    public bool IsPCBoxFull(int boxIndex)
    {
        if (boxIndex < 0 || boxIndex >= _pc.Boxes.Count)
            return true;
        return _pc.Boxes[boxIndex].RemainingCapacity == 0;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["flags"] = (byte)new BitsByte(HasChosenStarter, _receivedShinyCharm);
        if (_activeSlot >= 0)
            tag["activeSlot"] = _activeSlot;
        SaveParty(tag);
        SavePokedex(tag);
        SavePC(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("flags"))
        {
            BitsByte flags = tag.GetByte("flags");
            HasChosenStarter = flags[0];
            _receivedShinyCharm = flags[1];
        }

        if (tag.TryGet("activeSlot", out int slot))
            _activeSlot = _lastActiveSlot = slot;
        else _lastActiveSlot = 0;

        LoadParty(tag);
        LoadPokedex(tag);
        LoadPC(tag);
    }

    private void SaveParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            if (Party[i] == null) continue;
            tag[$"p{i}"] = Party[i].SerializeData();
        }
    }

    private void LoadParty(TagCompound tag)
    {
        for (var i = 0; i < Party.Length; i++)
        {
            var tagName = $"p{i}";
            if (tag.ContainsKey(tagName)) Party[i] = PokemonData.Load(tag.GetCompound(tagName));
            // Pokémon caught before the original trainer was saved belong to this player
            Party[i]?.ClaimIfUnowned(Player.name);
        }
    }

    private void SavePokedex(TagCompound tag)
    {
        var pokedexEntries = _pokedex.GetEntriesForSaving();
        if (pokedexEntries.Count > 0)
            tag["pokedex"] = pokedexEntries;
        var shinyDexEntries = _shinyDex.GetEntriesForSaving();
        if (shinyDexEntries.Count > 0)
            tag["shinyDex"] = shinyDexEntries;
    }

    private void LoadPokedex(TagCompound tag)
    {
        const string tagName = "pokedex";
        const string shinyTagName = "shinyDex";
        if (tag.ContainsKey(tagName)) _pokedex.LoadEntries(tag.GetList<int[]>(tagName));
        if (tag.ContainsKey(shinyTagName)) _shinyDex.LoadEntries(tag.GetList<int[]>(shinyTagName));
    }

    private void SavePC(TagCompound tag)
    {
        tag["pc"] = _pc.Boxes.Select(box => box.SerializeData()).ToList();
    }

    private void LoadPC(TagCompound tag)
    {
        const string tagName = "pc";
        if (!tag.ContainsKey(tagName)) return;
        _pc.Boxes.Clear();
        var boxes = tag.GetList<TagCompound>(tagName).Select(PCBox.Load);
        foreach (var box in boxes)
        {
            box.Service = _pc;
            for (var i = 0; i < PCBox.Capacity; i++)
                box[i]?.ClaimIfUnowned(Player.name);
            _pc.Boxes.Add(box);
        }
    }

    public string GetPackedTeam()
    {
        StringBuilder sb = new();
        for (var i = 0; i < Party.Length; i++)
        {
            var p = Party[i];
            if (p == null) break;
            sb.Append($"{p.GetPacked(i)}]");
        }

        return sb.ToString().TrimEnd(']');
    }

    /// <param name="defeated">The Pokémon that was knocked out (or caught).</param>
    /// <param name="battleLogs">Whether to write the EXP gained and level ups to the chat.</param>
    /// <param name="deferredLogs">
    ///     If given, the chat lines are added here instead of being shown, so they can be shown later (e.g. with the
    ///     faint that gave the EXP, see <see cref="BattlePlayback" />).
    /// </param>
    public void DefeatedPokemon(PokemonData defeated, bool battleLogs,
        List<(string Text, Color Color)> deferredLogs = null)
    {
        if (!ExpShareOn)
        {
            var active = GetActivePokemon();
            if (active is null || active.Status == NonVolatileStatus.Fnt)
                return;
            var expGain = active.ExperienceFromDefeat(defeated, 1f, this);
            var evGain = active.EVsFromDefeat(defeated, false);
            active.GainExperience(expGain, out var levelsGained, out _);
            active.TrainEVs(evGain, out _);
            if (battleLogs)
                LogYield(active, expGain, levelsGained, evGain, deferredLogs);
            return;
        }

        var p = Party;
        ParticipantSettings.Recalculate(p, true);
        NonParticipantSettings.Recalculate(p, false);
        for (var i = 0; i < Party.Length; i++)
        {
            var poke = Party[i];
            if (poke is null)
                continue;
            ref var settings =
                ref (poke.Participated ? ref ParticipantSettings : ref NonParticipantSettings);
            var myMult = settings[i];
            if (myMult == 0f)
                continue;
            var expGain = poke.ExperienceFromDefeat(defeated, myMult, this);
            var evGain = poke.EVsFromDefeat(defeated, settings.Disabled[i]);
            poke.GainExperience(expGain, out var levelsGained, out _);
            poke.TrainEVs(evGain, out _);
            if (battleLogs)
                LogYield(poke, expGain, levelsGained, evGain, deferredLogs);
        }
    }

    private static void LogYield(PokemonData recipient, int expGain, int levelsGained,
        IEnumerable<(StatID Stat, byte EffortIncrease)> gains, List<(string Text, Color Color)> deferredLogs)
    {
        void Say(string text, Color color)
        {
            if (deferredLogs != null)
                deferredLogs.Add((text, color));
            else
                BattleLog.Say(text, color);
        }

        BattleInstance.Log($"{recipient.DisplayName} gained {expGain} EXP!", BattleInstance.BattleReceiveFollowup);
        Say(Language.GetTextValue("Mods.Terramon.Battle.GainedExp", BattleLog.HighlightMon(recipient.DisplayName, true),
            BattleLog.Highlight(expGain, "ffd75e")), Color.White);
        if (levelsGained != 0)
        {
            BattleInstance.Log($"{recipient.DisplayName} is now level {recipient.Level}!",
                BattleInstance.BattleReceiveFollowup);
            Say(Language.GetTextValue("Mods.Terramon.Battle.LevelUp", BattleLog.HighlightMon(recipient.DisplayName, true),
                recipient.Level), Color.Gold);
        }
        if (gains is null)
            return;
        foreach (var (stat, increase) in gains)
            BattleInstance.Log($"{recipient.DisplayName}'s {stat} EV increased by {increase}!",
                BattleInstance.MetaFollowup);
    }

    #region Growth (move learning and evolution after level ups)

    /// <summary>
    ///     Once out of battle, evolves party Pokémon that reached their evolution level and teaches them the moves
    ///     they reached, asking which move to forget when they already know four. Runs on the owning client only;
    ///     species and level changes reach other players through the regular player sync.
    /// </summary>
    private void ProcessPartyGrowth()
    {
        if (Main.GameUpdateCount % 10 != 0 || MoveLearnUI.IsOpen || Player.dead)
            return;
        if (_battleClient != null && (_battleClient.Foe != null || _battleClient.State != ClientBattleState.None))
            return;
        if (TestBattleUI.Instance is { Visible: true })
            return;

        var fastEvolution = Content.Configs.ClientConfig.Instance.FastEvolution;

        for (var i = 0; i < Party.Length; i++)
        {
            var mon = Party[i];
            if (mon == null)
                continue;

            // Also catches Pokémon that reached their evolution level some other way (commands, the test panel,
            // a level set by the battle), since the flag only comes from LevelUp
            if (mon.PendingEvolutionCheck ||
                (fastEvolution && mon.GetQueuedEvolution(EvolutionTrigger.LevelUp) != 0))
            {
                mon.PendingEvolutionCheck = false;
                TryLevelUpEvolution(mon);
            }

            // New moves wait until the evolution sequence is over
            if (EvolutionQueue.IsPlaying)
                continue;

            var pending = mon.PendingMoves;
            while (pending is { Count: > 0 })
            {
                var move = pending[0];
                if (mon.KnowsMove(move) || Terramon.DatabaseV2.GetMove(move) == null)
                {
                    pending.RemoveAt(0);
                    continue;
                }

                if (mon.TryLearnMove(move))
                {
                    pending.RemoveAt(0);
                    Main.NewText(Language.GetTextValue("Mods.Terramon.MoveLearn.Learned", mon.DisplayName,
                        MoveLearnUI.MoveName(move)), new Color(50, 255, 130));
                    SoundEngine.PlaySound(SoundID.Item4);
                    continue;
                }

                // All four slots are taken: ask the player, one move at a time
                MoveLearnUI.Open(mon, move);
                return;
            }
        }
    }

    /// <summary>
    ///     Evolves a Pokémon that is ready for a level-up evolution (when Fast Evolution is on), or tells the player
    ///     how to evolve it.
    /// </summary>
    private void TryLevelUpEvolution(PokemonData mon)
    {
        var queued = mon.GetQueuedEvolution(EvolutionTrigger.LevelUp);
        if (queued == 0)
            return;

        var clientConfig = Content.Configs.ClientConfig.Instance;
        if (!clientConfig.FastEvolution)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolutionReady", mon.DisplayName) + " " +
                         Language.GetTextValue("Mods.Terramon.MoveLearn.EvolveHint"), new Color(50, 255, 130));
            return;
        }

        // The evolution sequence says what it evolved into (and plays the fanfare) once the new form is revealed
        for (var step = 0; step < 3 && queued != 0; step++)
        {
            var evolvedSpeciesName = Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(queued);
            mon.EvolveInto(queued);
            var justRegistered = UpdatePokedex(queued, PokedexEntryStatus.Registered, shiny: mon.IsShiny);
            if (justRegistered && clientConfig.ShowPokedexRegistrationMessages)
                Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokedexRegistered", evolvedSpeciesName),
                    new Color(159, 162, 173));
            queued = mon.GetQueuedEvolution(EvolutionTrigger.LevelUp);
        }

        // Evolution sparkles around the player
        for (var j = 0; j < 40; j++)
        {
            var speed = Main.rand.NextVector2CircularEdge(1f, 1f);
            var d = Dust.NewDustPerfect(Player.Center + speed * 26, DustID.FrostHydra);
            d.noGravity = true;
        }
    }

    #endregion

    #region IBattleProvider

    public BattleProviderType ProviderType => BattleProviderType.Player;

    public BattleClient BattleClient => _battleClient;

    public Entity SyncedEntity => Player;

    public string BattleName => Player.name;

    public PokemonData[] GetBattleTeam() => Party;

    public void StartBattleEffects(bool before)
    {
        if (before)
        {
            if (Player.whoAmI != Main.myPlayer) return;
            BattleUI.ApplyStartEffects();
        }
        else ActivePetProjectile?.ConfrontFoe(_battleClient);
    }

    public void StopBattleEffects()
    {
        if (Player.whoAmI == Main.myPlayer)
        {
            BattleUI.ApplyEndEffects();

            // Battles between players are friendly: both teams are restored afterwards
            if (_battleClient?.Foe is TerramonPlayer)
            {
                foreach (var data in Party)
                {
                    if (data == null)
                        continue;
                    data.Heal(data.MaxHP);
                    data.Status = NonVolatileStatus.None;
                }

                Main.NewText(Language.GetTextValue("Mods.Terramon.Healing.Friendly"), Color.LightGreen);
            }
        }
        if (ActivePetProjectile != null)
            ActivePetProjectile.CustomTargetPosition = null;
    }

    public void Reply(BattleMessage m)
    {
        switch (m)
        {
            case ChallengeQuestion q:

                // Can't battle right now (or already deciding on another challenge): decline straight away
                var reason = ChallengeRefusalReason(m.Sender) ??
                             (ChallengePopupUI.HasPending ? MultiplayerText("ReasonBusy") : null);
                if (reason != null)
                {
                    Main.NewText(MultiplayerText("AutoDeclined", m.Sender.BattleName, reason), Color.Orange);
                    m.Return(new ChallengeAnswer(yes: false));
                    break;
                }

                // Ask the player with the accept/decline popup
                Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.ChallengeReceived", m.Sender.BattleName),
                    Color.LightSkyBlue);
                ChallengePopupUI.Open(q);
                break;
            case ChallengeTakeback:
                ChallengePopupUI.Close();
                Main.NewText(MultiplayerText("TakenBack", m.Sender.BattleName), Color.Orange);
                break;
            case ChallengeError:
                Main.NewText(MultiplayerText("Unavailable", m.Sender.BattleName), Color.Orange);
                break;
            case ChallengeAnswer c:

                if (c.Yes)
                {
                    Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.ChallengeAccepted", m.Sender.BattleName));
                    // Imbalanced operation: Will send battle pick to foe here,
                    // and the other one's pick will be sent in SlotChoice to server
                    var slot = (byte)(_activeSlot == -1 ? 1 : _activeSlot + 1);
                    m.Return(new SlotChoice(slot: slot));
                }
                else
                    Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.ChallengeDeclined", m.Sender.BattleName));

                break;
            case SlotChoice:
                var slot0 = (byte)(_activeSlot == -1 ? 1 : _activeSlot + 1);
                var pick = new SlotChoice(slot: slot0)
                {
                    Sender = this
                };
                pick.Send();
                break;
            case TeamQuestion:
                m.Return(new TeamAnswer(this.GetNetTeam()));
                break;
            case TieQuestion:

                // Show like a little notification and/or change the color of the tie button
                Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.TieAsked", m.Sender.BattleName));
                break;
            case TieTakeback:

                // Reverse the effect from the above message
                Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.TieTakeback", m.Sender.BattleName));
                break;
        }
    }

    public void Witness(BattleMessage m)
    {
        Console.WriteLine($"Client: Witnessing a {m.GetType().Name}");

        switch (m)
        {
            case ResetEverythingStatement:

                BattlePlayback.Clear();
                foreach (var plr in Main.ActivePlayers)
                    ((IBattleProvider)plr.Terramon()).BattleStopped();
                foreach (var npc in Main.ActiveNPCs)
                {
                    if (npc.ModNPC is IBattleProvider p)
                        p.BattleStopped();
                }

                break;
            case ChallengeQuestion:

                // Set client fields
                // Keep in mind that this is also witnessed by the sender
                m.Sender.State = ClientBattleState.Requested;
                m.Sender.Foe = m.Recipient;
                break;
            case ChallengeTakeback:

                // Unset client fields
                m.Sender.State = ClientBattleState.None;
                m.Sender.Foe = null;
                break;
            case ChallengeError:

                // The server refused the challenge (or answer): unset the fields of whoever it was sent back to,
                // as long as they were still waiting on this very foe
                if (m.Recipient?.BattleClient is { } refused && refused.Foe == m.Sender &&
                    refused.State is ClientBattleState.Requested or ClientBattleState.PollingSlot)
                {
                    refused.Battle = null;
                    refused.Foe = null;
                }

                break;
            case ChallengeAnswer c:

                if (c.Yes) // If the sender accepted
                {
                    // Set client fields
                    m.Sender.State = m.Recipient.State = ClientBattleState.PollingSlot;
                    m.Sender.Foe = m.Recipient;

                    // Create battle field early to avoid nullrefs. In singleplayer the battle already made the
                    // field both sides use, and a second one would show every turn twice
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        var bf = new BattleField
                        {
                            A = new(m.Recipient),
                            B = new(m.Sender),
                        };
                        bf.LinkSides();

                        m.Recipient.Field = m.Sender.Field = bf;
                    }
                }
                else
                {
                    // Unset client fields
                    m.Recipient.State = ClientBattleState.None;
                    m.Recipient.Foe = null;
                }

                break;
            case SlotChoice s:
                m.Sender.Pick = s.Slot;
                break;
            case TeamAnswer t:
                m.Sender.State = ClientBattleState.SetTeam;
                if (m.Sender is TerramonPlayer { IsLocalProvider: false } remote)
                    remote.UseTeamForBattle(t.Team);
                break;
            case StartBattleStatement s:

                // Show start message and do battle start effects
                var owner = s.BattleOwner;
                var other = owner.Foe;
                var a = owner.BattleName;
                var b = other.BattleName;

                Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.Started", a, b), Color.Aqua);

                // If we're in singleplayer, break early so this stuff only runs from BattleManager
                if (Main.netMode == NetmodeID.SinglePlayer)
                    break;

                // Set states
                owner.State = other.State = ClientBattleState.Ongoing;

                // Effects
                owner.StartBattleEffects(before: true);
                other.StartBattleEffects(before: false);
                owner.StartBattleEffects(before: false);
                break;
            case ForfeitStatement f:
            {
                // Show forfeit message and do battle end effects (once the last turn has been shown)
                var forfeiter = f.Forfeiter;
                var winner = forfeiter.Foe;
                // A wild Pokémon "forfeits" when it's caught mid-battle; the catch shows its own message
                var text = forfeiter is Content.NPCs.PokemonNPC { PlasmaState: true }
                    ? null
                    : Language.GetTextValue("Mods.Terramon.Battle.Forfeited", forfeiter.BattleName, winner.BattleName);

                AfterLastTurn(forfeiter, winner, () =>
                {
                    if (text != null)
                        Main.NewText(text, Color.Lime);
                });
                break;
            }
            case WinStatement w:
            {
                // Show win message and do battle end effects (once the final hit has been shown)
                var winner = w.Winner;
                var loser = winner.Foe;
                var text = Language.GetTextValue("Mods.Terramon.Battle.Defeated", winner.BattleName, loser.BattleName);

                AfterLastTurn(winner, loser, () =>
                {
                    Main.NewText(text, Color.Lime);

                    // Reward the local player for beating a trainer
                    if (loser is TrainerNPC trainer && winner == this)
                        trainer.OnDefeatedBy(this);
                });
                break;
            }
            case TieQuestion:
                m.Sender.TieRequest = true;
                break;
            case TieTakeback:
                m.Sender.TieRequest = false;
                break;
            case TieStatement t:
            {
                // Show tie messsage and do battle end effects (once the last turn has been shown)
                var either = t.EitherParticipant;
                var foe = either.Foe;
                var first = either.BattleName;
                var second = foe.BattleName;
                var (text, color) = t.Type switch
                {
                    TieStatement.TieType.Agreed => (
                        Language.GetTextValue("Mods.Terramon.Battle.TieAgreed", first, second), Color.Yellow),
                    TieStatement.TieType.Forced => (
                        Language.GetTextValue("Mods.Terramon.Battle.TieForced", first, second), Color.Red),
                    _ => (Language.GetTextValue("Mods.Terramon.Battle.TieRegular", first, second), Color.Yellow)
                };

                AfterLastTurn(either, foe, () => Main.NewText(text, color));
                break;
            }
        }
    }

    /// <summary>
    ///     Ends a battle for both participants after <paramref name="announce" />: right away, or once the battle's last
    ///     turn has been shown (see <see cref="BattlePlayback" />), so the result never shows up before the final hit.
    /// </summary>
    private static void AfterLastTurn(IBattleProvider first, IBattleProvider second, Action announce)
    {
        var field = first?.BattleClient?.Battle ?? second?.BattleClient?.Battle;

        void End()
        {
            announce();
            Stop(first);
            Stop(second);
        }

        // Stops a participant unless it already left this battle (or started another one meanwhile)
        void Stop(IBattleProvider provider)
        {
            if (provider?.BattleClient is not { } client)
                return;
            if (field != null && client.Battle != null && client.Battle != field)
                return;
            provider.BattleStopped();
        }

        if (!BattlePlayback.Defer(field, End))
            End();
    }

    public void SetActiveSlot(byte newSlot)
    {
        // Only the owner really switches. Other machines just hold a copy of the active Pokémon (in Party[0]),
        // which the active Pokémon sync updates once the owner has switched
        if (IsLocalProvider)
            ActiveSlot = newSlot;

        if (Main.dedServ)
            return;

        var loc = BattleClient.LocalClient;
        var mon = _battleClient?.Side is { } side && newSlot < side.Team.Length
            ? side.Team[newSlot].Data ?? GetActivePokemon()
            : GetActivePokemon();

        if (loc == BattleClient)
        {
            TestBattleUI.PlayerPanel.CurrentMon = mon;
        }
        else if (loc.Foe == this)
        {
            TestBattleUI.FoePanel.CurrentMon = mon;
        }
    }

    private bool IsLocalProvider => Main.netMode == NetmodeID.SinglePlayer || Player.whoAmI == Main.myPlayer;

    /// <summary>
    ///     Other machines only know this player's active Pokémon, so their battle side is filled in from the team
    ///     the player sent for the battle (the same data the server battles with).
    /// </summary>
    private void UseTeamForBattle(SimplePackedPokemon[] team)
    {
        var side = _battleClient?.Battle is { } field ? field.A.Provider == this ? field.A : field.B : null;
        if (side == null || team == null)
            return;

        side.TeamCount = 0;
        for (var i = 0; i < side.Team.Length; i++)
        {
            if (i >= team.Length)
            {
                side.Team[i].Data = null;
                continue;
            }

            var packed = team[i];
            var data = PokemonData.Create(packed.Species, packed.Level).Build();
            data.SetFromNetPacked(packed);
            data.HP = packed.HP == SimplePackedPokemon.FullHP ? data.MaxHP : packed.HP;
            side.Team[i].Data = data;
            side.TeamCount++;
        }
    }

    #endregion

    #region Network Sync

    /// <summary>
    ///     The bitmask of the <see cref="PokemonData" /> fields that require syncing for the player's active Pokémon.
    ///     These fields will be observed for changes and if they are changed, their new values will be forwarded to other
    ///     clients. Everything is synced so other players see the right HP bar, status tint, shininess and so on.
    /// </summary>
    private const int ActivePokemonSyncFields = PokemonData.AllFieldsBitmask;

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        var activePokemonData = GetActivePokemon();
        if (activePokemonData != null)
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData), toWho, fromWho);
        if (HasChosenStarter)
            Mod.SendPacket(new PlayerFlagsRpc((byte)Player.whoAmI, true), toWho, fromWho);
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        var clone = (TerramonPlayer)targetCopy;
        for (var i = 0; i < Party.Length; i++)
            if (clone.Party[i] != null && Party[i] == null)
                clone.Party[i] = null;
            else if (clone.Party[i] == null && Party[i] != null)
                clone.Party[i] = Party[i].ShallowCopy();
            else if (clone.Party[i] != null && Party[i] != null)
                Party[i].CopyNetStateTo(clone.Party[i], ActivePokemonSyncFields);
        clone.ActiveSlot = ActiveSlot;
        clone.HasChosenStarter = HasChosenStarter;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        var clone = (TerramonPlayer)clientPlayer;
        var activePokemonData = GetActivePokemon();
        var cloneActivePokemonData = clone.GetActivePokemon();
        // A different Pokémon was sent out (even one of the same species and level): send all of it
        if ((activePokemonData == null && cloneActivePokemonData != null) ||
            (activePokemonData != null && cloneActivePokemonData == null) ||
            (activePokemonData != null && ActiveSlot != clone.ActiveSlot))
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData), -1, Main.myPlayer, true);
        else if (activePokemonData != null &&
                 activePokemonData.IsNetStateDirty(cloneActivePokemonData, ActivePokemonSyncFields,
                     out var dirtyFields))
            Mod.SendPacket(new UpdateActivePokemonRpc((byte)Player.whoAmI, activePokemonData, dirtyFields), -1,
                Main.myPlayer, true);
        if (HasChosenStarter != clone.HasChosenStarter)
            Mod.SendPacket(new PlayerFlagsRpc((byte)Player.whoAmI, HasChosenStarter), -1, Main.myPlayer, true);
    }

    #endregion
}