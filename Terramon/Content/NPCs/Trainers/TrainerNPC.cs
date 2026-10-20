using Terramon.Content.GUI.TurnBased;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Base class for NPC trainers (such as Gym Leaders) that the player can challenge to a turn-based battle.
/// </summary>
public abstract class TrainerNPC : ModNPC, IBattleProvider
{
    /// <summary>
    ///     How far (in pixels) a player has to be for the trainer to count as left alone. 100 tiles.
    /// </summary>
    private const float LonelyDistance = 100 * 16f;

    /// <summary>
    ///     How long (in ticks) a trainer stays with nobody around before leaving.
    /// </summary>
    private const int LonelyTime = 60 * 60;

    /// <summary>
    ///     The team used in the current battle. Built fresh (at full HP) for every battle.
    /// </summary>
    private PokemonData[] _team = [];

    /// <summary>
    ///     Counts up while no player is nearby. Only used on the server (or in singleplayer).
    /// </summary>
    private int _lonelyTimer;

    /// <summary>
    ///     Counts down after a battle ends; the trainer leaves when it reaches zero. -1 when not counting.
    /// </summary>
    private int _leaveTimer = -1;

    /// <summary>
    ///     Set on the winner's client when they beat this trainer, so the end of the battle isn't treated as a loss.
    /// </summary>
    private bool _defeatedByLocalPlayer;

    /// <summary>
    ///     The species and level of each Pokémon this trainer battles with, in order (up to 6).
    /// </summary>
    protected abstract (ushort ID, byte Level)[] TeamSpec { get; }

    /// <summary>
    ///     The item given to the player the first time they defeat this trainer, or 0 for none.
    /// </summary>
    protected virtual int RewardItemType => 0;

    /// <summary>
    ///     Extra items given together with <see cref="RewardItemType" /> the first time the player wins.
    /// </summary>
    protected virtual IEnumerable<(int Type, int Stack)> FirstWinRewards(Player player) => [];

    /// <summary>
    ///     The vanilla town NPC this trainer borrows its look (texture and animation) from.
    /// </summary>
    protected virtual int LookAlike => NPCID.Guide;

    /// <summary>
    ///     The range each IV of this trainer's Pokémon is rolled in. Stronger trainers have better trained Pokémon.
    /// </summary>
    protected virtual (int Min, int Max) IVRange => (10, 31);

    /// <summary>
    ///     How long (in ticks) the trainer sticks around after a battle ends before leaving.
    /// </summary>
    protected virtual int LeaveDelayAfterBattle => 30 * 60;

    /// <summary>
    ///     A texture of this trainer's own (a single standing frame), used instead of <see cref="LookAlike" />.
    /// </summary>
    protected virtual string CustomTexture => null;

    public override string Texture => CustomTexture ?? $"Terraria/Images/NPC_{LookAlike}";

    /// <summary>
    ///     Whether the player is allowed to challenge this trainer, e.g. only after a certain boss is defeated.
    /// </summary>
    public virtual bool CanChallenge(Player player) => true;

    protected string Dialogue(string key) => Language.GetTextValue(this.GetLocalizationKey("Dialogue." + key));

