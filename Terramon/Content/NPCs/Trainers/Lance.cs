using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The last of the Elite Four, a Dragon-type master.
/// </summary>
public class Lance : LeagueMember
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Gyarados, 58),
        (NationalDexID.Dragonair, 56),
        (NationalDexID.Dragonair, 56),
        (NationalDexID.Aerodactyl, 60),
        (NationalDexID.Dragonite, 62)
    ];

    protected override int LookAlike => NPCID.Pirate;
}
