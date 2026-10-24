using EasyPacketsLib;
using Terramon.Content.Configs;
using Terramon.Content.GUI;
using Terramon.Core.Systems;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Content.NPCs;

/// <summary>
///     Professor Oak. He comes to greet players who are new to Pokémon (no starter yet, never met him), tells them a
///     short story about this world and hands them over to the starter selection. Afterwards he can be talked to for
///     tips until he leaves.
/// </summary>
public class ProfessorOak : ModNPC
{
    /// <summary>
    ///     How far (in pixels) a player has to be for Oak to count as left alone. 100 tiles.
    /// </summary>
    private const float LonelyDistance = 100 * 16f;

    /// <summary>
    ///     Oak walks up to a new trainer until this close (in pixels). 3 tiles.
    /// </summary>
    private const float StopDistance = 3 * 16f;

    /// <summary>
    ///     He only follows new trainers who are closer than this (in pixels). 50 tiles.
    /// </summary>
    private const float FollowDistance = 50 * 16f;

    /// <summary>
    ///     How long (in ticks) he stays once nobody around him still needs a starter, or nobody is around at all.
    /// </summary>
    private const int StayTime = 3 * 60 * 60;

    private const float WalkSpeed = 1.1f;

    /// <summary>
    ///     Number of pages of the welcome story (Dialogue.Intro1 ... IntroN).
    /// </summary>
    private const int IntroPages = 6;

    /// <summary>
    ///     Number of tips (Dialogue.Tip1 ... TipN).
    /// </summary>
    private const int TipCount = 7;

    private enum ChatMode : byte
    {
        Intro,
        Choose,
        Tips
    }

    // The local player's conversation with Oak (each client has its own)
    private static ChatMode _mode;
    private static int _page;
    private static int _tip = -1;

    /// <summary>
    ///     Counts up while nobody around still needs a starter. Only used on the server (or in singleplayer).
    /// </summary>
    private int _idleTimer;

    public override string Texture => "Terramon/Assets/NPCs/ProfessorOak";

    public override void SetStaticDefaults()
    {
        // 8 walking frames of the custom art (facing left), frame 0 is the standing pose
        Main.npcFrameCount[Type] = 8;
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }

    public override void SetDefaults()
    {
        NPC.width = 18;
        NPC.height = 44;
        NPC.aiStyle = -1;
        NPC.lifeMax = 250;
        NPC.friendly = true;
        NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0f;
        NPC.HitSound = SoundID.NPCHit1;
    }

    // Vanilla despawning is replaced by the timer in AI
    public override bool CheckActive() => false;

    public override bool NeedSaving() => false;

    private static string Dialogue(string key, params object[] args) =>
        Language.GetTextValue("Mods.Terramon.NPCs.ProfessorOak.Dialogue." + key, args);

    /// <summary>
    ///     Whether this player still has to meet Oak and pick a starter.
    /// </summary>
    private static bool IsNewTrainer(Player player) => player.active && !player.dead &&
                                                      !player.Terramon().HasChosenStarter;

    public override void AI()
    {
        var server = Main.netMode != NetmodeID.MultiplayerClient;
        var talkedTo = Main.player.Any(p => p.active && p.talkNPC == NPC.whoAmI);

        // Walk up to the closest new trainer, otherwise stand and look at whoever is closest
        Player target = null;
        var closest = float.MaxValue;
        foreach (var player in Main.ActivePlayers)
        {
            if (!IsNewTrainer(player))
                continue;
            var d = player.DistanceSQ(NPC.Center);
            if (d < closest && d < FollowDistance * FollowDistance)
            {
                closest = d;
                target = player;
            }
        }

        var watched = target ?? Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
        var dx = watched.Center.X - NPC.Center.X;
        var walking = target != null && !talkedTo && Math.Abs(dx) > StopDistance;
        if (walking)
        {
            var dir = dx > 0 ? 1 : -1;
            NPC.direction = NPC.spriteDirection = dir;
            NPC.velocity.X = dir * WalkSpeed;
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                ref NPC.gfxOffY);
            // Hop over small walls
            if (NPC.collideX && NPC.velocity.Y == 0f)
                NPC.velocity.Y = -6f;
            if (server && Main.GameUpdateCount % 30 == 0)
                NPC.netUpdate = true;
        }
        else
        {
            if (server && NPC.velocity.X != 0f)
                NPC.netUpdate = true;
            NPC.velocity.X = 0f;
            if (watched.active)
                NPC.direction = NPC.spriteDirection = dx > 0 ? 1 : -1;
        }

        // Greets the local new trainer on his own the first time he reaches them
        if (!Main.dedServ)
            TryGreetLocalPlayer();

        if (!server)
            return;

