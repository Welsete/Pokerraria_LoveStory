using System.Runtime.CompilerServices;
using System.Text.Json;
using Terramon.Content.NPCs;
using Terramon.Core.Battling.BattlePackets;
using Terramon.ID;

namespace Terramon.Core.Battling;

/// <summary>
///     Picks moves and switches for NPC battlers (wild Pokémon and trainers) from a Showdown request.
///     Runs wherever the battle stream runs (singleplayer or server).
/// </summary>
public static class BattleAI
{
    private static Terramon Mod => Terramon.Instance;

    #region Public API

    /// <summary>
    ///     Answers a Showdown request for an NPC battler. Wild Pokémon pick mostly at random,
    ///     trainers score their moves and sometimes switch out of a bad matchup.
    /// </summary>
    /// <param name="source">The battle the request belongs to.</param>
    /// <param name="client">The NPC's battle client (its provider must be a <see cref="PokemonNPC"/> or a trainer).</param>
    /// <param name="request">The root element of the parsed request JSON.</param>
    /// <returns>Whether a choice was submitted. When false, the caller should fall back to <see cref="BattleChoice.Default"/>.</returns>
    public static bool TryChoose(BattleInstance source, BattleClient client, JsonElement request)
    {
        if (request.TryGetProperty("wait", out _) || request.TryGetProperty("teamPreview", out _))
            return false;
        if (!request.TryGetProperty("side", out var sideEl) || !sideEl.TryGetProperty("pokemon", out var pokemonEl))
            return false;

        var sd = client == source.ClientA ? 1 : 2;
        var state = GetState(source, sd);
        state.ErrorRetries = 0;

        var ctx = new Context(source, client, sd, ReadTeam(pokemonEl, client, source.Omniscient?[sd]));
        if (ctx.Active is null && !request.TryGetProperty("forceSwitch", out _))
            return false;

        if (request.TryGetProperty("forceSwitch", out _))
            return ChooseForcedSwitch(ctx, state);

        if (!request.TryGetProperty("active", out var activeArr) || activeArr.GetArrayLength() == 0)
            return false;
        var active = activeArr[0];

        // Track switch-ins so per-Pokémon memory resets
        if (ctx.Active.PartySlot != state.ActiveSlot)
        {
            state.ActiveSlot = ctx.Active.PartySlot;
            state.TurnsSinceSwitchIn = 0;
            state.UsesSinceSwitchIn.Clear();
        }
        else
        {
            state.TurnsSinceSwitchIn++;
        }
        state.Turn++;

        var moves = ReadMoves(active);
        if (moves.Count == 0)
            return false;

        var isTrainer = client.Provider is not PokemonNPC;
        if (isTrainer)
        {
            var trapped = active.TryGetProperty("trapped", out var t) && t.ValueKind == JsonValueKind.True ||
                          active.TryGetProperty("maybeTrapped", out var mt) && mt.ValueKind == JsonValueKind.True;
            if (!trapped && TryStrategicSwitch(ctx, state, moves))
                return true;
        }

        var pick = isTrainer ? PickTrainerMove(ctx, state, moves) : PickWildMove(ctx, moves);
        if (pick is null)
            return false;

        Remember(state, ctx.Active.PartySlot, pick.ID);
        Mod.Logger.Debug($"BattleAI: {client.Provider.BattleName} uses move {pick.Index + 1} ({pick.RawID})");
        return client.MakeChoice(BattleChoice.Move, pick.Index);
    }

    /// <summary>
    ///     Called when Showdown rejected a choice an NPC made. Retries with <see cref="BattleChoice.Default"/> a couple of times
    ///     so the battle doesn't hang waiting for the NPC.
    /// </summary>
    public static void OnChoiceError(BattleInstance source, int side)
    {
        if (side is not (1 or 2) || source.Stream is null || source.Stream.IsDisposed)
            return;
        var state = GetState(source, side);
        if (state.ErrorRetries >= 2)
            return;
        state.ErrorRetries++;
        Mod.Logger.Warn($"BattleAI: choice for side {side} was rejected, retrying with the default choice");
        source.SubmitChoice(side, BattleChoice.Default, 0);
    }

    /// <summary>
    ///     Type effectiveness multiplier of an attacking type against a defending type (Gen 6+ chart).
    /// </summary>
    public static float Effectiveness(PokemonType attack, PokemonType defense)
    {
        var a = (int)attack;
        var d = (int)defense;
        if (a <= 0 || a > 18 || d <= 0 || d > 18)
            return 1f;
        return Chart[a, d];
    }

