using ReLogic.Content;
using ReLogic.Utilities;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;

namespace Terramon.Content.Projectiles;

/// <summary>
///     The evolution sequence, played in the world on the Pokémon itself, like in the main games: it turns into a white
///     silhouette that switches between its old and new forms faster and faster among light rays and sparkles, then a
///     flash reveals the new form in color, with its cry and the fanfare.
///     <para />
///     Spawned by the owner's game (<see cref="EvolutionQueue" />); projectiles sync on their own, so every player
///     nearby sees the same thing. While it plays on the owner's following Pokémon, that pet is hidden and held in
///     place (see <see cref="PokemonPet" />) and the owner can't be hurt (<see cref="EvolutionPlayer" />).
///     Purely visual: the evolution itself has already happened (and is saved) when this starts, so skipping it or
///     leaving the world halfway never loses anything.
/// </summary>
public sealed class EvolutionAnimation : ModProjectile
{
    /// <summary>Set in <c>ai[2]</c> when the Pokémon is shiny.</summary>
    internal const int FlagShiny = 1;

    /// <summary>Set in <c>ai[2]</c> when this plays on the owner's following Pokémon (and not next to the owner).</summary>
    internal const int FlagOnPet = 2;

    /// <summary>Set in <c>ai[2]</c> when the Pokémon faces right.</summary>
    internal const int FlagFlipped = 4;

    // Timeline, in ticks
    // Timed to the evolution theme (TerramonSoundID.EvolutionTheme, 120 BPM, one beat = 30 ticks)
    private const int WhitenStart = 60; // 1.0 s: after the intro's pause, the Pokémon starts turning white
    private const int SwapStart = 120; // 2.0 s: white silhouette; old and new forms switch on the beat
    private const int HalfBeatStart = 540; // 9.0 s: switches every half beat
    private const int QuarterBeatStart = 780; // 13.0 s: switches every quarter beat
    private const int FlashStart = 1050; // 17.5 s: the music swells; the light flashes
    private const int RevealStart = 1080; // 18.0 s: the music's peak; the new form appears in color
    private const int CongratsTime = 1200; // 20.0 s: the closing fanfare starts
    private const int FadeOutStart = 1290;
    private const int End = 1335; // 22.25 s: the music ends

    private static Asset<Texture2D> _ray, _sparkle;

    private readonly List<Sparkle> _sparkles = [];
    private bool _announced;
    private SlotId _theme;
    private float _height = 40f;
    private bool _lastShowedNew;
    private string _oldName = string.Empty;
    private float _scale = 1f;
    private int _time;

    public override string Texture => "Terramon/Assets/Misc/Evolution/Glow";

    private ushort From => (ushort)Projectile.ai[0];

    private ushort To => (ushort)Projectile.ai[1];

    private int Flags => (int)Projectile.ai[2];

    /// <summary>
    ///     Whether this plays on the owner's following Pokémon, which then stays hidden and still until it's over.
    /// </summary>
    public bool OnPet => (Flags & FlagOnPet) != 0;

    /// <summary>
    ///     Where the Pokémon stands, in world coordinates: the spot a following Pokémon is held at.
    /// </summary>
    public Vector2 Feet => Projectile.Center;

    /// <summary>
    ///     The middle of the Pokémon, where the light comes from.
    /// </summary>
    private Vector2 BodyCenter => Feet - new Vector2(0f, _height * 0.5f);

    public override void Load()
    {
        if (Main.dedServ)
            return;
        _ray = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Ray");
        _sparkle = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Sparkle");
    }

    public override void Unload()
    {
        _ray = _sparkle = null;
    }

    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    public override bool? CanDamage() => false;

    public override bool ShouldUpdatePosition() => false;

    /// <summary>
    ///     Sets what the other players can't work out on their own, right after the owner spawns this.
    /// </summary>
    internal void Setup(string oldName, float height, float scale)
    {
        _oldName = oldName ?? string.Empty;
        _height = height > 4f ? height : 40f;
        _scale = scale > 0f ? scale : 1f;
        Projectile.netUpdate = true;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_time);
        writer.Write(_height);
        writer.Write(_scale);
        writer.Write(_oldName ?? string.Empty);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var time = reader.ReadInt32();
        _height = reader.ReadSingle();
        _scale = reader.ReadSingle();
        _oldName = reader.ReadString();