        // Leaves once nobody around needs him anymore (he's still there for tips for a while)
        var needed = talkedTo || Main.player.Any(p => IsNewTrainer(p) &&
                                                      p.DistanceSQ(NPC.Center) < LonelyDistance * LonelyDistance);
        _idleTimer = needed ? 0 : _idleTimer + 1;
        if (_idleTimer >= StayTime)
            Leave();
    }

    private void TryGreetLocalPlayer()
    {
        var player = Main.LocalPlayer;
        var oak = player.GetModPlayer<OakPlayer>();
        if (oak.Greeted || oak.MetOak || !IsNewTrainer(player) || player.talkNPC != -1 || Main.playerInventory ||
            Main.inFancyUI || player.DistanceSQ(NPC.Center) > (StopDistance + 16f) * (StopDistance + 16f))
            return;

        oak.Greeted = true;
        player.SetTalkNPC(NPC.whoAmI);
        Main.npcChatCornerItem = 0;
        Main.npcChatText = GetChat();
        SoundEngine.PlaySound(in SoundID.Chat);
    }

    /// <summary>
    ///     Removes Oak from the world with a puff of smoke. Called on the server (or in singleplayer).
    /// </summary>
    private void Leave()
    {
        Smoke(NPC);
        NPC.active = false;
        NPC.life = 0;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
    }

    private static void Smoke(NPC npc)
    {
        if (Main.dedServ)
            return;
        for (var i = 0; i < 20; i++)
            Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, 0f, -1f, 100, default, 1.4f);
    }

    public override void FindFrame(int frameHeight)
    {
        // Frame 0 while standing, the whole sheet as a walk cycle while moving
        if (NPC.velocity.X == 0f || NPC.velocity.Y != 0f)
        {
            NPC.frameCounter = 0;
            NPC.frame.Y = 0;
            return;
        }

        NPC.frameCounter += Math.Abs(NPC.velocity.X);
        const double pixelsPerFrame = 5.0;
        NPC.frame.Y = (int)(NPC.frameCounter / pixelsPerFrame) % Main.npcFrameCount[Type] * frameHeight;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Drawn by hand so his feet are exactly on the ground; the art faces left
        var texture = Terraria.GameContent.TextureAssets.Npc[Type].Value;
        var frameHeight = texture.Height / Main.npcFrameCount[Type];
        var source = new Rectangle(0, NPC.frame.Y, texture.Width, frameHeight);
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
        spriteBatch.Draw(texture, feet.Floor(), source, NPC.GetAlpha(drawColor), 0f,
            new Vector2(texture.Width / 2f, frameHeight), 1f, effects, 0f);
        return false;
    }

    public override bool CanChat() => true;

    public override string GetChat()
    {
        var player = Main.LocalPlayer;
        if (!player.GetModPlayer<OakPlayer>().MetOak && IsNewTrainer(player))
        {
            _mode = ChatMode.Intro;
            _page = 0;
            return IntroPage();
        }

        if (IsNewTrainer(player))
        {
            _mode = ChatMode.Choose;
            return Dialogue("StillChoosing", player.name);
        }

        _mode = ChatMode.Tips;
        return NextTip();
    }

    private static string IntroPage()
    {
        // The last page counts as having met him (the story has been told)
        if (_page >= IntroPages - 1)
            Main.LocalPlayer.GetModPlayer<OakPlayer>().MetOak = true;
        return Dialogue("Intro" + (_page + 1), Main.LocalPlayer.name, KeyName(KeybindSystem.OpenSummaryKeybind),
            KeyName(KeybindSystem.HubKeybind));
    }

    private static string NextTip()
    {
        _tip = (_tip + 1 + Main.rand.Next(TipCount - 1)) % TipCount;
        return Dialogue("Tip" + (_tip + 1), KeyName(KeybindSystem.OpenSummaryKeybind),
            KeyName(KeybindSystem.HubKeybind));
    }

    private static string KeyName(ModKeybind keybind)
    {
        var keys = keybind?.GetAssignedKeys();
        return keys is { Count: > 0 } ? keys[0] : Dialogue("Unbound");
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        var chosen = Main.LocalPlayer.Terramon().HasChosenStarter;
        switch (_mode)
        {
            case ChatMode.Intro:
                button = _page < IntroPages - 1 ? Dialogue("NextButton")
                    : chosen ? Dialogue("ThanksButton") : Dialogue("ChooseButton");
                if (_page < IntroPages - 1)
                    button2 = Dialogue("SkipButton");
                break;
            case ChatMode.Choose:
                button = chosen ? Dialogue("ThanksButton") : Dialogue("ChooseButton");
                button2 = Dialogue("AgainButton");
                break;
            default:
                button = Dialogue("TipButton");
                button2 = Dialogue("AgainButton");
                break;
        }
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        switch (_mode)
        {
            case ChatMode.Intro when firstButton && _page < IntroPages - 1:
                _page++;
                Main.npcChatText = IntroPage();
                SoundEngine.PlaySound(in SoundID.MenuTick);
                break;
            case ChatMode.Intro when !firstButton:
                _page = IntroPages - 1;
                Main.npcChatText = IntroPage();
                break;
            case ChatMode.Intro:
            case ChatMode.Choose when firstButton:
                OpenStarterSelection();
                break;
            case ChatMode.Tips when firstButton:
                Main.npcChatText = NextTip();
                SoundEngine.PlaySound(in SoundID.MenuTick);
                break;
            default:
                // Tell the story again
                _mode = ChatMode.Intro;
                _page = 0;
                Main.npcChatText = IntroPage();
                break;
        }
    }

    /// <summary>
    ///     Closes the chat and shows the starter selection (if the local player still needs a starter).
    /// </summary>
    private static void OpenStarterSelection()
    {
        var player = Main.LocalPlayer;
        player.GetModPlayer<OakPlayer>().MetOak = true;
        player.SetTalkNPC(-1);
        Main.npcChatText = string.Empty;
        if (player.Terramon().HasChosenStarter)
            return;

        // The legacy selection screen opens by itself once the chat is closed
        if (!ClientConfig.Instance.LegacyStarterSelectUI)
            StarterSelectUI.Show();
    }

    /// <summary>
    ///     Brings Oak next to the given player, unless he's already near them. Can be called from any side: clients ask
    ///     the server to do it. <paramref name="test" /> (test panel, debug mode only) calls him even for players who
    ///     already have a starter.
    /// </summary>
    public static void RequestVisit(Player player, bool test = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            Terramon.Instance.SendPacket(new OakVisitRpc(test));
            return;
        }

        SpawnNear(player);
    }

    /// <summary>
    ///     Spawns Oak on the ground a few tiles from the player. Only runs on the server or in singleplayer.
    /// </summary>
    internal static void SpawnNear(Player player)
    {
        var type = ModContent.NPCType<ProfessorOak>();
        foreach (var npc in Main.ActiveNPCs)
        {
            // He's already around (he'll walk up to them)
            if (npc.type == type && npc.DistanceSQ(player.Center) < FollowDistance * FollowDistance)
                return;
        }

        // One Oak per new trainer at most, and never a crowd of them
        if (Main.npc.Count(n => n.active && n.type == type) >= 4)
            return;

        var spot = FindSpot(player);
        var index = NPC.NewNPC(player.GetSource_FromThis(), (int)spot.X, (int)spot.Y, type);
        if (index >= Main.maxNPCs)
            return;

        var oak = Main.npc[index];
        oak.direction = oak.spriteDirection = player.Center.X > oak.Center.X ? 1 : -1;
        Smoke(oak);
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);
    }

    /// <summary>
    ///     Feet position for Oak: standing ground about 6 tiles in front of the player (or behind them), or right next
    ///     to the player when there is none.
    /// </summary>
    private static Vector2 FindSpot(Player player)
    {
        var px = (int)(player.Center.X / 16f);
        var py = (int)(player.Bottom.Y / 16f);
        foreach (var offset in new[] { 6, 7, 5, 8, 4 })
        {
            foreach (var side in new[] { player.direction, -player.direction })
            {
                var x = px + side * offset;
                for (var y = py - 4; y <= py + 6; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10) || !Solid(x, y) || Solid(x, y - 1) || Solid(x, y - 2) ||
                        Solid(x, y - 3))
                        continue;
                    return new Vector2(x * 16f + 8f, y * 16f);
                }
            }
        }

        return player.Bottom;
    }

    private static bool Solid(int x, int y)
    {
        var tile = Main.tile[x, y];
        return tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
    }
}

