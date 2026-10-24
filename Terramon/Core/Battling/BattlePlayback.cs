using Terramon.ID;

namespace Terramon.Core.Battling;

/// <summary>
///     Plays the events of a battle turn one after another, like the main games: a move is used (its animation and the
///     "X used Y!" line), then its hit lands (the HP bar goes down, the Hurt animation and the damage lines), then a
///     short pause before the next move.
///     <para>
///         Showdown sends a whole turn at once and the battle applies it right away (HP, PP, faints), so the battle data is
///         never delayed. Only what players see is: while a turn plays, the HP, status, level and active Pokémon shown in
///         the battle UI and in the world are the ones of the event being played (see <see cref="ShownHP" />), and the
///         chat lines, animations, the end of the battle and the next choice wait for their moment.
///     </para>
///     Visual only: nothing is queued on a server. Every client mirroring a battle plays the same sequence.
/// </summary>
public sealed class BattlePlayback : ModSystem
{
    // How long each kind of moment lasts (ticks, 60 per second)
    private const int PhysicalHitDelay = 20;
    private const int SpecialHitDelay = 26;
    private const int StatusHitDelay = 24;
    private const int StillHitDelay = 10;
    // Every message stays at least this long, and longer ones get more time to be read (see HoldFor)
    private const int LineHold = 80;
    private const int LongestHold = 220;

    // The time a hit's HP bar gets to go down before the next message
    private const int HPHold = 40;
    private const int FaintHold = 85;
    private const int SwitchHold = 14;
    private const int MoveGap = 110;
    private const int EndPause = 60;

    // The last message of a turn stays a moment before the menu comes back
    private const int TurnHold = 75;

    // A timeline that never empties (it shouldn't happen) is played out at once after this long, so nobody gets stuck
    private const int MaxBusyTicks = 60 * 30;

    private enum Kind : byte
    {
        Move,
        HP,
        Status,
        Look,
        Active,
        Line,
        Faint,
        Turn,
        Pause,
        End
    }

    private sealed class Step
    {
        public Kind Kind;
        public int Hold;

        /// <summary>
        ///     Shown together with the start of a move ("X used Y!"), before its hit.
        /// </summary>
        public bool Intro;

        public BattleSide Side;
        public int SideIndex;
        public PokemonData Mon;

        // Move
        public ushort MoveId;
        public bool Still;
        public bool Miss;

        // HP
        public SimpleHPValue Value;
        public bool Refinable;
        public bool Refined;

        // Status
        public NonVolatileStatus Status;

        // Look (Transform)
        public int Slot;
        public ushort Into;
        public bool IntoShiny;

        // Line and Faint
        public string Text;
        public Color Color;
        public bool Print;
        public bool HistoryOnly;

        /// <summary>
        ///     Shown once its hit has landed (see <see cref="Append" />).
        /// </summary>
        public bool AfterHit;
        public List<(string Text, Color Color)> Extra;
        public PokemonData[] ReleaseExp;

        // End
        public Action Action;
    }

    /// <summary>
    ///     An HP value as Showdown sent it: exact ("23/40") or, in the public half of the battle, a percentage ("57/100").
    /// </summary>
    private readonly record struct SimpleHPValue(ushort HP, ushort MaxHP)
    {
        public ushort For(PokemonData mon)
        {
            // No max HP means it fainted
            if (HP == 0 || MaxHP == 0)
                return 0;
            if (mon == null)
                return HP;
            if (MaxHP == mon.MaxHP)
                return Math.Min(HP, mon.MaxHP);
            var scaled = (int)Math.Round(HP * mon.MaxHP / (double)MaxHP);
            return (ushort)Math.Clamp(scaled, 1, mon.MaxHP);
        }
    }

    private sealed class Timeline(BattleField field)
    {
        public readonly BattleField Field = field;
        public readonly LinkedList<Step> Steps = [];
        public int Wait;
        public int PendingHit;
        public int SinceMove = MoveGap;
        public int BusyTicks;
        public string LastText;

        /// <summary>
        ///     Ticks this timeline is kept after it empties, so what it holds isn't dropped between a value being held
        ///     and its step being queued (singleplayer battles are read on another thread).
        /// </summary>
        public int Grace;

