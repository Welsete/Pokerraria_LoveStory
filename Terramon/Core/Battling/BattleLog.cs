using Showdown.NET.Protocol;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Localization;

namespace Terramon.Core.Battling;

/// <summary>
///     Writes what happens in a battle (moves used, effectiveness, stat changes...) to the chat.
///     The lines are written into the battle's actions (<see cref="BattleActionID.LogLine" />), so every client shows them
///     at their moment in the turn's playback (see <see cref="BattlePlayback" />), in order with the hits.
/// </summary>
public static class BattleLog
{
    private const string Prefix = "Mods.Terramon.Battle.Log.";
    private static readonly Color LogColor = new(200, 200, 215);
    private static readonly Color TurnColor = new(255, 210, 90);

    /// <summary>
    ///     Everything logged in the local player's latest battle, shown by the battle history window.
    ///     Lock it before reading, since battles run on a background thread.
    /// </summary>
    public static readonly List<(string Text, Color Color)> History = [];

    /// <summary>
    ///     Goes up every time <see cref="History" /> changes, so the window knows when to refresh.
    /// </summary>
    public static int HistoryVersion { get; private set; }

    private static BattleField _historyBattle;
    private static int _turn;

    /// <summary>
    ///     Adds a line to the battle history. A line from a new battle clears the previous one.
    /// </summary>
    public static void Record(string text, Color color, BattleField battle = null)
    {
        battle ??= BattleClient.LocalClient?.Battle;
        lock (History)
        {
            StartHistory(battle);
            History.Add((text, color));
            HistoryVersion++;
        }
    }

    /// <summary>
    ///     Adds the "Turn N" header of a new turn to the battle history.
    /// </summary>
    public static void RecordTurn(BattleField battle)
    {
        lock (History)
        {
            StartHistory(battle);
            _turn++;
            History.Add((Language.GetTextValue("Mods.Terramon.GUI.BattleLog.Turn", _turn), TurnColor));
            HistoryVersion++;
        }
    }

    // A line from a new battle clears the previous one's history. Call while holding the History lock
    private static void StartHistory(BattleField battle)
    {
        if (battle == null || battle == _historyBattle)
            return;
        History.Clear();
        _historyBattle = battle;
        _turn = 0;
    }

    /// <summary>
    ///     Shows a message in chat and, while the local player is battling, keeps it in the battle history.
    /// </summary>
    public static void Say(string text, Color? color = null)
    {
        Main.NewText(text, color ?? Color.White);
        if (BattleClient.LocalBattleOngoing)
            Record(text, color ?? Color.White);
    }

    /// <summary>
    ///     Wraps text in a chat color tag, e.g. to highlight a Pokémon's name or a number.
    /// </summary>
    public static string Highlight(object text, string hex) => $"[c/{hex}:{text}]";

    /// <summary>
    ///     Highlights a Pokémon's name: blue for the local player's own, orange for everyone else's.
    /// </summary>
    public static string HighlightMon(string name, bool own) => Highlight(name, own ? AllyHex : FoeHex);

    private const string AllyHex = "7fc8ff";
    private const string FoeHex = "ff9a6a";
    private const string MoveHex = "ffd75e";
    private const string DamageHex = "ff6b6b";
    private const string HealHex = "6ee06e";

    private static readonly Color Good = new(110, 224, 110);
    private static readonly Color Bad = new(255, 120, 110);
    private static readonly Color Notable = new(255, 170, 70);
    private static readonly Color Muted = new(160, 160, 175);
    private static readonly Color StatusColor = new(200, 150, 255);

    // Sides whose next "must recharge" line is hidden, because their turn was used up on purpose (e.g. by a Bag item)
    private static readonly HashSet<(BattleInstance, int Side)> SkippedTurns = [];

    /// <summary>
    ///     Hides the "must recharge" line of a side whose turn is skipped with the mustrecharge trick this turn.
    /// </summary>
    public static void ExpectSkippedTurn(BattleInstance source, int side)
    {
        lock (SkippedTurns)
            SkippedTurns.Add((source, side));
    }

