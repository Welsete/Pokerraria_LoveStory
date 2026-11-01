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

    // Her own sheet, facing right, 39x54 per frame: standing, four walking frames, throwing (when she sends out a
    // Pokémon), victory (after she wins) and sitting (not used yet), cut with tools/sprites/cut_frames.py --dark-pupils --wide=1.15
    protected override string CustomTexture => "Terramon/Assets/NPCs/Trainers/Misty";

    protected override int CustomFrameCount => 8;

    protected override (int First, int Count) CustomWalkFrames => (1, 4);

    protected override int CustomThrowFrame => 5;

    protected override int CustomVictoryFrame => 6;

    protected override bool CustomTextureFacesRight => true;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss2;

    protected override int RewardItemType => ModContent.ItemType<CascadeBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<WaterStone>(), 1);
}
