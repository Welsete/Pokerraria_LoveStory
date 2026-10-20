using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Celadon City's Gym Leader. Unlocked by defeating the Wall of Flesh.
/// </summary>
public class Erika : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Victreebel, 29),
        (NationalDexID.Tangela, 24),
        (NationalDexID.Vileplume, 29)
    ];

    protected override int LookAlike => NPCID.Dryad;

    protected override bool GateBossDefeated => Main.hardMode;

    protected override int RewardItemType => ModContent.ItemType<RainbowBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<LeafStone>(), 1);
}