        public readonly Dictionary<PokemonData, ushort> HP = new(ReferenceEqualityComparer.Instance);
        public readonly Dictionary<PokemonData, NonVolatileStatus> Status = new(ReferenceEqualityComparer.Instance);
        public readonly Dictionary<PokemonData, (byte Level, int Exp)> Exp = new(ReferenceEqualityComparer.Instance);
        public readonly Dictionary<int, PokemonData> Active = [];
        public readonly Dictionary<(int Side, int Slot), (ushort Into, bool Shiny)> Looks = [];

        public bool Busy => Steps.Count > 0 || Wait > 0 || PendingHit > 0;
    }

    private static readonly List<Timeline> Timelines = [];

    // Battles are read on a background thread in singleplayer, and drawn on the main thread
    private static readonly object Sync = new();

    private static bool Enabled => !Main.dedServ;

    private static Timeline Get(BattleField field, bool create)
    {
        if (field == null)
            return null;
        foreach (var timeline in Timelines)
        {
            if (timeline.Field == field)
                return timeline;
        }

        if (!create)
            return null;
        var made = new Timeline(field);
        Timelines.Add(made);
        return made;
    }

    private static Timeline Touch(BattleField field)
    {
        var timeline = Get(field, true);
        timeline.Grace = 2;
        return timeline;
    }

    private static void Add(BattleField field, Step step)
    {
        if (!Enabled || field == null)
            return;
        lock (Sync)
            Append(Touch(field), step);
    }

    // A Pokémon faints in the battle as soon as its HP hits 0, before the lines and hits that come with that blow
    // (the damage line, recoil, items...). Those go before its faint, so "X fainted!" and its EXP come last, like in
    // the main games. Call while holding Sync
    private static void Append(Timeline timeline, Step step)
    {
        if (step.Kind is Kind.Line or Kind.HP or Kind.Status)
        {
            LinkedListNode<Step> firstFaint = null;
            for (var node = timeline.Steps.Last; node != null && node.Value.Kind == Kind.Faint; node = node.Previous)
                firstFaint = node;
            if (firstFaint != null)
            {
                timeline.Steps.AddBefore(firstFaint, step);
                return;
            }
        }

        // Showdown says "super effective" or "critical hit" before the hit's damage, but the main games show the HP
        // going down first, then what happened: the hit goes before those lines
        if (step.Kind == Kind.HP)
        {
            LinkedListNode<Step> firstAfterHit = null;
            for (var node = timeline.Steps.Last; node is { Value: { Kind: Kind.Line, AfterHit: true } };
                 node = node.Previous)
                firstAfterHit = node;
            if (firstAfterHit != null)
            {
                for (var node = firstAfterHit; node != null; node = node.Next)
                    node.Value.AfterHit = false; // Only before this hit, not a later one (recoil, items...)
                timeline.Steps.AddBefore(firstAfterHit, step);
                return;
            }
        }

        timeline.Steps.AddLast(step);
    }

    #region Queueing (called while the battle is applied)

    /// <summary>
    ///     Called just before a Pokémon's HP changes in the battle: it keeps showing its current HP until the change's
    ///     moment comes.
    /// </summary>
    public static void QueueHP(BattleField field, BattleSide side, PokemonData mon, ushort hp, ushort maxHP)
    {
        if (!Enabled || field == null)
            return;
        lock (Sync)
        {
            var timeline = Touch(field);
            if (mon != null)
                timeline.HP.TryAdd(mon, mon.HP);
            Append(timeline, new Step
            {
                Kind = Kind.HP,
                Side = side,
                Mon = mon,
                Value = new SimpleHPValue(hp, maxHP),
                // The exact HP of a hit comes a moment later in the battle's secret half (see RefineHP). Faints
                // ("0 fnt") have no max HP in either half, so they never take another hit's exact HP
                Refinable = maxHP != 0,
                // A moment for the bar to go down before what comes next
                Hold = HPHold
            });
        }
    }

    /// <summary>
    ///     Called with the exact HP of a hit (the battle's secret half), right after its public percentage, so the
    ///     numbers shown are the real ones.
    /// </summary>
    public static void RefineHP(BattleField field, PokemonData mon, ushort hp, ushort maxHP)
    {
        // Faints carry no number to refine
        if (!Enabled || field == null || mon == null || maxHP == 0)
            return;
        lock (Sync)
        {
            var timeline = Get(field, false);
            if (timeline == null)
                return;

            var pending = false;
            foreach (var step in timeline.Steps)
            {
                if (step.Kind != Kind.HP || step.Mon != mon)
                    continue;
                pending = true;
                if (!step.Refinable || step.Refined)
                    continue;
                step.Value = new SimpleHPValue(hp, maxHP);
                step.Refined = true;
                return;
            }

            // Its moment already played (multiplayer: the exact HP came in a later packet)
            if (!pending && timeline.HP.ContainsKey(mon))
                timeline.HP[mon] = new SimpleHPValue(hp, maxHP).For(mon);
        }
    }

