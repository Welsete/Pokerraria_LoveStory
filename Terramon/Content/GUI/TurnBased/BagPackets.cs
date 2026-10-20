using EasyPacketsLib;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;

namespace Terramon.Content.GUI.TurnBased;

/// <summary>
///     Sent from a client to the server: use a recovery item from the Bag on a party slot, in place of this turn's action.
/// </summary>
public struct BagItemRpc(int itemType, byte partySlot, string pokemonName) : IEasyPacket
{
    private int _itemType = itemType;
    private byte _partySlot = partySlot;
    private string _pokemonName = pokemonName;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_itemType);
        writer.Write(_partySlot);
        writer.Write(_pokemonName ?? string.Empty);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _itemType = reader.ReadInt32();
        _partySlot = reader.ReadByte();
        _pokemonName = reader.ReadString();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        if (Main.netMode != NetmodeID.Server)
            return;

        var name = _pokemonName.Length > 40 ? _pokemonName[..40] : _pokemonName;
        var client = new BattleParticipant(sender.WhoAmI, BattleProviderType.Player).Client;
        var accepted = client != null && BattleManager.Instance.UseItem(client, _itemType, _partySlot, name);
        Terramon.Instance.SendPacket(new BagItemResultRpc(accepted, _itemType, _partySlot), sender.WhoAmI);
    }
}

/// <summary>
///     Sent from the server back to the client that asked to use a Bag item, saying whether it was used.
/// </summary>
public struct BagItemResultRpc(bool accepted, int itemType, byte partySlot) : IEasyPacket
{
    private bool _accepted = accepted;
    private int _itemType = itemType;
    private byte _partySlot = partySlot;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_accepted);
        writer.Write(_itemType);
        writer.Write(_partySlot);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _accepted = reader.ReadBoolean();
        _itemType = reader.ReadInt32();
        _partySlot = reader.ReadByte();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        TestBattleUI.BagItemResult(_accepted, _itemType, _partySlot);
    }
}
