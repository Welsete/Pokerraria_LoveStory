using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Cerulean City's Gym Leader. Unlocked by defeating the Eater of Worlds or the Brain of Cthulhu.
/// </summary>
public class Misty : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Staryu, 18),
        (NationalDexID.Starmie, 21)
    ];

    protected override int LookAlike => NPCID.Stylist;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss2;

    protected override int RewardItemType => ModContent.ItemType<CascadeBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<WaterStone>(), 1);
}
