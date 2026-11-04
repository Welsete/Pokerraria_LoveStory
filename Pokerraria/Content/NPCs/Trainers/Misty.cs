using Pokerraria.Content.Items;
using Pokerraria.ID;

namespace Pokerraria.Content.NPCs.Trainers;

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

    // Her own sheet, facing right, 39x54 per frame: standing, six walking frames, throwing (when she sends out a
    // Pokémon), victory (after she wins) and sitting (not used yet). Cut with tools/sprites/cut_frames.py --dark-pupils
    // --wide=1.15 --own-scale=andando, then tools/sprites/misty_walk.py builds the walk: a step with the near leg in
    // front and the same step with the legs' shading swapped, so both legs take turns going forward
    protected override string CustomTexture => "Pokerraria/Assets/NPCs/Trainers/Misty";

    protected override int CustomFrameCount => 10;

    protected override (int First, int Count) CustomWalkFrames => (1, 6);

    protected override int CustomThrowFrame => 7;

    protected override int CustomVictoryFrame => 8;

    protected override bool CustomTextureFacesRight => true;

    // She stands at her spot in the Gym (no WanderRange): her walk frames are kept for later

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss2;

    protected override int RewardItemType => ModContent.ItemType<CascadeBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<WaterStone>(), 1);
}
