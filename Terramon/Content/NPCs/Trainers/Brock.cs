using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Pewter City's Gym Leader. Accepts challenges once the Eye of Cthulhu has been defeated.
/// </summary>
public class Brock : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Geodude, 12),
        (NationalDexID.Onix, 14)
    ];

    protected override int LookAlike => NPCID.Guide;

    protected override string CustomTexture => "Terramon/Assets/NPCs/Trainers/Brock";

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss1;

    protected override int RewardItemType => ModContent.ItemType<BoulderBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<GreatBallItem>(), 5);
}
