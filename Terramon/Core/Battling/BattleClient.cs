using EasyPacketsLib;
using Terramon.Core.Battling.BattlePackets;

namespace Terramon.Core.Battling;

/// <summary>
///     Contains information related to the public version of a Showdown battle, instanced per-player to allow for visual replication across all clients.
///     On singleplayer, the methods used to interface with the <see cref="BattleManager"/> interact directly, otherwise they send packets.
/// </summary>
public sealed class BattleClient(IBattleProvider provider)
{
    public IBattleProvider Provider { get; } = provider;
    public BattleParticipant ID => Provider.ID;
    private IBattleProvider _foe;
    public IBattleProvider Foe
    {
        get => _foe;
        set
        {
            if (value is null)
                State = ClientBattleState.None;
            _foe = value;
        }
    }
    public BattleParticipant FoeID => _foe?.ID ?? BattleParticipant.None;
    public ClientBattleState State;
    public ShowdownRequest CurrentRequest;

    /// <summary>
    ///     The latest request that asked for a choice, restored when Showdown rejects a choice so it can be made again.
    /// </summary>
    public ShowdownRequest LastRequest;

    // Instance is shared between both battlers
    public BattleField Battle;

    public BattleSide Side => Battle is null ? null : Provider == Battle.A.Provider ? Battle[1] : Battle[2];

    // These arent't used by remote clients
    public byte Pick;
    public bool TieRequest;

    // This is only used by the server
    public string CachedTeamSpec;

    /// <summary>
    ///     The party slot of each Pokémon in the order Showdown currently keeps them, which changes after every switch.
    ///     Null when unknown (multiplayer clients).
    /// </summary>
    public int[] SwitchOrder;

    //public string Name => Provider.BattleName;
    //public Entity Entity => Provider.SyncedEntity;
    public bool BattleOngoing
    {
        get
        {
            return State == ClientBattleState.Ongoing;
        }
    }
    //public bool IsLocal => Provider.IsLocal;
    public int SideIndex
    {
        get
        {
            if (!BattleOngoing)
                return 0;
            return Battle.A.Provider == Provider ? 1 : 2;
        }
    }

    public static bool LocalBattleOngoing
    {
        get
        {
            var local = LocalClient;
            return local is { BattleOngoing: true };
        }
    }

    public static BattleClient LocalClient
    {
        get
        {
            var modPlayer = TerramonPlayer.LocalPlayer;
            modPlayer._battleClient ??= new BattleClient(modPlayer);
            return modPlayer._battleClient;
        }
    }

    public bool CanMakeChoice(BattleChoice choice)
    {
        if (State != ClientBattleState.Ongoing)
            return false;
        // A forced switch (e.g. after fainting mid-charge) must always be answerable
        if (Side.Trapped != 0 && CurrentRequest != ShowdownRequest.ForcedSwitch)
            return false;
        // Wait for the Poké Ball to finish shaking
        if (Foe is Content.NPCs.PokemonNPC { PlasmaState: true })
            return false;
        // The local player chooses once the last turn has been shown (NPCs answer right away)
        if (!Main.dedServ && Provider is TerramonPlayer { Player.whoAmI: var who } && who == Main.myPlayer &&
            BattlePlayback.IsBusy(Battle))
            return false;

        return CurrentRequest switch
        {
            ShowdownRequest.None => false,
            ShowdownRequest.Any => true,
            ShowdownRequest.Wait => false,
            ShowdownRequest.ForcedSwitch => choice is BattleChoice.Default or BattleChoice.Switch,
            _ => false,
        };
    }

    public bool MakeChoice(BattleChoice choice, int operand = -1)
    {
        if (!CanMakeChoice(choice))
            return false;

        // Showdown switches by its own team order, not by party slot
        if (choice == BattleChoice.Switch && SwitchOrder != null)
        {
            operand = Array.IndexOf(SwitchOrder, operand);
            if (operand < 0)
                return false;
        }

        CurrentRequest = ShowdownRequest.Wait;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = new BattleChoiceRpc(choice, (byte)operand);
            Terramon.Instance.SendPacket(packet);
        }
        else
        {
            BattleManager.Instance.HandleChoice(ID, choice, (byte)operand);
        }

        return true;
    }

    public void BattleStopped()
    {
        // Sleep, freeze and stat stages only last during battle, so nobody stays asleep in the world afterwards
        var team = Provider is TerramonPlayer or Content.NPCs.PokemonNPC ? Provider.GetBattleTeam() : null;
        foreach (var mon in team ?? [])
        {
            if (mon?.Status is NonVolatileStatus.Slp or NonVolatileStatus.Frz)
                mon.Status = NonVolatileStatus.None;
            if (mon != null)
                mon.StatStages = default;
        }

        Provider.StopBattleEffects();
        Battle = null;
        Pick = 0;
        TieRequest = false;
        Foe = null;
        CurrentRequest = ShowdownRequest.None;
        LastRequest = ShowdownRequest.None;
        SwitchOrder = null;
    }
}

public enum ClientBattleState : byte
{
    None,
    Requested,
    PollingSlot,
    SetSlot,
    PollingTeam,
    SetTeam,
    Ongoing,
}