    /// <summary>
    ///     Type effectiveness multiplier of an attacking type against a list of defending types.
    /// </summary>
    public static float Effectiveness(PokemonType attack, IEnumerable<PokemonType> defense)
    {
        var mult = 1f;
        if (defense is null)
            return mult;
        foreach (var t in defense)
            mult *= Effectiveness(attack, t);
        return mult;
    }

    #endregion

    #region State

    private sealed class SideState
    {
        public int Turn;
        public int LastSwitchTurn = -100;
        public int ActiveSlot = -1;
        public int TurnsSinceSwitchIn;
        public int ErrorRetries;
        public readonly Dictionary<MoveID, int> UsesSinceSwitchIn = [];
        public readonly HashSet<(int, MoveID)> UsedThisBattle = [];
    }

    private static readonly ConditionalWeakTable<BattleInstance, SideState[]> States = new();

    private static SideState GetState(BattleInstance source, int side)
    {
        var arr = States.GetValue(source, _ => [new SideState(), new SideState()]);
        return arr[side == 2 ? 1 : 0];
    }

    private static void Remember(SideState state, int partySlot, MoveID id)
    {
        if (id == MoveID.None)
            return;
        state.UsesSinceSwitchIn[id] = state.UsesSinceSwitchIn.GetValueOrDefault(id) + 1;
        state.UsedThisBattle.Add((partySlot, id));
    }

    #endregion

    #region Reading the request

    private sealed class TeamMon
    {
        public int Position; // 0-based index in Showdown's current team order
        public int PartySlot;
        public bool Active;
        public bool Fainted;
        public float HPFraction;
        public NonVolatileStatus Status;
        public List<PokemonType> Types = [];
        public List<MoveID> Moves = [];
        public int Atk = 100, SpA = 100, Spe = 100;
    }

    private sealed class MoveOption
    {
        public int Index; // 0-based index in active[0].moves
        public string RawID;
        public MoveID ID;
        public DatabaseV2.MoveSchema Data;
    }

    private sealed class Context(BattleInstance source, BattleClient client, int side, List<TeamMon> team)
    {
        public readonly BattleInstance Source = source;
        public readonly BattleClient Client = client;
        public readonly int Side = side;
        public readonly List<TeamMon> Team = team;
        public TeamMon Active => Team.FirstOrDefault(m => m.Active && !m.Fainted) ?? Team.FirstOrDefault(m => m.Active);

        public BattleSide FoeSide => Source.Omniscient?[3 - Side];
        public BattleSide OwnSide => Source.Omniscient?[Side];

        public List<PokemonType> FoeTypes
        {
            get
            {
                var foe = FoeSide;
                if (foe is null)
                    return [];
                return TypesOf(foe.ActivePokemon.Species);
            }
        }
    }

    private static List<PokemonType> TypesOf(ushort species)
    {
        var schema = Terramon.DatabaseV2?.GetPokemon(species);
        return schema?.Types?.Where(t => t is > PokemonType.None and <= PokemonType.Fairy).ToList() ?? [];
    }

    private static List<TeamMon> ReadTeam(JsonElement pokemonEl, BattleClient client, BattleSide own)
    {
        var list = new List<TeamMon>();
        var pos = 0;
        foreach (var p in pokemonEl.EnumerateArray())
        {
            var mon = new TeamMon { Position = pos };
            // Nicknames are party slots plus one (see SimplePackedPokemon.Team)
            var slot = client.SwitchOrder != null && pos < client.SwitchOrder.Length ? client.SwitchOrder[pos] : -1;
            if (slot < 0 && p.TryGetProperty("ident", out var ident) &&
                int.TryParse(ident.GetString()?.Split(": ", 2)[^1], out var n))
                slot = n - 1;
            mon.PartySlot = slot < 0 ? pos : slot;

            mon.Active = p.TryGetProperty("active", out var act) && act.ValueKind == JsonValueKind.True;

            var condition = p.TryGetProperty("condition", out var cond) ? cond.GetString() ?? string.Empty : string.Empty;
            var parts = condition.Split(' ');
            var hpParts = parts[0].Split('/');
            var hp = hpParts.Length > 0 && float.TryParse(hpParts[0], out var h) ? h : 0f;
            var max = hpParts.Length > 1 && float.TryParse(hpParts[1], out var m) ? m : 0f;
            mon.HPFraction = max > 0 ? hp / max : 0f;
            mon.Fainted = hp <= 0 || parts.Contains("fnt");
            if (parts.Length > 1 && Enum.TryParse(parts[1], true, out NonVolatileStatus status))
                mon.Status = status;

            if (p.TryGetProperty("stats", out var stats))
            {
                if (stats.TryGetProperty("atk", out var a) && a.TryGetInt32(out var ai)) mon.Atk = ai;
                if (stats.TryGetProperty("spa", out var s) && s.TryGetInt32(out var si)) mon.SpA = si;
                if (stats.TryGetProperty("spe", out var e) && e.TryGetInt32(out var ei)) mon.Spe = ei;
            }

            if (p.TryGetProperty("moves", out var mvs))
                foreach (var mv in mvs.EnumerateArray())
                    if (TryParseMove(mv.GetString(), out var id))
                        mon.Moves.Add(id);

            if (own != null && mon.PartySlot is >= 0 and < 6 && own[mon.PartySlot].Species != 0)
                mon.Types = TypesOf(own[mon.PartySlot].Species);
            else if (p.TryGetProperty("details", out var details))
            {
                // "Pikachu, L50, M"
                var species = details.GetString()?.Split(',')[0];
                try
                {
                    if (species != null)
                        mon.Types = TypesOf(NationalDexID.FromSpecies(species));
                }
                catch (Exception)
                {
                    // Unknown species name; leave its types empty
                    mon.Types = [];
                }
            }

            list.Add(mon);
            pos++;
        }
        return list;
    }

