using Terramon.ID;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Core.Battling;

/// <summary>
///     Shows a move being used in the world: physical moves hit the target with a burst, special moves fly from the
///     user to the target, status moves glow around the user. The colors and sounds follow the move's type.
///     Visual only, so it never runs on a server.
/// </summary>
public sealed class MoveVisuals : ModSystem
{
    private const int TravelTicks = 18;
    private const int AuraTicks = 30;

    private sealed class Effect
    {
        public IBattleProvider User;
        public IBattleProvider Target;
        public PokemonType Type;
        public MoveCategory Category;
        public bool Miss;
        public int Time;
        public int Duration;
    }

    private static readonly List<Effect> Effects = [];

    /// <summary>
    ///     Starts the animation of a move. <paramref name="still" /> moves (e.g. the charging turn of Solar Beam) have
    ///     no animation in the games either.
    /// </summary>
    public static void Play(BattleSide userSide, ushort moveId, bool still, bool miss)
    {
        if (Main.dedServ || still || userSide?.Provider == null)
            return;

        var move = Terramon.DatabaseV2.GetMove(moveId);
        if (move == null)
            return;

        PokemonAnimations.PlayMove(userSide.Provider, moveId, move.Category, miss);

        var effect = new Effect
        {
            User = userSide.Provider,
            Target = userSide.Opposite?.Provider,
            Type = move.Type,
            Category = move.Category,
            Miss = miss
        };
        effect.Duration = effect.Category switch
        {
            MoveCategory.Special => TravelTicks + 6,
            MoveCategory.Status => AuraTicks,
            _ => 8
        };

        if (Position(effect.User) is not { } from)
            return;
        SoundEngine.PlaySound(SoundFor(effect.Type, effect.Category), from);
        Effects.Add(effect);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        for (var i = Effects.Count - 1; i >= 0; i--)
        {
            var effect = Effects[i];
            if (!Step(effect) || ++effect.Time > effect.Duration)
                Effects.RemoveAt(i);
        }
    }

    public override void ClearWorld() => Effects.Clear();

    /// <summary>
    ///     Draws one tick of an effect. Returns false if it can't go on (e.g. the target left).
    /// </summary>
    private static bool Step(Effect effect)
    {
        if (Position(effect.User) is not { } from)
            return false;
        var to = Position(effect.Target) ?? from + new Vector2(64f, 0f);
        var dust = DustFor(effect.Type);
        var color = ColorFor(effect.Type);

        switch (effect.Category)
        {
            case MoveCategory.Status:
                // Sparkles rising around the user
                if (effect.Time % 2 == 0)
                {
                    var angle = effect.Time * 0.6f;
                    var offset = new Vector2(MathF.Cos(angle) * 22f, 14f - effect.Time * 1.2f);
                    Spawn(from + offset, dust, new Vector2(0f, -1.2f), 1.3f);
                    Spawn(from - offset with { Y = offset.Y }, dust, new Vector2(0f, -1.2f), 1.3f);
                }

                Lighting.AddLight(from, color.ToVector3() * 0.6f);
                break;

            case MoveCategory.Special:
                if (effect.Time <= TravelTicks)
                {
                    // An orb flying to the target, leaving a trail
                    var progress = effect.Time / (float)TravelTicks;
                    var arc = new Vector2(0f, -MathF.Sin(progress * MathF.PI) * 18f);
                    var position = Vector2.Lerp(from, to, progress) + arc;
                    for (var k = 0; k < 3; k++)
                        Spawn(position + Main.rand.NextVector2Circular(6f, 6f), dust, Vector2.Zero, 1.5f);
                    Lighting.AddLight(position, color.ToVector3());
                }
                else if (effect.Time == TravelTicks + 1)
                    Impact(effect, to, dust, 16);

                break;

            default:
                // Physical: a quick dash of dust from the user, then the hit
                if (effect.Time < 4)
                {
                    var position = Vector2.Lerp(from, to, (effect.Time + 1) / 4f);
                    Spawn(position, DustID.Smoke, Vector2.Zero, 1f);
                }
                else if (effect.Time == 4)
                    Impact(effect, to, dust, 22);

                break;
        }

        return true;
    }