        // The owner skipped ahead (or this player arrived halfway through)
        if (Math.Abs(time - _time) > 10)
        {
            _time = time;
            _sparkles.Clear();
        }
    }

    /// <summary>
    ///     Jumps to the reveal, or to the end if the new form is already showing. Owner only; synced to everyone else.
    /// </summary>
    internal void Skip()
    {
        if (Projectile.owner != Main.myPlayer)
            return;
        if (_time < RevealStart)
        {
            _time = RevealStart - 1;
            _sparkles.Clear();
            StopTheme();
            SoundEngine.PlaySound(SoundID.MenuClose);
        }
        else if (_time < FadeOutStart)
        {
            _time = FadeOutStart;
            StopTheme();
        }
        else
            return;

        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        var owner = Main.player[Projectile.owner];
        if (!owner.active || Terramon.DatabaseV2.GetPokemon(From) == null ||
            Terramon.DatabaseV2.GetPokemon(To) == null)
        {
            Projectile.Kill();
            return;
        }

        Projectile.velocity = Vector2.Zero;
        Projectile.timeLeft = 2;

        _time++;
        if (!Main.dedServ)
        {
            PlayCues();
            AddLight();
            UpdateSparkles();
        }

        if (_time >= End)
            Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        StopTheme();
        // Cut short (skipped past, or the owner left): the owner still gets told what it evolved into
        Announce();
    }

    /// <summary>
    ///     Tells the owner in chat what the Pokémon evolved into, once, when the new form is revealed.
    /// </summary>
    private void Announce()
    {
        if (_announced || Main.dedServ || Projectile.owner != Main.myPlayer)
            return;
        _announced = true;
        EvolutionQueue.AnnounceInChat(OldName, NewName);
    }

    private string OldName => string.IsNullOrEmpty(_oldName)
        ? Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(From)
        : _oldName;

    private string NewName => Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(To);

    /// <summary>
    ///     Sounds and floating text at fixed points of the timeline, plus a rising chime every time the form switches.
    ///     Sounds come from the Pokémon, so players nearby hear them too.
    /// </summary>
    private void PlayCues()
    {
        var isOwner = Projectile.owner == Main.myPlayer;
        switch (_time)
        {
            case 1:
                // The owner hears the theme over quieter music; players nearby hear it from the Pokémon
                _theme = isOwner
                    ? TerramonWorld.PlaySoundOverBGM(TerramonSoundID.EvolutionTheme, 0.15f)
                    : SoundEngine.PlaySound(TerramonSoundID.EvolutionTheme, Feet);
                break;
            case 10:
                SoundEngine.PlaySound(TerramonSoundID.GetCry(From, 0.25f), Feet);
                break;
            case WhitenStart:
                FloatingText(Language.GetTextValue("Mods.Terramon.EvolutionScene.Evolving", OldName), Color.White);
                break;
            case RevealStart:
                RevealBurst();
                break;
            case RevealStart + 20:
                SoundEngine.PlaySound(TerramonSoundID.GetCry(To, 0.3f), Feet);
                break;
            case CongratsTime:
                FloatingText(Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.EvolutionCongrats",
                    OldName, NewName), new Color(120, 255, 170));
                break;
        }

        // After a skip these may have been jumped over
        if (_time >= CongratsTime)
            Announce();

        if (_time is < SwapStart or >= FlashStart)
            return;
        var showsNew = ShowsNewForm();
        if (showsNew != _lastShowedNew)
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.12f, Pitch = -0.4f + SwapProgress * 0.9f }, Feet);
        _lastShowedNew = showsNew;
    }

    /// <summary>
    ///     Text rising from above the Pokémon (only seen by players nearby, like damage numbers).
    /// </summary>
    private void FloatingText(string text, Color color)
    {
        var top = Feet - new Vector2(0f, _height + 8f);
        CombatText.NewText(new Rectangle((int)top.X - 20, (int)top.Y - 10, 40, 10), color, text, true);
    }

    private void RevealBurst()
    {
        for (var i = 0; i < 24; i++)
        {
            var velocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
            var dust = Dust.NewDustPerfect(BodyCenter, DustID.RainbowMk2, velocity, 0, new Color(220, 240, 255),
                1.3f);
            dust.noGravity = true;
        }
    }

    /// <summary>
    ///     0 to 1 through the switching part of the sequence.
    /// </summary>
    private float SwapProgress => Math.Clamp((_time - SwapStart) / (float)(FlashStart - SwapStart), 0f, 1f);

    /// <summary>
    ///     0 to 1 to 0 around the reveal.
    /// </summary>
    private float Flash => _time switch
    {
        >= FlashStart and < RevealStart => (_time - FlashStart) / (float)(RevealStart - FlashStart),
        >= RevealStart and < RevealStart + 40 => 1f - (_time - RevealStart) / 40f,
        _ => 0f
    };

    /// <summary>
    ///     1, fading to 0 at the very end.
    /// </summary>
    private float FadeOut => 1f - Math.Clamp((_time - FadeOutStart) / (float)(End - FadeOutStart), 0f, 1f);

    /// <summary>
    ///     How strong the light around the Pokémon is: growing while it switches, bursting at the reveal.
    /// </summary>
    private float LightStrength
    {
        get
        {
            if (_time < RevealStart)
                return Math.Clamp(_time / (float)WhitenStart, 0f, 1f) * (0.4f + 0.6f * SwapProgress);
            return Math.Max(0.3f * FadeOut, 1f - Math.Clamp((_time - RevealStart) / 90f, 0f, 1f));
        }
    }

    /// <summary>
    ///     Whether the silhouette shows the new form right now. Switches on the music's beat, then every half and
    ///     quarter beat, then as fast as it can just before the flash.
    /// </summary>
    private bool ShowsNewForm()
    {
        if (_time >= RevealStart)
            return true;
        if (_time < SwapStart)
            return false;
        int switches;
        if (_time < HalfBeatStart)
            switches = (_time - SwapStart) / 30;
        else if (_time < QuarterBeatStart)
            switches = (HalfBeatStart - SwapStart) / 30 + (_time - HalfBeatStart) / 15;
        else if (_time < FlashStart)
            switches = (HalfBeatStart - SwapStart) / 30 + (QuarterBeatStart - HalfBeatStart) / 15 +
                       (_time - QuarterBeatStart) * 2 / 15;
        else
            switches = (HalfBeatStart - SwapStart) / 30 + (QuarterBeatStart - HalfBeatStart) / 15 +
                       (FlashStart - QuarterBeatStart) * 2 / 15 + (_time - FlashStart) / 4;
        return switches % 2 == 1;
    }

    private void StopTheme()
    {
        if (SoundEngine.TryGetActiveSound(_theme, out var sound))
            sound.Stop();
        _theme = SlotId.Invalid;
    }

    private void AddLight()
    {
        var strength = Math.Max(LightStrength, Flash) * 1.2f;
        Lighting.AddLight(BodyCenter, 0.85f * strength, 0.95f * strength, strength);
    }

    private void UpdateSparkles()
    {
        // More and more sparkles while switching, a burst at the reveal
        var spawn = 0;
        if (_time is >= WhitenStart and < FlashStart)
            spawn = Main.rand.NextFloat() < 0.15f + SwapProgress * 0.85f ? 1 + (int)(SwapProgress * 2f) : 0;
        else if (_time == RevealStart)
            spawn = 30;

        var spread = Math.Max(_height, 24f);
        for (var i = 0; i < spawn; i++)
        {
            var burst = _time == RevealStart;
            var angle = Main.rand.NextFloat(MathHelper.TwoPi);
            var distance = burst
                ? Main.rand.NextFloat(4f, 16f)
                : Main.rand.NextFloat(spread * 0.5f, spread * 1.6f);
            var velocity = burst
                ? angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 4f)
                : new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-1.2f, -0.3f));
            _sparkles.Add(new Sparkle(angle.ToRotationVector2() * distance, velocity, Main.rand.Next(40, 80),
                Main.rand.NextFloat(0.3f, 0.7f)));
        }

        for (var i = _sparkles.Count - 1; i >= 0; i--)
        {
            var s = _sparkles[i];
            s.Offset += s.Velocity;
            s.Velocity *= 0.97f;
            if (++s.Age >= s.Life)
                _sparkles.RemoveAt(i);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.dedServ || _ray?.Value == null || _sparkle?.Value == null)
            return false;

        var spriteBatch = Main.spriteBatch;
        var transform = Main.GameViewMatrix.TransformationMatrix;
        var center = BodyCenter - Main.screenPosition;
        var feet = Feet - Main.screenPosition;
        var fadeOut = FadeOut;
        var fadeIn = Math.Clamp(_time / (float)WhitenStart, 0f, 1f);
        var p = SwapProgress;
        var light = _time < RevealStart ? LightStrength : 1f - Math.Clamp((_time - RevealStart) / 90f, 0f, 1f);
        var size = Math.Max(_height, 24f) / 40f;

        // Light: a glow behind the Pokémon and rays that grow while it switches, bursting at the reveal
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        var glow = TextureAssets.Projectile[Type].Value;
        var reveal = _time >= RevealStart ? light : 0f;
        var glowScale = size * (0.7f + p * 0.8f + reveal * 1.2f) * (1f + 0.06f * MathF.Sin(_time * 0.3f));
        spriteBatch.Draw(glow, center, null, new Color(200, 230, 255) * (0.6f * light + 0.3f * fadeIn * fadeOut),
            0f, glow.Size() / 2f, glowScale, SpriteEffects.None, 0f);

        var ray = _ray.Value;
        const int rays = 12;
        var rayLength = size * (0.12f + p * 0.4f + reveal * 0.6f) * (1f + 0.1f * MathF.Sin(_time * 0.2f));
        for (var i = 0; i < rays; i++)
        {
            var rotation = i * MathHelper.TwoPi / rays + _time * 0.01f;
            var width = (i % 2 == 0 ? 0.6f : 0.35f) * size;
            spriteBatch.Draw(ray, center, null, Color.White * (0.35f * light), rotation,
                new Vector2(ray.Width / 2f, ray.Height), new Vector2(width, rayLength), SpriteEffects.None, 0f);
        }

        var sparkle = _sparkle.Value;
        foreach (var s in _sparkles)
        {
            var life = s.Age / (float)s.Life;
            var alpha = MathF.Sin(life * MathHelper.Pi) * fadeOut;
            spriteBatch.Draw(sparkle, center + s.Offset, null, Color.White * alpha, s.Age * 0.05f,
                sparkle.Size() / 2f, s.Scale * (0.6f + 0.4f * MathF.Sin(s.Age * 0.4f)), SpriteEffects.None, 0f);
        }

        // The Pokémon: in color, then fading to a white silhouette that switches forms, then the new form in color
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        var id = ShowsNewForm() ? To : From;
        var shiny = (Flags & FlagShiny) != 0;
        var flipped = (Flags & FlagFlipped) != 0;
        float white;
        if (_time < RevealStart)
            white = Math.Clamp((_time - WhitenStart) / (float)(SwapStart - WhitenStart), 0f, 1f);
        else
            white = 1f - Math.Clamp((_time - RevealStart) / 30f, 0f, 1f);

        // A squash on every switch, so the change of shape reads clearly
        var scale = _scale * PokemonAnimations.SizeOf(id);
        if (_time is >= SwapStart and < RevealStart)
            scale *= 1f + 0.08f * MathF.Abs(MathF.Sin(_time * (0.15f + p * 0.6f)));

        // Lit by the world like any Pokémon, but brighter while the light shines on it
        var color = Color.Lerp(lightColor, Color.White, Math.Clamp(light, 0f, 1f) * 0.7f);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        if (white < 1f)
            PokemonAnimations.DrawInUI(spriteBatch, id, shiny, feet, scale, facingRight: flipped, color: color);
        if (white > 0f)
        {
            ShaderAssets.FadeToColor.UseColor(Color.White).UseOpacity(white).Apply();
            PokemonAnimations.DrawInUI(spriteBatch, id, shiny, feet, scale, facingRight: flipped, color: color);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        // The flash: a burst of light from the Pokémon instead of the whole screen
        var flash = Flash;
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, transform);
        if (flash > 0f)
        {
            spriteBatch.Draw(glow, center, null, Color.White * flash, 0f, glow.Size() / 2f, size * 2.5f * flash,
                SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, center, null, Color.White * flash, 0f, glow.Size() / 2f, size * 1.2f,
                SpriteEffects.None, 0f);
        }

        // Hand the batch back the way projectiles are drawn
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);
        return false;
    }

    /// <summary>
    ///     The evolution playing on <paramref name="owner" />'s following Pokémon, or null.
    /// </summary>
    public static EvolutionAnimation FindOnPet(int owner)
    {
        foreach (var projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner == owner && projectile.ModProjectile is EvolutionAnimation { OnPet: true } animation)
                return animation;
        }

        return null;
    }

    /// <summary>
    ///     Whether one of <paramref name="owner" />'s Pokémon is evolving right now (or, for the local player, about to
    ///     play its evolution). Swaps its pet for the new form without going back into the ball, and keeps the trainer
    ///     from getting hurt.
    /// </summary>
    public static bool IsPlayingFor(int owner)
    {
        if (owner == Main.myPlayer && EvolutionQueue.IsPlaying)
            return true;
        foreach (var projectile in Main.ActiveProjectiles)
        {
            if (projectile.owner == owner && projectile.ModProjectile is EvolutionAnimation)
                return true;
        }

        return false;
    }

    private sealed class Sparkle(Vector2 offset, Vector2 velocity, int life, float scale)
    {
        public int Age;
        public Vector2 Offset = offset;
        public Vector2 Velocity = velocity;
        public int Life { get; } = life;
        public float Scale { get; } = scale;
    }
}