    /// <summary>
    ///     Called just before a Pokémon's status changes in the battle: it keeps showing its current status until then.
    /// </summary>
    public static void HoldStatus(BattleField field, PokemonData mon)
    {
        if (!Enabled || field == null || mon == null)
            return;
        lock (Sync)
            Touch(field).Status.TryAdd(mon, mon.Status);
    }

    /// <summary>
    ///     Shows a Pokémon's new status (see <see cref="HoldStatus" />) when its moment comes.
    /// </summary>
    public static void QueueStatus(BattleField field, PokemonData mon, NonVolatileStatus status)
    {
        if (mon == null)
            return;
        Add(field, new Step { Kind = Kind.Status, Mon = mon, Status = status });
    }

    /// <summary>
    ///     Called just before a Pokémon's Transform looks change in the battle (Transform, switching out, fainting):
    ///     it keeps its current looks until the change is shown.
    /// </summary>
    public static void HoldLook(BattleField field, int sideIndex, int slot, ushort into, bool shiny)
    {
        if (!Enabled || field == null)
            return;
        lock (Sync)
            Touch(field).Looks.TryAdd((sideIndex, slot), (into, shiny));
    }

    /// <summary>
    ///     Shows a Pokémon's new Transform looks (see <see cref="HoldLook" />) when their moment comes.
    /// </summary>
    public static void QueueLook(BattleField field, int sideIndex, int slot, ushort into, bool shiny)
        => Add(field, new Step { Kind = Kind.Look, SideIndex = sideIndex, Slot = slot, Into = into, IntoShiny = shiny });

    /// <summary>
    ///     A move being used: its animation and the user's attack animation. Its hit comes a moment later.
    /// </summary>
    public static void QueueMove(BattleField field, BattleSide userSide, ushort moveId, bool still, bool miss)
        => Add(field, new Step { Kind = Kind.Move, Side = userSide, MoveId = moveId, Still = still, Miss = miss });

    /// <summary>
    ///     Called just before a Pokémon is sent out: the side keeps showing its current Pokémon until then.
    /// </summary>
    public static void QueueSwitch(BattleField field, int sideIndex, PokemonData current, PokemonData switchedIn)
    {
        if (!Enabled || field == null)
            return;
        lock (Sync)
        {
            var timeline = Touch(field);
            timeline.Active.TryAdd(sideIndex, current);
            timeline.Steps.AddLast(new Step
            {
                Kind = Kind.Active,
                SideIndex = sideIndex,
                Mon = switchedIn,
                Hold = SwitchHold
            });
        }
    }

    /// <summary>
    ///     A battle message. Only the players in the battle see it in chat; everyone else watching just gets the pause.
    /// </summary>
    public static void QueueLine(BattleField field, string text, Color color, string key, bool print)
    {
        var name = key?[(key.LastIndexOf('.') + 1)..];
        var intro = name is "UsedMove" or "MoveEffect";
        // How much HP went down or up is already shown by the HP bar: it's only kept in the battle history (L), with
        // a short moment for the bar to move
        var historyOnly = name is "Damage" or "Healed";
        Add(field, new Step
        {
            Kind = Kind.Line,
            Text = text,
            Color = color,
            Print = print,
            Intro = intro,
            HistoryOnly = historyOnly,
            AfterHit = name is "Crit" or "SuperEffective" or "Resisted",
            Hold = historyOnly ? 0 : HoldFor(text)
        });
    }

    /// <summary>
    ///     Keeps showing the level and EXP of these Pokémon until the faint that gave them EXP is shown.
    /// </summary>
    public static void HoldExp(BattleField field, PokemonData[] party)
    {
        if (!Enabled || field == null || party == null)
            return;
        lock (Sync)
        {
            var timeline = Touch(field);
            foreach (var mon in party)
            {
                if (mon != null)
                    timeline.Exp.TryAdd(mon, (mon.Level, mon.TotalEXP));
            }
        }
    }

