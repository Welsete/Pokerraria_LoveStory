using Terramon.Core.Loaders;
using Terraria.DataStructures;

namespace Terramon.Core.Systems;

/// <summary>
///     Levels of wild Pokémon, scaled by the furthest boss progression reached in the world and by depth,
///     and the evolution stage that matches a level.
/// </summary>
public static class WildPokemonLevels
{
    private static Dictionary<ushort, ushort> _prevolutions;

    // Level picked by the spawn pool for the spawn happening this tick, so the spawned NPC uses the same level
    // that was used to choose its evolution stage.
    private static byte _pendingLevel;
    private static ulong _pendingTick = ulong.MaxValue;

    /// <summary>
    ///     The level range of wild Pokémon for the current world progression, before depth bonuses.
    /// </summary>
    public static (int Min, int Max) GetProgressionRange()
    {
        if (NPC.downedMoonlord) return (50, 60);
        if (NPC.downedAncientCultist) return (45, 55);
        if (NPC.downedGolemBoss) return (40, 50);
        if (NPC.downedPlantBoss) return (35, 45);
        if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) return (30, 40);
        if (NPC.downedMechBossAny) return (25, 35);
        if (Main.hardMode) return (20, 30);
        if (NPC.downedBoss3) return (12, 20);
        if (NPC.downedBoss2) return (8, 16);
        if (NPC.downedBoss1) return (5, 12);
        return (2, 7);
    }

    /// <summary>
    ///     Extra levels for Pokémon found deeper in the world.
    /// </summary>
    public static int GetDepthBonus(int tileY)
    {
        if (tileY > Main.UnderworldLayer) return 6;
        if (tileY > Main.rockLayer) return 4;
        if (tileY > Main.worldSurface) return 2;
        return 0;
    }

    public static (int Min, int Max) GetLevelRange(int tileY)
    {
        var (min, max) = GetProgressionRange();
        var bonus = GetDepthBonus(tileY);
        return (Math.Clamp(min + bonus, 1, Terramon.MaxPokemonLevel), Math.Clamp(max + bonus, 1, Terramon.MaxPokemonLevel));
    }

    public static byte RollLevel(int tileY)
    {
        var (min, max) = GetLevelRange(tileY);
        return (byte)Main.rand.Next(min, max + 1);
    }

    private static bool IsLoaded(ushort id) => PokemonEntityLoader.IDToNPCType?.ContainsKey(id) ?? false;

    private static Dictionary<ushort, ushort> Prevolutions
    {
        get
        {
            if (_prevolutions != null)
                return _prevolutions;

            _prevolutions = [];
            foreach (var (id, schema) in Terramon.DatabaseV2.Pokemon)
            {
                if (schema.Evolution is not { AtLevel: > 0 } evo || evo.ID == id)
                    continue;
                _prevolutions.TryAdd(evo.ID, id);
            }

            return _prevolutions;
        }
    }

    /// <summary>
    ///     Returns the species of the given evolution line that a wild Pokémon of this level should be,
    ///     going up through level-up evolutions or down to prevolutions. Only loaded species are returned.
    /// </summary>
    public static ushort GetStageForLevel(ushort id, byte level)
    {
        var current = id;
        var db = Terramon.DatabaseV2;

        // Too low for this stage: go back to the prevolution
        for (var i = 0; i < 3; i++)
        {
            if (!Prevolutions.TryGetValue(current, out var pre) || !IsLoaded(pre))
                break;
            var preEvo = db.GetPokemon(pre)?.Evolution;
            if (preEvo == null || level >= preEvo.AtLevel)
                break;
            current = pre;
        }

        // High enough to have evolved
        for (var i = 0; i < 3; i++)
        {
            var evo = db.GetPokemon(current)?.Evolution;
            if (evo is not { AtLevel: > 0 } || evo.ID == current || level < evo.AtLevel || !IsLoaded(evo.ID))
                break;
            current = evo.ID;
        }

        return current;
    }

    /// <summary>
    ///     The levels at which a wild Pokémon of this species makes sense (between its prevolution's evolution level
    ///     and its own evolution level).
    /// </summary>
    public static (byte Min, byte Max) GetStageBounds(ushort id)
    {
        var db = Terramon.DatabaseV2;
        byte min = 1, max = (byte)Terramon.MaxPokemonLevel;
        if (Prevolutions.TryGetValue(id, out var pre) && db.GetPokemon(pre)?.Evolution is { AtLevel: > 0 } preEvo)
            min = preEvo.AtLevel;
        if (db.GetPokemon(id)?.Evolution is { AtLevel: > 1 } evo && evo.ID != id && IsLoaded(evo.ID))
            max = (byte)(evo.AtLevel - 1);
        if (max < min)
            max = min;
        return (min, max);
    }

    /// <summary>
    ///     Called by the spawn pool with the level it used to choose evolution stages.
    /// </summary>
    public static void SetPendingSpawnLevel(byte level)
    {
        _pendingLevel = level;
        _pendingTick = Main.GameUpdateCount;
    }

    /// <summary>
    ///     The level for a newly spawned wild Pokémon of the given species at the given position.
    /// </summary>
    public static byte GetSpawnLevel(ushort id, Vector2 worldPosition, IEntitySource source)
    {
        byte level;
        if (source is EntitySource_SpawnNPC && _pendingTick == Main.GameUpdateCount)
        {
            level = _pendingLevel;
            _pendingTick = ulong.MaxValue;
        }
        else
        {
            level = RollLevel((int)(worldPosition.Y / 16f));
        }

        var (min, max) = GetStageBounds(id);
        return (byte)Math.Clamp(level, min, max);
    }
}
