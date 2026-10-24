using EasyPacketsLib;
using Terramon.Content.NPCs.Trainers;

namespace Terramon.Core.Systems;

/// <summary>
///     The highest level a player's Pokémon obey at, set by the Gym badges that character has (like the badges in
///     the main games). Pokémon above it can still be caught, traded and trained, but sometimes ignore orders in
///     battle. Champions have no limit.
/// </summary>
public static class LevelCap
{
    /// <summary>
    ///     The limit for 0 to 8 badges.
    /// </summary>
    private static readonly int[] ByBadges = [15, 21, 27, 33, 39, 45, 50, 55, 62];

    /// <summary>
    ///     The limit for this character, worked out from their own badges.
    /// </summary>
    public static int Compute(Player player)
    {
        var gym = player.GetModPlayer<GymPlayer>();
        if (gym.HallOfFame.Count > 0)
            return Terramon.MaxPokemonLevel;
        return ByBadges[Math.Clamp(League.BadgeCount(player), 0, ByBadges.Length - 1)];
    }

    /// <summary>
    ///     The limit for this player. Other players' badges only live on their own game, so the server and other
    ///     clients use the limit their game sent.
    /// </summary>
    public static int Of(Player player)
    {
        if (Main.netMode == NetmodeID.SinglePlayer || player.whoAmI == Main.myPlayer)
            return Compute(player);
        var synced = player.GetModPlayer<GymPlayer>().SyncedLevelCap;
        return synced > 0 ? synced : ByBadges[0];
    }

    public static bool HasLimit(int cap) => cap < Terramon.MaxPokemonLevel;

    /// <summary>
    ///     The >eval that makes the Pokémon of one side of a Showdown battle disobey above the given level. The check
    ///     follows the main games: the further above the limit, the more often it happens. A disobeying Pokémon
    ///     either uses another of its moves or does nothing that turn (loafing, turning away or napping).
    /// </summary>
    public static IEnumerable<string> ShowdownCommands(int side, int cap)
    {
        // Installed once per battle, for every side that has a limit
        yield return ">eval if (!battle.terramonObey) { battle.terramonObey = true; " +
                     "battle.onEvent('BeforeMove', battle.format, -100, function (pokemon, target, move) { " +
                     "const cap = pokemon.m && pokemon.m.obeyCap; " +
                     "if (!cap || pokemon.level <= cap || !move || move.id === 'struggle' || move.isExternal) return; " +
                     "if (pokemon.volatiles['lockedmove'] || pokemon.volatiles['twoturnmove']) return; " +
                     "if (this.random(pokemon.level + cap) < cap) return; " +
                     "const others = pokemon.moveSlots.filter(s => s.id !== move.id && s.pp > 0 && !s.disabled); " +
                     "if (others.length && this.randomChance(1, 2)) { " +
                     "this.add('cant', pokemon, 'disobeymove'); " +
                     "this.actions.useMove(this.sample(others).id, pokemon); return false; } " +
                     "this.add('cant', pokemon, this.sample(['disobey', 'loaf', 'nap'])); return false; }); }";
        yield return $">eval for (const p of battle.sides[{side - 1}].pokemon) p.m.obeyCap = {cap}";
    }
}

/// <summary>
///     Sends a player's level limit to the server and the other clients, since badges are only saved by the
///     player's own game.
/// </summary>
public struct LevelCapRpc(byte player, byte cap) : IEasyPacket
{
    private byte _player = player;
    private byte _cap = cap;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_player);
        writer.Write(_cap);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _player = reader.ReadByte();
        _cap = reader.ReadByte();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        if (_player >= Main.maxPlayers)
            return;
        Main.player[_player].GetModPlayer<GymPlayer>().SyncedLevelCap = _cap;
    }
}
