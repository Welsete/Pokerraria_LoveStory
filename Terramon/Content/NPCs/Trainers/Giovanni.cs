using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Viridian City's Gym Leader (and boss of Team Rocket). Unlocked by defeating Golem.
/// </summary>
public class Giovanni : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Rhyhorn, 45),
        (NationalDexID.Dugtrio, 42),
        (NationalDexID.Nidoqueen, 44),
        (NationalDexID.Nidoking, 45),
        (NationalDexID.Rhydon, 50)
    ];

    protected override int LookAlike => NPCID.TaxCollector;

    protected override bool GateBossDefeated => Terraria.NPC.downedGolemBoss;

    protected override int RewardItemType => ModContent.ItemType<EarthBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<ExpCharm>(), 1);
}
