using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The third of the Elite Four, a Ghost-type master.
/// </summary>
public class Agatha : LeagueMember
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Gengar, 56),
        (NationalDexID.Golbat, 56),
        (NationalDexID.Haunter, 55),
        (NationalDexID.Arbok, 58),
        (NationalDexID.Gengar, 60)
    ];

    protected override int LookAlike => NPCID.WitchDoctor;
}
