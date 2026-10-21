namespace Terramon.Content.Projectiles;

/// <summary>
///     A white glow with sparkles around a Pokémon that is evolving, so other players see it happen too. Spawned by
///     the owner's game when the evolution sequence starts (projectiles sync on their own); purely visual.
/// </summary>
public sealed class EvolutionGlow : ModProjectile
{
    private const int Duration = 150;

    public override string Texture => "Terramon/Assets/Misc/Evolution/Glow";

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Duration;
    }

    public override bool? CanDamage() => false;

    /// <summary>
    ///     0 to 1 to 0 over the projectile's life.
    /// </summary>
    private float Strength => MathF.Sin((1f - Projectile.timeLeft / (float)Duration) * MathHelper.Pi);

    public override void AI()
    {
        // Stay on the owner's Pokémon (or on the owner, if it isn't out)
        var owner = Main.player[Projectile.owner];
        var pet = owner.active ? owner.Terramon().ActivePetProjectile?.Projectile : null;
        Projectile.Center = pet is { active: true } ? pet.Center : owner.Center;
        Projectile.velocity = Vector2.Zero;

        var strength = Strength;
        Lighting.AddLight(Projectile.Center, 0.9f * strength, 0.95f * strength, strength);

        if (Main.dedServ || !Main.rand.NextBool(2))
            return;
        var offset = Main.rand.NextVector2Circular(36f, 36f);
        var dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.TreasureSparkle,
            new Vector2(0f, Main.rand.NextFloat(-1.5f, -0.3f)), Scale: Main.rand.NextFloat(0.7f, 1.2f));
        dust.noGravity = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
        var strength = Strength;
        var pulse = 1f + 0.1f * MathF.Sin(Projectile.timeLeft * 0.4f);

        // Alpha 0 makes the color add up like light
        var color = new Color(255, 255, 255, 0) * (0.8f * strength);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, 0f,
            texture.Size() / 2f, 1.1f * pulse, SpriteEffects.None);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color * 0.5f, 0f,
            texture.Size() / 2f, 0.55f * pulse, SpriteEffects.None);
        return false;
    }
}
