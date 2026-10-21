using ReLogic.Content;
using Terramon.Content.Projectiles;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

/// <summary>
///     The evolution sequence, like in the main games: the screen dims, the Pokémon turns into a white silhouette that
///     switches between its old and new forms faster and faster among light rays and sparkles, then a flash reveals
///     the new form.
///     <para />
///     Purely visual and client-side: the evolution itself has already happened (and is saved) when this starts, so
///     skipping it, closing the game or leaving the world halfway never loses anything. Players nearby see a smaller
///     glow around the Pokémon (<see cref="EvolutionGlow" />).
/// </summary>
public sealed class EvolutionScene : ModSystem
{
    // Timeline, in ticks
    private const int WhitenStart = 30; // Fully dimmed; the Pokémon starts turning white
    private const int SwapStart = 90; // White silhouette; old and new forms start switching
    private const int FlashStart = 330; // Switching is at its fastest; the screen flashes white
    private const int RevealStart = 350; // The new form appears in color
    private const int FadeOutStart = 560;
    private const int End = 600;

    private const float PokemonScale = 2f;

    private static readonly Queue<Evolution> Pending = new();
    private static readonly List<Sparkle> Sparkles = [];

    private static Evolution _current;
    private static int _time;
    private static bool _lastShowedNew;

    private static Asset<Texture2D> _glow, _ray, _sparkle;

    /// <summary>
    ///     Whether an evolution sequence is on screen (or waiting to be). New moves wait until it's over.
    /// </summary>
    public static bool IsPlaying => _current != null || Pending.Count > 0;

    public override void Load()
    {
        if (Main.dedServ)
            return;
        _glow = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Glow");
        _ray = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Ray");
        _sparkle = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Sparkle");
        PokemonData.Evolved += OnEvolved;
    }

    public override void Unload()
    {
        PokemonData.Evolved -= OnEvolved;
        _glow = _ray = _sparkle = null;
        Clear();
    }

    public override void ClearWorld() => Clear();

    private static void Clear()
    {
        Pending.Clear();
        Sparkles.Clear();
        _current = null;
        _time = 0;
    }

