using Terramon.Content.NPCs;
using Terraria.Audio;
using Terraria.GameContent;

namespace Terramon.Content.Projectiles;

/// <summary>
///     Stone spikes that burst out of the ground under a target, used by wild Onix against Terraria's monsters.
///     The bottom center of the sprite sits on the ground point the projectile was spawned at.
/// </summary>
public sealed class OnixRockSpikes : ModProjectile
{
    private const int FrameCount = 8;
    private const int TicksPerFrame = 5;

    public override string Texture => "Terramon/Assets/Projectiles/OnixRockSpikes";

    public override void SetStaticDefaults() => Main.projFrames[Type] = FrameCount;

    public override void SetDefaults()
    {
        Projectile.width = 180;
        Projectile.height = 140;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = FrameCount * TicksPerFrame;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1; // Each monster is hit once
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        Projectile.frame = Math.Min((FrameCount * TicksPerFrame - Projectile.timeLeft) / TicksPerFrame,
            FrameCount - 1);

        if (Main.dedServ)
            return;

        var ground = Projectile.Bottom;
        switch (Projectile.frame)
        {
            case 0 when Projectile.timeLeft == FrameCount * TicksPerFrame:
                // Warning: the ground cracks and glows first
                SoundEngine.PlaySound(SoundID.Item69 with { Pitch = -0.3f }, ground);
                break;
            case 2 when Projectile.timeLeft % TicksPerFrame == 0:
                SoundEngine.PlaySound(SoundID.Item70, ground);
                for (var i = 0; i < 20; i++)
                    Dust.NewDust(ground - new Vector2(80, 16), 160, 16, DustID.Stone,
                        Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-6f, -2f));
                break;
        }

        if (Projectile.frame < 4)
            Lighting.AddLight(ground, 0.9f, 0.6f, 0.15f);
    }

    // Only the frames where the spikes are out hurt
    public override bool? CanDamage() => Projectile.frame is >= 2 and <= 5;

    public override bool? CanHitNPC(NPC target) =>
        target.friendly || target.townNPC || target.ModNPC is PokemonNPC ? false : null;

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = TextureAssets.Projectile[Type].Value;
        var frameHeight = texture.Height / FrameCount;
        var source = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
        var position = Projectile.Bottom - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
        var origin = new Vector2(texture.Width / 2f, frameHeight);
        Main.EntitySpriteDraw(texture, position, source, Color.Lerp(lightColor, Color.White, 0.4f), 0f, origin, 1f,
            SpriteEffects.None);
        return false;
    }
}
