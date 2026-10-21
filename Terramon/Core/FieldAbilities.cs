using Terramon.ID;
using Terraria.Localization;

namespace Terramon.Core;

/// <summary>
///     What a Pokémon could do in the field (outside battles), like the HMs of the main games: surf, fly, dig, be
///     ridden, light up caves and push boulders. These only answer the question; the mod has no surfing, flying or
///     riding mechanics yet (see mecanicas-campo.md), so future mounts can build on these rules.
///     <para />
///     Heights are in decimeters and weights in hectograms, as in the Pokémon data (12 = 1.2 m, 540 = 54 kg).
///     Rules, decided by species (types, size) and, for an individual Pokémon, also by the moves it knows:
///     <list type="bullet">
///         <item>Surf: knows Surf, Waterfall or Dive, or is a Water type at least 0.8 m tall.</item>
///         <item>Fly: knows Fly, or is a Flying type at least 1.0 m tall (Pidgeotto, Charizard, Dragonite).</item>
///         <item>Dig: knows Dig, or is a Ground type.</item>
///         <item>Ride: at least 1.4 m tall and 40 kg (Ponyta's line, Arcanine, Tauros, Lapras, Onix...).</item>
///         <item>Flash: knows Flash, or is an Electric or Fire type.</item>
///         <item>Strength: knows Strength, or is a Fighting type, or has a base Attack of 100 or more.</item>
///     </list>
/// </summary>
public static class FieldAbilities
{
    public const ushort MinSurfHeight = 8;
    public const ushort MinFlyHeight = 10;
    public const ushort MinRideHeight = 14;
    public const ushort MinRideWeight = 400;
    public const byte MinStrengthAttack = 100;

    private static bool HasType(DatabaseV2.PokemonSchema schema, PokemonType type) =>
        schema?.Types != null && schema.Types.Contains(type);

    private static bool Knows(PokemonData data, params MoveID[] moves) =>
        data != null && !data.Moves.IsDefault && moves.Any(data.KnowsMove);

    public static bool CanSurf(DatabaseV2.PokemonSchema schema) =>
        HasType(schema, PokemonType.Water) && schema.Height >= MinSurfHeight;

    public static bool CanSurf(PokemonData data) =>
        Knows(data, MoveID.Surf, MoveID.Waterfall, MoveID.Dive) || CanSurf(data?.Schema);

    public static bool CanFly(DatabaseV2.PokemonSchema schema) =>
        HasType(schema, PokemonType.Flying) && schema.Height >= MinFlyHeight;

    public static bool CanFly(PokemonData data) => Knows(data, MoveID.Fly) || CanFly(data?.Schema);

    public static bool CanDig(DatabaseV2.PokemonSchema schema) => HasType(schema, PokemonType.Ground);

    public static bool CanDig(PokemonData data) => Knows(data, MoveID.Dig) || CanDig(data?.Schema);

    public static bool CanRide(DatabaseV2.PokemonSchema schema) =>
        schema != null && schema.Height >= MinRideHeight && schema.Weight >= MinRideWeight;

    public static bool CanRide(PokemonData data) => CanRide(data?.Schema);

    public static bool CanFlash(DatabaseV2.PokemonSchema schema) =>
        HasType(schema, PokemonType.Electric) || HasType(schema, PokemonType.Fire);

    public static bool CanFlash(PokemonData data) => Knows(data, MoveID.Flash) || CanFlash(data?.Schema);

    public static bool CanStrength(DatabaseV2.PokemonSchema schema) =>
        HasType(schema, PokemonType.Fighting) ||
        (schema?.BaseStats is { Count: > 1 } stats && stats[1] >= MinStrengthAttack);

    public static bool CanStrength(PokemonData data) =>
        Knows(data, MoveID.Strength) || CanStrength(data?.Schema);

    /// <summary>
    ///     The localized names of everything <paramref name="data" /> can do in the field, e.g. "Surf, Ride".
    ///     Empty if nothing.
    /// </summary>
    public static string Describe(PokemonData data)
    {
        if (data == null)
            return string.Empty;
        var names = new List<string>();
        if (CanSurf(data)) names.Add(Name("Surf"));
        if (CanFly(data)) names.Add(Name("Fly"));
        if (CanDig(data)) names.Add(Name("Dig"));
        if (CanRide(data)) names.Add(Name("Ride"));
        if (CanFlash(data)) names.Add(Name("Flash"));
        if (CanStrength(data)) names.Add(Name("Strength"));
        return string.Join(", ", names);
    }

    private static string Name(string key) => Language.GetTextValue("Mods.Terramon.FieldAbilities." + key);
}
