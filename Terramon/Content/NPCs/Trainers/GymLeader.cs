namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     One of the eight Gym Leaders. Each can only be challenged once its gate boss is defeated and the player
///     holds the previous Gym's badge, so the Gyms are always beaten in order.
/// </summary>
public abstract class GymLeader : TrainerNPC
{
    /// <summary>
    ///     Whether the Terraria boss that unlocks this Gym has been defeated in this world.
    /// </summary>
    protected abstract bool GateBossDefeated { get; }

    /// <summary>
    ///     The item given (besides the badge) the first time this Gym Leader is beaten.
    /// </summary>
    protected virtual (int Type, int Stack) PrizeItem => (0, 0);

    /// <summary>
    ///     The position of this Gym in the League order, starting at 0 for Brock.
    /// </summary>
    public int GymIndex => Array.IndexOf(League.GymLeaders, Name);

    public override bool CanChallenge(Player player)
    {
        if (!GateBossDefeated)
            return false;

        var index = GymIndex;
        return index <= 0 || player.GetModPlayer<GymPlayer>().Badges.Contains(League.GymLeaders[index - 1]);
    }

    protected override IEnumerable<(int Type, int Stack)> FirstWinRewards(Player player)
    {
        yield return PrizeItem;
    }
}
