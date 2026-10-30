using Terramon.Content.WorldGeneration;
using Terramon.Core.Systems;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Town;

/// <summary>
///     Nurse Joy, who stays at the Pokémon Center and heals the party for free. She looks like the Nurse until she
///     gets art of her own.
/// </summary>
public class NurseJoy : ModNPC
{
    private const int GreetingCount = 4;

    // Whether the local player's chat with her is on the "about the Center" page
    private static bool _aboutPage;

    public override string Texture => "Terraria/Images/NPC_" + NPCID.Nurse;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Nurse];
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
        Language.GetTextValue("Mods.Terramon.NPCs.NurseJoy.Dialogue." + key, args);

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

    public override void FindFrame(int frameHeight) => NPC.frame.Y = 0;

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Town NPC sheets are drawn 4 pixels below the feet, like vanilla does
        var texture = TextureAssets.Npc[Type].Value;
        var frameHeight = texture.Height / Main.npcFrameCount[Type];
        var source = new Rectangle(0, NPC.frame.Y, texture.Width, frameHeight);
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 4f);
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
        if (!player.Terramon().HasChosenStarter)
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
