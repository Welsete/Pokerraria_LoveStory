using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The second of the Elite Four, a Fighting-type master.
/// </summary>
public class Bruno : LeagueMember
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Onix, 53),
        (NationalDexID.Hitmonchan, 55),
        (NationalDexID.Hitmonlee, 55),
        (NationalDexID.Onix, 56),
        (NationalDexID.Machamp, 58)
    ];

    protected override int LookAlike => NPCID.DD2Bartender;
}