    /// <summary>
    ///     A Pokémon fainting: its message, then the EXP the local player's Pokémon got for it.
    /// </summary>
    public static void QueueFaint(BattleField field, PokemonData mon, string text, bool print,
        List<(string Text, Color Color)> yields, PokemonData[] expHolders)
        => Add(field, new Step
        {
            Kind = Kind.Faint,
            Mon = mon,
            Text = text,
            Color = new Color(255, 120, 110),
            Print = print,
            Extra = yields,
            ReleaseExp = expHolders,
            Hold = FaintHold
        });

    /// <summary>
    ///     A new turn starts (written in the battle history).
    /// </summary>
    public static void QueueTurn(BattleField field, bool print)
        => Add(field, new Step { Kind = Kind.Turn, Print = print, Hold = TurnHold });

    /// <summary>
    ///     Runs <paramref name="action" /> once everything already queued for this battle has been shown (e.g. the end
    ///     of the battle after its final hit). Returns false if nothing is playing, so the caller runs it right away.
    /// </summary>
    public static bool Defer(BattleField field, Action action)
    {
        if (!Enabled || field == null || action == null)
            return false;
        lock (Sync)
        {
            var timeline = Get(field, false);
            if (timeline is not { Busy: true })
                return false;
            // A last moment to read the final message before the battle closes (only once)
            if (timeline.Steps.Last?.Value.Kind != Kind.End)
                timeline.Steps.AddLast(new Step { Kind = Kind.Pause, Hold = EndPause });
            timeline.Steps.AddLast(new Step { Kind = Kind.End, Action = action });
            return true;
        }
    }

    #endregion

    #region What is shown

    /// <summary>
    ///     Whether a battle still has events to show. The local player can't choose anything until it's done.
    /// </summary>
    public static bool IsBusy(BattleField field)
    {
        if (!Enabled || field == null)
            return false;
        lock (Sync)
            return Get(field, false) is { Busy: true };
    }

    /// <summary>
    ///     Whether the local player's battle still has events to show.
    /// </summary>
    public static bool LocalBusy => IsBusy(BattleClient.LocalClient?.Battle);

    /// <summary>
    ///     The latest messages shown in the local player's battle while it plays (the last two, older first, one per
    ///     line), without color tags.
    /// </summary>
    public static string LocalLine
    {
        get
        {
            var battle = BattleClient.LocalClient?.Battle;
            lock (Sync)
                return Get(battle, false)?.LastText;
        }
    }

    /// <summary>
    ///     The HP to show for a Pokémon: the HP of the event being played if its battle is playing one, else its HP.
    /// </summary>
    public static ushort ShownHP(PokemonData mon)
    {
        if (mon == null)
            return 0;
        if (Enabled && Timelines.Count > 0)
        {
            lock (Sync)
            {
                foreach (var timeline in Timelines)
                {
                    if (timeline.HP.TryGetValue(mon, out var hp))
                        return Math.Min(hp, mon.MaxHP);
                }
            }
        }

        return mon.HP;
    }

    /// <summary>
    ///     The status condition to show for a Pokémon (see <see cref="ShownHP" />).
    /// </summary>
    public static NonVolatileStatus ShownStatus(PokemonData mon)
    {
        if (mon == null)
            return NonVolatileStatus.None;
        if (Enabled && Timelines.Count > 0)
        {
            lock (Sync)
            {
                foreach (var timeline in Timelines)
                {
                    if (timeline.Status.TryGetValue(mon, out var status))
                        return status;
                }
            }
        }

        return mon.Status;
    }

    /// <summary>
    ///     The level and total EXP to show for a Pokémon (see <see cref="ShownHP" />).
    /// </summary>
    public static (byte Level, int Exp) ShownExp(PokemonData mon)
    {
        if (mon == null)
            return (0, 0);
        if (Enabled && Timelines.Count > 0)
        {
            lock (Sync)
            {
                foreach (var timeline in Timelines)
                {
                    if (timeline.Exp.TryGetValue(mon, out var exp))
                        return exp;
                }
            }
        }

        return (mon.Level, mon.TotalEXP);
    }

    /// <summary>
    ///     Whether a side of a playing battle still shows another Pokémon than its active one (it was switched out in
    ///     the battle, but the switch hasn't been shown yet). <paramref name="shown" /> is that Pokémon.
    /// </summary>
    public static bool TryGetShownActive(BattleField field, int sideIndex, out PokemonData shown)
    {
        shown = null;
        if (!Enabled || field == null)
            return false;
        lock (Sync)
            return Get(field, false)?.Active.TryGetValue(sideIndex, out shown) == true && shown != null;
    }

