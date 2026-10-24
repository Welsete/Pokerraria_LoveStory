using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.ID;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The Pokémon League Champion, faced right after the Elite Four once the Moon Lord has been defeated.
/// </summary>
public class Blue : LeagueMember
{
    protected override (ushort ID, byte Level)[] TeamSpec { get; } =
    [
        (NationalDexID.Pidgeot, 61),
        (NationalDexID.Alakazam, 59),
        (NationalDexID.Rhydon, 61),
        (NationalDexID.Gyarados, 61),
        (NationalDexID.Arcanine, 63),
        (NationalDexID.Venusaur, 65)
    ];

    protected override int LookAlike => NPCID.Golfer;

    protected override (int Min, int Max) IVRange => (25, 31);

    protected override IEnumerable<(int Type, int Stack)> FirstWinRewards(Player player)
    {
        yield return (ModContent.ItemType<MasterBallItem>(), 1);

        var shinyCharm = ModContent.ItemType<ShinyCharm>();
        if (!player.HasItem(shinyCharm))
            yield return (shinyCharm, 1);
    }

    protected override void OnWin(Player player, bool firstWin)
    {
        base.OnWin(player, firstWin);

        // Enter the Hall of Fame with the current party
        var gym = player.GetModPlayer<GymPlayer>();
        var party = player.Terramon().Party.Where(p => p != null).Select(p => p.ID).ToArray();
        gym.HallOfFame.Add((DateTime.Now, party));

        var names = string.Join(", ", player.Terramon().Party.Where(p => p != null).Select(p => p.DisplayName));
        Main.NewText(LeagueText(firstWin ? "HallOfFameFirst" : "HallOfFame", player.name, names), Color.Gold);
    }
}