/// <summary>
///     Plays an evolution sequence (<see cref="EvolutionAnimation" />) whenever one of the local player's party Pokémon
///     evolves, wherever that happened (level up, evolution items, the Poké Mart clerk...), one after another when
///     several come at once. New moves wait until they're all over (<see cref="IsPlaying" />).
/// </summary>
public sealed class EvolutionQueue : ModSystem
{
    private static readonly Queue<Request> Pending = new();
    private static readonly List<Request> Recent = [];
    private static int _current = -1;
    private static bool _toldSkipKey;

    /// <summary>
    ///     Whether an evolution sequence of the local player is playing (or waiting to).
    /// </summary>
    public static bool IsPlaying => Pending.Count > 0 || Current != null;

    private static EvolutionAnimation Current
    {
        get
        {
            if (_current < 0 || _current >= Main.maxProjectiles)
                return null;
            var projectile = Main.projectile[_current];
            return projectile.active && projectile.owner == Main.myPlayer
                ? projectile.ModProjectile as EvolutionAnimation
                : null;
        }
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;
        PokemonData.Evolved += OnEvolved;
    }

    public override void Unload()
    {
        PokemonData.Evolved -= OnEvolved;
        Clear();
    }

    public override void ClearWorld() => Clear();

    private static void Clear()
    {
        Pending.Clear();
        Recent.Clear();
        _current = -1;
    }

