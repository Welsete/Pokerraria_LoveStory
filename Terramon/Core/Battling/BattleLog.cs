using Showdown.NET.Protocol;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Chat;
using Terraria.Localization;

namespace Terramon.Core.Battling;

/// <summary>
///     Writes what happens in a battle (moves used, effectiveness, stat changes...) to the chat.
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
            if (battle != null && battle != _historyBattle)
            {
                History.Clear();
                _historyBattle = battle;
                _turn = 0;
            }

            History.Add((text, color));
            HistoryVersion++;
        }
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
    ///     Called for every protocol element of a battle (the secret, exact-HP version of split ones).
    /// </summary>
    public static void Handle(BattleInstance source, ProtocolElement element)
    {
        switch (element)
        {
            case CantElement { Reason: "recharge" } skipped when ConsumeSkippedTurn(source, new SimpleMon(skipped.Pokemon).Side):
                break;
            case TurnElement:
                lock (SkippedTurns)
                    SkippedTurns.RemoveWhere(t => t.Item1 == source);
                if (Main.dedServ)
                    break;
                Record(Language.GetTextValue("Mods.Terramon.GUI.BattleLog.Turn", _turn + 1), TurnColor,
                    source.Omniscient);
                _turn++;
                break;
            case SwitchElement sw:
                TrackHP(source, sw.Pokemon, sw.HP);
                Send(source, "SwitchedIn", LogColor, Mon(source, sw.Pokemon));
                break;
            case DragElement drag:
                TrackHP(source, drag.Pokemon, drag.HP);
                Send(source, "SwitchedIn", LogColor, Mon(source, drag.Pokemon));
                break;
            case DamageElement damage:
            {
                var lost = TrackHP(source, damage.Pokemon, damage.HP);
                if (lost > 0)
                    Send(source, "Damage", LogColor, Mon(source, damage.Pokemon), Highlight(lost, DamageHex),
                        HPLeft(source, damage.Pokemon));
                break;
            }
            case HealElement heal:
            {
                var gained = -TrackHP(source, heal.Pokemon, heal.HP);
                if (gained > 0)
                    Send(source, "Healed", LogColor, Mon(source, heal.Pokemon), Highlight(gained, HealHex),
                        HPLeft(source, heal.Pokemon));
                break;
            }
            case MoveElement move:
                Send(source, "UsedMove", LogColor, Mon(source, move.Pokemon), Highlight(Move(move.Move), MoveHex));
                break;
            case SuperEffectiveElement:
                Send(source, "SuperEffective", Notable);
                break;
            case ResistedElement:
                Send(source, "Resisted", Muted);
                break;
            case ImmuneElement immune:
                Send(source, "Immune", Muted, Mon(source, immune.Pokemon));
                break;
            case CritElement:
                Send(source, "Crit", Notable);
                break;
            case MissElement miss:
                Send(source, "Missed", Muted, Mon(source, miss.Source));
                break;
            case FailElement:
                Send(source, "Failed", Muted);
                break;
            case BoostElement boost:
                Send(source, boost.Amount > 1 ? "StatRoseSharply" : "StatRose", Good, Mon(source, boost.Pokemon),
                    Stat(boost.Stat.ToString()));
                break;
            case UnboostElement unboost:
                Send(source, unboost.Amount > 1 ? "StatFellHarshly" : "StatFell", Bad, Mon(source, unboost.Pokemon),
                    Stat(unboost.Stat.ToString()));
                break;
            case StatusElement status:
                Send(source, "Status." + status.Status, StatusVisuals.ColorOf(status.Status.ToString()) ?? StatusColor,
                    Mon(source, status.Pokemon));
                break;
            case CureStatusElement cure:
                Send(source, "Cured", Good, Mon(source, cure.Pokemon));
                break;
            case CantElement cant:
                Send(source, "Cant." + cant.Reason, StatusVisuals.ColorOf(cant.Reason.ToString()) ?? StatusColor,
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
            Send(source, amount > 1 ? "StatRoseSharply" : "StatRose", Good, Mon(source, showdownMon), Stat(stat));
        else
            Send(source, amount > 1 ? "StatFellHarshly" : "StatFell", Bad, Mon(source, showdownMon), Stat(stat));
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

    private static string Move(string showdownMove)
    {
        var key = Enum.TryParse(showdownMove.Replace(" ", string.Empty), out MoveID move)
            ? $"Mods.Terramon.Moves.{move}.DisplayName"
            : null;
        return key != null && Language.Exists(key) ? Language.GetTextValue(key) : showdownMove;
    }

    private static string Stat(string stat)
    {
        var key = Prefix + "Stats." + stat;
        return Language.Exists(key) ? Language.GetTextValue(key) : stat;
    }

    private static object[] Format(object[] args, int viewerSide)
        => args.Select(a => a is MonName mon ? HighlightMon(mon.Name, mon.Side == viewerSide) : a).ToArray();

    private static void Send(BattleInstance source, string key, Color color, params object[] args)
    {
        key = Prefix + key;
        if (!Language.Exists(key))
            return; // Not something worth logging (e.g. an unknown reason a move failed)

        if (!Main.dedServ)
        {
            var line = Language.GetTextValue(key, Format(args, BattleClient.LocalClient.SideIndex));
            Main.NewText(line, color);
            Record(line, color, source.Omniscient);
            return;
        }

        // Multiplayer: only the players in this battle see it, each with their own Pokémon highlighted
        for (var side = 1; side <= 2; side++)
        {
            var client = side == 1 ? source.ClientA : source.ClientB;
            if (client.Provider is not TerramonPlayer player)
                continue;
            var text = NetworkText.FromKey(key,
                Format(args, side).Select(a => NetworkText.FromLiteral(a.ToString())).ToArray());
            ChatHelper.SendChatMessageToClient(text, color, player.Player.whoAmI);
        }
    }
}