/// <summary>
///     Remembers whether a player has met Professor Oak, and calls him the first time a new trainer enters a world.
/// </summary>
public class OakPlayer : ModPlayer
{
    /// <summary>
    ///     Ticks to wait after entering a world before calling Oak, so the world is loaded around the player.
    /// </summary>
    private const int VisitDelay = 3 * 60;

    private int _visitTimer = -1;

    /// <summary>
    ///     Whether this player heard Oak's story (saved with the player).
    /// </summary>
    public bool MetOak { get; set; }

    /// <summary>
    ///     Whether Oak already opened the chat by himself since this player entered the world.
    /// </summary>
    public bool Greeted { get; set; }

    public override void OnEnterWorld()
    {
        Greeted = false;
        _visitTimer = !MetOak && !Player.Terramon().HasChosenStarter ? VisitDelay : -1;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || _visitTimer < 0 || --_visitTimer > 0)
            return;

        _visitTimer = -1;
        if (!MetOak && !Player.dead && !Player.Terramon().HasChosenStarter)
            ProfessorOak.RequestVisit(Player);
    }

    public override void SaveData(TagCompound tag)
    {
        if (MetOak)
            tag["metOak"] = true;
    }

    public override void LoadData(TagCompound tag)
    {
        MetOak = tag.GetBool("metOak");
    }
}

/// <summary>
///     Sent by a new trainer's client to ask the server to bring Professor Oak next to them.
/// </summary>
public struct OakVisitRpc(bool test) : IEasyPacket
{
    private bool _test = test;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_test);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _test = reader.ReadBoolean();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        if (Main.netMode != NetmodeID.Server)
            return;

        // Only next to the player who asked, and only while they have no starter (or for testing in debug mode)
        var player = Main.player[sender.WhoAmI];
        if (player?.active == true &&
            (!player.Terramon().HasChosenStarter || (_test && GameplayConfig.Instance.DebugMode)))
            ProfessorOak.SpawnNear(player);
    }
}
