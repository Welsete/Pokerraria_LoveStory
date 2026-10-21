using System.Text;
using ReLogic.Content;
using Terramon.Core.Loaders;
using Terramon.ID;

namespace Terramon.Core;

/// <summary>
///     What a Pokémon is doing, for Pokémon drawn with a full animation set.
/// </summary>
public enum PokemonAnimState : byte
{
    Idle,
    Walk,
    Attack,
    SpAttack,
    Status,
    Hurt,
    Sleep,
    Faint,
    Strike,
    QuickStrike,
    Swing,
    Double,
    Hop,
    Sit,
    Laying,
    Wake,
    Trip,
    Eat,
    Pose
}

/// <summary>
///     Full animation sets in the Mystery Dungeon style (idle, walk, attacks, hurt, sleep, faint), for Pokémon that have
///     a folder in <c>Assets/Pokemon/Anim/&lt;Name&gt;</c>. Each animation is one strip of frames facing left, and
///     <c>Anim.txt</c> says the frame size and how many ticks each frame lasts.
/// </summary>
public sealed class PokemonAnimations : ModSystem
{
    private static readonly Dictionary<string, AnimSet> Sets = [];
    private static readonly Dictionary<object, (PokemonAnimState State, uint Start)> OneShots = [];
    private static readonly Dictionary<object, uint> IdleSince = [];
    private static readonly HashSet<object> Asleep = [];

    // How long a Pokémon stands around before it sits, then lies down
    private const uint SitAfter = 60 * 10;
    private const uint LayAfter = 60 * 30;

    // Moves that go first, slash, or hit more than once get their own physical attack animation
    private static readonly HashSet<string> QuickMoves =
    [
        "QuickAttack", "ExtremeSpeed", "MachPunch", "BulletPunch", "AquaJet", "IceShard", "ShadowSneak",
        "SuckerPunch", "VacuumWave", "Feint", "AccelRock", "FakeOut", "FirstImpression", "JetPunch"
    ];

    private static readonly string[] SwingWords =
        ["Slash", "Cut", "Claw", "Scratch", "Blade", "Scissor", "Tail", "Bone", "Whip", "Sword", "Scythe", "Cutter"];

    private static readonly string[] StrikeWords =
        ["Punch", "Kick", "Chop", "Palm", "Smash", "Mash", "Combat", "Fist", "Hammer", "Knee", "Jab", "Uppercut"];

    private static readonly string[] DoubleWords = ["Double", "Triple", "Fury", "Rush", "Barrage", "Twineedle"];

    public sealed class Anim
    {
        public Asset<Texture2D> Texture;
        public Asset<Texture2D> ShinyTexture;

        /// <summary>Shiny version made from <see cref="Texture" /> with <see cref="AnimSet.ShinyColors" />.</summary>
        public Texture2D Recolored;

        public int FrameWidth;
        public int FrameHeight;
        public int[] Durations;
        public int Total;
    }

    public sealed class AnimSet
    {
        public readonly Dictionary<PokemonAnimState, Anim> Anims = [];

        /// <summary>How far below the middle of a frame the feet are.</summary>
        public int Ground;

        /// <summary>How much bigger to draw it (Mystery Dungeon sprites are small next to Terraria's).</summary>
        public float Scale = 1f;

        /// <summary>
        ///     For floating Pokémon: how far above <see cref="Ground" /> its lowest pixel stays. Pokémon that fly around
        ///     (and have no ground under them) are drawn from there instead.
        /// </summary>
        public int Float;

        /// <summary>
        ///     Shiny colors as a palette swap (packed RGB, normal to shiny), for sets without <c>_S</c> strips.
        /// </summary>
        public Dictionary<uint, uint> ShinyColors;
    }