    // Last known HP of every Pokémon, used to tell how much damage a hit did
    private static readonly Dictionary<(BattleInstance, int Side, int Slot), (int HP, int MaxHP)> LastHP = [];

    /// <summary>
    ///     Called for every protocol element of a battle (the secret, exact-HP version of split ones), on the machine
    ///     running it. The lines are written to <paramref name="w" />, the battle's public actions.
    /// </summary>
    public static void Handle(BattleInstance source, ProtocolElement element, BinaryWriter w)
    {
        void Send(string key, Color color, params object[] args) => BattleLog.Send(w, key, color, args);

        switch (element)
        {
            case CantElement { Reason: "recharge" } skipped when ConsumeSkippedTurn(source, new SimpleMon(skipped.Pokemon).Side):
                break;
            case TurnElement:
                // The "Turn N" header is added by each client when the turn starts (see BattlePlayback)
                lock (SkippedTurns)
                    SkippedTurns.RemoveWhere(t => t.Item1 == source);
                break;
            case SwitchElement sw:
                TrackHP(source, sw.Pokemon, sw.HP);
                Send("SwitchedIn", LogColor, Mon(source, sw.Pokemon));
                break;
            case DragElement drag:
                TrackHP(source, drag.Pokemon, drag.HP);
                Send("SwitchedIn", LogColor, Mon(source, drag.Pokemon));
                break;
            case DamageElement damage:
            {
                var lost = TrackHP(source, damage.Pokemon, damage.HP);
                if (lost > 0)
                    Send("Damage", LogColor, Mon(source, damage.Pokemon), Highlight(lost, DamageHex),
                        HPLeft(source, damage.Pokemon));
                break;
            }
            case HealElement heal:
            {
                var gained = -TrackHP(source, heal.Pokemon, heal.HP);
                if (gained > 0)
                    Send("Healed", LogColor, Mon(source, heal.Pokemon), Highlight(gained, HealHex),
                        HPLeft(source, heal.Pokemon));
                break;
            }
            case MoveElement move:
                Send("UsedMove", LogColor, Mon(source, move.Pokemon), Move(move.Move));
                if (StatusMoveEffect(move.Move) is { } effect)
                    Send("MoveEffect", Muted, effect);
                break;
            case SuperEffectiveElement:
                Send("SuperEffective", Notable);
                break;
            case ResistedElement:
                Send("Resisted", Muted);
                break;
            case ImmuneElement immune:
                Send("Immune", Muted, Mon(source, immune.Pokemon));
                break;
            case CritElement:
                Send("Crit", Notable);
                break;
            case MissElement miss:
                Send("Missed", Muted, Mon(source, miss.Source));
                break;
            case FailElement:
                Send("Failed", Muted);
                break;
            case BoostElement boost:
                Send(boost.Amount > 1 ? "StatRoseSharply" : "StatRose", Good, Mon(source, boost.Pokemon),
                    Stat(boost.Stat.ToString()));
                break;
            case UnboostElement unboost:
                Send(unboost.Amount > 1 ? "StatFellHarshly" : "StatFell", Bad, Mon(source, unboost.Pokemon),
                    Stat(unboost.Stat.ToString()));
                break;
            case StatusElement status:
                Send("Status." + status.Status, StatusVisuals.ColorOf(status.Status.ToString()) ?? StatusColor,
                    Mon(source, status.Pokemon));
                break;
            case TransformElement transform:
            {
                var target = new SimpleMon(transform.Species);
                var species = (ushort)(source.Omniscient[target.Side]?[target.Slot].Species ?? 0);
                if (species != 0)
                    Send("Transformed", Notable, Mon(source, transform.Pokemon),
                        Terramon.DatabaseV2.GetLocalizedPokemonNameDirect(species));
                break;
            }
            case CureStatusElement cure:
                Send("Cured", Good, Mon(source, cure.Pokemon));
                break;
            case CantElement cant:
                Send("Cant." + cant.Reason, StatusVisuals.ColorOf(cant.Reason.ToString()) ?? StatusColor,
                    Mon(source, cant.Pokemon));
                break;
        }
    }

