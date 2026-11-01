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

    // Her own sheet, facing right, 39x54 per frame: standing, eight walking frames, throwing (when she sends out a
    // Pokémon), victory (after she wins) and sitting (not used yet), cut with tools/sprites/cut_frames.py --dark-pupils
    // --wide=1.15 --own-scale=andando (the walking frames were drawn bigger than the rest)
    protected override string CustomTexture => "Terramon/Assets/NPCs/Trainers/Misty";

    protected override int CustomFrameCount => 12;

    protected override (int First, int Count) CustomWalkFrames => (1, 8);

    protected override int CustomThrowFrame => 9;

    protected override int CustomVictoryFrame => 10;

    protected override bool CustomTextureFacesRight => true;

    // Strolls around her spot in the Gym, like Brock
    protected override int WanderRange => 4;

    // The foot on the ground goes back about 4 pixels per frame, so she moves 4 pixels per frame and it stays put
    protected override float WanderSpeed => 1f;

    protected override double WalkPixelsPerFrame => 4.0;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss2;

    protected override int RewardItemType => ModContent.ItemType<CascadeBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<WaterStone>(), 1);
}