    /// <summary>
    ///     Plays the sequence when one of the local player's party Pokémon evolves, wherever that happened
    ///     (level up, evolution items, the Poké Mart clerk...).
    /// </summary>
    private static void OnEvolved(PokemonData data, ushort oldID)
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer == null || !Main.LocalPlayer.active)
            return;
        if (!TerramonPlayer.LocalPlayer.Party.Contains(data))
            return;

        var oldName = string.IsNullOrEmpty(data.Nickname)
            ? Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(oldID)
            : data.Nickname;
        Play(oldID, data.ID, data.IsShiny, oldName);
    }

    /// <summary>
    ///     Queues an evolution sequence from <paramref name="from" /> into <paramref name="to" />. Only visual.
    /// </summary>
    public static void Play(ushort from, ushort to, bool shiny, string oldName)
    {
        if (Main.dedServ)
            return;

        // A Pokémon evolving twice in a row (e.g. lots of Rare Candies) plays one sequence per stage
        Pending.Enqueue(new Evolution(from, to, shiny, oldName,
            Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(to)));
    }

    /// <summary>
    ///     Jumps to the reveal, or closes the sequence if the new form is already showing.
    /// </summary>
    public static void Skip()
    {
        if (_current == null)
            return;
        if (_time < RevealStart)
        {
            _time = RevealStart - 1;
            Sparkles.Clear();
            SoundEngine.PlaySound(SoundID.MenuClose);
        }
        else if (_time < FadeOutStart)
            _time = FadeOutStart;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.gameMenu)
        {
            Clear();
            return;
        }

        if (_current == null)
        {
            if (Pending.Count == 0)
                return;
            _current = Pending.Dequeue();
            _time = 0;
            _lastShowedNew = false;
            Sparkles.Clear();
            Start();
        }

        _time++;
        PlayCues();
        UpdateSparkles();

        if (_time >= End)
        {
            _current = null;
            Sparkles.Clear();
        }
    }

    private static void Start()
    {
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.4f });

        // A glow around the Pokémon in the world, for everyone nearby to see
        var player = Main.LocalPlayer;
        var pet = TerramonPlayer.LocalPlayer.ActivePetProjectile?.Projectile;
        var center = pet is { active: true } ? pet.Center : player.Center;
        Projectile.NewProjectile(player.GetSource_Misc("Evolution"), center, Vector2.Zero,
            ModContent.ProjectileType<EvolutionGlow>(), 0, 0f, player.whoAmI);
    }

    /// <summary>
    ///     Sounds at fixed points of the timeline, plus a rising chime every time the form switches.
    /// </summary>
    private static void PlayCues()
    {
        switch (_time)
        {
            case 15:
                SoundEngine.PlaySound(TerramonSoundID.GetCry(_current.From, 0.3f));
                break;
            case WhitenStart:
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f, Pitch = -0.5f });
                break;
            case FlashStart:
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.3f });
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.8f });
                break;
            case RevealStart + 10:
                SoundEngine.PlaySound(TerramonSoundID.GetCry(_current.To, 0.3f));
                break;
            case RevealStart + 40:
                TerramonWorld.PlaySoundOverBGM(TerramonSoundID.PkballCatchPla);
                break;
        }

        if (_time is < SwapStart or >= FlashStart)
            return;
        var showsNew = ShowsNewForm();
        if (showsNew != _lastShowedNew)
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.25f, Pitch = -0.4f + SwapProgress * 0.9f });
        _lastShowedNew = showsNew;
    }

    /// <summary>
    ///     0 to 1 through the switching part of the sequence.
    /// </summary>
    private static float SwapProgress => Math.Clamp((_time - SwapStart) / (float)(FlashStart - SwapStart), 0f, 1f);

    /// <summary>
    ///     Whether the silhouette shows the new form right now. Switches faster and faster (about 14 times in all).
    /// </summary>
    private static bool ShowsNewForm()
    {
        if (_time >= RevealStart)
            return true;
        if (_time < SwapStart)
            return false;
        var p = SwapProgress;
        var cycles = 2f * p + 12f * p * p;
        return MathF.Cos(cycles * MathHelper.TwoPi) < 0f;
    }

    private static Vector2 ScreenSize => new(Main.screenWidth / Main.UIScale, Main.screenHeight / Main.UIScale);

    private static Vector2 PokemonCenter => new(ScreenSize.X / 2f, ScreenSize.Y * 0.45f);

    private static void UpdateSparkles()
    {
        // More and more sparkles while switching, a burst at the reveal
        var spawn = 0;
        if (_time is >= WhitenStart and < FlashStart)
            spawn = Main.rand.NextFloat() < 0.15f + SwapProgress * 0.85f ? 1 + (int)(SwapProgress * 2f) : 0;
        else if (_time == RevealStart)
            spawn = 40;

        for (var i = 0; i < spawn; i++)
        {
            var burst = _time == RevealStart;
            var angle = Main.rand.NextFloat(MathHelper.TwoPi);
            var distance = burst ? Main.rand.NextFloat(10f, 40f) : Main.rand.NextFloat(40f, 160f);
            var position = PokemonCenter + angle.ToRotationVector2() * distance;
            var velocity = burst
                ? angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f)
                : new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.4f));
            Sparkles.Add(new Sparkle(position, velocity, Main.rand.Next(40, 80), Main.rand.NextFloat(0.4f, 1f)));
        }

        for (var i = Sparkles.Count - 1; i >= 0; i--)
        {
            var s = Sparkles[i];
            s.Position += s.Velocity;
            s.Velocity *= 0.97f;
            if (++s.Age >= s.Life)
                Sparkles.RemoveAt(i);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (index < 0)
            index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("Terramon: Evolution", () =>
        {
            if (_current != null && !Main.gameMenu)
                Draw(Main.spriteBatch);
            return true;
        }, InterfaceScaleType.UI));
    }

    private static void Draw(SpriteBatch spriteBatch)
    {
        var size = ScreenSize;
        var center = PokemonCenter;
        var feet = center + new Vector2(0f, 48f);
        var fadeOut = 1f - Math.Clamp((_time - FadeOutStart) / (float)(End - FadeOutStart), 0f, 1f);
        var dim = Math.Clamp(_time / (float)WhitenStart, 0f, 1f) * fadeOut;
        var p = SwapProgress;

        // Dim the screen
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)size.X + 1, (int)size.Y + 1),
            Color.Black * (0.8f * dim));

        // Light: a glow behind the Pokémon and rays that grow while it switches, bursting at the reveal
        var reveal = _time >= RevealStart ? 1f - Math.Clamp((_time - RevealStart) / 90f, 0f, 1f) : 0f;
        var light = _time < RevealStart ? Math.Clamp((_time - WhitenStart) / 60f, 0f, 1f) * (0.4f + 0.6f * p) : reveal;
        BeginAdditive(spriteBatch);
        var glow = _glow.Value;
        var glowScale = (1.4f + p * 1.6f + reveal * 2f) * (1f + 0.06f * MathF.Sin(_time * 0.3f));
        spriteBatch.Draw(glow, center, null, new Color(200, 230, 255) * (0.6f * light + 0.25f * dim * fadeOut),
            0f, glow.Size() / 2f, glowScale, SpriteEffects.None, 0f);

        var ray = _ray.Value;
        const int rays = 12;
        var rayLength = (0.3f + p * 1.2f + reveal * 1.5f) * (1f + 0.1f * MathF.Sin(_time * 0.2f));
        for (var i = 0; i < rays; i++)
        {
            var rotation = i * MathHelper.TwoPi / rays + _time * 0.01f;
            var width = i % 2 == 0 ? 1.2f : 0.7f;
            spriteBatch.Draw(ray, center, null, Color.White * (0.35f * light), rotation,
                new Vector2(ray.Width / 2f, ray.Height), new Vector2(width, rayLength), SpriteEffects.None, 0f);
        }

        var sparkle = _sparkle.Value;
        foreach (var s in Sparkles)
        {
            var life = s.Age / (float)s.Life;
            var alpha = MathF.Sin(life * MathHelper.Pi) * fadeOut;
            spriteBatch.Draw(sparkle, s.Position, null, Color.White * alpha, s.Age * 0.05f, sparkle.Size() / 2f,
                s.Scale * (0.6f + 0.4f * MathF.Sin(s.Age * 0.4f)), SpriteEffects.None, 0f);
        }

        // The Pokémon: in color, then fading to a white silhouette that switches forms, then the new form in color
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        var showsNew = ShowsNewForm();
        var id = showsNew ? _current.To : _current.From;
        float white;
        if (_time < RevealStart)
            white = Math.Clamp((_time - WhitenStart) / (float)(SwapStart - WhitenStart), 0f, 1f);
        else
            white = 1f - Math.Clamp((_time - RevealStart) / 30f, 0f, 1f);

        // A squash on every switch, so the change of shape reads clearly
        var scale = PokemonScale;
        if (_time is >= SwapStart and < RevealStart)
            scale *= 1f + 0.08f * MathF.Abs(MathF.Sin(_time * (0.15f + p * 0.6f)));

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        var color = Color.White * fadeOut;
        if (white < 1f)
            PokemonAnimations.DrawInUI(spriteBatch, id, _current.Shiny, feet, scale, color: color);
        if (white > 0f)
        {
            ShaderAssets.FadeToColor.UseColor(Color.White).UseOpacity(white * fadeOut).Apply();
            PokemonAnimations.DrawInUI(spriteBatch, id, _current.Shiny, feet, scale, color: color);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        // The flash
        var flash = _time switch
        {
            >= FlashStart and < RevealStart => (_time - FlashStart) / (float)(RevealStart - FlashStart),
            >= RevealStart and < RevealStart + 40 => 1f - (_time - RevealStart) / 40f,
            _ => 0f
        };
        if (flash > 0f)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)size.X + 1, (int)size.Y + 1),
                Color.White * flash);

        // Text: "What? X is evolving!", then "Congratulations! X evolved into Y!"
        var textPosition = new Vector2(size.X / 2f, center.Y + 120f);
        if (_time is >= WhitenStart and < RevealStart)
        {
            var alpha = Math.Clamp((_time - WhitenStart) / 20f, 0f, 1f);
            Utils.DrawBorderStringBig(spriteBatch,
                Language.GetTextValue("Mods.Terramon.EvolutionScene.Evolving", _current.OldName), textPosition,
                Color.White * alpha, 0.6f, 0.5f, 0.5f);
        }
        else if (_time >= RevealStart + 30)
        {
            var alpha = Math.Clamp((_time - RevealStart - 30) / 20f, 0f, 1f) * fadeOut;
            Utils.DrawBorderStringBig(spriteBatch,
                Language.GetTextValue("Mods.Terramon.NPCs.PokemartClerk.Dialogue.EvolutionCongrats",
                    _current.OldName, _current.NewName), textPosition, new Color(120, 255, 170) * alpha, 0.7f, 0.5f,
                0.5f);
        }

        // How to skip
        var key = KeybindSystem.SkipEvolutionKeybind?.GetAssignedKeys().FirstOrDefault();
        if (!string.IsNullOrEmpty(key) && _time < FadeOutStart)
            Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.Terramon.EvolutionScene.Skip", key),
                new Vector2(size.X / 2f, size.Y - 60f), Color.LightGray * (0.8f * dim), 0.9f, 0.5f, 0.5f);
    }

    private static void BeginAdditive(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
    }

    private sealed record Evolution(ushort From, ushort To, bool Shiny, string OldName, string NewName);

    private sealed class Sparkle(Vector2 position, Vector2 velocity, int life, float scale)
    {
        public int Age;
        public Vector2 Position = position;
        public Vector2 Velocity = velocity;
        public int Life { get; } = life;
        public float Scale { get; } = scale;
    }
}

/// <summary>
///     Skips the evolution sequence with its keybind.
/// </summary>
public sealed class EvolutionScenePlayer : ModPlayer
{
    public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
    {
        if (KeybindSystem.SkipEvolutionKeybind?.JustPressed == true)
            EvolutionScene.Skip();
    }
}
