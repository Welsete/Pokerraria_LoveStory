using System.Collections;

namespace Terramon.Core.Battling;
public sealed class BattleSide : IEnumerable<PokemonData>
{
    public BattleSide Opposite;

    /// <summary>
    ///     The battle this side is part of (set by <see cref="BattleField.LinkSides" />).
    /// </summary>
    public BattleField Field;
    private byte _activeSlot;
    public int TeamCount;
    public BattlePokemon[] Team = new BattlePokemon[6];
    public IBattleProvider Provider;
    /// <summary>
    ///     The side conditions (Tailwind, Reflect, Spikes...) up on this side, one bit per <see cref="SideCondition" />.
    /// </summary>
    public uint Conditions;
    public bool Has(SideCondition condition) => (Conditions & (1u << (int)condition)) != 0;
    public int Trapped;
    public ref BattlePokemon ActivePokemon => ref Team[_activeSlot];
    public void SetActivePokemon(byte slot)
    {
        _activeSlot = slot;
        Provider.SetActiveSlot(slot);
    }
    /// <summary>
    ///     Gives every Pokémon of this side back its own looks (e.g. a transformed Ditto that switches out).
    /// </summary>
    public void ClearTransforms()
    {
        for (var i = 0; i < Team.Length; i++)
            Team[i].TransformedInto = 0;
    }

    /// <summary>
    ///     The species (and shininess) the active battle Pokémon of <paramref name="provider" /> currently looks like
    ///     because of Transform or Imposter. False when it isn't battling or isn't transformed.
    /// </summary>
    public static bool TryGetTransformed(IBattleProvider provider, out ushort species, out bool shiny)
    {
        species = 0;
        shiny = false;
        var client = provider?.BattleClient;
        if (client?.Battle?.A == null || client.Battle.B == null)
            return false;
        var side = client.Side;
        if (side == null)
            return false;
        ref var active = ref side.ActivePokemon;
        var into = active.TransformedInto;
        var intoShiny = active.TransformedShiny;
        // What the battle turn being shown says (see BattlePlayback)
        if (side.Field != null &&
            BattlePlayback.TryGetShownLook(side.Field, side.Field.A == side ? 1 : 2, active.Slot, out var shownInto,
                out var shownShiny))
        {
            into = shownInto;
            intoShiny = shownShiny;
        }

        var fainted = active.Data != null ? BattlePlayback.ShownHP(active.Data) == 0 : active.HP == 0;
        if (into == 0 || fainted)
            return false;
        species = into;
        shiny = intoShiny;
        return true;
    }

    public void AdvanceTurn()
    {
        if (Trapped != 0)
            Trapped--;
    }
    public BattleSide(IBattleProvider baseOn)
    {
        Provider = baseOn;
        var team = baseOn.GetBattleTeam();
        for (int i = 0; i < team.Length; i++)
        {
            Team[i] = new()
            {
                Side = this,
                Slot = (byte)i
            };

            var mon = team[i];
            if (mon is null)
                continue;
            TeamCount++;
            Team[i].Data = mon;
        }
    }

    public IEnumerator<PokemonData> GetEnumerator()
    {
        for (int i = 0; i < Team.Length; i++)
            yield return Team[i].Data;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public ref BattlePokemon this[int slot] => ref Team[slot];

    public static BattleSide LocalSide
    {
        get
        {
            var loc = BattleClient.LocalClient;
            var sd = loc.SideIndex;
            if (sd == 0)
                return null;
            return loc.Battle[sd];
        }
    }
}

public enum SideCondition : byte
{
    None,
    Mist,
    Spikes,
    ToxicSpikes,
    StealthRock,
    FirePledge,
    StickyWeb,
    LightScreen,
    Reflect,
    Tailwind,
    Safeguard,
    LuckyChant,
    GMaxCannonade,
    GMaxSteelsurge,
    GMaxVineLash,
    GMaxVolcalith,
    GMaxWildfire,
}

public enum ShowdownRequest : byte
{
    None,
    Any,
    Wait,
    ForcedSwitch,
}
