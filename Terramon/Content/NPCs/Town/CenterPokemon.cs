using Terramon.Content.WorldGeneration;
using Terramon.Core;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Town;

/// <summary>
///     A Pokémon helping Nurse Joy at the Pokémon Center. It walks around inside, can be talked to and can't be
///     battled or caught. Drawn with its Mystery Dungeon animations.
/// </summary>
public abstract class CenterPokemon : ModNPC
{
    private const float WalkSpeed = 0.7f;

    /// <summary>
    ///     The name of its animation folder (Assets/Pokemon/Anim/&lt;name&gt;).
    /// </summary>
    protected abstract string Identifier { get; }

    // Invisible hitbox; the animation is drawn by hand
    public override string Texture => "Terramon/Assets/Pokemon/Chansey_Mini";

    private ref float WanderTimer => ref NPC.ai[0];
    private ref float TargetX => ref NPC.ai[1];

    public override void SetStaticDefaults()
    {
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
    }

    public override void SetDefaults()
    {
        NPC.width = 24;
        NPC.height = 26;
        NPC.aiStyle = -1;
        NPC.lifeMax = 250;
        NPC.friendly = true;
        NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0f;
        NPC.npcSlots = 0f;
    }

    public override bool CheckActive() => false;

    public override bool NeedSaving() => false;

    public override void AI()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && PokemonCenterSystem.Exists)
        {
            var inside = PokemonCenterSystem.Interior;
            // Somewhere new to walk to every few seconds, without leaving the Center
            if (--WanderTimer <= 0f)
            {
                WanderTimer = Main.rand.Next(180, 420);
                TargetX = Main.rand.Next(inside.X + 2, inside.Right - 2) * 16f;
                NPC.netUpdate = true;
            }

            if (!inside.Contains(NPC.Center.ToTileCoordinates()))
            {
                NPC.Bottom = new Vector2(inside.Center.X * 16f, inside.Bottom * 16f);
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
            }
        }

        // Stops to look at a player who's talking to it
        var talkedTo = Main.player.Any(p => p.active && p.talkNPC == NPC.whoAmI);
        var dx = TargetX - NPC.Center.X;
        if (!talkedTo && TargetX > 0f && Math.Abs(dx) > 6f)
        {
            NPC.direction = NPC.spriteDirection = dx > 0 ? 1 : -1;
            NPC.velocity.X = NPC.direction * WalkSpeed;
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed,
                ref NPC.gfxOffY);
        }
        else
        {
            NPC.velocity.X = 0f;
            if (talkedTo)
            {
                var player = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                NPC.direction = NPC.spriteDirection = player.Center.X > NPC.Center.X ? 1 : -1;
            }
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var set = PokemonAnimations.Get(Identifier);
        if (set == null)
            return false;

        PokemonAnimations.GetFrame(set, this, false, Math.Abs(NPC.velocity.X) > 0.1f, false, false, false, false,
            out var texture, out var source, out var origin);
        // Mystery Dungeon sprites face left
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 1f);
        spriteBatch.Draw(texture, feet.Floor(), source, NPC.GetAlpha(drawColor), 0f, origin, set.WorldScale, effects,
            0f);
        return false;
    }

    public override bool CanChat() => true;

    public override string GetChat()
        => Language.GetTextValue($"Mods.Terramon.NPCs.{Name}.Chat{Main.rand.Next(1, 4)}", Main.LocalPlayer.name);
}

public class CenterChansey : CenterPokemon
{
    protected override string Identifier => "Chansey";
}

public class CenterBlissey : CenterPokemon
{
    protected override string Identifier => "Blissey";
}