    private static bool IsStruggle(MoveOption move)
        => move.ID == MoveID.Struggle || move.RawID.Equals("struggle", StringComparison.OrdinalIgnoreCase);

    private static List<MoveOption> ReadMoves(JsonElement active)
    {
        var list = new List<MoveOption>();
        if (!active.TryGetProperty("moves", out var moves))
            return list;

        var i = 0;
        foreach (var mv in moves.EnumerateArray())
        {
            var index = i++;
            if (mv.TryGetProperty("disabled", out var dis) &&
                (dis.ValueKind == JsonValueKind.True || dis.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(dis.GetString())))
                continue;
            // Locked moves (recharge, Struggle, two-turn moves) don't always carry pp
            if (mv.TryGetProperty("pp", out var pp) && pp.TryGetInt32(out var ppLeft) && ppLeft <= 0 &&
                mv.TryGetProperty("maxpp", out _))
                continue;

            var raw = mv.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
            TryParseMove(raw, out var id);
            list.Add(new MoveOption
            {
                Index = index,
                RawID = raw,
                ID = id,
                Data = id == MoveID.None ? null : Terramon.DatabaseV2?.GetMove(id),
            });
        }
        // Every Pokémon also carries Struggle (see PokemonMoves.PackedString). It's only for when nothing else is left,
        // like in the main games, not a move to pick: it barely hurts and costs a quarter of the user's HP
        if (list.Any(m => !IsStruggle(m)))
            list.RemoveAll(IsStruggle);
        return list;
    }

    private static bool TryParseMove(string showdownID, out MoveID id)
    {
        id = MoveID.None;
        if (string.IsNullOrEmpty(showdownID))
            return false;
        // Showdown ids are lowercase names without spaces, e.g. "thunderwave", "hiddenpowerfire60"
        if (showdownID.StartsWith("hiddenpower"))
            showdownID = "hiddenpower";
        return Enum.TryParse(showdownID, true, out id) && id != MoveID.None;
    }

    #endregion

    #region Move kinds

    private enum StatusKind : byte
    {
        None,
        Sleep,
        Paralysis,
        Poison,
        Burn,
    }

    private static HashSet<MoveID> Set(params string[] names)
    {
        var set = new HashSet<MoveID>();
        foreach (var n in names)
            if (Enum.TryParse(n, out MoveID id))
                set.Add(id);
        return set;
    }

    private static readonly HashSet<MoveID> SleepMoves = Set("SleepPowder", "Spore", "Hypnosis", "Sing", "LovelyKiss",
        "GrassWhistle", "DarkVoid", "Yawn");
    private static readonly HashSet<MoveID> ParalysisMoves = Set("ThunderWave", "StunSpore", "Glare");
    private static readonly HashSet<MoveID> PoisonMoves = Set("Toxic", "PoisonPowder", "PoisonGas");
    private static readonly HashSet<MoveID> BurnMoves = Set("WillOWisp");
    private static readonly HashSet<MoveID> PowderMoves = Set("SleepPowder", "StunSpore", "PoisonPowder", "Spore");

    private static readonly HashSet<MoveID> ConfusionMoves = Set("ConfuseRay", "Supersonic", "SweetKiss", "TeeterDance",
        "Swagger", "Flatter");

