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

    // Isa's walk cycle: 8 frames of 20x32 pixel art facing right, frame 0 doubles as the standing pose.
    // Drawn at 1x so he stays about as tall as his old sprite; set the scale to 2 for a much taller Brock.
    protected override string CustomTexture => "Terramon/Assets/NPCs/Trainers/Brock";

    protected override int CustomFrameCount => 8;

    protected override bool CustomTextureFacesRight => true;

    protected override int CustomTextureScale => 1;

    // Strolls along his stage in the Gym
    protected override int WanderRange => 4;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss1;

    protected override int RewardItemType => ModContent.ItemType<BoulderBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<GreatBallItem>(), 5);
}
