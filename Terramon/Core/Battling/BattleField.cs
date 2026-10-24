using Showdown.NET.Definitions;
using Terramon.Core.Battling.BattlePackets;
using Terramon.ID;

namespace Terramon.Core.Battling;
public sealed class BattleField
{
    public BattleSide A;
    public BattleSide B;
    public BattleWeather Weather;
    public FieldCondition Condition;
    public int Turn;

    /// <summary>
    ///     Points each side at the other through <see cref="BattleSide.Opposite" />. Call once both sides are set.
    /// </summary>
    public void LinkSides()
    {
        if (A == null || B == null)
            return;
        A.Opposite = B;
        B.Opposite = A;
        A.Field = B.Field = this;
    }

    /// <summary>
    ///     The side (1 or 2) of the local player in this battle, or 0 if they're only watching it.
    /// </summary>
    public int LocalSideIndex
    {
        get
        {
            if (Main.dedServ)
                return 0;
            var local = TerramonPlayer.LocalPlayer;
            return A?.Provider == local ? 1 : B?.Provider == local ? 2 : 0;
        }
    }

    public void ModifyBoosts(BoostModifierAction action)
    {
        for (int i = 0; i < 6; i++)
        {
            A[i].ModifyBoosts(action);
            B[i].ModifyBoosts(action);
        }
    }