    /// <summary>
    ///     The Transform looks a Pokémon of a playing battle still shows (see <see cref="HoldLook" />). False when
    ///     nothing different is shown, so its own values apply.
    /// </summary>
    public static bool TryGetShownLook(BattleField field, int sideIndex, int slot, out ushort into, out bool shiny)
    {
        into = 0;
        shiny = false;
        if (!Enabled || field == null)
            return false;
        lock (Sync)
        {
            var timeline = Get(field, false);
            if (timeline == null || !timeline.Looks.TryGetValue((sideIndex, slot), out var look))
                return false;
            (into, shiny) = look;
            return true;
        }
    }

    #endregion

    #region Playing

    public override void PostUpdateEverything()
    {
        if (!Enabled)
            return;
        Timeline[] timelines;
        lock (Sync)
        {
            if (Timelines.Count == 0)
                return;
            timelines = Timelines.ToArray();
        }

        foreach (var timeline in timelines)
        {
            bool stuck;
            lock (Sync)
            {
                timeline.SinceMove++;
                stuck = ++timeline.BusyTicks > MaxBusyTicks;
            }

            if (stuck)
                Terramon.Instance.Logger.Warn("A battle turn took too long to show, so it was shown at once");
            Advance(timeline, fast: stuck);
        }
    }

    /// <summary>
    ///     Shows the rest of what the local player's battle has queued at once (skipping the animations).
    /// </summary>
    public static void SkipLocal()
    {
        var field = BattleClient.LocalClient?.Battle;
        Timeline timeline;
        lock (Sync)
            timeline = Get(field, false);
        if (timeline != null)
            Advance(timeline, fast: true);
    }

    /// <summary>
    ///     Shows everything still queued at once, e.g. when leaving the world, so battles still end properly.
    /// </summary>
    public static void FlushAll()
    {
        Timeline[] timelines;
        lock (Sync)
            timelines = Timelines.ToArray();
        foreach (var timeline in timelines)
            Advance(timeline, fast: true);
        lock (Sync)
            Timelines.Clear();
    }

    /// <summary>
    ///     Forgets everything queued without showing it (battles reset by a command).
    /// </summary>
    public static void Clear()
    {
        lock (Sync)
            Timelines.Clear();
    }

    public override void PreSaveAndQuit() => FlushAll();

    public override void ClearWorld() => FlushAll();

    private static void Advance(Timeline timeline, bool fast)
    {
        while (true)
        {
            Step step;
            lock (Sync)
            {
                if (fast)
                {
                    timeline.Wait = 0;
                    timeline.PendingHit = 0;
                }
                else if (timeline.Wait > 0)
                {
                    timeline.Wait--;
                    return;
                }

                if (timeline.Steps.Count == 0)
                {
                    if (timeline.PendingHit > 0)
                    {
                        timeline.Wait = timeline.PendingHit - 1;
                        timeline.PendingHit = 0;
                        return;
                    }

                    if (timeline.Grace > 0)
                    {
                        timeline.Grace--;
                        return;
                    }

                    Finish(timeline);
                    return;
                }

                step = timeline.Steps.First!.Value;
                if (!fast)
                {
                    if (step.Kind == Kind.Move)
                    {
                        timeline.PendingHit = 0;
                        // A short pause between two moves
                        if (timeline.SinceMove < MoveGap)
                        {
                            timeline.Wait = MoveGap - timeline.SinceMove - 1;
                            timeline.SinceMove = MoveGap; // Counted by the wait from here
                            return;
                        }
                    }
                    else if (timeline.PendingHit > 0 && !step.Intro)
                    {
                        // The move's hit lands now
                        timeline.Wait = timeline.PendingHit - 1;
                        timeline.PendingHit = 0;
                        return;
                    }
                }

                timeline.Steps.RemoveFirst();
                timeline.Wait = fast ? 0 : step.Hold;
                if (step.Kind == Kind.Move)
                {
                    timeline.SinceMove = 0;
                    timeline.PendingHit = fast ? 0 : HitDelay(step);
                }
            }

            try
            {
                Run(timeline, step, fast);
            }
            catch (Exception ex)
            {
                Terramon.Instance.Logger.Error($"Couldn't show a battle event ({step.Kind}): {ex}");
            }
        }
    }

