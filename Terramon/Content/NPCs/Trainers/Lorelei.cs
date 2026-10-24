using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The first of the Elite Four, an Ice-type master.
/// </summary>
public class Lorelei : LeagueMember
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Dewgong, 54),
        (NationalDexID.Cloyster, 53),
        (NationalDexID.Slowbro, 54),
        (NationalDexID.Jynx, 56),
        (NationalDexID.Lapras, 56)
    ];

    protected override int LookAlike => NPCID.Steampunker;
}
