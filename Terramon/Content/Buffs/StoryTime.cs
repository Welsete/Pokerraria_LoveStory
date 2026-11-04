namespace Terramon.Content.Buffs;

/// <summary>
///     "Story time": while it lasts, no enemies or wild Pokémon spawn near the player, so a diary page can be read in
///     peace. Given by reading a page and while the journal is open.
/// </summary>
public class StoryTime : ModBuff
{
    public override string Texture => "Terramon/Assets/Buffs/StoryTime";

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
    }
}

/// <summary>
///     Stops spawns around players under <see cref="StoryTime" />.
/// </summary>
public class StoryTimeSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!player.HasBuff<StoryTime>())
            return;
        maxSpawns = 0;
        spawnRate = 100000;
    }
}