    private static int HitDelay(Step move)
    {
        if (move.Still)
            return StillHitDelay;
        return Terramon.DatabaseV2.GetMove(move.MoveId)?.Category switch
        {
            MoveCategory.Physical => PhysicalHitDelay,
            MoveCategory.Special => SpecialHitDelay,
            _ => StatusHitDelay
        };
    }

    private static void Finish(Timeline timeline)
    {
        // Everything has been shown, so the battle's own values are shown again
        Timelines.Remove(timeline);
    }

    private static void Run(Timeline timeline, Step step, bool fast)
    {
        switch (step.Kind)
        {
            case Kind.Move:
                if (!fast)
                    MoveVisuals.Play(step.Side, step.MoveId, step.Still, step.Miss);
                break;

            case Kind.HP:
            {
                var mon = step.Mon;
                if (mon == null)
                    break;
                ushort before;
                var after = step.Value.For(mon);
                lock (Sync)
                {
                    before = timeline.HP.TryGetValue(mon, out var shown) ? shown : mon.HP;
                    timeline.HP[mon] = after;
                }

                if (fast)
                    break;
                if (after < before && after > 0)
                    PokemonAnimations.Play(step.Side?.Provider, PokemonAnimState.Hurt);
                else if (after > before && before > 0)
                    // Healing that isn't from a move (items, berries, Leftovers): eats
                    PokemonAnimations.PlayIfQuiet(step.Side?.Provider, PokemonAnimState.Eat);
                else if (after == 0 && before > 0)
                    // The other side celebrates the knockout
                    PokemonAnimations.Play(step.Side?.Opposite?.Provider, PokemonAnimState.Pose);
                break;
            }

            case Kind.Status:
                lock (Sync)
                    timeline.Status[step.Mon] = step.Status;
                break;

            case Kind.Active:
                lock (Sync)
                    timeline.Active[step.SideIndex] = step.Mon;
                break;

            case Kind.Look:
                lock (Sync)
                    timeline.Looks[(step.SideIndex, step.Slot)] = (step.Into, step.IntoShiny);
                break;

            case Kind.Line:
                Say(timeline, step.Text, step.Color, step.Print, step.HistoryOnly);
                break;

            case Kind.Faint:
                lock (Sync)
                {
                    if (step.Mon != null)
                    {
                        timeline.HP[step.Mon] = 0;
                        timeline.Status[step.Mon] = NonVolatileStatus.Fnt;
                    }

                    if (step.ReleaseExp != null)
                    {
                        foreach (var mon in step.ReleaseExp)
                        {
                            if (mon != null)
                                timeline.Exp.Remove(mon);
                        }
                    }
                }

                Say(timeline, step.Text, step.Color, step.Print);
                // The EXP and level up lines come next, each with its own moment
                if (step.Extra != null)
                {
                    lock (Sync)
                    {
                        for (var i = step.Extra.Count - 1; i >= 0; i--)
                        {
                            var (text, color) = step.Extra[i];
                            timeline.Steps.AddFirst(new Step
                                { Kind = Kind.Line, Text = text, Color = color, Print = step.Print, Hold = HoldFor(text) });
                        }
                    }
                }

                break;

            case Kind.Turn:
                if (step.Print)
                    BattleLog.RecordTurn(timeline.Field);
                break;

            case Kind.End:
                step.Action?.Invoke();
                break;
        }
    }

    /// <summary>
    ///     How long a message stays on screen: long enough to read it, more for longer ones.
    /// </summary>
    private static int HoldFor(string text)
        => Math.Clamp(40 + (int)((StripTags(text)?.Length ?? 0) * 1.5f), LineHold, LongestHold);

    private static void Say(Timeline timeline, string text, Color color, bool print, bool historyOnly = false)
    {
        if (!print || string.IsNullOrEmpty(text))
            return;
        // Shown in the battle box and kept in the battle history (L), not in chat, so it isn't said twice
        BattleLog.Record(text, color, timeline.Field);
        if (historyOnly)
            return;
        lock (Sync)
            timeline.LastText = StripTags(text).Trim(' ', '»');
    }

    /// <summary>
    ///     Removes chat color tags ("[c/ff9a6a:Pikachu]" becomes "Pikachu") for places that don't draw them.
    /// </summary>
    public static string StripTags(string text)
        => string.IsNullOrEmpty(text)
            ? text
            : System.Text.RegularExpressions.Regex.Replace(text, @"\[c/[0-9a-fA-F]+:([^\]]*)\]", "$1");

    #endregion
}
