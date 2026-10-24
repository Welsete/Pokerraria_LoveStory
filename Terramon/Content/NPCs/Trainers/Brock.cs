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

    // Close to his team in the games: Geodude rolls up with Defense Curl into Rollout, Onix slows foes with Rock Tomb
    protected override Dictionary<ushort, MoveID[]> TeamMoves { get; } = new()
    {
        [NationalDexID.Geodude] = [MoveID.RockThrow, MoveID.Bulldoze, MoveID.DefenseCurl, MoveID.Rollout],
        [NationalDexID.Onix] = [MoveID.RockTomb, MoveID.SmackDown, MoveID.Bind, MoveID.RockPolish]
    };

    protected override int LookAlike => NPCID.Guide;

    // Isa's walk cycle: 8 frames of her 20x32 pixel art (facing left), stored at 1.5x (30x48 per frame) so he is
    // about 45 pixels tall, a little taller than the player (42). Frame 0 doubles as the standing pose.
    protected override string CustomTexture => "Terramon/Assets/NPCs/Trainers/Brock";

    protected override int CustomFrameCount => 8;

    // Strolls along his stage in the Gym
    protected override int WanderRange => 4;

    protected override bool GateBossDefeated => Terraria.NPC.downedBoss1;

    protected override int RewardItemType => ModContent.ItemType<BoulderBadge>();

    protected override (int Type, int Stack) PrizeItem => (ModContent.ItemType<GreatBallItem>(), 5);
}
