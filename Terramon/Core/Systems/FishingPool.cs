using Terramon.Content.Items;
using Terramon.Core.Loaders;
using Terramon.ID;
using Terraria.DataStructures;

namespace Terramon.Core.Systems;

/// <summary>
///     Chooses which Water-type Pokémon bites the line, by biome, depth and rod. The species is then evolved to match
///     the wild level of the spot, using the same rules as regular wild spawns.
/// </summary>
public static class FishingPool
{
    public static ushort Choose(FishingAttempt attempt, Player player)
    {
        var pool = new List<(ushort ID, float Weight)>();
        var pole = attempt.playerFishingConditions.Pole?.ModItem;
        var isSuperRod = pole is SuperRod;
        var isGoodRod = pole is GoodRod;

        if (player.ZoneBeach)
        {
            pool.Add((NationalDexID.Tentacool, 30));
            pool.Add((NationalDexID.Krabby, 25));
            pool.Add((NationalDexID.Staryu, 20));
            pool.Add((NationalDexID.Shellder, 20));
            pool.Add((NationalDexID.Horsea, 20));
            pool.Add((NationalDexID.Seel, 10));
            pool.Add((NationalDexID.Magikarp, 15));
            if (isSuperRod)
                pool.Add((NationalDexID.Lapras, 2));
        }
        else if (player.ZoneSnow)
        {
            pool.Add((NationalDexID.Seel, 30));
            pool.Add((NationalDexID.Shellder, 25));
            pool.Add((NationalDexID.Poliwag, 10));
            pool.Add((NationalDexID.Magikarp, 15));
        }
        else if (player.ZoneJungle)
        {
            pool.Add((NationalDexID.Poliwag, 30));
            pool.Add((NationalDexID.Slowpoke, 25));
            pool.Add((NationalDexID.Psyduck, 15));
            pool.Add((NationalDexID.Magikarp, 15));
        }
        else if (attempt.heightLevel >= 2) // Underground and caverns
        {
            pool.Add((NationalDexID.Poliwag, 20));
            pool.Add((NationalDexID.Slowpoke, 20));
            pool.Add((NationalDexID.Goldeen, 15));
            pool.Add((NationalDexID.Krabby, 10));
            pool.Add((NationalDexID.Magikarp, 15));
        }
        else // Lakes and ponds
        {
            pool.Add((NationalDexID.Goldeen, 30));
            pool.Add((NationalDexID.Poliwag, 25));
            pool.Add((NationalDexID.Psyduck, 25));
            pool.Add((NationalDexID.Magikarp, 35));
            if (player.ZoneHallow)
                pool.Add((NationalDexID.Staryu, 15));
        }

        if (isSuperRod && !player.ZoneBeach)
            pool.Add((NationalDexID.Dratini, 3));

        // Weaker rods mostly hook Magikarp, like in the main games
        var magikarpMultiplier = isSuperRod ? 0.5f : isGoodRod ? 1f : 3f;
        for (var i = 0; i < pool.Count; i++)
            if (pool[i].ID == NationalDexID.Magikarp)
                pool[i] = (pool[i].ID, pool[i].Weight * magikarpMultiplier);

        pool.RemoveAll(entry => !PokemonEntityLoader.IDToNPCType.ContainsKey(entry.ID));
        if (pool.Count == 0)
            return 0;

        var roll = Main.rand.NextFloat(pool.Sum(entry => entry.Weight));
        var chosen = pool[^1].ID;
        foreach (var (id, weight) in pool)
        {
            if (roll < weight)
            {
                chosen = id;
                break;
            }

            roll -= weight;
        }

        var level = WildPokemonLevels.RollLevel(attempt.X, attempt.Y);
        return WildPokemonLevels.GetStageForLevel(chosen, level);
    }
}