    /// <summary>
    ///     Receives and handles <see cref="BattleAction"/>s, a basic structure containing information that must be replicated across all clients.
    ///     This method is called on all clients, but should only operate on this <see cref="BattleClient"/> instance.
    /// </summary>
    /// <param name="a">The received action.</param>
    /// <param name="r">The reader, used to read extra information depending on the <see cref="BattleAction.ID"/> of the action.</param>
    public void Receive(in BattleAction a, BinaryReader r, bool refine = false)
    {
        // Each event gets its moment in the turn's playback (see BattlePlayback), except the exact copies of the
        // secret half (refine), which only correct what the public half already queued
        var show = !refine && !Main.dedServ;
        switch (a.ID)
        {
            case BattleActionID.SetPokemonDetails:
                GetMon(r).Details = r.ReadPokemonDetails();
                break;
            case BattleActionID.SetPokemonSpecies:
                GetMon(r).Species = r.ReadUInt16();
                break;
            case BattleActionID.SetPokemonHP:
                ref var hurt = ref GetMon(r);
                var hp = r.ReadPokemonHP();
                // The HP bar goes down (and the Hurt animation plays) when the hit is shown, not right now
                if (show)
                    BattlePlayback.QueueHP(this, hurt.Side, hurt.Data, hp.HP, hp.MaxHP);
                hurt.HPData = hp;
                if (refine)
                    BattlePlayback.RefineHP(this, hurt.Data, hp.HP, hp.MaxHP);
                break;
            case BattleActionID.MoveAnimation:
                ref var user = ref GetMon(r);
                var moveId = r.ReadUInt16();
                user.LastMove = moveId;
                user.LastMoveTurn = Turn;
                if (show)
                    BattlePlayback.QueueMove(this, user.Side, moveId, a.Flags[0], a.Flags[1]);
                // The owner's game takes the PP right away. Showdown's next request sets the exact amount, but no
                // request comes after the move that ends the battle
                if (!Main.dedServ && user.Side?.Provider is TerramonPlayer { Player.whoAmI: var who } && who == Main.myPlayer)
                    user.Data?.UseMovePP((MoveID)moveId);
                break;
            case BattleActionID.SwitchPokemon:
                var switchedID = r.ReadPokemonID();
                ref var switchedIn = ref GetMon(switchedID);
                // The side keeps showing its current Pokémon until the switch is shown
                if (show && switchedIn.Side != null)
                    BattlePlayback.QueueSwitch(this, switchedID.Side, switchedIn.Side.ActivePokemon.Data,
                        switchedIn.Data);
                // A transformed Pokémon goes back to its own looks when it leaves the field (when the switch is shown)
                if (show && switchedIn.Side != null)
                {
                    for (var i = 0; i < switchedIn.Side.Team.Length; i++)
                    {
                        ref var leaving = ref switchedIn.Side.Team[i];
                        if (leaving.TransformedInto == 0)
                            continue;
                        BattlePlayback.HoldLook(this, switchedID.Side, i, leaving.TransformedInto,
                            leaving.TransformedShiny);
                        BattlePlayback.QueueLook(this, switchedID.Side, i, 0, false);
                    }
                }

                // Disable ends when the Pokémon leaves the field
                if (switchedIn.Side != null)
                    switchedIn.Side.ActivePokemon.DisabledMove = 0;
                // Stat stages stay with the Pokémon only while it's on the field
                if (switchedIn.Side.ActivePokemon.Data is { } left && left != switchedIn.Data)
                    left.StatStages = default;
                switchedIn.Side.ClearTransforms();
                switchedIn.SetAsActive();
                // Whatever was charging or recharging has left the field
                switchedIn.Side.Trapped = 0;
                break;
            case BattleActionID.ActionFail:
                break;
            case BattleActionID.PokemonWait:
                this[r.ReadByte()].Trapped = 1;
                break;
            case BattleActionID.SetPokemonStatus:
                ref var mon = ref GetMon(r);
                // The status badge and tint change when the status is shown
                if (show)
                    BattlePlayback.HoldStatus(this, mon.Data);
                if (a.Flags[0]) // is cure status
                    mon.CureStatus();
                else
                    mon.Status = (NonVolatileStatus)r.ReadByte();
                if (show)
                    BattlePlayback.QueueStatus(this, mon.Data, mon.Status);
                break;
            case BattleActionID.SetPokemonBoost: // all three possibilities are so disparate that they should probably be their own separate things tbh :sob:
                if (a.Flags[1]) // is swap
                {
                    var pair = r.ReadPokemonIDs();
                    ref var src = ref GetMon(pair.Source);
                    ref var tgt = ref GetMon(pair.Target);
                    HoldBoosts(show, src.Data, tgt.Data);
                    StatID stat = (StatID)r.ReadByte();
                    var srcValue = src.StatStages[stat];
                    var tgtValue = tgt.StatStages[stat];
                    src.SetStat(stat, tgtValue, true);
                    tgt.SetStat(stat, srcValue, true);
                    QueueBoosts(show, src.Data, tgt.Data);
                }
                else if (a.Flags[2]) // is copy
                {
                    var pair = r.ReadPokemonIDs();
                    ref var src = ref GetMon(pair.Source);
                    ref var tgt = ref GetMon(pair.Target);
                    HoldBoosts(show, tgt.Data);
                    tgt.PackedStats = src.PackedStats;
                    QueueBoosts(show, tgt.Data);
                }
                else
                {
                    ref var boosted = ref GetMon(r);
                    HoldBoosts(show, boosted.Data);
                    boosted.SetStat(r.ReadByte(), r.ReadSByte(), a.Flags[0]);
                    QueueBoosts(show, boosted.Data);
                }
                break;
            case BattleActionID.AllPokemonBoost:
                if (a.Flags[0]) // is clear-all
                {
                    for (var i = 0; i < 6; i++)
                        HoldBoosts(show, A[i].Data, B[i].Data);
                    ModifyBoosts(BoostModifierAction.ClearAll);
                    for (var i = 0; i < 6; i++)
                        QueueBoosts(show, A[i].Data, B[i].Data);
                }
                else
                {
                    ref var modified = ref GetMon(r);
                    HoldBoosts(show, modified.Data);
                    modified.ModifyBoosts((BoostModifierAction)r.ReadByte());
                    QueueBoosts(show, modified.Data);
                }
                break;
            case BattleActionID.AdvanceTurn:
                Turn++;
                A.AdvanceTurn();
                B.AdvanceTurn();
                if (show)
                    BattlePlayback.QueueTurn(this, LocalSideIndex != 0);
                break;
            case BattleActionID.SetWeather:
                if (show)
                    BattlePlayback.HoldField(this);
                Weather = (BattleWeather)r.ReadByte();
                if (show)
                    BattlePlayback.QueueField(this);
                break;
            case BattleActionID.SetFieldCondition:
                if (show)
                    BattlePlayback.HoldField(this);
                Condition = (FieldCondition)r.ReadByte();
                if (show)
                    BattlePlayback.QueueField(this);
                break;
            case BattleActionID.SetSideCondition:
                if (show)
                    BattlePlayback.HoldField(this);
                if (a.Flags[0])
                {
                    (A.Conditions, B.Conditions) = (B.Conditions, A.Conditions);
                }
                else
                {
                    var side = this[r.ReadByte()];
                    var bit = 1u << r.ReadByte();
                    if (a.Flags[1]) // is end
                        side.Conditions &= ~bit;
                    else
                        side.Conditions |= bit;
                }

                if (show)
                    BattlePlayback.QueueField(this);
                break;
            case BattleActionID.SetPokemonVolatile:
                mon = ref GetMon(r);
                var vol = (VolatileEffect)r.ReadByte();
                if (a.Flags[0]) // is start
                    mon.Volatiles.Add(vol);
                else
                    mon.Volatiles.Remove(vol);
                // Disable blocks the last move the Pokémon used
                if (vol == VolatileEffect.Disable)
                {
                    mon.DisabledMove = a.Flags[0] ? mon.LastMove : (ushort)0;
                    mon.DisabledTurn = Turn;
                    mon.DisabledAfterMoving = mon.LastMoveTurn == Turn;
                }
                // todo: certain volatiles have more effects than just being set as flags on the mon
                break;
            case BattleActionID.PokemonItem:
                mon = ref GetMon(r);
                if (a.Flags[0]) // is reveal
                    mon.HeldItem = r.ReadUInt16();
                else
                    mon.HeldItem = 0;
                break;
            case BattleActionID.PokemonAbility:
                mon = ref GetMon(r);
                if (a.Flags[0]) // is reveal
                    mon.Ability = (AbilityID)r.ReadUInt16();
                else
                    mon.AbilitySuppressed = true;
                break;
            case BattleActionID.PokemonTransformDetails:
                var transformedID = r.ReadPokemonID();
                ref var transformed = ref GetMon(transformedID);
                // It keeps its own looks until its Transform is shown
                if (show)
                    BattlePlayback.HoldLook(this, transformedID.Side, transformedID.Slot, transformed.TransformedInto,
                        transformed.TransformedShiny);
                transformed.TransformedInto = r.ReadUInt16();
                transformed.TransformedShiny = a.Flags[0];
                if (show)
                    BattlePlayback.QueueLook(this, transformedID.Side, transformedID.Slot, transformed.TransformedInto,
                        transformed.TransformedShiny);
                break;
            case BattleActionID.LogLine:
                // Always read, so the actions after it stay in place
                var line = BattleLogLine.Read(r);
                if (show)
                {
                    var viewer = LocalSideIndex;
                    BattlePlayback.QueueLine(this, line.Format(viewer), line.Color, line.Key, viewer != 0);
                }

                break;
        }
    }