    /// <summary>
    ///     Logs an accuracy or evasion change, which Showdown.NET can't parse as a normal boost.
    /// </summary>
    public static void AccuracyStage(BattleInstance source, string kind, string showdownMon, string stat, int amount)
    {
        if (kind == "-setboost")
            return;
        stat = stat == "accuracy" ? "Accuracy" : "Evasion";
        if (kind == "-boost")
            Send(amount > 1 ? "StatRoseSharply" : "StatRose", Good, Mon(source, showdownMon), Stat(stat));
        else
            Send(amount > 1 ? "StatFellHarshly" : "StatFell", Bad, Mon(source, showdownMon), Stat(stat));
    }

    private static bool ConsumeSkippedTurn(BattleInstance source, int side)
    {
        lock (SkippedTurns)
            return SkippedTurns.Remove((source, side));
    }

    /// <summary>
    ///     Remembers a Pokémon's new HP and returns how much it went down by (negative if it went up).
    /// </summary>
    private static int TrackHP(BattleInstance source, string showdownMon, string showdownHP)
    {
        if (string.IsNullOrEmpty(showdownHP))
            return 0;

        var id = new SimpleMon(showdownMon);
        var key = (source, id.Side, id.Slot);
        var parts = showdownHP.Split(' ')[0].Split('/');
        if (!int.TryParse(parts[0], out var hp))
            return 0;
        var maxHP = parts.Length > 1 && int.TryParse(parts[1], out var max) ? max : 0;

        // Forget other battles, so the table doesn't grow forever
        if (LastHP.Keys.Any(k => k.Item1 != source))
            foreach (var old in LastHP.Keys.Where(k => k.Item1 != source).ToArray())
                LastHP.Remove(old);

        var change = LastHP.TryGetValue(key, out var last) ? last.HP - hp : 0;
        LastHP[key] = (hp, maxHP == 0 ? last.MaxHP : maxHP);
        return change;
    }

    private static string HPLeft(BattleInstance source, string showdownMon)
    {
        var id = new SimpleMon(showdownMon);
        return LastHP.TryGetValue((source, id.Side, id.Slot), out var hp) && hp.MaxHP > 0
            ? $"{hp.HP}/{hp.MaxHP}"
            : "?";
    }

    /// <summary>
    ///     A Pokémon's name in a log line, colored depending on who reads it.
    /// </summary>
    private readonly record struct MonName(int Side, string Name);

    private static MonName Mon(BattleInstance source, string showdownMon)
    {
        var id = new SimpleMon(showdownMon);
        return new MonName(id.Side, source.Omniscient[id.Side]?[id.Slot].PokeMessage ?? showdownMon);
    }

    // A move's name, translated by each client in its own language
    private static LogArg Move(string showdownMove)
    {
        var key = Enum.TryParse(showdownMove.Replace(" ", string.Empty), out MoveID move)
            ? $"Mods.Terramon.Moves.{move}.DisplayName"
            : null;
        return key != null && Language.Exists(key)
            ? LogArg.Key(key, MoveHex)
            : LogArg.Literal(showdownMove, MoveHex);
    }

    /// <summary>
    ///     What a status move does, so players know why nothing got hit. Null for moves that deal damage.
    /// </summary>
    private static object StatusMoveEffect(string showdownMove)
    {
        if (!Enum.TryParse(showdownMove.Replace(" ", string.Empty).Replace("-", string.Empty), out MoveID move) ||
            Terramon.DatabaseV2.GetMove(move) is not { Category: MoveCategory.Status })
            return null;
        var key = $"Mods.Terramon.Moves.{move}.FlavorText";
        return Language.Exists(key) ? LogArg.Key(key) : null;
    }

