using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Saffron City's Gym Leader. Unlocked by defeating all three mechanical bosses.
/// </summary>
public class Sabrina : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Kadabra, 38),
        (NationalDexID.MrMime, 37),
        (NationalDexID.Venomoth, 38),
        (NationalDexID.Alakazam, 43)
    ];

    protected override int LookAlike => NPCID.Nurse;

    protected override bool GateBossDefeated => Terraria.NPC.downedMechBoss1 && Terraria.NPC.downedMechBoss2 && Terraria.NPC.downedMechBoss3;

    protected override int RewardItemType => ModContent.ItemType<MarshBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<UltraBallItem>(), 5);
}