    /// <summary>
    ///     The highest level in this trainer's team, used for the prize money.
    /// </summary>
    public int HighestLevel => TeamSpec.Length == 0 ? 1 : TeamSpec.Max(s => s.Level);

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = CustomTexture == null ? Main.npcFrameCount[LookAlike] : 1;
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }

    public override void SetDefaults()
    {
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = -1; // Stand still
        NPC.lifeMax = 250;
        NPC.friendly = true;
        NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0f;
        NPC.HitSound = SoundID.NPCHit1;
        AnimationType = CustomTexture == null ? LookAlike : -1;

        BattleClient = new BattleClient(this);
    }

    // Vanilla despawning is replaced by the timers in AI
    public override bool CheckActive() => false;

    public override void AI()
    {
        NPC.velocity.X = 0f;

        var inBattle = BattleClient?.Foe != null;

        // Face the closest player while waiting for a challenger
        if (!inBattle)
        {
            var closest = Player.FindClosest(NPC.position, NPC.width, NPC.height);
            if (Main.player[closest].active)
                NPC.spriteDirection = NPC.direction = Main.player[closest].Center.X > NPC.Center.X ? 1 : -1;
        }

        // Only the server (or singleplayer) decides when the trainer leaves
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (inBattle || IsBeingTalkedTo())
        {
            _lonelyTimer = 0;
            _leaveTimer = -1;
            return;
        }

        var playerNearby = Main.player.Any(p =>
            p.active && !p.dead && p.DistanceSQ(NPC.Center) < LonelyDistance * LonelyDistance);
        _lonelyTimer = playerNearby ? 0 : _lonelyTimer + 1;

        if (_leaveTimer > 0)
            _leaveTimer--;

        if (_lonelyTimer >= LonelyTime || _leaveTimer == 0)
            Leave();
    }

    private bool IsBeingTalkedTo()
    {
        foreach (var player in Main.ActivePlayers)
        {
            if (player.talkNPC == NPC.whoAmI)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Removes the trainer from the world. Called on the server (or in singleplayer).
    /// </summary>
    private void Leave()
    {
        NPC.active = false;
        NPC.life = 0;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
    }

    public override bool CanChat() => true;

    public override string GetChat()
    {
        var player = Main.LocalPlayer;
        if (!CanChallenge(player))
            return Dialogue("Locked");
        return player.GetModPlayer<GymPlayer>().Badges.Contains(Name)
            ? Dialogue("Rematch")
            : Dialogue("Greeting");
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = Language.GetTextValue("Mods.Terramon.NPCs.Trainers.BattleButton");
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
            TryStartBattle(Main.LocalPlayer);
    }

    private void TryStartBattle(Player player)
    {
        var modPlayer = player.Terramon();

        if (!CanChallenge(player))
        {
            Main.npcChatText = Dialogue("Locked");
            return;
        }

        if (!modPlayer.HasChosenStarter)
        {
            Main.npcChatText = Language.GetTextValue("Mods.Terramon.Misc.RequireStarter");
            return;
        }

        if (modPlayer.GetActivePokemon() == null)
        {
            Main.npcChatText = Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon");
            return;
        }

        if (BattleClient.Foe != null)
        {
            Main.npcChatText = Language.GetTextValue("Mods.Terramon.NPCs.Trainers.Busy", DisplayName.Value);
            return;
        }

        if (modPlayer.BattleClient is null || modPlayer.BattleClient.Foe != null)
            return;

        // Close the chat window before the battle starts
        player.SetTalkNPC(-1);
        Main.npcChatText = string.Empty;

        SoundEngine.PlaySound(in SoundID.MenuTick);
        modPlayer.StartBattle(this);
    }

    /// <summary>
    ///     Called on the winning player's client after they defeat this trainer.
    /// </summary>
    public void OnDefeatedBy(TerramonPlayer winner)
    {
        var player = winner.Player;

        // Rewards are only handed out by the winner's own game, so nothing is duplicated in multiplayer
        if (Main.netMode == NetmodeID.Server || player.whoAmI != Main.myPlayer)
            return;

        _defeatedByLocalPlayer = true;

        var firstWin = player.GetModPlayer<GymPlayer>().Badges.Add(Name);
        var source = player.GetSource_GiftOrReward();

        if (firstWin)
        {
            Main.NewText($"{DisplayName.Value}: {Dialogue("Defeated")}", Color.Gold);
            if (RewardItemType > 0)
                player.QuickSpawnItem(source, RewardItemType);
            foreach (var (type, stack) in FirstWinRewards(player))
            {
                if (type > 0 && stack > 0)
                    player.QuickSpawnItem(source, type, stack);
            }
        }

        // Prize money for every win, doubled the first time
        long money = (long)HighestLevel * HighestLevel * 20;
        if (firstWin)
            money *= 2;
        League.GiveMoney(player, money);
        Main.NewText(Language.GetTextValue("Mods.Terramon.NPCs.Trainers.PrizeMoney", League.FormatMoney(money)),
            Color.LightGoldenrodYellow);

        OnWin(player, firstWin);
    }

    /// <summary>
    ///     Called on the local player's client after they beat this trainer (after the rewards were given).
    /// </summary>
    protected virtual void OnWin(Player player, bool firstWin)
    {
    }

    /// <summary>
    ///     Called on the local player's client when their battle against this trainer ends without a win
    ///     (they lost, forfeited or it was a tie).
    /// </summary>
    protected virtual void OnLoss(Player player)
    {
    }

    private PokemonData[] BuildTeam()
    {
        var (min, max) = IVRange;
        return TeamSpec
            .Take(6)
            .Select(s =>
            {
                var data = PokemonData.Create(s.ID, s.Level).Build();
                data.IVs = new PokemonIVs(RollIV(min, max), RollIV(min, max), RollIV(min, max),
                    RollIV(min, max), RollIV(min, max), RollIV(min, max));
                data.HP = data.MaxHP; // IVs changed the max HP
                return data;
            })
            .ToArray();
    }

    private static byte RollIV(int min, int max) => (byte)Math.Clamp(Main.rand.Next(min, max + 1), 0, 31);

    public void AutoBattleChoice()
    {
        // TODO: Smarter move selection (type matchups, switching out)
        BattleClient.MakeChoice(BattleChoice.Default);
    }

    #region IBattleProvider

    public BattleProviderType ProviderType => BattleProviderType.TrainerNPC;

    public BattleClient BattleClient { get; private set; }

    public Entity SyncedEntity => NPC;

    public string BattleName => DisplayName.Value;

    public PokemonData[] GetBattleTeam()
    {
        if (_team.Length == 0)
            _team = BuildTeam();
        return _team;
    }

    public void StartBattleEffects(bool before)
    {
        // Turn towards the challenger
        NPC.spriteDirection = NPC.direction = BattleClient.Foe.SyncedEntity.position.X > NPC.position.X ? 1 : -1;
        _defeatedByLocalPlayer = false;
    }

    public void StopBattleEffects()
    {
        // Heal up for the next challenger
        _team = [];
        _lonelyTimer = 0;
        _leaveTimer = LeaveDelayAfterBattle;

        // Runs on every machine before the foe is cleared; only the challenger's own game reacts to a loss
        if (Main.netMode != NetmodeID.Server && BattleClient?.Foe is TerramonPlayer foe &&
            foe.Player.whoAmI == Main.myPlayer)
        {
            if (!_defeatedByLocalPlayer)
                OnLoss(foe.Player);
            _defeatedByLocalPlayer = false;
        }
    }

    public void SetActiveSlot(byte newSlot)
    {
        var team = GetBattleTeam();
        if (BattleClient.LocalClient.Foe == this && newSlot < team.Length)
            TestBattleUI.FoePanel.CurrentMon = team[newSlot];
    }

    public void Reply(BattleMessage m)
    {
        switch (m)
        {
            case ChallengeQuestion:

                // Trainers always accept a challenge
                m.Return(new ChallengeAnswer(yes: true));
                break;
            case ChallengeAnswer c:

                if (c.Yes)
                    m.Return(new SlotChoice(slot: 1));

                break;
            case SlotChoice:
                var pick = new SlotChoice(slot: 1)
                {
                    Sender = this
                };
                pick.Send();
                break;
            case TeamQuestion:
                m.Return(new TeamAnswer(this.GetNetTeam()));
                break;
        }
    }

    public void Witness(BattleMessage message)
    {
    }

    #endregion
}
