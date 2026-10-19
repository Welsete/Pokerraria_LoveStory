using Showdown.NET.Protocol;
using Terramon.Core.Battling.BattlePackets;
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

    /// <summary>
    ///     Called for every public protocol element of a battle.
    /// </summary>
    public static void Handle(BattleInstance source, ProtocolElement element)
    {
        switch (element)
        {
            case MoveElement move:
                Send(source, "UsedMove", Mon(source, move.Pokemon), Move(move.Move));
                break;
            case SuperEffectiveElement:
                Send(source, "SuperEffective");
                break;
            case ResistedElement:
                Send(source, "Resisted");
                break;
            case ImmuneElement immune:
                Send(source, "Immune", Mon(source, immune.Pokemon));
                break;
            case CritElement:
                Send(source, "Crit");
                break;
            case MissElement miss:
                Send(source, "Missed", Mon(source, miss.Source));
                break;
            case FailElement:
                Send(source, "Failed");
                break;
            case BoostElement boost:
                Send(source, boost.Amount > 1 ? "StatRoseSharply" : "StatRose", Mon(source, boost.Pokemon),
                    Stat(boost.Stat.ToString()));
                break;
            case UnboostElement unboost:
                Send(source, unboost.Amount > 1 ? "StatFellHarshly" : "StatFell", Mon(source, unboost.Pokemon),
                    Stat(unboost.Stat.ToString()));
                break;
            case StatusElement status:
                Send(source, "Status." + status.Status, Mon(source, status.Pokemon));
                break;
            case CureStatusElement cure:
                Send(source, "Cured", Mon(source, cure.Pokemon));
                break;
            case CantElement cant:
                Send(source, "Cant." + cant.Reason, Mon(source, cant.Pokemon));
                break;
        }
    }

    private static string Mon(BattleInstance source, string showdownMon)
    {
        var id = new SimpleMon(showdownMon);
        return source.Omniscient[id.Side]?[id.Slot].PokeMessage ?? showdownMon;
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

    private static void Send(BattleInstance source, string key, params object[] args)
    {
        key = Prefix + key;
        if (!Language.Exists(key))
            return; // Not something worth logging (e.g. an unknown reason a move failed)

        if (!Main.dedServ)
        {
            Main.NewText(Language.GetTextValue(key, args), LogColor);
            return;
        }

        // Multiplayer: only the players in this battle see it
        var text = NetworkText.FromKey(key, args.Select(a => NetworkText.FromLiteral(a.ToString())).ToArray());
        foreach (var client in new[] { source.ClientA, source.ClientB })
        {
            if (client.Provider is TerramonPlayer player)
                ChatHelper.SendChatMessageToClient(text, LogColor, player.Player.whoAmI);
        }
    }
}
