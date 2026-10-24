using ReLogic.Content;
using Terramon.Content.Dusts;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;

namespace Terramon.Content.Projectiles;

/// <summary>
///     The flashes of a Pokémon going in and out of its ball, on every client: the white burst when the ball pops open
///     (<see cref="PopOpen" />) and the red beam pulling the Pokémon back in when it's recalled (<see cref="AddRecall" />),
///     which keeps playing after the pet itself is gone. Purely visual.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class PokeBallVisuals : ModSystem
{
    private const int FlashTime = 14;
    private const int RecallTime = 18;

    private static readonly Color RecallRed = new(255, 70, 70);

    private static readonly List<Flash> Flashes = [];
    private static readonly List<Recall> Recalls = [];
    private static Asset<Texture2D> _glow;

    private sealed class Flash
    {
        public Vector2 Position;
        public int Age;
    }

    private sealed class Recall
    {
        public int Owner;
        public Texture2D Texture;
        public Rectangle Source;
        public Vector2 Origin;
        public float Scale;
        public SpriteEffects Effects;
        public Vector2 Feet;
        public int Age;
    }

    public override void Load()
    {
        _glow = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Glow");
    }

    public override void Unload()
    {
        _glow = null;
    }

    public override void ClearWorld()
    {
        Flashes.Clear();
        Recalls.Clear();
    }

    /// <summary>
    ///     The ball popping open at <paramref name="position" />: a white and red burst with the release sound.
    /// </summary>
    public static void PopOpen(Vector2 position, BallID ball)
    {
        if (Main.dedServ)
            return;
        SoundEngine.PlaySound(TerramonSoundID.PkmnRecall, position);
        Flashes.Add(new Flash { Position = position });
        Lighting.AddLight(position, 1f, 1f, 1f);

        for (var i = 0; i < 18; i++)
        {
            var velocity = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * Main.rand.NextFloat(2f, 3.5f);
            var dust = Dust.NewDustPerfect(position, DustID.RainbowMk2, velocity, 0,
                i % 3 == 0 ? RecallRed : Color.White, 1.3f);
            dust.noGravity = true;
        }

        var cloud = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 6; i++)
            Dust.NewDust(position - new Vector2(12f), 24, 24, cloud);
    }

    /// <summary>
    ///     A recalled Pokémon turning red and shrinking into a beam back to its trainer, from the last frame its pet
    ///     drew (<paramref name="feet" /> in world coordinates).
    /// </summary>
    public static void AddRecall(int owner, Texture2D texture, Rectangle source, Vector2 origin, float scale,
        SpriteEffects effects, Vector2 feet, bool playSound)
    {
        if (Main.dedServ || texture == null || Recalls.Count > 20)
            return;
        Recalls.Add(new Recall
        {
            Owner = owner, Texture = texture, Source = source, Origin = origin, Scale = scale, Effects = effects,
            Feet = feet
        });

        // The trainer who recalled it already heard it from the party sidebar
        if (playSound)
            SoundEngine.PlaySound(TerramonSoundID.PkballConsume, feet);
        for (var i = 0; i < 10; i++)
        {
            var dust = Dust.NewDustPerfect(feet - new Vector2(0f, source.Height * scale * 0.4f) +
                                           Main.rand.NextVector2Circular(14f, 14f), DustID.RainbowMk2,
                Main.rand.NextVector2Circular(1.5f, 1.5f), 0, RecallRed, 1.1f);
            dust.noGravity = true;
        }
    }

    public override void PostUpdateEverything()
    {
        foreach (var flash in Flashes)
            flash.Age++;
        Flashes.RemoveAll(flash => flash.Age >= FlashTime);

        foreach (var recall in Recalls)
        {
            recall.Age++;
            var owner = Main.player[recall.Owner];
            if (owner.active)
                Lighting.AddLight(recall.Feet, 0.6f, 0.15f, 0.15f);
        }

        Recalls.RemoveAll(recall => recall.Age >= RecallTime);
    }

    public override void PostDrawTiles()
    {
        if (Flashes.Count == 0 && Recalls.Count == 0)
            return;

        var spriteBatch = Main.spriteBatch;
        var transform = Main.GameViewMatrix.TransformationMatrix;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        foreach (var recall in Recalls)
        {
            // Turns red, then shrinks toward its middle as the beam pulls it in
            var t = recall.Age / (float)RecallTime;
            var shrink = 1f - t * t;
            var height = recall.Source.Height * recall.Scale;
            var center = recall.Feet - new Vector2(0f, height * 0.4f);
            var position = Vector2.Lerp(recall.Feet, center, t) - Main.screenPosition;
            PokemonPet.DrawSilhouette(recall.Texture, position, recall.Source, recall.Origin,
                recall.Scale * Math.Max(shrink, 0.02f), recall.Effects, RecallRed, Math.Min(1f, 1.5f - t), transform);
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        var pixel = TextureAssets.MagicPixel.Value;
        foreach (var recall in Recalls)
        {
            var owner = Main.player[recall.Owner];
            if (!owner.active)
                continue;
            var t = recall.Age / (float)RecallTime;
            var from = owner.MountedCenter + new Vector2(owner.direction * 8f, -6f);
            var to = recall.Feet - new Vector2(0f, recall.Source.Height * recall.Scale * 0.4f);
            var beam = to - from;
            var thickness = 4f * (1f - t);
            spriteBatch.Draw(pixel, from - Main.screenPosition, new Rectangle(0, 0, 1, 1), RecallRed * (1f - t),
                beam.ToRotation(), new Vector2(0f, 0.5f), new Vector2(beam.Length(), thickness), SpriteEffects.None,
                0f);
            spriteBatch.Draw(_glow.Value, to - Main.screenPosition, null, RecallRed * (0.6f * (1f - t)), 0f,
                _glow.Size() / 2f, 0.5f * (1f - t * 0.5f), SpriteEffects.None, 0f);
        }

        foreach (var flash in Flashes)
        {
            var t = flash.Age / (float)FlashTime;
            var size = 0.25f + 0.6f * MathF.Sqrt(t);
            spriteBatch.Draw(_glow.Value, flash.Position - Main.screenPosition, null, Color.White * (1f - t), 0f,
                _glow.Size() / 2f, size, SpriteEffects.None, 0f);
            spriteBatch.Draw(_glow.Value, flash.Position - Main.screenPosition, null, RecallRed * (0.5f * (1f - t)),
                0f, _glow.Size() / 2f, size * 0.55f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }
}
