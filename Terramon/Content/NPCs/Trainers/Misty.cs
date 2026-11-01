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

    // Strolls around her spot in the Gym, like Brock
    protected override int WanderRange => 4;

    // Her frames 1-3 are all strides with the front foot in the same spot, so playing them in a row made her foot slide
    // forward. Instead: stride, legs passing, stride, legs passing. The planted foot goes about 20 pixels back over two
    // frames, so she moves 10 pixels per frame and her feet stay put on the ground
    protected override int[] CustomWalkSequence { get; } = [1, 4, 2, 4];

    protected override float WanderSpeed => 1.4f;

    protected override double WalkPixelsPerFrame => 10.0;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss2;

    protected override int RewardItemType => ModContent.ItemType<CascadeBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<WaterStone>(), 1);
}
