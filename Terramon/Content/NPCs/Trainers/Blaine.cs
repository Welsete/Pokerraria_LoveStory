using Terramon.Content.Items;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Cinnabar Island's Gym Leader. Unlocked by defeating Plantera.
/// </summary>
public class Blaine : GymLeader
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Growlithe, 42),
        (NationalDexID.Ponyta, 40),
        (NationalDexID.Rapidash, 42),
        (NationalDexID.Arcanine, 47)
    ];

    protected override int LookAlike => NPCID.Clothier;

    protected override bool GateBossDefeated => Terraria.NPC.downedPlantBoss;

    protected override int RewardItemType => ModContent.ItemType<VolcanoBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<FireStone>(), 1);
}
