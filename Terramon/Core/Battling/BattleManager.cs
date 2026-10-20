using Showdown.NET.Protocol;
using System.Text;
using System.Text.Json;
using EasyPacketsLib;
using Terramon.Content.Commands;
using Terramon.Content.NPCs;
using Terramon.Content.NPCs.Trainers;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.ID;

namespace Terramon.Core.Battling;

/// <summary>
///     Contains information related to the toClients version of a Showdown battle, instanced locally on Singleplayer and server-side on Multiplayer.
///     It is in charge of receiving, managing and synchronizing battle requests and actions made by players in battle.
/// </summary>
public sealed class BattleManager
{
    private static Terramon Mod => Terramon.Instance;

    public static BattleManager Instance { get; set; }
    private readonly Dictionary<BattleParticipant, BattleInstance> _activeBattles = [];

    public void Reply(BattleMessage m)
    {
        // Even though the server owns NPCs, messages sent to those are received specifically by them
        // This method only takes into account messages sent specifically to the server

        switch (m)
        {
            case ForfeitOrder:
                // Send the forfeit message to self (server)
                var forfeit = new ForfeitStatement(forfeiter: m.Sender);
                forfeit.Send();
                break;
            case ForfeitStatement w:
                // Remove from active battle list
                var inst = _activeBattles[w.Forfeiter.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
            case WinStatement w:
                // Remove from active battle list
                inst = _activeBattles[w.Winner.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
            case SlotChoice:
                // Received when both have chosen
                inst = _activeBattles[m.Sender.ID];

                // Set states
                inst.ClientA.State = inst.ClientB.State = ClientBattleState.PollingTeam;

                // Start the stream
                inst.EnsureStreamStarted();

                // Ask both participants for their team
                var teamQuestion = new TeamQuestion();
                teamQuestion.Send(inst.ClientA.Provider);
                teamQuestion.Send(inst.ClientB.Provider);
                break;
            case TeamAnswer s:

                // Received once for each participant
                inst = _activeBattles[s.Sender.ID];
                // Set state
                Console.WriteLine($"Setting state of {s.Sender.BattleName} to SetTeam");
                s.Sender.State = ClientBattleState.SetTeam;

                var shouldStart =
                    inst.ClientA.State == ClientBattleState.SetTeam &&
                    inst.ClientB.State == ClientBattleState.SetTeam;

                // Check if both participants have submitted their team
                if (shouldStart)
                {
                    // If so, start the battle
                    Console.WriteLine("Setting both clients states to Ongoing");
                    inst.StartEffects();

                    // Send start message to self
                    // TODO: Test to make sure this doesn't require some sort of delay later
                    var start = new StartBattleStatement(inst.ClientA.Provider);
                    start.Send();
                }

                inst.SubmitTeam(s.Sender.BattleClient, s.Team);
                break;
            case TieStatement t:
                // Remove from active battles list
                inst = _activeBattles[t.EitherParticipant.ID];
                _activeBattles.Remove(inst.ClientA.ID);
                _activeBattles.Remove(inst.ClientB.ID);

                // Reset client fields
                BattleInstance.Destroy(inst);
                break;
        }
    }
    
    public bool Witness(BattleMessage m)
    {
        // Messages that are meant for a client or server-owned provider pass through here first
        // Return false to intercept the message if it shouldn't be sent to the recipient

        Console.WriteLine($"Server: Witnessing a {m.GetType().Name}");

        switch (m)
        {
            case ResetEverythingStatement:
                foreach (var inst0 in _activeBattles.Values)
                    inst0.Stop();
                _activeBattles.Clear();
                break;

            // Messages related to challenging someone to a battle

            case ChallengeQuestion:

                // This only checks if the recipient is already in a battle,
                // since the ability for a client to request a battle is handled clientside
                if (_activeBattles.ContainsKey(m.Recipient.ID))
                    // If they're already in a battle, intercept with an error
                    return m.Return<ChallengeError>();

                var inst = BattleInstance.Create(
                    m.Sender.BattleClient,
                    m.Recipient.BattleClient);

                // Add the requester
                // The requestee is only added if they accept
                _activeBattles.Add(m.Sender.ID, inst);

                // Set states
                m.Sender.State = ClientBattleState.Requested;
                break;
            case ChallengeTakeback:

                // Sent by original requester

                // Remove from active battles list
                _activeBattles.Remove(m.Sender.ID, out inst);

                // Reset requester's fields
                inst.ClientA.Battle = null;
                inst.ClientA.Foe = null;
                inst.ClientA.State = ClientBattleState.None;
                break;
            case ChallengeAnswer c:

                // It's possible that when trying to answer a question,
                // that provider's question has already been taken back.
                // In that case, intercept, since they don't need the answer anymore
                if (!_activeBattles.TryGetValue(m.Recipient.ID, out inst))
                    return false;

                if (c.Yes) // If the sender accepted
                {
                    // Add the requestee
                    _activeBattles.Add(m.Sender.ID, inst);

                    // Set states
                    inst.State = BattleState.Picking;
                    m.Sender.State = m.Recipient.State = ClientBattleState.PollingSlot;
                }
                else // If the sender declined
                {
                    // Reset client fields
                    BattleInstance.Destroy(inst);

                    // Remove from active battles list
                    _activeBattles.Remove(m.Recipient.ID);
                }
                break;

            // Messages related to setting up a battle

            case SlotChoice s:
                m.Sender.Pick = s.Slot;
                m.Sender.State = ClientBattleState.SetSlot;
                break;

            // Messages related to intentionally ending a battle

            case TieQuestion:

                // Set tie request flag
                m.Sender.TieRequest = true;

                // Check if the other client also requested a tie
                if (m.Recipient.TieRequest)
                {
                    // If so, intercept, and send the agreed tie message to self (server)
                    var agreedTie = new TieStatement(eitherParticipant: m.Sender, type: TieStatement.TieType.Agreed);
                    agreedTie.Send();
                    return false;
                }
                break;
            case TieTakeback:

                // Unset tie request flag
                m.Sender.TieRequest = false;
                break;

            // Messages related to ending a battle

        }

        return true;
    }
    
    public void HandleSingleElement(BattleInstance source, BinaryWriter w, ProtocolElement element, int toSide)
    {
        switch (element)
        {
            case CantElement:
            case FailElement:
            case BlockElement:
            case NoTargetElement:
            case MissElement:
            case ImmuneElement:
                Write(BattleActionID.ActionFail);
                break;
            case PrepareElement prep:
                var side = prep.Attacker[1] - '0';
                Write(BattleActionID.PokemonWait);
                {
                    w.Write((byte)side);
                }
                break;
            case MustRechargeElement prep:
                side = prep.Pokemon[1] - '0';
                Write(BattleActionID.PokemonWait);
                {
                    w.Write((byte)side);
                }
                break;
            case MoveElement move:
                var mvD = move.Details;
                Write(BattleActionID.MoveAnimation, mvD.Still, mvD.Miss);
                {
                    w.Write(new SimpleMon(move.Pokemon));
                    var anim = mvD.Animation ?? move.Move;
                    if (!Enum.TryParse(anim.Replace(" ", string.Empty), out MoveID animID))
                        Mod.Logger.Error($"{anim} not recognized as a move in MoveID");
                    w.Write((ushort)animID);
                }
                break;
            case DamageElement hp:
                Write(BattleActionID.SetPokemonHP, false); // set indirectly
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case HealElement hp:
                Write(BattleActionID.SetPokemonHP, false);
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case SetHPElement hp:
                Write(BattleActionID.SetPokemonHP, true); // set directly
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(new SimpleHP(hp.HP));
                }
                break;
            case FaintElement hp:
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(new SimpleMon(hp.Pokemon));
                    w.Write(0u);
                }
                break;
            case SwitchElement sw:
                var pk = new SimpleMon(sw.Pokemon);
                Write(BattleActionID.SwitchPokemon);
                {
                    w.Write(pk);
                }
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(sw.HP));
                }
                Write(BattleActionID.SetPokemonStatus);
                {
                    w.Write(pk);
                    w.Write((byte)sw.Status);
                }
                break;
            case DragElement sw:
                pk = new SimpleMon(sw.Pokemon);
                Write(BattleActionID.SwitchPokemon);
                {
                    w.Write(pk);
                }
                Write(BattleActionID.SetPokemonHP, true);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(sw.HP));
                }
                Write(BattleActionID.SetPokemonStatus);
                {
                    w.Write(pk);
                    w.Write((byte)sw.Status);
                }
                break;
            case DetailsChangeElement dc:
                pk = new SimpleMon(dc.Pokemon);
                Write(BattleActionID.SetPokemonDetails);
                {
                    w.Write(pk);
                    w.Write(new SimpleDetails(dc.Details));
                }
                Write(BattleActionID.SetPokemonHP);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(dc.HP));
                }
                break;
            case FormeChangeElement dc:
                pk = new SimpleMon(dc.Pokemon);
                Write(BattleActionID.SetPokemonSpecies);
                {
                    w.Write(pk);
                    w.Write(ParseSpecies(dc.Species));
                }
                Write(BattleActionID.SetPokemonHP);
                {
                    w.Write(pk);
                    w.Write(new SimpleHP(dc.HP));
                }
                break;
            case ReplaceElement dc:
                Write(BattleActionID.SetPokemonDetails);
                {
                    w.Write(new SimpleMon(dc.Pokemon));
                    w.Write(new SimpleDetails(dc.Details));
                }
                break;
            case StatusElement nvs:
                Write(BattleActionID.SetPokemonStatus, false);
                {
                    w.Write(new SimpleMon(nvs.Pokemon));
                    w.Write((byte)nvs.Status);
                }
                break;
            case CureStatusElement nvs:
                Write(BattleActionID.SetPokemonStatus, true);
                {
                    w.Write(new SimpleMon(nvs.Pokemon));
                }
                break;
            case CureTeamElement nvs:
                side = nvs.Pokemon[1] - '0';
                var sideTeamCount = source.Omniscient[side].TeamCount;
                for (int i = 0; i < sideTeamCount; i++)
                {
                    Write(BattleActionID.SetPokemonStatus, true);
                    {
                        w.Write(new SimpleMon(side, i));
                    }
                }
                break;
            case BoostElement bst:
                Write(BattleActionID.SetPokemonBoost, false); // set indirectly
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)bst.Amount);
                }
                break;
            case UnboostElement bst:
                Write(BattleActionID.SetPokemonBoost, false);
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)-bst.Amount);
                }
                break;
            case SetBoostElement bst:
                Write(BattleActionID.SetPokemonBoost, true); // set directly
                {
                    w.Write(new SimpleMon(bst.Pokemon));
                    w.Write((byte)bst.Stat);
                    w.Write((sbyte)bst.Amount);
                }
                break;
            case SwapBoostElement bst:
                var pair = new SimpleMonPair(bst.Source, bst.Target);
                for (int i = 0; i < bst.Stats.Length; i++)
                {
                    Write(BattleActionID.SetPokemonBoost, true, true); // is swap
                    {
                        w.Write(pair);
                        w.Write((byte)bst.Stats[i]);
                    }
                }
                break;
            case CopyBoostElement bst:
                Write(BattleActionID.SetPokemonBoost, true, false, true); // is copy
                {
                    w.Write(new SimpleMonPair(bst.Source, bst.Target));
                }
                break;
            case InvertBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.Invert);
                }
                break;
            case ClearBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearAll);
                }
                break;
            case ClearNegativeBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearNegative);
                }
                break;
            case ClearPositiveBoostElement mbst:
                Write(BattleActionID.AllPokemonBoost);
                {
                    w.Write(new SimpleMon(mbst.Pokemon));
                    w.Write((byte)BoostModifierAction.ClearPositive);
                }
                break;
            case ClearAllBoostElement:
                Write(BattleActionID.AllPokemonBoost, true);
                break;
            case TurnElement:
                Write(BattleActionID.AdvanceTurn);
                break;
            case WeatherElement wth:
                Write(BattleActionID.SetWeather, wth.Upkeep);
                {
                    w.Write((byte)ParseWeather(wth.Weather));
                }
                break;
            case FieldStartElement fld:
                Write(BattleActionID.SetFieldCondition);
                {
                    w.Write((byte)ParseOrNone<FieldCondition>(fld.Condition));
                }
                break;
            case FieldEndElement:
                Write(BattleActionID.SetFieldCondition);
                {
                    w.Write((byte)0);
                }
                break;
            case SideStartElement sd:
                Write(BattleActionID.SetSideCondition, sd.Tags != null && sd.Tags.Contains("[persistent]"));
                {
                    w.Write((byte)sd.Side.Player);
                    w.Write((byte)ParseOrNone<SideCondition>(sd.Condition));
                }
                break;
            case SideEndElement sd:
                Write(BattleActionID.SetSideCondition);
                {
                    w.Write((byte)sd.Side.Player);
                    w.Write((byte)0);
                }
                break;
            case SwapSideConditionsElement:
                Write(BattleActionID.SetSideCondition, true); // is swap
                break;
            case StartVolatileElement vol:
                Write(BattleActionID.SetPokemonVolatile, true, vol.Tags != null && vol.Tags.Contains("[silent]"), vol.Tags != null && vol.Tags.Contains("[upkeep]")); // is start
                {
                    w.Write(new SimpleMon(vol.Pokemon));
                    w.Write((byte)GetVolatile(vol.Effect));
                }
                break;
            case EndVolatileElement vol:
                Write(BattleActionID.SetPokemonVolatile, false); // is end
                {
                    w.Write(new SimpleMon(vol.Pokemon));
                    w.Write((byte)GetVolatile(vol.Effect));
                }
                break;
            case ItemElement it:
                // my brain is too bad to know what exactly i should do here but ik you can save
                // some writes here
                Write(BattleActionID.PokemonItem, true); // is reveal
                {
                    w.Write(new SimpleMon(it.Pokemon));
                    w.Write(ParseItem(it.Item));
                }
                break;
            case EndItemElement it:
                Write(BattleActionID.PokemonItem, false);
                {
                    w.Write(new SimpleMon(it.Pokemon));
                }
                break;
            case AbilityElement ab:
                // my brain is too bad to know what exactly i should do here but ik you can save
                // some writes here
                Write(BattleActionID.PokemonAbility, true); // is reveal
                {
                    w.Write(new SimpleMon(ab.Pokemon));
                    w.Write((ushort)ParseOrNone<AbilityID>(ab.Ability));
                }
                break;
            case EndAbilityElement ab:
                Write(BattleActionID.PokemonAbility, false);
                {
                    w.Write(new SimpleMon(ab.Pokemon));
                }
                break;
            // Messages with special handling
            case RequestElement req:
                HandleRequest(source, req.Request);
                break;
            case ErrorElement err:
                // Errors arrive in the side update of the side that made the bad choice
                HandleError(source, err, source.SideUpdatePlayer is 1 or 2 ? source.SideUpdatePlayer : LatestInteractor);
                break;
            case WinElement win:
                var winner = source[win.Username[0] - '0'].Provider;
                var dubs = new WinStatement(winner);
                dubs.Send();
                break;
            case TieElement:
                var tie = new TieStatement(eitherParticipant: source.ClientA.Provider, TieStatement.TieType.Regular);
                tie.Send();
                break;
        }

        void Write(BattleActionID id, params bool[] flags)
        {
            BitsByte b = default;
            for (int i = 0; i < flags.Length; i++)
                b[i] = flags[i];
            w.Write((byte)id);
            w.Write(b);
        }
    }
    
    private static readonly HashSet<string> LoggedUnknownNames = [];

    /// <summary>
    ///     Logs an unrecognized protocol name only the first time it shows up.
    /// </summary>
    private static void LogUnknownOnce(string kind, string name)
    {
        lock (LoggedUnknownNames)
        {
            if (!LoggedUnknownNames.Add($"{kind}:{name}"))
                return;
        }
        Mod.Logger.Warn($"Unrecognized {kind} '{name}' from Showdown; ignoring it");
    }

    /// <summary>
    ///     Strips an effect prefix ("move: ", "ability: ", "item: ") and spaces/punctuation from a Showdown effect name.
    /// </summary>
    private static string SanitizeEffect(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return string.Empty;
        var colon = rawName.IndexOf(": ", StringComparison.Ordinal);
        var name = colon >= 0 ? rawName[(colon + 2)..] : rawName;
        return new string(name.Where(char.IsLetterOrDigit).ToArray());
    }

    private static T ParseOrNone<T>(string rawName) where T : struct, Enum
    {
        var sanitized = SanitizeEffect(rawName);
        if (sanitized.Length != 0 && Enum.TryParse<T>(sanitized, true, out var value))
            return value;
        LogUnknownOnce(typeof(T).Name, rawName);
        return default;
    }

    private static BattleWeather ParseWeather(string rawName)
    {
        if (string.IsNullOrEmpty(rawName) || rawName is "none")
            return BattleWeather.None;
        // Gen 9 calls Snowscape's weather "Snow"
        if (SanitizeEffect(rawName).Equals("Snow", StringComparison.OrdinalIgnoreCase))
            return BattleWeather.Snowscape;
        return ParseOrNone<BattleWeather>(rawName);
    }

    private static ushort ParseItem(string rawName)
    {
        try
        {
            return ItemID.Search.Terramon(SanitizeEffect(rawName));
        }
        catch (Exception)
        {
            LogUnknownOnce("item", rawName);
            return 0;
        }
    }

    private static ushort ParseSpecies(string rawName)
    {
        try
        {
            return NationalDexID.FromSpecies(rawName);
        }
        catch (Exception)
        {
            // Formes like "Castform-Sunny" aren't in the dex list; fall back to the base species
            try
            {
                return NationalDexID.FromSpecies(rawName.Split('-')[0]);
            }
            catch (Exception)
            {
                LogUnknownOnce("species", rawName);
                return 0;
            }
        }
    }

    private static VolatileEffect GetVolatile(string rawName)
    {
        // ooh boy
        var sanitized = SanitizeEffect(rawName);
        if (sanitized.Length == 0)
            return VolatileEffect.None;

        // Perish Song counters and Supreme Overlord's fallen allies carry a number at the end
        if (sanitized.StartsWith("perish", StringComparison.OrdinalIgnoreCase))
            return VolatileEffect.Perish;
        if (sanitized.StartsWith("fallen", StringComparison.OrdinalIgnoreCase))
            return VolatileEffect.Fallen;

        if (Enum.TryParse<VolatileEffect>(sanitized, true, out var vol))
            return vol;
        if (Enum.TryParse<NatureID>(sanitized, out _))
            return VolatileEffect.NatureEffect;

        // Unknown volatiles are only cosmetic on our side, so don't let them end the battle
        LogUnknownOnce("volatile effect", rawName);
        return VolatileEffect.None;
    }
    
    public static void HandleError(BattleInstance source, ErrorElement error, int toSide)
    {
        if (toSide <= 0)
            return;

        var id = BattleErrorParser.Parse(error.Message);
        var client = source[toSide];
        if (client is null)
            return;

        // NPCs have nobody to fix their choice, so retry with the default one
        if (client.Provider is not TerramonPlayer)
        {
            client.CurrentRequest = ShowdownRequest.None;
            BattleAI.OnChoiceError(source, toSide);
            return;
        }

        if (Main.dedServ && client.Provider is TerramonPlayer plr)
        {
            var sendErr = new BattleErrorRpc(error.Type, id);
            Mod.SendPacket(sendErr, plr.Player.whoAmI);
        }
        else
        {
            // Showdown keeps waiting for a choice after rejecting one, so let the player choose again
            client.CurrentRequest = client.LastRequest;
            // do stuff with error type and subtype
        }
    }
    
    public static void HandleRequest(BattleInstance source, string rawRequest)
    {
        using var req = JsonDocument.Parse(rawRequest);

        var root = req.RootElement;

        if (!root.TryGetProperty("side", out var side))
            return;

        var sd = side.GetProperty("id").ToString()[1] - '0';
        var client = source[sd];

        if (root.TryGetProperty("teamPreview", out _))
        {
            var spec = client.CachedTeamSpec;
            Console.WriteLine(spec);
            source.Stream.Write(spec);
            client.CachedTeamSpec = null;
            return;
        }

        var forceSwitch = root.TryGetProperty("forceSwitch", out _);
        var wait = root.TryGetProperty("wait", out _);

        var sr = forceSwitch ? ShowdownRequest.ForcedSwitch : wait ? ShowdownRequest.Wait : ShowdownRequest.Any;

        // Nicknames are party slots plus one (see SimplePackedPokemon.Team), e.g. "p1: 3" is party slot 2
        if (side.TryGetProperty("pokemon", out var pokemon))
        {
            client.SwitchOrder = pokemon.EnumerateArray()
                .Select(p => p.TryGetProperty("ident", out var ident) &&
                             int.TryParse(ident.GetString()?.Split(": ", 2)[^1], out var n) ? n - 1 : -1)
                .ToArray();
        }

        // Kept on the server too, so server-side code (e.g. skipping a turn) knows what's being asked
        client.CurrentRequest = sr;
        if (sr != ShowdownRequest.Wait)
            client.LastRequest = sr;

        if (Main.dedServ && client.Provider.SyncedEntity is Player plr)
        {
            var sendReq = new ShowdownRequestRpc(sr, client.SwitchOrder);
            Mod.SendPacket(sendReq, toClient: plr.whoAmI);
        }
        else
        {
            // Nothing to answer while waiting for the foe
            if (sr != ShowdownRequest.Wait && client.Provider is PokemonNPC or TrainerNPC)
                RespondAutomatically(source, client, root);
        }
    }
    
    /// <summary>
    ///     Lets an NPC answer a request with <see cref="BattleAI"/>, falling back to the default choice if anything goes wrong.
    /// </summary>
    private static void RespondAutomatically(BattleInstance source, BattleClient client, JsonElement root)
    {
        Console.WriteLine($"{client.Provider.BattleName} is responding automatically...");

        var chosen = false;
        try
        {
            chosen = BattleAI.TryChoose(source, client, root);
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"BattleAI failed for {client.Provider.BattleName}, using the default choice: {ex}");
        }

        if (chosen)
            return;

        if (client.Provider is PokemonNPC npc)
            npc.AutoBattleChoice();
        else if (client.Provider is TrainerNPC trainer)
            trainer.AutoBattleChoice();
    }

    public int LatestInteractor;

    /// <summary>
    ///     Uses up a participant's turn without doing anything (e.g. a thrown Poké Ball), so only the foe acts.
    /// </summary>
    public void SkipTurn(BattleClient client)
    {
        if (!_activeBattles.TryGetValue(client.ID, out var b) || b.Stream is null || b.Stream.IsDisposed)
            return;
        if (client.CurrentRequest != ShowdownRequest.Any)
            return; // Already chose an action this turn

        var side = client == b.ClientA ? 1 : 2;
        client.CurrentRequest = ShowdownRequest.Wait;

        // Same effect as needing to recharge after Hyper Beam: whatever move is chosen, the turn is skipped
        b.Stream.Write($">eval battle.sides[{side - 1}].active[0].addVolatile('mustrecharge')");
        b.SubmitChoice(side, BattleChoice.Move, 1);
    }

    /// <summary>
    ///     Uses a recovery item from the Bag on one of a participant's Pokémon (by party slot), which uses up their turn.
    ///     Showdown has no Bag, so the heal is applied with >eval and announced with a -heal line so every viewer updates.
    ///     Returns false if the item can't be used right now.
    /// </summary>
    public bool UseItem(BattleClient client, int itemType, int partySlot, string pokemonName)
    {
        if (!_activeBattles.TryGetValue(client.ID, out var b) || b.Stream is null || b.Stream.IsDisposed)
            return false;
        // Multiplayer clients check this themselves, as the server doesn't track their requests
        if (!Main.dedServ && client.CurrentRequest != ShowdownRequest.Any)
            return false;
        if (partySlot is < 0 or > 5 || itemType < ItemID.Count || itemType >= ItemLoader.ItemCount)
            return false;

        var side = client == b.ClientA ? 1 : 2;
        var item = ModContent.GetModItem(itemType);
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        // Nicknames are party slots plus one (see SimplePackedPokemon.Team)
        var find = $"const s = battle.sides[{side - 1}]; const p = s.pokemon.find(m => m.name === '{partySlot + 1}');";
        var effect = item switch
        {
            Content.Items.BasePotionItem potion =>
                $"{find} if (!p || p.fainted || !p.hp || p.hp >= p.maxhp) return false; p.heal({potion.BattleHealAmount}); " +
                $"battle.add('-heal', p, p.getHealth, '[from] item: {item.Name}'); return true;",
            Content.Items.BaseReviveItem revive =>
                $"{find} if (!p || !p.fainted) return false; p.fainted = false; p.faintQueued = false; p.switchFlag = false; " +
                $"p.status = ''; p.hp = Math.max(1, Math.ceil(p.maxhp * {revive.BattleRestorationPercentage.ToString(inv)})); " +
                $"s.pokemonLeft++; battle.add('-heal', p, p.getHealth, '[from] item: {item.Name}'); return true;",
            _ => null
        };
        if (effect is null)
            return false;

        client.CurrentRequest = ShowdownRequest.Wait;
        LatestInteractor = side;

        // "{player} used {item} on {Pokémon}!"
        var user = client.Provider is TerramonPlayer plr ? plr.Player.name : client.Provider.BattleName;
        const string key = "Mods.Terramon.Bag.Used";
        var color = new Color(110, 224, 110);
        if (!Main.dedServ)
        {
            BattleLog.Say(Terraria.Localization.Language.GetTextValue(key, user, item.DisplayName.Value,
                BattleLog.HighlightMon(pokemonName, client.Provider == TerramonPlayer.LocalPlayer)), color);
        }
        else
        {
            for (var viewer = 1; viewer <= 2; viewer++)
            {
                if (b[viewer].Provider is not TerramonPlayer watcher)
                    continue;
                var text = Terraria.Localization.NetworkText.FromKey(key,
                    Terraria.Localization.NetworkText.FromLiteral(user),
                    Terraria.Localization.NetworkText.FromKey(item.DisplayName.Key),
                    Terraria.Localization.NetworkText.FromLiteral(BattleLog.HighlightMon(pokemonName, viewer == side)));
                Terraria.Chat.ChatHelper.SendChatMessageToClient(text, color, watcher.Player.whoAmI);
            }
        }

        // The item takes the turn: same trick as SkipTurn, without the "must recharge" line
        BattleLog.ExpectSkippedTurn(b, side);
        b.Stream.Write($">eval (() => {{ {effect} }})()");
        b.Stream.Write($">eval battle.sides[{side - 1}].active[0].addVolatile('mustrecharge')");
        b.SubmitChoice(side, BattleChoice.Move, 1);
        return true;
    }

    public void HandleChoice(BattleParticipant participant, BattleChoice choice, int operand)
    {
        var b = _activeBattles[participant];
        var c = participant.Client;

        LatestInteractor = c == b.ClientA ? 1 : 2;

        b.SubmitChoice(LatestInteractor, choice, operand + 1);
    }
    
    public void Observe(BattleParticipant battleOwner, MemoryStream buffer, bool onlyToSelf)
    {
        buffer.Position = 0;
        using var reader = new BinaryReader(buffer, Encoding.UTF8, true);
        var b = _activeBattles[battleOwner];

        b.Omniscient.Receive(reader);
        buffer.Position = 0;

        // In singleplayer and on the server the participants share the omniscient field,
        // so reading the same actions twice would double boosts, turns and the like
        if (!onlyToSelf && b.ClientA.Battle != null && b.ClientA.Battle != b.Omniscient)
        {
            b.ClientA.Battle.Receive(reader);
        }

        reader.Dispose();
    }

    public static IBattleProvider GetProvider(byte whoAmI, BattleProviderType type = BattleProviderType.Player)
    {
        /*
        var asker = Main.myPlayer;
        var askFor = whoAmI;

        Mod.Logger.Error($"Request made for provider by {asker} for {askFor}");
        */

        return type switch
        {
            BattleProviderType.Player => Main.player[whoAmI].Terramon(),
            BattleProviderType.PokemonNPC => Main.npc[whoAmI].Pokemon(),
            BattleProviderType.TrainerNPC => Main.npc[whoAmI].ModNPC as TrainerNPC,
            _ => null,
        };
    }
    
    public static IBattleProvider GetProvider(BattleParticipant participant)
        => GetProvider(participant.WhoAmI, participant.Type);

    public static BattleClient GetClient(byte whoAmI, BattleProviderType type = BattleProviderType.Player)
        => GetProvider(whoAmI, type).BattleClient;
    
    public static BattleClient GetClient(BattleParticipant participant)
        => GetClient(participant.WhoAmI, participant.Type);
}
