using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Vermilion City's Gym Leader. Unlocked by defeating Skeletron.
/// </summary>
public class LtSurge : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Voltorb, 21),
        (NationalDexID.Pikachu, 18),
        (NationalDexID.Raichu, 24)
    ];

    protected override int LookAlike => NPCID.Cyborg;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss3;

    protected override int RewardItemType => ModContent.ItemType<ThunderBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<ExpShare>(), 1);
}