    private static LogArg Stat(string stat)
    {
        var key = Prefix + "Stats." + stat;
        return Language.Exists(key) ? LogArg.Key(key) : LogArg.Literal(stat);
    }

    /// <summary>
    ///     Writes a battle line to the battle's actions. Lines without a translation aren't worth logging (e.g. an
    ///     unknown reason a move failed).
    /// </summary>
    private static void Send(BinaryWriter w, string key, Color color, params object[] args)
    {
        key = Prefix + key;
        if (w == null || !Language.Exists(key))
            return;

        var line = new BattleLogLine(key, color, args.Select(a => a switch
        {
            LogArg arg => arg,
            MonName mon => LogArg.Mon(mon.Side, mon.Name),
            _ => LogArg.Literal(a?.ToString() ?? string.Empty)
        }).ToArray());

        // Written whole, so a line that fails halfway never leaves half an action behind
        using var buffer = new MemoryStream();
        using (var lineWriter = new BinaryWriter(buffer, System.Text.Encoding.UTF8, true))
        {
            lineWriter.Write((byte)BattleActionID.LogLine);
            lineWriter.Write((byte)0);
            line.Write(lineWriter);
        }

        w.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}

/// <summary>
///     A value in a battle line: plain text, a translation key (so each player reads it in their own language) or a
///     Pokémon's name (highlighted blue for the reader's own Pokémon, orange for the others).
/// </summary>
public readonly record struct LogArg(byte Kind, string Text, string Hex, byte Side)
{
    public const byte LiteralKind = 0;
    public const byte KeyKind = 1;
    public const byte MonKind = 2;

    public static LogArg Literal(string text, string hex = null) => new(LiteralKind, text, hex, 0);
    public static LogArg Key(string key, string hex = null) => new(KeyKind, key, hex, 0);
    public static LogArg Mon(int side, string name) => new(MonKind, name, null, (byte)side);

    /// <summary>
    ///     The text of this value for someone reading from <paramref name="viewerSide" /> (0 if just watching).
    /// </summary>
    public string Format(int viewerSide)
    {
        var text = Kind == KeyKind ? Language.GetTextValue(Text) : Text ?? string.Empty;
        if (Kind == MonKind)
            return BattleLog.HighlightMon(text, Side == viewerSide);
        return string.IsNullOrEmpty(Hex) ? text : BattleLog.Highlight(text, Hex);
    }
}

/// <summary>
///     A battle line as it travels with the battle's actions: its translation key, color and values.
/// </summary>
public sealed class BattleLogLine(string key, Color color, LogArg[] args)
{
    public string Key { get; } = key;
    public Color Color { get; } = color;
    public LogArg[] Args { get; } = args;

    public void Write(BinaryWriter w)
    {
        w.Write(Key ?? string.Empty);
        w.Write(Color.PackedValue);
        var count = Math.Min(Args?.Length ?? 0, byte.MaxValue);
        w.Write((byte)count);
        for (var i = 0; i < count; i++)
        {
            var arg = Args![i];
            w.Write(arg.Kind);
            w.Write(arg.Text ?? string.Empty);
            w.Write(arg.Hex ?? string.Empty);
            w.Write(arg.Side);
        }
    }

    public static BattleLogLine Read(BinaryReader r)
    {
        var key = r.ReadString();
        var color = new Color { PackedValue = r.ReadUInt32() };
        var count = r.ReadByte();
        var args = new LogArg[count];
        for (var i = 0; i < count; i++)
        {
            var kind = r.ReadByte();
            var text = r.ReadString();
            var hex = r.ReadString();
            var side = r.ReadByte();
            args[i] = new LogArg(kind, text, hex.Length == 0 ? null : hex, side);
        }

        return new BattleLogLine(key, color, args);
    }

    /// <summary>
    ///     The line in the local language, for someone reading from <paramref name="viewerSide" /> (0 if just watching).
    /// </summary>
    public string Format(int viewerSide)
        => Language.GetTextValue(Key, Args.Select(a => (object)a.Format(viewerSide)).ToArray());
}