    private static readonly HashSet<MoveID> SetupMoves = Set("SwordsDance", "DragonDance", "NastyPlot", "CalmMind",
        "BulkUp", "Agility", "IronDefense", "Growth", "Harden", "Withdraw", "DefenseCurl", "Amnesia", "QuiverDance",
        "ShellSmash", "CosmicPower", "Coil", "HoneClaws", "WorkUp", "Barrier", "AcidArmor", "Meditate", "Sharpen",
        "Howl", "RockPolish", "TailGlow", "ShiftGear", "Autotomize", "Minimize", "DoubleTeam", "FocusEnergy", "Charge",
        "Curse", "VictoryDance", "TidyUp", "Stockpile");

    private static readonly HashSet<MoveID> DebuffMoves = Set("Growl", "TailWhip", "Leer", "StringShot", "SandAttack",
        "Smokescreen", "ScaryFace", "Screech", "Charm", "FakeTears", "MetalSound", "FeatherDance", "CottonSpore",
        "Kinesis", "Flash", "SweetScent", "Tickle", "Captivate", "Confide", "BabyDollEyes", "PlayNice", "NobleRoar",
        "EerieImpulse", "Memento");

    private static readonly HashSet<MoveID> HealMoves = Set("Recover", "SoftBoiled", "Roost", "Synthesis", "Moonlight",
        "MorningSun", "SlackOff", "MilkDrink", "HealOrder", "ShoreUp", "Wish", "StrengthSap", "LifeDew", "Swallow");

    private static readonly HashSet<MoveID> FieldMoves = Set("Reflect", "LightScreen", "StealthRock", "Spikes",
        "ToxicSpikes", "AuroraVeil", "Safeguard", "Mist", "Tailwind", "StickyWeb", "RainDance", "SunnyDay", "Sandstorm",
        "Hail", "Snowscape", "LuckyChant", "Substitute", "LeechSeed", "Ingrain", "AquaRing");

    private static readonly HashSet<MoveID> UselessMoves = Set("Splash", "Celebrate", "HoldHands", "Teleport",
        "Transform", "HappyHour");

    private static readonly HashSet<MoveID> SelfKOMoves = Set("Explosion", "SelfDestruct", "MistyExplosion",
        "FinalGambit", "HealingWish", "LunarDance");

    private static readonly HashSet<MoveID> ChargeMoves = Set("SolarBeam", "SolarBlade", "SkyAttack", "RazorWind",
        "SkullBash", "MeteorBeam", "FreezeShock", "IceBurn", "Geomancy");

    private static readonly HashSet<MoveID> SemiInvulnerableMoves = Set("Fly", "Dig", "Dive", "Bounce",
        "PhantomForce", "ShadowForce");

    private static readonly HashSet<MoveID> ThreeHitMoves = Set("DoubleSlap", "CometPunch", "FuryAttack", "PinMissile",
        "SpikeCannon", "Barrage", "FurySwipes", "BoneRush", "BulletSeed", "IcicleSpear", "RockBlast", "TailSlap",
        "ScaleShot", "WaterShuriken", "ArmThrust", "TripleKick", "TripleAxel", "PopulationBomb");

    private static readonly HashSet<MoveID> TwoHitMoves = Set("DoubleKick", "Bonemerang", "DualChop", "DoubleHit",
        "TwinNeedle", "DualWingbeat", "GearGrind", "DoubleIronBash", "DragonDarts", "TwinBeam", "TachyonCutter");

    private static readonly HashSet<MoveID> OHKOMoves = Set("Guillotine", "HornDrill", "Fissure", "SheerCold");

    private static StatusKind StatusOf(MoveID id)
    {
        if (SleepMoves.Contains(id)) return StatusKind.Sleep;
        if (ParalysisMoves.Contains(id)) return StatusKind.Paralysis;
        if (PoisonMoves.Contains(id)) return StatusKind.Poison;
        if (BurnMoves.Contains(id)) return StatusKind.Burn;
        return StatusKind.None;
    }

    private static bool Is(MoveID id, string name) => Enum.TryParse(name, out MoveID other) && other == id;

    private static bool HasAbility(AbilityID ability, params string[] names)
    {
        if (ability == AbilityID.None)
            return false;
        foreach (var n in names)
            if (Enum.TryParse(n, out AbilityID a) && a == ability)
                return true;
        return false;
    }

    private static bool IsDamaging(MoveOption m)
        => m.Data != null && m.Data.Category is MoveCategory.Physical or MoveCategory.Special;

    #endregion

    #region Wild Pokémon

