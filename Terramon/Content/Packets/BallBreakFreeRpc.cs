using EasyPacketsLib;
using Terramon.Content.NPCs;
using Terramon.Core.Battling;

namespace Terramon.Content.Packets;

/// <summary>
///     Sent by the thrower's client to the server when a wild Pokémon it's battling breaks free of its Poké Ball.
///     The thrower decides the catch (it also adds the Pokémon on a success), so the server follows its result:
///     the Pokémon comes back out and the thrower's turn is used up.
/// </summary>
public struct BallBreakFreeRpc(byte npc, Vector2 position) : IEasyPacket
{
    private byte _npc = npc;
    private Vector2 _position = position;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_npc);
        writer.WriteVector2(_position);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _npc = reader.ReadByte();
        _position = reader.ReadVector2();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        if (Main.netMode != NetmodeID.Server || _npc >= Main.maxNPCs)
            return;

        // Only the player battling this Pokémon can make it break free
        var npc = Main.npc[_npc];
        if (!npc.active || npc.ModNPC is not PokemonNPC { PlasmaState: true } pokemon ||
            pokemon.BattleClient?.Foe is not TerramonPlayer thrower || thrower.Player.whoAmI != sender.WhoAmI)
            return;

        pokemon.BreakFree(_position);
        BattleManager.Instance.SkipTurn(thrower.BattleClient);
    }
}
