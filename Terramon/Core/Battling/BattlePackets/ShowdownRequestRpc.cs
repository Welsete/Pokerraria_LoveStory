using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

public struct ShowdownRequestRpc(ShowdownRequest request, int[] switchOrder = null) : IEasyPacket
{
    private ShowdownRequest _request = request;

    // Party slot of each Pokémon in Showdown's current team order (see BattleClient.SwitchOrder)
    private int[] _switchOrder = switchOrder;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_request);
        var count = _switchOrder?.Length ?? 0;
        writer.Write((byte)count);
        for (var i = 0; i < count; i++)
            writer.Write((sbyte)_switchOrder![i]);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _request = (ShowdownRequest)reader.ReadByte();
        var count = reader.ReadByte();
        _switchOrder = count == 0 ? null : new int[count];
        for (var i = 0; i < count; i++)
            _switchOrder![i] = reader.ReadSByte();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        // Sent from server to target client only
        handled = true;
        var client = BattleClient.LocalClient;
        client.CurrentRequest = _request;
        if (_request != ShowdownRequest.Wait)
            client.LastRequest = _request;
        if (_switchOrder != null)
            client.SwitchOrder = _switchOrder;
    }
}
