using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Fuchsia City's Gym Leader. Unlocked by defeating any mechanical boss.
/// </summary>
public class Koga : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Koffing, 37),
        (NationalDexID.Muk, 39),
        (NationalDexID.Koffing, 37),
        (NationalDexID.Weezing, 43)
    ];

    protected override int LookAlike => NPCID.Painter;

    protected override bool GateBossDefeated => Terraria.NPC.downedMechBossAny;

    protected override int RewardItemType => ModContent.ItemType<SoulBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<LuckyEgg>(), 1);
}
