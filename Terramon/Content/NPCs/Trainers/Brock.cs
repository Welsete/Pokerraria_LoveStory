using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Pewter City's Gym Leader. Accepts challenges once the Eye of Cthulhu has been defeated.
/// </summary>
public class Brock : TrainerNPC
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Geodude, 12),
        (NationalDexID.Onix, 14)
    ];

    protected override int RewardItemType => ModContent.ItemType<BoulderBadge>();

    protected override bool CanChallenge(Player player) => Terraria.NPC.downedBoss1;
}
