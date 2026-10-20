using Terramon.Content.Items;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     A member of the Elite Four, or the Champion. They're fought in a single run: beating one makes the next
///     appear, and losing to any of them sends the player back to the first one.
/// </summary>
public abstract class LeagueMember : TrainerNPC
{
    /// <summary>
    ///     Delay (in ticks) before the next opponent of the run shows up.
    /// </summary>
    private const int NextOpponentDelay = 4 * 60;

    // The League's Pokémon are very well trained
    protected override (int Min, int Max) IVRange => (20, 31);

    // Make room for the next opponent quickly
    protected override int LeaveDelayAfterBattle => 10 * 60;

    /// <summary>
    ///     The position of this trainer in the run: 0-3 for the Elite Four, 4 for the Champion.
    /// </summary>
    public int RunIndex => Array.IndexOf(League.EliteFourAndChampion, Name);

    protected static string LeagueText(string key, params object[] args)
        => Language.GetTextValue("Mods.Terramon.NPCs.Trainers.League." + key, args);

    /// <summary>
    ///     Whether the player may start a League run (and so challenge the first Elite Four member).
    /// </summary>
    public static bool CanEnterLeague(Player player)
        => League.HasAllBadges(player) && Terraria.NPC.downedAncientCultist;

    public override bool CanChallenge(Player player)
    {
        var index = RunIndex;
        if (index == 0)
            return CanEnterLeague(player);
        if (index == League.EliteFourAndChampion.Length - 1 && !Terraria.NPC.downedMoonlord)
            return false;
        return player.GetModPlayer<GymPlayer>().LeagueRunProgress == index;
    }

    protected override IEnumerable<(int Type, int Stack)> FirstWinRewards(Player player)
    {
        yield return (ModContent.ItemType<RareCandy>(), 3);
    }

    protected override void OnWin(Player player, bool firstWin)
    {
        var gym = player.GetModPlayer<GymPlayer>();
        var index = RunIndex;
        var nextIndex = index + 1;
        if (nextIndex >= League.EliteFourAndChampion.Length)
        {
            gym.LeagueRunProgress = 0;
            return;
        }

        gym.LeagueRunProgress = nextIndex;
        var next = League.EliteFourAndChampion[nextIndex];
        var nextType = League.TrainerType(next);
        var nextName = Lang.GetNPCNameValue(nextType);

        // The Champion only accepts challengers strong enough to have beaten the Moon Lord
        if (nextIndex == League.EliteFourAndChampion.Length - 1 && !Terraria.NPC.downedMoonlord)
        {
            Main.NewText(LeagueText("ChampionNeedsMoonLord", nextName), Color.Orange);
            return;
        }

        Main.NewText(LeagueText("NextOpponent", nextName), Color.Orange);
        gym.ScheduleSpawn(nextType, NextOpponentDelay);
    }

    protected override void OnLoss(Player player)
    {
        var gym = player.GetModPlayer<GymPlayer>();
        gym.LeagueRunProgress = 0;
        gym.CancelScheduledSpawn();
        Main.NewText(LeagueText("RunOver"), Color.OrangeRed);
    }
}