    /// <summary>
    ///     The animation set of a Pokémon, or null if it uses a normal sprite sheet.
    /// </summary>
    public static AnimSet Get(string identifier)
    {
        if (Main.dedServ || identifier == null)
            return null;
        if (Sets.TryGetValue(identifier, out var set))
            return set;

        var folder = $"Assets/Pokemon/Anim/{identifier}/";
        var mod = Terramon.Instance;
        if (mod.FileExists(folder + "Anim.txt"))
        {
            set = new AnimSet();
            foreach (var line in Encoding.UTF8.GetString(mod.GetFileBytes(folder + "Anim.txt")).Split('\n'))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0] == "Ground")
                    set.Ground = int.Parse(parts[1]);
                if (parts.Length == 2 && parts[0] == "Scale")
                    set.Scale = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                if (parts.Length == 2 && parts[0] == "Float")
                    set.Float = int.Parse(parts[1]);
                if (parts.Length > 1 && parts[0] == "Shiny")
                    set.ShinyColors = ParseColors(parts.Skip(1));
                if (parts.Length != 4 || !Enum.TryParse(parts[0], out PokemonAnimState state))
                    continue;
                var durations = parts[3].Split(',').Select(int.Parse).ToArray();
                set.Anims[state] = new Anim
                {
                    Texture = mod.Assets.Request<Texture2D>(folder + parts[0]),
                    ShinyTexture = ModContent.HasAsset($"Terramon/{folder}{parts[0]}_S")
                        ? mod.Assets.Request<Texture2D>(folder + parts[0] + "_S")
                        : null,
                    FrameWidth = int.Parse(parts[1]),
                    FrameHeight = int.Parse(parts[2]),
                    Durations = durations,
                    Total = durations.Sum()
                };
            }

            if (!set.Anims.ContainsKey(PokemonAnimState.Idle))
                set = null;
        }

        Sets[identifier] = set;
        return set;
    }

    /// <summary>
    ///     The animation set of a Pokémon as it looks: its female form has its own set when there is one
    ///     (<c>Anim/&lt;Name&gt;F</c>).
    /// </summary>
    public static AnimSet Get(string identifier, PokemonData data)
    {
        if (data is { Gender: Gender.Female } && identifier != null && Get(identifier + "F") is { } female)
            return female;
        return Get(identifier);
    }

    /// <summary>Reads <c>RRGGBB&gt;RRGGBB</c> pairs into packed colors (the way <see cref="Color.PackedValue" /> has them).</summary>
    private static Dictionary<uint, uint> ParseColors(IEnumerable<string> pairs)
    {
        static uint Pack(string hex)
        {
            var rgb = Convert.ToUInt32(hex, 16);
            return ((rgb >> 16) & 0xFF) | (rgb & 0xFF00) | ((rgb & 0xFF) << 16);
        }

        var colors = new Dictionary<uint, uint>();
        foreach (var pair in pairs)
        {
            var sides = pair.Split('>');
            if (sides.Length == 2 && sides[0].Length == 6 && sides[1].Length == 6)
                colors[Pack(sides[0])] = Pack(sides[1]);
        }

        return colors;
    }

    /// <summary>
    ///     The texture to draw. Shiny sets either have their own <c>_S</c> strips or get their normal strips recolored
    ///     here, the first time they're drawn (always on the main thread, since it's while drawing).
    /// </summary>
    private static Texture2D TextureOf(AnimSet set, Anim anim, bool shiny)
    {
        if (!shiny)
            return anim.Texture.Value;
        if (anim.ShinyTexture != null)
            return anim.ShinyTexture.Value;
        if (set.ShinyColors == null || set.ShinyColors.Count == 0)
            return anim.Texture.Value;
        if (anim.Recolored != null)
            return anim.Recolored;

        var source = Terramon.Instance.Assets.Request<Texture2D>(anim.Texture.Name, AssetRequestMode.ImmediateLoad)
            .Value;
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (var i = 0; i < pixels.Length; i++)
        {
            var packed = pixels[i].PackedValue;
            if ((packed & 0xFF000000) != 0 && set.ShinyColors.TryGetValue(packed & 0xFFFFFF, out var swap))
                pixels[i].PackedValue = (packed & 0xFF000000) | swap;
        }

        anim.Recolored = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
        anim.Recolored.SetData(pixels);
        return anim.Recolored;
    }

    /// <summary>
    ///     Plays a one-time animation (attack, hurt...) on whatever <paramref name="owner" /> draws: a wild
    ///     <see cref="Content.NPCs.PokemonNPC" />, or the <see cref="TerramonPlayer" /> whose pet is out.
    /// </summary>
    public static void Play(object owner, PokemonAnimState state)
    {
        if (Main.dedServ || owner == null)
            return;
        OneShots[owner] = (state, Main.GameUpdateCount);
    }

    /// <summary>
    ///     Plays the animation of a move: one that missed trips, physical ones depend on how they hit.
    /// </summary>
    public static void PlayMove(object owner, ushort moveId, MoveCategory category, bool miss)
    {
        if (miss)
        {
            Play(owner, PokemonAnimState.Trip);
            return;
        }

        var name = ((MoveID)moveId).ToString();
        Play(owner, category switch
        {
            MoveCategory.Special => PokemonAnimState.SpAttack,
            MoveCategory.Status => PokemonAnimState.Status,
            _ when QuickMoves.Contains(name) => PokemonAnimState.QuickStrike,
            _ when DoubleWords.Any(name.Contains) => PokemonAnimState.Double,
            _ when SwingWords.Any(name.Contains) => PokemonAnimState.Swing,
            _ when StrikeWords.Any(name.Contains) => PokemonAnimState.Strike,
            _ => PokemonAnimState.Attack
        });
    }

    /// <summary>
    ///     Like <see cref="Play(object, PokemonAnimState)" />, but doesn't cut off another one-time animation that
    ///     started less than <paramref name="quietTicks" /> ago (e.g. eating after a move that heals).
    /// </summary>
    public static void PlayIfQuiet(object owner, PokemonAnimState state, uint quietTicks = 45)
    {
        if (owner != null && OneShots.TryGetValue(owner, out var shot) &&
            Main.GameUpdateCount - shot.Start < quietTicks)
            return;
        Play(owner, state);
    }

    /// <summary>
    ///     What to show instead when a Pokémon doesn't have an animation.
    /// </summary>
    private static PokemonAnimState Fallback(PokemonAnimState state) => state switch
    {
        PokemonAnimState.Strike or PokemonAnimState.QuickStrike or PokemonAnimState.Swing
            or PokemonAnimState.Double => PokemonAnimState.Attack,
        PokemonAnimState.Laying => PokemonAnimState.Sit,
        PokemonAnimState.Hop => PokemonAnimState.Walk,
        // Lying there with its eyes closed looks the most like fainting
        PokemonAnimState.Faint => PokemonAnimState.Sleep,
        _ => PokemonAnimState.Idle
    };

    private static bool TryGet(AnimSet set, ref PokemonAnimState state, out Anim anim)
    {
        for (var i = 0; i < 3; i++)
        {
            if (set.Anims.TryGetValue(state, out anim))
                return true;
            state = Fallback(state);
        }

        state = PokemonAnimState.Idle;
        return set.Anims.TryGetValue(state, out anim);
    }

    /// <summary>
    ///     Picks the frame to draw right now. <paramref name="origin" /> is the point of the frame that goes on the
    ///     Pokémon's feet. <paramref name="hovering" /> is for Pokémon that fly around: they don't hop or sit down, and
    ///     are drawn from their lowest pixel instead of from the ground under them.
    /// </summary>
    public static void GetFrame(AnimSet set, object owner, bool shiny, bool moving, bool airborne, bool inBattle,
        bool asleep, bool fainted, out Texture2D texture, out Rectangle source, out Vector2 origin,
        bool hovering = false)
    {
        if (hovering)
            airborne = false;
        var now = Main.GameUpdateCount;
        var state = airborne ? PokemonAnimState.Hop : moving ? PokemonAnimState.Walk : PokemonAnimState.Idle;
        var elapsed = now;
        var once = false;

        // Waking up from Sleep
        if (asleep)
            Asleep.Add(owner);
        else if (Asleep.Remove(owner) && !fainted)
            Play(owner, PokemonAnimState.Wake);

        // Standing around for a while outside battle: sits down, then lies down
        if (state != PokemonAnimState.Idle || inBattle || asleep || fainted || hovering || OneShots.ContainsKey(owner))
            IdleSince[owner] = now;
        else if (!IdleSince.ContainsKey(owner))
            IdleSince[owner] = now;
        else if (now - IdleSince[owner] >= SitAfter)
        {
            // Plays through once and stays on the last frame. Pokémon without a sitting pose (the Mystery Dungeon
            // one is seen from behind, so most sets leave it out) keep idling until they lie down.
            var lying = now - IdleSince[owner] >= LayAfter;
            if (lying && (set.Anims.ContainsKey(PokemonAnimState.Laying) || set.Anims.ContainsKey(PokemonAnimState.Sit)))
            {
                state = PokemonAnimState.Laying;
                elapsed = now - IdleSince[owner] - LayAfter;
                once = true;
            }
            else if (!lying && set.Anims.ContainsKey(PokemonAnimState.Sit))
            {
                state = PokemonAnimState.Sit;
                elapsed = now - IdleSince[owner] - SitAfter;
                once = true;
            }
        }

        OneShots.TryGetValue(owner, out var shot);
        if (fainted)
        {
            if (shot.State != PokemonAnimState.Faint || !OneShots.ContainsKey(owner))
                OneShots[owner] = shot = (PokemonAnimState.Faint, now);
        }
        else if (OneShots.ContainsKey(owner) && shot.State == PokemonAnimState.Faint)
        {
            OneShots.Remove(owner);
            shot = default;
        }

        var shotState = shot.State;
        if (OneShots.ContainsKey(owner) && TryGet(set, ref shotState, out var shotAnim) &&
            (shot.State == PokemonAnimState.Faint || now - shot.Start < shotAnim.Total))
        {
            state = shotState;
            elapsed = now - shot.Start;
            once = true;
        }
        else
        {
            if (OneShots.ContainsKey(owner) && shot.State != PokemonAnimState.Faint)
                OneShots.Remove(owner);
            if (asleep && set.Anims.ContainsKey(PokemonAnimState.Sleep))
                state = PokemonAnimState.Sleep;
        }

        TryGet(set, ref state, out var anim);

        var frame = anim.Durations.Length - 1;
        var t = once ? elapsed : elapsed % (uint)anim.Total;
        for (var i = 0; i < anim.Durations.Length; i++)
        {
            if (t < (uint)anim.Durations[i])
            {
                frame = i;
                break;
            }

            t -= (uint)anim.Durations[i];
        }

        texture = TextureOf(set, anim, shiny);
        source = new Rectangle(frame * anim.FrameWidth, 0, anim.FrameWidth, anim.FrameHeight);
        origin = new Vector2(anim.FrameWidth / 2f, anim.FrameHeight / 2f + set.Ground - (hovering ? set.Float : 0));
    }

    /// <summary>
    ///     Draws a Pokémon's animation in a UI (summary, Pokédex...), standing on <paramref name="feet" /> (in screen
    ///     pixels). Pokémon without an animation set draw the first frame of their normal sprite sheet instead.
    ///     <paramref name="state" /> loops; pass a different one to show attacks, sleeping, etc.
    /// </summary>
    public static void DrawInUI(SpriteBatch spriteBatch, ushort id, bool shiny, Vector2 feet, float scale,
        PokemonAnimState state = PokemonAnimState.Idle, bool facingRight = false, Color? color = null)
    {
        var identifier = Terramon.DatabaseV2.GetPokemon(id)?.Identifier;
        if (identifier == null)
            return;
        var effects = facingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var set = Get(identifier);
        if (set == null)
        {
            var path = $"Terramon/Assets/Pokemon/{identifier}{(shiny ? "_S" : string.Empty)}";
            if (!ModContent.RequestIfExists<Texture2D>(path, out var sheet, AssetRequestMode.ImmediateLoad))
                return;
            var frames = PokemonEntityLoader.IDToNPCType.TryGetValue(id, out var npcType)
                ? Main.npcFrameCount[npcType]
                : 1;
            var frame = sheet.Frame(1, Math.Max(frames, 1));
            spriteBatch.Draw(sheet.Value, feet, frame, color ?? Color.White, 0f,
                new Vector2(frame.Width / 2f, frame.Height), scale, effects, 0f);
            return;
        }

        if (!TryGet(set, ref state, out var anim))
            return;
        var t = Main.GameUpdateCount % (uint)anim.Total;
        var index = anim.Durations.Length - 1;
        for (var i = 0; i < anim.Durations.Length; i++)
        {
            if (t < (uint)anim.Durations[i])
            {
                index = i;
                break;
            }

            t -= (uint)anim.Durations[i];
        }

        var texture = TextureOf(set, anim, shiny);
        spriteBatch.Draw(texture, feet, new Rectangle(index * anim.FrameWidth, 0, anim.FrameWidth, anim.FrameHeight),
            color ?? Color.White, 0f, new Vector2(anim.FrameWidth / 2f, anim.FrameHeight / 2f + set.Ground),
            scale * set.Scale, effects, 0f);
    }

    public override void ClearWorld()
    {
        OneShots.Clear();
        IdleSince.Clear();
        Asleep.Clear();
    }

    public override void Unload()
    {
        var recolored = Sets.Values.Where(set => set != null).SelectMany(set => set.Anims.Values)
            .Select(anim => anim.Recolored).Where(texture => texture != null).ToList();
        if (recolored.Count > 0)
            Main.QueueMainThreadAction(() => recolored.ForEach(texture => texture.Dispose()));
        Sets.Clear();
        ClearWorld();
    }
}