    private static MoveOption PickWildMove(Context ctx, List<MoveOption> moves)
    {
        var foeTypes = ctx.FoeTypes;
        var foeStatus = ctx.FoeSide?.ActivePokemon.Status ?? NonVolatileStatus.None;

        var weights = new List<(MoveOption, float)>();
        foreach (var m in moves)
        {
            float w;
            if (IsDamaging(m))
                w = Effectiveness(m.Data.Type, foeTypes) == 0 ? 0.15f : 1.6f;
            else if (UselessMoves.Contains(m.ID))
                w = 0.4f;
            else if (StatusOf(m.ID) != StatusKind.None && foeStatus != NonVolatileStatus.None)
                w = 0.15f;
            else
                w = 1f;
            weights.Add((m, w));
        }
        return WeightedPick(weights);
    }

    #endregion

    #region Trainers

    private static MoveOption PickTrainerMove(Context ctx, SideState state, List<MoveOption> moves)
    {
        var scored = new List<(MoveOption Move, float Score)>();
        foreach (var m in moves)
        {
            var s = ScoreMove(ctx, state, m);
            // A little randomness so trainers aren't fully predictable
            s *= 0.85f + Random.Shared.NextSingle() * 0.3f;
            scored.Add((m, s));
        }

        var best = scored.Max(s => s.Score);
        if (best <= 0f)
        {
            // Nothing looks useful (e.g. every move is resisted to zero); just pick any
            return moves[Random.Shared.Next(moves.Count)];
        }

        // Usually the best move, sometimes another good one
        if (Random.Shared.NextSingle() < 0.8f)
            return scored.First(s => s.Score == best).Move;

        var good = scored.Where(s => s.Score >= best * 0.7f).Select(s => (s.Move, s.Score * s.Score)).ToList<(MoveOption, float)>();
        return WeightedPick(good);
    }

