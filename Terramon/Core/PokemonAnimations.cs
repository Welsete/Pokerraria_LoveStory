using System.Text;
using ReLogic.Content;
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
    ///     Pokémon's feet.
    /// </summary>
    public static void GetFrame(AnimSet set, object owner, bool shiny, bool moving, bool airborne, bool inBattle,
        bool asleep, bool fainted, out Texture2D texture, out Rectangle source, out Vector2 origin)
    {
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
        if (state != PokemonAnimState.Idle || inBattle || asleep || fainted || OneShots.ContainsKey(owner))
            IdleSince[owner] = now;
        else if (!IdleSince.ContainsKey(owner))
            IdleSince[owner] = now;
        else if (now - IdleSince[owner] >= SitAfter)
        {
            // Plays through once and stays on the last frame
            var lying = now - IdleSince[owner] >= LayAfter;
            state = lying ? PokemonAnimState.Laying : PokemonAnimState.Sit;
            elapsed = now - IdleSince[owner] - (lying ? LayAfter : SitAfter);
            once = true;
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

        texture = (shiny ? anim.ShinyTexture ?? anim.Texture : anim.Texture).Value;
        source = new Rectangle(frame * anim.FrameWidth, 0, anim.FrameWidth, anim.FrameHeight);
        origin = new Vector2(anim.FrameWidth / 2f, anim.FrameHeight / 2f + set.Ground);
    }

    public override void ClearWorld()
    {
        OneShots.Clear();
        IdleSince.Clear();
        Asleep.Clear();
    }

    public override void Unload()
    {
        Sets.Clear();
        ClearWorld();
    }
}
