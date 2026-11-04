using Pokerraria.Content.WorldGeneration;
using Pokerraria.Core.Systems;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace Pokerraria.Content.NPCs.Town;

/// <summary>
///     Nurse Joy, who stays at the Pokémon Center and heals the party for free.
/// </summary>
public class NurseJoy : ModNPC
{
    private const int GreetingCount = 4;

    // Whether the local player's chat with her is on the "about the Center" page
    private static bool _aboutPage;

    // Her own sheet, facing right, 41x54 per frame (about as tall as the player with hair, a little wider than the art): standing, greeting, holding a Poké Ball, four walking frames and
    // sitting (tools/sprites/cut_frames.py --dark-pupils --wide=1.15)
    private const int FrameStanding = 0;
    private const int FrameGreeting = 1;
    private const int FrameHoldingBall = 2;
    private const int FrameCount = 8;

    public override string Texture => "Pokerraria/Assets/NPCs/NurseJoy";

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = FrameCount;
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }

    public override void SetDefaults()
    {
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = -1;
        NPC.lifeMax = 250;
        NPC.friendly = true;
        NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0f;
        NPC.npcSlots = 0f;
    }

    // The Pokémon Center brings her back whenever someone is nearby
    public override bool CheckActive() => false;

    public override bool NeedSaving() => false;

    private static string Dialogue(string key, params object[] args) =>
        Language.GetTextValue("Mods.Pokerraria.NPCs.NurseJoy.Dialogue." + key, args);

    public override void AI()
    {
        NPC.velocity.X = 0f;

        // Stays behind her counter, and goes back there if something moved her
        if (PokemonCenterSystem.Exists && Main.netMode != NetmodeID.MultiplayerClient)
        {
            var spot = PokemonCenterSystem.JoySpot.ToWorldCoordinates(8f, 0f);
            if (NPC.Bottom.DistanceSQ(spot) > 16 * 16 * 4)
            {
                NPC.Bottom = spot;
                NPC.netUpdate = true;
            }
        }

        // A warm light over her spot, so the counter isn't lit only by the red glow of the wall behind it
        if (!Main.dedServ)
            Lighting.AddLight(NPC.Top + new Vector2(0f, -8f), 0.95f, 0.88f, 0.86f);

        // Looks at whoever is closest
        var player = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
        if (player.active)
            NPC.direction = NPC.spriteDirection = player.Center.X > NPC.Center.X ? 1 : -1;
    }

    // Bows while someone talks to her, and holds a Poké Ball while the machine heals
    public override void FindFrame(int frameHeight)
    {
        var frame = FrameStanding;
        if (HealingMachine.Busy)
            frame = FrameHoldingBall;
        else if (Main.LocalPlayer.talkNPC == NPC.whoAmI)
            frame = FrameGreeting;
        NPC.frame.Y = frame * frameHeight;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Feet on the bottom row of each frame; the art faces right
        var texture = TextureAssets.Npc[Type].Value;
        var frameHeight = texture.Height / Main.npcFrameCount[Type];
        var source = new Rectangle(0, NPC.frame.Y, texture.Width, frameHeight);
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
        spriteBatch.Draw(texture, feet.Floor(), source, NPC.GetAlpha(drawColor), 0f,
            new Vector2(texture.Width / 2f, frameHeight), 1f, effects, 0f);
        return false;
    }

    public override bool CanChat() => true;

    public override string GetChat()
    {
        _aboutPage = false;
        return Dialogue("Greeting" + Main.rand.Next(1, GreetingCount + 1), Main.LocalPlayer.name);
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = Dialogue("HealButton");
        button2 = Dialogue(_aboutPage ? "BackButton" : "AboutButton");
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (!firstButton)
        {
            _aboutPage = !_aboutPage;
            Main.npcChatText = _aboutPage
                ? Dialogue("About", LevelCap.Compute(Main.LocalPlayer))
                : Dialogue("Greeting1", Main.LocalPlayer.name);
            SoundEngine.PlaySound(in SoundID.MenuTick);
            return;
        }

        var player = Main.LocalPlayer;
        if (!player.Pokerraria().HasChosenStarter)
        {
            Main.npcChatText = Dialogue("NoPokemon");
            return;
        }

        if (HealingMachine.Busy)
        {
            Main.npcChatText = Dialogue("Healing");
            return;
        }

        var healing = player.GetModPlayer<PartyHealingPlayer>();
        if (!healing.CanHealParty)
        {
            Main.npcChatText = Dialogue(healing.PartyBattling ? "InBattle" : "AlreadyHealthy");
            return;
        }

        // The balls go on the machine and the party heals when they light up; she says "Healed" when it's done
        HealingMachine.Start(NPC);
        Main.npcChatText = Dialogue("Healing");
    }
}