    private static float ScoreMove(Context ctx, SideState state, MoveOption m)
    {
        var me = ctx.Active;
        var foeSide = ctx.FoeSide;
        var foeTypes = ctx.FoeTypes;
        var foe = foeSide != null ? foeSide.ActivePokemon : default;
        var foeStatus = foeSide != null ? foe.Status : NonVolatileStatus.None;
        var foeAbility = foeSide != null ? foe.Ability : AbilityID.None;
        var foeHP = 1f;
        if (foeSide != null && foe.Data != null && foe.Data.MaxHP > 0)
            foeHP = foe.HP / (float)foe.Data.MaxHP;

        // Unknown move (e.g. "recharge" or "struggle"): neutral score
        if (m.Data == null)
            return 40f;

        if (IsDamaging(m))
        {
            var eff = Effectiveness(m.Data.Type, foeTypes);
            if (eff == 0f || AbilityImmune(foeAbility, m.Data.Type))
                return 0f;

            float power = m.Data.Power ?? 0;
            if (OHKOMoves.Contains(m.ID))
                power = 150f;
            else if (power <= 1f)
                power = 60f; // fixed or variable damage moves
            if (ThreeHitMoves.Contains(m.ID))
                power *= 3f;
            else if (TwoHitMoves.Contains(m.ID))
                power *= 2f;

            var stab = me.Types.Contains(m.Data.Type) ? 1.5f : 1f;
            var acc = m.Data.Accuracy is { } a and > 0 ? Math.Min(a, (byte)100) / 100f : 1f;

            // Prefer the attacker's stronger side
            var physical = m.Data.Category == MoveCategory.Physical;
            var myStat = physical ? me.Atk : me.SpA;
            var statFactor = 0.5f + 0.5f * myStat / Math.Max(1f, Math.Max(me.Atk, me.SpA));

            // And the defender's weaker side
            var foeSchema = foeSide != null ? Terramon.DatabaseV2?.GetPokemon(foe.Species) : null;
            var defFactor = 1f;
            if (foeSchema?.BaseStats is { Count: >= 6 } bs)
            {
                float def = physical ? bs.Defense : bs.SpDef;
                var avg = (bs.Defense + bs.SpDef) / 2f;
                defFactor = 2f * avg / Math.Max(1f, def + avg);
            }

            var score = power * stab * eff * acc * statFactor * defFactor;

            if (physical && me.Status == NonVolatileStatus.Brn)
                score *= 0.5f;

            var weather = ctx.Source.Omniscient?.Weather ?? BattleWeather.None;
            var rain = weather is BattleWeather.RainDance or BattleWeather.PrimordialSea;
            var sun = weather is BattleWeather.SunnyDay or BattleWeather.DesolateLand;
            if (m.Data.Type == PokemonType.Water && rain || m.Data.Type == PokemonType.Fire && sun)
                score *= 1.5f;
            else if (m.Data.Type == PokemonType.Water && sun || m.Data.Type == PokemonType.Fire && rain)
                score *= 0.5f;

            if (ChargeMoves.Contains(m.ID) && !(sun && (Is(m.ID, "SolarBeam") || Is(m.ID, "SolarBlade"))))
                score *= 0.55f;
            else if (SemiInvulnerableMoves.Contains(m.ID))
                score *= 0.85f;

            if (SelfKOMoves.Contains(m.ID))
                score *= me.HPFraction < 0.3f ? 1f : 0.05f;
            if (Is(m.ID, "DreamEater") && foeStatus != NonVolatileStatus.Slp)
                return 0f;
            if (Is(m.ID, "Snore") && me.Status != NonVolatileStatus.Slp)
                return 0f;
            if ((Is(m.ID, "FakeOut") || Is(m.ID, "FirstImpression")) && state.TurnsSinceSwitchIn > 0)
                return 0f;
            if (Is(m.ID, "FocusPunch"))
                score *= 0.4f;

            // A finishing blow on a weakened foe is worth taking
            if (foeHP < 0.25f)
                score *= 1.2f;

            return score;
        }

        // Status moves
        var uses = state.UsesSinceSwitchIn.GetValueOrDefault(m.ID);
        var usedBefore = state.UsedThisBattle.Contains((me.PartySlot, m.ID));

        var kind = StatusOf(m.ID);
        if (kind != StatusKind.None)
        {
            if (foeStatus != NonVolatileStatus.None || StatusImmune(kind, m, foeTypes, foeAbility))
                return 0f;
            var acc = m.Data.Accuracy is { } a and > 0 ? Math.Min(a, (byte)100) / 100f : 1f;
            var baseScore = kind == StatusKind.Sleep ? 85f : 70f;
            if (me.HPFraction < 0.5f || foeHP < 0.3f)
                baseScore = 20f;
            return baseScore * acc;
        }

        if (UselessMoves.Contains(m.ID))
            return 1f;

        if (HealMoves.Contains(m.ID))
        {
            if (Is(m.ID, "Swallow") && uses == 0)
                return 0f;
            return me.HPFraction switch
            {
                < 0.4f => 95f,
                < 0.6f => 45f,
                _ => 0f,
            };
        }

        if (Is(m.ID, "Rest"))
        {
            if (me.HPFraction < 0.35f || me.Status != NonVolatileStatus.None && me.HPFraction < 0.6f)
                return 85f;
            return 0f;
        }

        if (Is(m.ID, "SleepTalk"))
            return me.Status == NonVolatileStatus.Slp ? 90f : 0f;

        if (SetupMoves.Contains(m.ID))
        {
            var boosts = 0;
            if (ctx.OwnSide != null && me.PartySlot is >= 0 and < 6)
            {
                var st = ctx.OwnSide[me.PartySlot].StatStages;
                boosts = Math.Max(0, st.Attack) + Math.Max(0, st.SpAtk) + Math.Max(0, st.Defense) +
                         Math.Max(0, st.SpDef) + Math.Max(0, st.Speed);
            }
            if (me.HPFraction >= 0.7f && boosts < 2 && uses < 2)
                return 65f;
            return 6f;
        }

        if (DebuffMoves.Contains(m.ID))
        {
            if (Is(m.ID, "Memento"))
                return me.HPFraction < 0.25f ? 50f : 0f;
            return uses < 1 && me.HPFraction >= 0.6f ? 30f : 6f;
        }

        if (FieldMoves.Contains(m.ID))
        {
            if (Is(m.ID, "LeechSeed") && (foeTypes.Contains(PokemonType.Grass) ||
                                          foeSide != null && foe.Volatiles.Contains(VolatileEffect.LeechSeed)))
                return 0f;
            var weather = ctx.Source.Omniscient?.Weather ?? BattleWeather.None;
            if (Is(m.ID, "RainDance") && weather == BattleWeather.RainDance ||
                Is(m.ID, "SunnyDay") && weather == BattleWeather.SunnyDay ||
                Is(m.ID, "Sandstorm") && weather == BattleWeather.Sandstorm ||
                (Is(m.ID, "Hail") || Is(m.ID, "Snowscape")) && weather is BattleWeather.Hail or BattleWeather.Snowscape)
                return 0f;
            if (Is(m.ID, "Substitute"))
                return me.HPFraction > 0.6f && uses == 0 ? 25f : 0f;
            return !usedBefore && me.HPFraction >= 0.6f ? 50f : 3f;
        }

        if (ConfusionMoves.Contains(m.ID))
        {
            if (foeSide != null && foe.Volatiles.Contains(VolatileEffect.Confusion))
                return 0f;
            return me.HPFraction >= 0.5f ? 35f : 10f;
        }

        // Anything else (Protect, Roar, Encore, ...): an occasional pick
        return uses < 1 ? 15f : 5f;
    }

