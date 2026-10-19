using Terraria.ModLoader.IO;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     Keeps track of which trainers (and Gym Leaders) a player has defeated.
/// </summary>
public class GymPlayer : ModPlayer
{
    /// <summary>
    ///     Internal names of the trainers this player has defeated at least once.
    /// </summary>
    public HashSet<string> Badges { get; } = [];

    public override void SaveData(TagCompound tag)
    {
        tag["badges"] = Badges.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        Badges.Clear();
        foreach (var badge in tag.GetList<string>("badges"))
            Badges.Add(badge);
    }
}
