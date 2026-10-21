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
    ///     A texture of this trainer's own, used instead of <see cref="LookAlike" />. Either a single standing frame
    ///     or a vertical sheet of <see cref="CustomFrameCount" /> frames (see <see cref="CustomFrameCount" />).
    /// </summary>
    protected virtual string CustomTexture => null;

    /// <summary>
    ///     How many frames <see cref="CustomTexture" /> has, stacked vertically. Frame 0 is the standing (idle)
    ///     frame; when there is more than one, all of them together are the walk cycle.
    /// </summary>
    protected virtual int CustomFrameCount => 1;

    /// <summary>
    ///     Whether <see cref="CustomTexture" /> is drawn facing right (vanilla sprites face left).
    /// </summary>
    protected virtual bool CustomTextureFacesRight => false;

    /// <summary>
    ///     The whole-number scale <see cref="CustomTexture" /> is drawn at, so pixel art stays crisp.
    /// </summary>
    protected virtual int CustomTextureScale => 1;

    /// <summary>
    ///     How far (in tiles) the trainer strolls left and right of where it appeared while nobody is talking to or
    ///     battling it. 0 keeps it standing still.
    /// </summary>
    protected virtual int WanderRange => 0;

    /// <summary>
    ///     Walking speed (in pixels per tick) while strolling around.
    /// </summary>
    protected virtual float WanderSpeed => 0.75f;

    /// <summary>
    ///     A player closer than this (in pixels) makes a strolling trainer stop and look at them, so it's easy to talk
    ///     to. 6 tiles.
    /// </summary>
    private const float GreetDistance = 6 * 16f;

    // Wandering state, kept in NPC.ai so it is synced to every client:
    // ai[0] = ticks left standing before the next stroll (server only), ai[1] = X it is walking to (0 = standing),
    // ai[2] = X of its home spot (where it appeared)
    private ref float WanderWait => ref NPC.ai[0];
    private ref float WanderTargetX => ref NPC.ai[1];
    private ref float HomeX => ref NPC.ai[2];

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
        Main.npcFrameCount[Type] = CustomTexture == null ? Main.npcFrameCount[LookAlike] : CustomFrameCount;
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
        var inBattle = BattleClient?.Foe != null;
        var talkedTo = IsBeingTalkedTo();
        var server = Main.netMode != NetmodeID.MultiplayerClient;

        if (server && HomeX == 0f)
        {
            HomeX = NPC.Center.X;
            NPC.netUpdate = true;
        }

        var closest = Player.FindClosest(NPC.position, NPC.width, NPC.height);
        var closestPlayer = Main.player[closest];
        var greeting = closestPlayer.active && !closestPlayer.dead &&
                       closestPlayer.DistanceSQ(NPC.Center) < GreetDistance * GreetDistance;

        // Stroll around only while nobody needs the trainer
        if (WanderRange > 0 && !inBattle && !talkedTo && !greeting)
            Wander(server);
        else
        {
            NPC.velocity.X = 0f;
            if (server && WanderTargetX != 0f)
            {
                WanderTargetX = 0f;
                WanderWait = Main.rand.Next(60, 180);
                NPC.netUpdate = true;
            }
        }

        // Face the closest player while standing and waiting for a challenger
        if (!inBattle && WanderTargetX == 0f && closestPlayer.active)
            NPC.spriteDirection = NPC.direction = closestPlayer.Center.X > NPC.Center.X ? 1 : -1;

        // Only the server (or singleplayer) decides when the trainer leaves
        if (!server)
            return;

        if (inBattle || talkedTo)
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

    /// <summary>
    ///     Walks towards <see cref="WanderTargetX" />, or stands for a while and then picks a new spot near home.
    ///     Only the server (or singleplayer) picks spots; clients just follow the synced target.
    /// </summary>
    private void Wander(bool server)
    {
        if (WanderTargetX == 0f)
        {
            NPC.velocity.X = 0f;
            if (!server || --WanderWait > 0)
                return;

            // Pick a spot at least 2 tiles away, inside the home range
            var range = WanderRange * 16f;
            var target = HomeX + Main.rand.NextFloat(-range, range);
            if (Math.Abs(target - NPC.Center.X) < 32f)
                target = HomeX + (NPC.Center.X > HomeX ? -1 : 1) * Main.rand.NextFloat(range * 0.5f, range);
            WanderTargetX = target;
            NPC.netUpdate = true;
            return;
        }

        var dx = WanderTargetX - NPC.Center.X;
        var dir = dx > 0 ? 1 : -1;
        NPC.spriteDirection = NPC.direction = dir;

        // Stop when there, when bumping into a wall, or right before a ledge
        if (Math.Abs(dx) < 4f || NPC.collideX || (NPC.velocity.Y == 0f && !GroundAhead(dir)))
        {
            NPC.velocity.X = 0f;
            if (server)
            {
                WanderTargetX = 0f;
                WanderWait = Main.rand.Next(120, 360);
                NPC.netUpdate = true;
            }

            return;
        }

        NPC.velocity.X = dir * WanderSpeed;
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
            ref NPC.gfxOffY);
    }

    /// <summary>
    ///     Whether there is something to stand on just in front of the trainer's feet.
    /// </summary>
    private bool GroundAhead(int dir)
    {
        var x = (int)((NPC.Center.X + dir * (NPC.width / 2f + 6f)) / 16f);
        var feetY = (int)((NPC.Bottom.Y + 2f) / 16f);
        for (var y = feetY; y <= feetY + 1; y++)
        {
            if (!WorldGen.InWorld(x, y))
                return false;
            var tile = Main.tile[x, y];
            if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                return true;
        }

        return false;
    }

    public override void FindFrame(int frameHeight)
    {
        if (CustomTexture == null || CustomFrameCount <= 1)
            return;

        // Frame 0 while standing, the whole sheet as a walk cycle while moving
        if (NPC.velocity.X == 0f || NPC.velocity.Y != 0f)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y = 0;
            return;
        }

        NPC.frameCounter += Math.Abs(NPC.velocity.X);
        const double pixelsPerFrame = 5.0;
        var frame = (int)(NPC.frameCounter / pixelsPerFrame) % CustomFrameCount;
        NPC.frame.Y = frame * frameHeight;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (CustomTexture == null || (!CustomTextureFacesRight && CustomTextureScale == 1))
            return true;

        // Drawn by hand to flip right-facing art and keep the feet on the ground at any scale
        var texture = Terraria.GameContent.TextureAssets.Npc[Type].Value;
        var frameHeight = texture.Height / Math.Max(1, Main.npcFrameCount[Type]);
        var source = new Rectangle(0, NPC.frame.Y, texture.Width, frameHeight);
        var facingRight = NPC.spriteDirection == 1;
        var effects = facingRight == CustomTextureFacesRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
        spriteBatch.Draw(texture, feet.Floor(), source, NPC.GetAlpha(drawColor), 0f,
            new Vector2(texture.Width / 2f, frameHeight), CustomTextureScale, effects, 0f);
        return false;
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
