using EasyPacketsLib;

namespace Terramon.Core.Battling.BattlePackets;

public struct ShowdownRequestRpc(
    ShowdownRequest request,
    int[] switchOrder = null,
    int ppSlot = -1,
    Dictionary<string, int> pp = null) : IEasyPacket
{
    private ShowdownRequest _request = request;

    // Party slot of each Pokémon in Showdown's current team order (see BattleClient.SwitchOrder)
    private int[] _switchOrder = switchOrder;

    // Party slot of the active Pokémon and the PP Showdown has left for its moves (by Showdown move id),
    // so the owner's party loses PP in battle too. -1 when the request doesn't say.
    private int _ppSlot = pp == null ? -1 : ppSlot;
    private Dictionary<string, int> _pp = pp;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write((byte)_request);
        var count = _switchOrder?.Length ?? 0;
        writer.Write((byte)count);
        for (var i = 0; i < count; i++)
            writer.Write((sbyte)_switchOrder![i]);

        writer.Write((sbyte)_ppSlot);
        if (_ppSlot < 0)
            return;
        writer.Write((byte)_pp!.Count);
        foreach (var (id, left) in _pp)
        {
            writer.Write(id);
            writer.Write((byte)Math.Clamp(left, 0, byte.MaxValue));
        }
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _request = (ShowdownRequest)reader.ReadByte();
        var count = reader.ReadByte();
        _switchOrder = count == 0 ? null : new int[count];
        for (var i = 0; i < count; i++)
            _switchOrder![i] = reader.ReadSByte();

        _ppSlot = reader.ReadSByte();
        _pp = null;
        if (_ppSlot < 0)
            return;
        var moves = reader.ReadByte();
        _pp = new Dictionary<string, int>(moves);
        for (var i = 0; i < moves; i++)
        {
            var id = reader.ReadString();
            _pp[id] = reader.ReadByte();
        }
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

        var party = TerramonPlayer.LocalPlayer.Party;
        if (_ppSlot >= 0 && _ppSlot < party.Length)
            BattleManager.ApplyPP(party[_ppSlot], _pp);
    }
}
