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
    ///     The team used in the current battle. Built fresh (at full HP) for every battle.
    /// </summary>
    private PokemonData[] _team = [];

    /// <summary>
    ///     The species and level of each Pokémon this trainer battles with, in order (up to 6).
    /// </summary>
    protected abstract (ushort ID, byte Level)[] TeamSpec { get; }

    /// <summary>
    ///     The item given to the player the first time they defeat this trainer, or 0 for none.
    /// </summary>
    protected virtual int RewardItemType => 0;

    // Placeholder sprite until trainers get their own art
    public override string Texture => $"Terraria/Images/NPC_{NPCID.Guide}";

    /// <summary>
    ///     Whether the player is allowed to challenge this trainer, e.g. only after a certain boss is defeated.
    /// </summary>
    protected virtual bool CanChallenge(Player player) => true;

    private string Dialogue(string key) => Language.GetTextValue(this.GetLocalizationKey("Dialogue." + key));

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Guide];
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
        AnimationType = NPCID.Guide;

        BattleClient = new BattleClient(this);
    }

    // Trainers never despawn on their own
    public override bool CheckActive() => false;

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
        if (!player.GetModPlayer<GymPlayer>().Badges.Add(Name))
            return; // Already defeated before, so no reward this time

        Main.NewText(Dialogue("Defeated"), Color.Gold);
        if (RewardItemType > 0)
            player.QuickSpawnItem(player.GetSource_GiftOrReward(), RewardItemType);
    }

    private PokemonData[] BuildTeam()
    {
        return TeamSpec
            .Take(6)
            .Select(s => PokemonData.Create(s.ID, s.Level).Build())
            .ToArray();
    }

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
    }

    public void StopBattleEffects()
    {
        // Heal up for the next challenger
        _team = [];
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