    private static bool AbilityImmune(AbilityID ability, PokemonType type)
        => type switch
        {
            PokemonType.Ground => HasAbility(ability, "Levitate", "EarthEater"),
            PokemonType.Fire => HasAbility(ability, "FlashFire", "WellBakedBody"),
            PokemonType.Water => HasAbility(ability, "WaterAbsorb", "StormDrain", "DrySkin"),
            PokemonType.Electric => HasAbility(ability, "VoltAbsorb", "LightningRod", "MotorDrive"),
            PokemonType.Grass => HasAbility(ability, "SapSipper"),
            _ => false,
        };

    private static bool StatusImmune(StatusKind kind, MoveOption m, List<PokemonType> foeTypes, AbilityID foeAbility)
    {
        if (PowderMoves.Contains(m.ID) && (foeTypes.Contains(PokemonType.Grass) || HasAbility(foeAbility, "Overcoat")))
            return true;
        // Thunder Wave and friends respect type immunity (Ground vs Thunder Wave)
        if (!Is(m.ID, "Glare") && Effectiveness(m.Data.Type, foeTypes) == 0f)
            return true;
        if (AbilityImmune(foeAbility, m.Data.Type) || HasAbility(foeAbility, "Comatose", "PurifyingSalt"))
            return true;
        return kind switch
        {
            StatusKind.Sleep => HasAbility(foeAbility, "Insomnia", "VitalSpirit", "SweetVeil"),
            StatusKind.Paralysis => foeTypes.Contains(PokemonType.Electric) || HasAbility(foeAbility, "Limber"),
            StatusKind.Poison => foeTypes.Contains(PokemonType.Poison) || foeTypes.Contains(PokemonType.Steel) ||
                                 HasAbility(foeAbility, "Immunity"),
            StatusKind.Burn => foeTypes.Contains(PokemonType.Fire) || HasAbility(foeAbility, "WaterVeil"),
            _ => false,
        };
    }

    #endregion

    #region Switching

    /// <summary>
    ///     How hard the foe's STAB types hit a set of types (the worst case).
    /// </summary>
    private static float Threat(List<PokemonType> foeTypes, List<PokemonType> myTypes)
    {
        if (foeTypes.Count == 0)
            return 1f;
        return foeTypes.Max(t => Effectiveness(t, myTypes));
    }

    /// <summary>
    ///     How well a Pokémon can hit the foe, judged by its known damaging moves (or its own types).
    /// </summary>
    private static float Offense(TeamMon mon, List<PokemonType> foeTypes)
    {
        var best = 0f;
        foreach (var id in mon.Moves)
        {
            var data = Terramon.DatabaseV2?.GetMove(id);
            if (data is null || data.Category is not (MoveCategory.Physical or MoveCategory.Special) || (data.Power ?? 0) == 0)
                continue;
            var e = Effectiveness(data.Type, foeTypes) * (mon.Types.Contains(data.Type) ? 1.5f : 1f);
            best = Math.Max(best, e);
        }
        if (best == 0f && mon.Types.Count > 0)
            best = mon.Types.Max(t => Effectiveness(t, foeTypes));
        return best == 0f ? 0.5f : best;
    }

    private static float MatchupScore(TeamMon mon, List<PokemonType> foeTypes)
    {
        var threat = Math.Max(0.25f, Threat(foeTypes, mon.Types));
        return (0.4f + mon.HPFraction) * Offense(mon, foeTypes) / threat;
    }

    private static bool ChooseForcedSwitch(Context ctx, SideState state)
    {
        var foeTypes = ctx.FoeTypes;
        var candidates = ctx.Team.Where(m => !m.Fainted && !m.Active).ToList();
        if (candidates.Count == 0)
            return false;

        var best = candidates.OrderByDescending(m => MatchupScore(m, foeTypes) * (0.9f + Random.Shared.NextSingle() * 0.2f)).First();
        state.LastSwitchTurn = state.Turn;
        return Switch(ctx, best);
    }

    private static bool TryStrategicSwitch(Context ctx, SideState state, List<MoveOption> moves)
    {
        var me = ctx.Active;
        if (me is null || state.TurnsSinceSwitchIn < 1 || state.Turn - state.LastSwitchTurn < 3)
            return false;

        var foeTypes = ctx.FoeTypes;
        if (foeTypes.Count == 0 || Threat(foeTypes, me.Types) < 2f)
            return false;

        // Stay in if it can hit back super effectively
        var canHitBack = moves.Any(m => IsDamaging(m) && (m.Data.Power ?? 0) > 1 && Effectiveness(m.Data.Type, foeTypes) >= 2f);
        if (canHitBack)
            return false;

        var candidates = ctx.Team
            .Where(m => !m.Fainted && !m.Active && m.HPFraction >= 0.5f && Threat(foeTypes, m.Types) <= 0.5f)
            .ToList();
        if (candidates.Count == 0)
            return false;

        // Not every time, so the player can't count on it
        if (Random.Shared.NextSingle() >= 0.6f)
            return false;

        var best = candidates.OrderByDescending(m => MatchupScore(m, foeTypes)).First();
        state.LastSwitchTurn = state.Turn;
        Mod.Logger.Debug($"BattleAI: {ctx.Client.Provider.BattleName} switches out of a bad matchup");
        return Switch(ctx, best);
    }