    /// <summary>
    ///     Reads and applies every action in <paramref name="r" /> until its footer.
    /// </summary>
    /// <param name="r">The actions.</param>
    /// <param name="refine">
    ///     Whether these are the exact copies (secret half) of actions already received, which only correct HP numbers
    ///     and get no moment of their own in the playback.
    /// </param>
    public void Receive(BinaryReader r, bool refine = false)
    {
        while (true)
        {
            var actionID = (BattleActionID)r.ReadByte();
            if (actionID == BattleActionID.None)
                break;
            var flags = (BitsByte)r.ReadByte();
            var action = new BattleAction(actionID, flags);
            Receive(in action, r, refine);
        }
    }

    private void HoldBoosts(bool show, params PokemonData[] mons)
    {
        if (!show)
            return;
        foreach (var mon in mons)
            BattlePlayback.HoldBoosts(this, mon);
    }

    private void QueueBoosts(bool show, params PokemonData[] mons)
    {
        if (!show)
            return;
        foreach (var mon in mons)
            BattlePlayback.QueueBoosts(this, mon);
    }

    /// <summary>
    ///     How many more turns (counting this one) Disable keeps blocking the move. It lasts 4 turns after the one it
    ///     was used in if the Pokémon had already moved, or 4 turns counting that one if it hadn't.
    /// </summary>
    public int DisabledTurnsLeft(in BattlePokemon mon)
    {
        var total = mon.DisabledAfterMoving ? 5 : 4;
        return Math.Clamp(total - (Turn - mon.DisabledTurn), 1, 4);
    }

    public ref BattlePokemon GetMon(BinaryReader r) => ref GetMon(r.ReadPokemonID());
    public ref BattlePokemon GetMon(SimpleMon ID) => ref this[ID.Side][ID.Slot];

    public BattleSide this[int side]
    {
        get
        {
            if (side == 1)
                return A;
            else if (side == 2)
                return B;
            else
                return null;
        }
    }
}

public enum BattleWeather : byte
{
    None,
    RainDance,
    DesolateLand,
    PrimordialSea,
    SunnyDay,
    Sandstorm,
    Hail,
    Snowscape,
    DeltaStream,
    StormSurge,
    DesertedDunes,
}

public enum FieldCondition : byte
{
    None,
    ElectricTerrain,
    PsychicTerrain,
    GrassyTerrain,
    MistyTerrain,
    MudSport,
    WaterSport,
    Gravity,
    TrickRoom,
    AnfieldAtmosphere,
}
