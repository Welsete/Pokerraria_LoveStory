namespace Terramon.Content.Buffs;

/// <summary>
///     From a Repel: no wild Pokémon spawn near the player while it lasts (see NPCSpawnController).
/// </summary>
public class RepelBuff : ModBuff
{
    public override string Texture => "Terramon/Assets/Buffs/RepelBuff";
}

/// <summary>
///     A Repel also keeps the other creatures down: half as many, half as often.
/// </summary>
public class RepelSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!player.HasBuff<RepelBuff>())
            return;
        spawnRate *= 2;
        maxSpawns = Math.Max(1, maxSpawns / 2);
    }
}