    private static bool Switch(Context ctx, TeamMon target)
    {
        // MakeChoice maps party slots to Showdown positions through SwitchOrder when it's known
        var operand = ctx.Client.SwitchOrder != null ? target.PartySlot : target.Position;
        return ctx.Client.MakeChoice(BattleChoice.Switch, operand);
    }

    #endregion

    private static T WeightedPick<T>(List<(T, float)> items) where T : class
    {
        if (items.Count == 0)
            return null;
        var total = items.Sum(i => Math.Max(0f, i.Item2));
        if (total <= 0f)
            return items[Random.Shared.Next(items.Count)].Item1;
        var roll = Random.Shared.NextSingle() * total;
        foreach (var (item, weight) in items)
        {
            roll -= Math.Max(0f, weight);
            if (roll <= 0f)
                return item;
        }
        return items[^1].Item1;
    }

    #region Type chart

    private static readonly float[,] Chart = BuildChart();

    private static float[,] BuildChart()
    {
        var c = new float[19, 19];
        for (var i = 0; i < 19; i++)
            for (var j = 0; j < 19; j++)
                c[i, j] = 1f;

        void Row(PokemonType atk, PokemonType[] sup, PokemonType[] notVery, PokemonType[] none)
        {
            foreach (var d in sup) c[(int)atk, (int)d] = 2f;
            foreach (var d in notVery) c[(int)atk, (int)d] = 0.5f;
            foreach (var d in none) c[(int)atk, (int)d] = 0f;
        }

        const PokemonType Nor = PokemonType.Normal, Fig = PokemonType.Fighting, Fly = PokemonType.Flying,
            Poi = PokemonType.Poison, Gro = PokemonType.Ground, Roc = PokemonType.Rock, Bug = PokemonType.Bug,
            Gho = PokemonType.Ghost, Ste = PokemonType.Steel, Fir = PokemonType.Fire, Wat = PokemonType.Water,
            Gra = PokemonType.Grass, Ele = PokemonType.Electric, Psy = PokemonType.Psychic, Ice = PokemonType.Ice,
            Dra = PokemonType.Dragon, Dar = PokemonType.Dark, Fai = PokemonType.Fairy;

        Row(Nor, [], [Roc, Ste], [Gho]);
        Row(Fig, [Nor, Roc, Ste, Ice, Dar], [Fly, Poi, Bug, Psy, Fai], [Gho]);
        Row(Fly, [Fig, Bug, Gra], [Roc, Ste, Ele], []);
        Row(Poi, [Gra, Fai], [Poi, Gro, Roc, Gho], [Ste]);
        Row(Gro, [Poi, Roc, Ste, Fir, Ele], [Bug, Gra], [Fly]);
        Row(Roc, [Fly, Bug, Fir, Ice], [Fig, Gro, Ste], []);
        Row(Bug, [Gra, Psy, Dar], [Fig, Fly, Poi, Gho, Ste, Fir, Fai], []);
        Row(Gho, [Gho, Psy], [Dar], [Nor]);
        Row(Ste, [Roc, Ice, Fai], [Ste, Fir, Wat, Ele], []);
        Row(Fir, [Bug, Ste, Gra, Ice], [Roc, Fir, Wat, Dra], []);
        Row(Wat, [Gro, Roc, Fir], [Wat, Gra, Dra], []);
        Row(Gra, [Gro, Roc, Wat], [Fly, Poi, Bug, Ste, Fir, Gra, Dra], []);
        Row(Ele, [Fly, Wat], [Gra, Ele, Dra], [Gro]);
        Row(Psy, [Fig, Poi], [Ste, Psy], [Dar]);
        Row(Ice, [Fly, Gro, Gra, Dra], [Ste, Fir, Wat, Ice], []);
        Row(Dra, [Dra], [Ste], [Fai]);
        Row(Dar, [Gho, Psy], [Fig, Dar, Fai], []);
        Row(Fai, [Fig, Dra, Dar], [Poi, Ste, Fir], []);
        return c;
    }

    #endregion
}