    private static void Impact(Effect effect, Vector2 at, int dust, int amount)
    {
        if (effect.Miss)
        {
            CombatText.NewText(new Rectangle((int)at.X - 10, (int)at.Y - 30, 20, 10), Color.LightGray,
                Language.GetTextValue("Mods.Terramon.Battle.MissPopup"));
            return;
        }

        for (var k = 0; k < amount; k++)
            Spawn(at, dust, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.2f), 1.6f);
        for (var k = 0; k < 6; k++)
            Spawn(at, DustID.SilverFlame, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
        Lighting.AddLight(at, ColorFor(effect.Type).ToVector3() * 1.5f);
        SoundEngine.PlaySound(effect.Category == MoveCategory.Physical ? SoundID.NPCHit4 : SoundID.Item10, at);
    }

    private static void Spawn(Vector2 at, int type, Vector2 velocity, float scale)
    {
        var d = Dust.NewDustPerfect(at, type, velocity, 100, default, scale);
        d.noGravity = true;
    }

    /// <summary>
    ///     Where a battle participant's Pokémon is in the world: a player's companion, the wild Pokémon itself, or the
    ///     trainer standing there.
    /// </summary>
    private static Vector2? Position(IBattleProvider provider)
    {
        switch (provider)
        {
            case TerramonPlayer player:
                var pet = player.ActivePetProjectile?.Projectile;
                if (pet is { active: true })
                    return pet.Center;
                return player.Player.active ? player.Player.Center : null;
            case Content.NPCs.Trainers.TrainerNPC trainer when trainer.NPC.active && trainer.BattlePokemonCenter is { } mon:
                return mon;
            default:
                var entity = provider?.SyncedEntity;
                return entity is { active: true } ? entity.Center : null;
        }
    }

    private static int DustFor(PokemonType type) => type switch
    {
        PokemonType.Fire => DustID.Torch,
        PokemonType.Water => DustID.DungeonWater,
        PokemonType.Electric => DustID.Electric,
        PokemonType.Grass => DustID.GrassBlades,
        PokemonType.Ice => DustID.IceTorch,
        PokemonType.Fighting => DustID.OrangeTorch,
        PokemonType.Poison => DustID.Venom,
        PokemonType.Ground => DustID.Dirt,
        PokemonType.Flying => DustID.Cloud,
        PokemonType.Psychic => DustID.PinkTorch,
        PokemonType.Bug => DustID.GreenTorch,
        PokemonType.Rock => DustID.Stone,
        PokemonType.Ghost => DustID.Shadowflame,
        PokemonType.Dragon => DustID.PurpleTorch,
        PokemonType.Dark => DustID.Wraith,
        PokemonType.Steel => DustID.Silver,
        PokemonType.Fairy => DustID.PinkFairy,
        _ => DustID.WhiteTorch
    };

    private static Color ColorFor(PokemonType type) => type switch
    {
        PokemonType.Fire => new Color(255, 120, 40),
        PokemonType.Water => new Color(70, 140, 255),
        PokemonType.Electric => new Color(255, 230, 60),
        PokemonType.Grass => new Color(90, 220, 90),
        PokemonType.Ice => new Color(140, 230, 255),
        PokemonType.Psychic => new Color(255, 100, 180),
        PokemonType.Ghost or PokemonType.Dragon => new Color(150, 90, 255),
        PokemonType.Poison => new Color(180, 80, 220),
        PokemonType.Fairy => new Color(255, 160, 220),
        _ => new Color(220, 220, 220)
    };

    private static SoundStyle SoundFor(PokemonType type, MoveCategory category)
    {
        if (category == MoveCategory.Status)
            return SoundID.Item4;

        return type switch
        {
            PokemonType.Fire => SoundID.Item20,
            PokemonType.Water => SoundID.Item21,
            PokemonType.Electric => SoundID.Item94,
            PokemonType.Ice => SoundID.Item28,
            PokemonType.Grass or PokemonType.Bug => SoundID.Item8,
            PokemonType.Psychic or PokemonType.Fairy => SoundID.Item9,
            PokemonType.Ghost or PokemonType.Dark => SoundID.Item103,
            PokemonType.Dragon => SoundID.Item119,
            PokemonType.Rock or PokemonType.Ground => SoundID.Item69,
            PokemonType.Poison => SoundID.Item17,
            _ => category == MoveCategory.Physical ? SoundID.Item1 : SoundID.Item8
        };
    }
}
