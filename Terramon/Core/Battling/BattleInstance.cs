using Showdown.NET.Definitions;
using Showdown.NET.Protocol;
using Showdown.NET.Simulator;
using System.Runtime.InteropServices;
using System.Text;
using EasyPacketsLib;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;

namespace Terramon.Core.Battling;

public sealed class BattleInstance
{
    public const ConsoleColor BattleAction = ConsoleColor.Yellow;
    public const ConsoleColor BattleFollowup = ConsoleColor.DarkYellow;
    public const ConsoleColor BattleReceive = ConsoleColor.Magenta;
    public const ConsoleColor BattleReceiveFollowup = ConsoleColor.DarkMagenta;
    public const ConsoleColor Meta = ConsoleColor.Cyan;
    public const ConsoleColor MetaProgress = ConsoleColor.DarkCyan;
    public const ConsoleColor MetaFollowup = ConsoleColor.DarkGray;
    public const ConsoleColor ChronoAction = ConsoleColor.Blue;
    public const ConsoleColor FieldAction = ConsoleColor.DarkBlue;
    public const ConsoleColor Faint = ConsoleColor.Red;
    public const ConsoleColor Error = ConsoleColor.DarkRed;
    public const ConsoleColor Win = ConsoleColor.Green;
    public const ConsoleColor NotWin = ConsoleColor.DarkGreen;

    public static BattleInstance Create(BattleClient a, BattleClient b)
    {
        var pa = a.Provider;
        var pb = b.Provider;

        var bf = new BattleField
        {
            A = new BattleSide(pa),
            B = new BattleSide(pb),
        };

        a.Battle = b.Battle = bf;
        a.Foe = pb;
        b.Foe = pa;

        return new BattleInstance
        {
            ClientA = a,
            ClientB = b,
            Omniscient = bf,
        };
    }
    public static void Destroy(BattleInstance i)
    {
        i.Stop();
        i.ClientA.Battle = i.ClientB.Battle = null;
        i.ClientA.Foe = i.ClientB.Foe = null;
        i.ClientA.State = i.ClientB.State = ClientBattleState.None;
    }

    public BattleState State;
    public BattleField Omniscient; // omniscient viewer
    public BattleClient ClientA; // requester
    public BattleClient ClientB; // requestee
    // 1-indices
    public BattleStream Stream; // battle stream

    /// <summary>
    ///     The side (1 or 2) whose side update is being read right now, or 0 outside of one. Errors belong to that side.
    /// </summary>
    public int SideUpdatePlayer;

    public bool ShouldStart =>
        State == BattleState.Picking &&
        ClientA.Pick != 0 &&
        ClientB.Pick != 0;

    public void SubmitTeam(BattleClient client, SimplePackedPokemon[] packedTeam)
    {
        EnsureStreamStarted();

        var plr = client == ClientA ? 1 : 2;
        var sb = new StringBuilder();
        for (var i = 0; i < packedTeam.Length - 1; i++)
        {
            sb.Append(packedTeam[i]);
            sb.Append(']');
        }
        sb.Append(packedTeam[^1]);

        const string defaultSpec = "123456";
        var spec = client.Pick == 1 ? defaultSpec : $"{client.Pick}{defaultSpec.Replace(client.Pick.ToString(), string.Empty)}";

        // Name is written as its side for simplicity when parsing, similar to how team names are written
        var setPlayer = ProtocolCodec.EncodeSetPlayerCommand(plr, plr.ToString(), sb.ToString());
        client.CachedTeamSpec = ProtocolCodec.EncodePlayerChoiceCommand(plr, "team", spec);

        Console.WriteLine(setPlayer);

        Stream.Write(setPlayer);

        // Showdown starts every Pokémon at full HP, so bring over the HP each one really has
        for (var i = 0; i < packedTeam.Length; i++)
        {
            var hp = packedTeam[i].HP;
            if (hp == SimplePackedPokemon.FullHP)
                continue;

            var mon = $"battle.sides[{plr - 1}].pokemon[{i}]";
            Stream.Write(hp == 0
                ? $">eval {mon}.hp = 0; {mon}.fainted = true; {mon}.status = 'fnt'; battle.sides[{plr - 1}].pokemonLeft--"
                : $">eval {mon}.hp = Math.min({hp}, {mon}.maxhp)");
        }
    }

