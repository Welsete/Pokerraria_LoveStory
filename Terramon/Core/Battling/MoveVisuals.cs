using System.Text;
using ReLogic.Content;
using Terramon.ID;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Core.Battling;

/// <summary>
///     Shows a move being used in the world: physical moves hit the target with a burst, special moves fly from the
///     user to the target, status moves glow around the user. The colors and sounds follow the move's type.
///     Visual only, so it never runs on a server.
///     A move can have its own drawn animation instead: <c>Assets/Moves/&lt;MoveID name&gt;.png</c>, a row of frames, and
///     a <c>.txt</c> next to it with <c>Frames N</c>, <c>Travel N</c> (how many of the first frames loop while it flies
///     from the user to the target; 0 = it all plays on the target), <c>Ticks N</c> (per frame) and <c>Scale X</c>.
///     Optional: <c>Face 1</c> mirrors the whole animation when the target is on the left (fists, beams, wind), and
///     <c>At user|middle|target</c> says where the frames after the flight play (status moves play on the user).
///     <c>Charge N</c>: the first N frames play on the user before anything flies (Solar Beam gathering light).
///     <c>Beam A B</c>: frames A to B are beams whose tip is at the frame's center; they are stretched and turned so
///     the beam goes all the way from the user to the target.
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
        public MoveSprite Sprite;
        public Vector2 From, To;
    }

    private sealed class MoveSprite
    {
        public Asset<Texture2D> Texture;
        public int Frames = 1, Travel, Ticks = 5, Charge;
        public int BeamFrom = -1, BeamTo = -1;
        public float Scale = 1f;
        public bool Face;
        public string At;
        public int ChargeTime => Charge * Ticks;
        public int TravelTime => Travel > 0 ? TravelTicks : 0;

        /// <summary>
        ///     When the move reaches the target: after the charge and the flight.
        /// </summary>
        public int HitTime => ChargeTime + TravelTime;

        public int Duration => HitTime + (Frames - Charge - Travel) * Ticks;

        public int FrameAt(int time)
        {
            if (time < ChargeTime)
                return time / Ticks;
            if (time < HitTime)
                return Charge + (time - ChargeTime) / Ticks % Travel;
            return Math.Min(Frames - 1, Charge + Travel + (time - HitTime) / Ticks);
        }
    }

    private static readonly List<Effect> Effects = [];
    private static readonly Dictionary<ushort, MoveSprite> Sprites = [];

    public override void Load()
    {
        if (!Main.dedServ)
            On_Main.DrawDust += DrawSpritesOverDust;
    }

    public override void Unload()
    {
        Sprites.Clear();
    }

    /// <summary>
    ///     The drawn animation of a move, if it has one (see the class summary).
    /// </summary>
    private static MoveSprite SpriteFor(ushort moveId)
    {
        if (Sprites.TryGetValue(moveId, out var cached))
            return cached;
        MoveSprite sprite = null;
        var name = ((MoveID)moveId).ToString();
        var mod = Terramon.Instance;
        var path = "Assets/Moves/" + name;
        if (ModContent.HasAsset($"Terramon/{path}"))
        {
            sprite = new MoveSprite { Texture = mod.Assets.Request<Texture2D>(path) };
            if (mod.FileExists(path + ".txt"))
            {
                foreach (var line in Encoding.UTF8.GetString(mod.GetFileBytes(path + ".txt")).Split('\n'))
                {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;
                    switch (parts[0])
                    {
                        case "Frames" when int.TryParse(parts[1], out var v): sprite.Frames = Math.Max(1, v); break;
                        case "Travel" when int.TryParse(parts[1], out var v): sprite.Travel = Math.Max(0, v); break;
                        case "Ticks" when int.TryParse(parts[1], out var v): sprite.Ticks = Math.Max(1, v); break;
                        case "Charge" when int.TryParse(parts[1], out var v): sprite.Charge = Math.Max(0, v); break;
                        case "Beam" when parts.Length > 2 && int.TryParse(parts[1], out var from) &&
                                         int.TryParse(parts[2], out var to):
                            sprite.BeamFrom = from;
                            sprite.BeamTo = to;
                            break;
                        case "Face" when int.TryParse(parts[1], out var v): sprite.Face = v != 0; break;
                        case "At": sprite.At = parts[1].ToLowerInvariant(); break;
                        case "Scale" when float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var f): sprite.Scale = f; break;
                    }
                }
            }

            sprite.Charge = Math.Min(sprite.Charge, sprite.Frames - 1);
            sprite.Travel = Math.Min(sprite.Travel, sprite.Frames - 1 - sprite.Charge);
        }

        Sprites[moveId] = sprite;
        return sprite;
    }

    /// <summary>
    ///     How long a move's own drawn animation takes, or 0 if it has none: its hit is shown once it's over.
    /// </summary>
    public static int AnimationTicks(ushort moveId) => Main.dedServ ? 0 : SpriteFor(moveId)?.Duration ?? 0;

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
            Miss = miss,
            Sprite = SpriteFor(moveId)
        };
        effect.Duration = effect.Category switch
        {
            MoveCategory.Special => TravelTicks + 6,
            MoveCategory.Status => AuraTicks,
            _ => 8
        };
        if (effect.Sprite is { } sprite)
            effect.Duration = sprite.Duration;

        if (Position(effect.User) is not { } from)
            return;
        SoundEngine.PlaySound(SoundFor(effect.Type, effect.Category), from);
        Effects.Add(effect);
    }

    private static readonly Queue<(ushort Move, string Label)> Showcase = [];
    private static BattleSide _showcaseSide;
    private static int _showcaseWait;

    /// <summary>
    ///     Plays the animations of these moves one after the other from <paramref name="side" />, without touching the
    ///     battle, so they can all be checked at once (test panel). Each one's label is written in the chat as it starts.
    /// </summary>
    public static void PlayShowcase(BattleSide side, IEnumerable<(ushort Move, string Label)> moves)
    {
        Showcase.Clear();
        foreach (var move in moves)
            Showcase.Enqueue(move);
        _showcaseSide = side;
        _showcaseWait = ShowcaseGap;
    }

    public static void StopShowcase() => Showcase.Clear();

    private const int ShowcaseGap = 30;

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

        // The next move of the showcase starts a little after the last one is over
        if (Showcase.Count == 0 || Effects.Count > 0 || ++_showcaseWait < ShowcaseGap)
            return;
        _showcaseWait = 0;
        if (_showcaseSide?.Provider == null)
        {
            Showcase.Clear();
            return;
        }

        var (move, label) = Showcase.Dequeue();
        Main.NewText(label, Color.Gold);
        Play(_showcaseSide, move, false, false);
    }

    public override void ClearWorld()
    {
        Effects.Clear();
        Showcase.Clear();
    }

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
        effect.From = from;
        effect.To = to;

        if (effect.Sprite is { } sprite)
        {
            // Its own animation: flying frames, then the rest on the target (drawn in DrawSpritesOverDust)
            if (effect.Time == sprite.HitTime)
            {
                if (effect.Miss)
                {
                    Impact(effect, to, dust, 0);
                    return false;
                }

                SoundEngine.PlaySound(effect.Category == MoveCategory.Physical ? SoundID.NPCHit4 : SoundID.Item14, to);
            }

            Lighting.AddLight(SpritePosition(effect, sprite), color.ToVector3());
            return true;
        }

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

    private static Vector2 SpritePosition(Effect effect, MoveSprite sprite)
    {
        if (effect.Time < sprite.ChargeTime)
            return effect.From;
        if (effect.Time >= sprite.HitTime)
            return (sprite.At ?? (effect.Category == MoveCategory.Status ? "user" : "target")) switch
            {
                "user" => effect.From,
                "middle" => Vector2.Lerp(effect.From, effect.To, 0.5f),
                _ => effect.To
            };
        var progress = (effect.Time - sprite.ChargeTime) / (float)sprite.TravelTime;
        return Vector2.Lerp(effect.From, effect.To, progress) + new Vector2(0f, -MathF.Sin(progress * MathF.PI) * 18f);
    }

    /// <summary>
    ///     Moves with their own animation are drawn right after dust, so they show in front of the Pokémon.
    /// </summary>
    private static void DrawSpritesOverDust(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        if (Effects.Count == 0 || !Effects.Exists(e => e.Sprite != null))
            return;

        var spriteBatch = Main.spriteBatch;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (var effect in Effects)
        {
            if (effect.Sprite is not { } sprite || effect.Time > effect.Duration)
                continue;
            var texture = sprite.Texture.Value;
            var width = texture.Width / sprite.Frames;
            var travelling = effect.Time >= sprite.ChargeTime && effect.Time < sprite.HitTime;
            var frame = sprite.FrameAt(effect.Time);
            var source = new Rectangle(frame * width, 0, width, texture.Height);
            var origin = new Vector2(width, texture.Height) * 0.5f;
            if (!travelling && frame >= sprite.BeamFrom && frame <= sprite.BeamTo)
            {
                // A beam: its tip on the target, stretched back to the user
                var line = effect.To - effect.From;
                var length = Math.Max(16f, line.Length());
                spriteBatch.Draw(texture, (effect.To - Main.screenPosition).Floor(), source, Color.White,
                    line.ToRotation(), origin, new Vector2(length / (width * 0.5f), sprite.Scale), SpriteEffects.None, 0f);
                continue;
            }

            // The sheet is drawn going right; flying left mirrors the flying frames (all of them for Face moves)
            var flip = (travelling || sprite.Face) && effect.To.X < effect.From.X
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            spriteBatch.Draw(texture, (SpritePosition(effect, sprite) - Main.screenPosition).Floor(), source,
                Color.White, 0f, origin, sprite.Scale, flip, 0f);
        }

        spriteBatch.End();
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