    private static void OnEvolved(PokemonData data, ushort oldID)
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer is not { active: true } || data == null)
            return;

        // Rare Candies evolve a Pokémon, put it back and evolve it again: that's one evolution, not two
        var now = Main.GameUpdateCount;
        Recent.RemoveAll(r => now - r.Tick > 60);
        if (Recent.Any(r => r.Data == data && r.From == oldID && r.To == data.ID))
            return;

        var oldName = string.IsNullOrEmpty(data.Nickname)
            ? Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(oldID)
            : data.Nickname;
        var request = new Request(data, oldID, data.ID, data.IsShiny, oldName, now);
        Recent.Add(request);

        // Not one of the party (shouldn't happen): just say so
        if (!TerramonPlayer.LocalPlayer.Party.Contains(data))
        {
            AnnounceInChat(oldName, Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(data.ID));
            TerramonWorld.PlaySoundOverBGM(TerramonSoundID.PkballCatchPla);
            return;
        }

        Pending.Enqueue(request);
        if (Current == null)
            StartNext();
    }

    /// <summary>
    ///     "X evolved into Y!" in chat, for the local player.
    /// </summary>
    internal static void AnnounceInChat(string oldName, string newName)
    {
        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.PokemonEvolved", oldName, newName),
            new Color(50, 255, 130));
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;
        if (Main.gameMenu)
        {
            Clear();
            return;
        }

        if (Current == null && Pending.Count > 0)
            StartNext();
    }

    /// <summary>
    ///     Starts the next sequence: on the Pokémon itself when it's the one following the player, otherwise right
    ///     next to the player (without sending it out, which would change who's following them).
    /// </summary>
    private static void StartNext()
    {
        _current = -1;
        var player = Main.LocalPlayer;
        var modPlayer = TerramonPlayer.LocalPlayer;
        while (Pending.Count > 0)
        {
            var request = Pending.Dequeue();
            var pet = modPlayer.ActivePetProjectile;
            var onPet = pet?.Projectile is { active: true } petProjectile && petProjectile.owner == player.whoAmI &&
                        petProjectile.ModProjectile == pet && modPlayer.GetActivePokemon() == request.Data;

            Vector2 feet;
            var height = 40f;
            var scale = 1f;
            var flags = request.Shiny ? EvolutionAnimation.FlagShiny : 0;
            if (onPet)
            {
                feet = pet.Projectile.Bottom;
                height = pet.DrawnHeight;
                scale = pet.Projectile.scale;
                flags |= EvolutionAnimation.FlagOnPet;
                if (pet.DrawnFlipped)
                    flags |= EvolutionAnimation.FlagFlipped;
            }
            else
            {
                // In front of the player, facing them
                feet = player.Bottom + new Vector2(player.direction * 40f, 0f);
                if (player.direction == -1)
                    flags |= EvolutionAnimation.FlagFlipped;
            }

            var index = Projectile.NewProjectile(player.GetSource_Misc("Evolution"), feet, Vector2.Zero,
                ModContent.ProjectileType<EvolutionAnimation>(), 0, 0f, player.whoAmI, request.From, request.To,
                flags);
            if (index < 0 || index >= Main.maxProjectiles ||
                Main.projectile[index].ModProjectile is not EvolutionAnimation animation)
            {
                // No room for it: just say so
                AnnounceInChat(request.OldName, Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(request.To));
                continue;
            }

            animation.Setup(request.OldName, height, scale);
            _current = index;
            TellSkipKey();
            return;
        }
    }

    /// <summary>
    ///     Once per session, says how to skip the sequence.
    /// </summary>
    private static void TellSkipKey()
    {
        if (_toldSkipKey)
            return;
        var key = KeybindSystem.SkipEvolutionKeybind?.GetAssignedKeys().FirstOrDefault();
        if (string.IsNullOrEmpty(key))
            return;
        _toldSkipKey = true;
        Main.NewText(Language.GetTextValue("Mods.Terramon.EvolutionScene.Skip", key), new Color(159, 162, 173));
    }

    /// <summary>
    ///     Jumps the local player's sequence to the reveal, or ends it if the new form is already showing.
    /// </summary>
    public static void Skip() => Current?.Skip();

    private sealed record Request(PokemonData Data, ushort From, ushort To, bool Shiny, string OldName, uint Tick);
}

/// <summary>
///     Skips the evolution sequence with its keybind, and keeps a trainer whose Pokémon is evolving from getting hurt.
/// </summary>
public sealed class EvolutionPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (KeybindSystem.SkipEvolutionKeybind?.JustPressed == true)
            EvolutionQueue.Skip();
    }

    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) =>
        EvolutionAnimation.IsPlayingFor(Player.whoAmI);

    public override void PostUpdate()
    {
        if (!EvolutionAnimation.IsPlayingFor(Player.whoAmI))
            return;

        // Also counts as invincible for what checks that instead of trying to hurt (contact damage, knockback)
        Player.immune = true;
        Player.immuneNoBlink = true;
        Player.immuneTime = Math.Max(Player.immuneTime, 2);
    }
}