    public void SubmitChoice(int plr, BattleChoice choice, int operand)
    {
        if (Stream is null || Stream.IsDisposed)
            return;

        string main;
        string secondary = null;
        if ((choice & BattleChoice.Move) != 0)
        {
            main = "move";
            if ((choice & BattleChoice.Mega) != 0)
                secondary = "mega";
            else if ((choice & BattleChoice.ZMove) != 0)
                secondary = "zmove";
            else if ((choice & BattleChoice.Max) != 0)
                secondary = "max";
        }
        else
        {
            main = choice switch
            {
                BattleChoice.Default => "default",
                BattleChoice.Pass => "pass",
                BattleChoice.Switch => "switch",
                _ => throw new Exception()
            };
        }

        var final = choice is BattleChoice.Default ?
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main) : secondary == null ?
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main, operand.ToString()) :
            ProtocolCodec.EncodePlayerChoiceCommand(plr, main, secondary, operand.ToString());

        Console.WriteLine(final);
        Stream.Write(final);
    }

    public void EnsureStreamStarted()
    {
        if (Stream != null)
            return;

        Stream = new BattleStream();
        Task.Run(RunAsync);
    }

    public void StartEffects()
    {
        State = BattleState.Ongoing;

        ClientA.State = ClientB.State = ClientBattleState.Ongoing;

        // Effects
        ClientA.Provider.StartBattleEffects(before: true);
        ClientB.Provider.StartBattleEffects(before: false);
        ClientA.Provider.StartBattleEffects(before: false);
    }

    // Set once the battle was ended on purpose (win, tie, forfeit), so the stream closing isn't treated as a crash
    private volatile bool _stopped;

    public void Stop()
    {
        _stopped = true;
        if (Main.netMode == NetmodeID.Server)
        {
            ClientA.BattleStopped();
            ClientB.BattleStopped();
        }

        Stream?.Dispose();
    }

    public BattleClient this[int side]
        => side switch
        {
            1 => ClientA,
            2 => ClientB,
            _ => null
        };

    private async Task RunAsync()
    {
        try
        {
            var start = ProtocolCodec.EncodeStartCommand(FormatID.Gen9CustomGame);
            Console.WriteLine(start);
            Stream.Write(start);
            var mgr = BattleManager.Instance;

            // I would love to write directly to a packet on multiplayer
            // But IDK if that's possible with EasyPacketsLib
            // I think that we can probably do some hack for it anyway
            // Or come up with a better solution than EasyPacketsLib but I digress

            var inMainFrame = false;

            // Kept per-round
            BinaryWriter pWriter = new(new MemoryStream());
            BinaryWriter sWriter = new(new MemoryStream());

            await foreach (var output in Stream.ReadOutputsAsync())
            {
                var frame = ProtocolCodec.Parse(output);
                if (frame is null || frame.Elements == null) continue;

                if (frame is UpdateFrame)
                {
                    SideUpdatePlayer = 0;
                    pWriter?.Dispose();
                    sWriter?.Dispose();

                    pWriter = new(new MemoryStream());
                    sWriter = new(new MemoryStream());

                    inMainFrame = true;
                }
                else if (frame is SideUpdateFrame sideUpdate)
                {
                    SideUpdatePlayer = sideUpdate.Player is { Length: >= 2 } who ? who[1] - '0' : 0;
                    if (inMainFrame)
                    {
                        Observe(ref pWriter, ref sWriter);
                        inMainFrame = false;
                    }
                }

                // Console.WriteLine($"Received message of type {frame.GetType().Name} from simulator");
                foreach (var element in CollectionsMarshal.AsSpan(frame.Elements))
                {
                    Log(element.ToString());

                    // No side update follows the end of a battle, so apply the final hits (and faints) before it ends
                    if (inMainFrame && element is WinElement or TieElement)
                    {
                        Observe(ref pWriter, ref sWriter);
                        inMainFrame = false;
                    }

                    if (element is ISplitElement split)
                    {
                        var tgt = (split.PlayerID is 1 or 2) ? split.PlayerID : -1;
                        HandleSafely(mgr, sWriter, split.Secret, toSide: -1);
                        HandleSafely(mgr, pWriter, split.Public, toSide: tgt);
                        // The secret half has exact HP numbers, so damage can be shown in HP instead of %
                        LogSafely(split.Secret is DamageElement or HealElement or SwitchElement or DragElement
                            ? split.Secret
                            : split.Public);
                        continue;
                    }
                    LogSafely(element);
                    HandleSafely(mgr, pWriter, element, toSide: -1);
                }
            }
        }
        catch (Exception ex) when (_stopped)
        {
            // The battle already ended normally and its stream was closed under us
            Log($"Battle stream closed after the battle ended ({ex.GetType().Name})", ConsoleColor.Yellow);
        }
        catch (Exception ex)
        {
            Log($"Battle encountered an error: {ex.GetType()}: {ex.Message}", ConsoleColor.Red);
            Log(ex.StackTrace ?? "No stack trace.", ConsoleColor.Red);
            var battleEnd = new TieStatement(eitherParticipant: ClientA.Provider, type: TieStatement.TieType.Forced);
            battleEnd.Send();
        }
        finally
        {
            if (Stream != null && !Stream.IsDisposed)
            {
                Log($"Disposing battle stream", ConsoleColor.Yellow);
                Stream.Dispose();
            }
        }
    }

    /// <summary>
    ///     Handles one protocol element, so that a single element we can't handle doesn't end the whole battle.
    ///     Anything half-written for that element is removed from the buffer so readers don't desync.
    /// </summary>
    private void HandleSafely(BattleManager mgr, BinaryWriter w, ProtocolElement element, int toSide)
    {
        var start = w.BaseStream.Position;
        try
        {
            mgr.HandleSingleElement(this, w, element, toSide);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            Log($"Couldn't handle {element.GetType().Name} ({element}): {ex.GetType().Name}: {ex.Message}", ConsoleColor.Red);
            Terramon.Instance.Logger.Error($"Battle element {element} couldn't be handled: {ex}");
            w.Flush();
            w.BaseStream.SetLength(start);
            w.BaseStream.Position = start;
        }
    }

    private void LogSafely(ProtocolElement element)
    {
        try
        {
            BattleLog.Handle(this, element);
        }
        catch (Exception ex)
        {
            Terramon.Instance.Logger.Error($"Battle log failed for {element}: {ex}");
        }
    }

    /// <summary>
    ///     Reads and handles all <see cref="BattleAction"/>s currently written to the buffers
    ///     Also resets those buffers
    /// </summary>
    public void Observe(ref BinaryWriter p, ref BinaryWriter s)
    {
        var owner = ClientA.Provider;

        if (p.BaseStream.Length != 0)
        {
            p.BaseStream.WriteByte(0); // footer

            if (Main.dedServ) // mp
            {
                Console.WriteLine($"Sent payload with {p.BaseStream.Length} bytes of content.");

                // The server's own view needs the public actions too (statuses, boosts, turns), e.g. for the battle AI
                ObserveSafely(owner.ID, (MemoryStream)p.BaseStream, onlyToSelf: true);

                var payload = new BattlePayloadRpc(owner, (MemoryStream)p.BaseStream);
                Terramon.Instance.SendPacket(payload);
            }
            else
            {
                ObserveSafely(owner.ID, (MemoryStream)p.BaseStream, onlyToSelf: false);
            }

            // Fresh buffers, in case more elements arrive before the next update frame
            p.Dispose();
            p = new BinaryWriter(new MemoryStream());
        }

        if (s.BaseStream.Length != 0)
        {
            s.BaseStream.WriteByte(0); // footer
            ObserveSafely(owner.ID, (MemoryStream)s.BaseStream, onlyToSelf: true);
            s.Dispose();
            s = new BinaryWriter(new MemoryStream());
        }
    }

    private static void ObserveSafely(BattleParticipant owner, MemoryStream buffer, bool onlyToSelf)
    {
        try
        {
            BattleManager.Instance.Observe(owner, buffer, onlyToSelf);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            // A bad action only breaks the mirrored view, not the simulation itself
            Terramon.Instance.Logger.Error($"Couldn't apply battle actions: {ex}");
        }
    }

    public static void Log(string str, ConsoleColor col = ConsoleColor.Gray)
    {
        lock (str)
        {
            Console.ForegroundColor = col;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }
}

public enum BattleState : byte
{
    Request,
    Picking,
    Ongoing,
    Ended,
}
