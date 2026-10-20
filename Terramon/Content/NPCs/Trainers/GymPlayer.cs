using Terraria.ModLoader.IO;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Keeps track of which trainers (and Gym Leaders) a player has defeated, and their Pokémon League run.
/// </summary>
public class GymPlayer : ModPlayer
{
    /// <summary>
    ///     Internal names of the trainers this player has defeated at least once.
    /// </summary>
    public HashSet<string> Badges { get; } = [];

    /// <summary>
    ///     How many opponents of the current Elite Four run have been beaten (0 = no run going, 4 = only the
    ///     Champion left). Not saved: leaving the world ends the run, as does losing a battle.
    /// </summary>
    public int LeagueRunProgress { get; set; }

    /// <summary>
    ///     Every time this player became Champion: the date and the species in their party.
    /// </summary>
    public List<(DateTime Date, ushort[] Party)> HallOfFame { get; } = [];

    private int _pendingSpawnType;
    private int _pendingSpawnTimer;

    /// <summary>
    ///     Summons a trainer next to this player after a short delay (the next opponent of a League run).
    /// </summary>
    public void ScheduleSpawn(int npcType, int delay)
    {
        _pendingSpawnType = npcType;
        _pendingSpawnTimer = delay;
    }

    public void CancelScheduledSpawn() => _pendingSpawnType = 0;

    /// <summary>
    ///     Forgets every badge, the current League run and the Hall of Fame (used by the test panel).
    /// </summary>
    public void ResetLeague()
    {
        Badges.Clear();
        HallOfFame.Clear();
        LeagueRunProgress = 0;
        CancelScheduledSpawn();
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || _pendingSpawnType == 0)
            return;

        if (--_pendingSpawnTimer > 0)
            return;

        var type = _pendingSpawnType;
        _pendingSpawnType = 0;
        League.RequestSpawn(Player, type);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["badges"] = Badges.ToList();
        if (HallOfFame.Count > 0)
        {
            tag["hallOfFame"] = HallOfFame
                .Select(e => new TagCompound
                {
                    ["date"] = e.Date.ToBinary(),
                    ["party"] = e.Party.Select(id => (int)id).ToList()
                })
                .ToList();
        }
    }

    public override void LoadData(TagCompound tag)
    {
        Badges.Clear();
        foreach (var badge in tag.GetList<string>("badges"))
            Badges.Add(badge);

        HallOfFame.Clear();
        foreach (var entry in tag.GetList<TagCompound>("hallOfFame"))
        {
            var date = DateTime.FromBinary(entry.GetLong("date"));
            var party = entry.GetList<int>("party").Select(id => (ushort)id).ToArray();
            HallOfFame.Add((date, party));
        }
    }
}
