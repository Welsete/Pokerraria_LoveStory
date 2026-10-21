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
    Faint
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

    public static void Play(object owner, MoveCategory category) => Play(owner, category switch
    {
        MoveCategory.Special => PokemonAnimState.SpAttack,
        MoveCategory.Status => PokemonAnimState.Status,
        _ => PokemonAnimState.Attack
    });

    /// <summary>
    ///     Picks the frame to draw right now. <paramref name="origin" /> is the point of the frame that goes on the
    ///     Pokémon's feet.
    /// </summary>
    public static void GetFrame(AnimSet set, object owner, bool shiny, bool moving, bool asleep, bool fainted,
        out Texture2D texture, out Rectangle source, out Vector2 origin)
    {
        var now = Main.GameUpdateCount;
        var state = moving ? PokemonAnimState.Walk : PokemonAnimState.Idle;
        var elapsed = now;
        var once = false;

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

        if (OneShots.ContainsKey(owner) && set.Anims.TryGetValue(shot.State, out var shotAnim) &&
            (shot.State == PokemonAnimState.Faint || now - shot.Start < shotAnim.Total))
        {
            state = shot.State;
            elapsed = now - shot.Start;
            once = true;
        }
        else if (asleep && set.Anims.ContainsKey(PokemonAnimState.Sleep))
            state = PokemonAnimState.Sleep;

        if (!set.Anims.TryGetValue(state, out var anim))
            anim = set.Anims[PokemonAnimState.Idle];

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

    public override void ClearWorld() => OneShots.Clear();

    public override void Unload()
    {
        Sets.Clear();
        OneShots.Clear();
    }
}
