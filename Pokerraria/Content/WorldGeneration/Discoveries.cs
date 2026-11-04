using Pokerraria.Content.Configs;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Pokerraria.Content.WorldGeneration;

/// <summary>
///     The places of the world a player has found by going there (each one counts once it's on screen). Nothing is
///     told when entering a world: the Gyms, the Pokémon Center and the League only show on the map once found. Kept
///     per character and per world. In Debug Mode the map shows everything.
/// </summary>
public class DiscoveryPlayer : ModPlayer
{
    // "worldId|place"
    private readonly HashSet<string> _found = [];
    private int _timer;

    private static string World => Main.ActiveWorldFileData?.UniqueId.ToString() ?? "";

    /// <summary>
    ///     Whether the local player has found this place in this world (always true in Debug Mode).
    /// </summary>
    public static bool Knows(string place)
    {
        if (GameplayConfig.Instance.DebugMode)
            return true;
        var player = Main.LocalPlayer;
        return player != null && player.active && player.GetModPlayer<DiscoveryPlayer>()._found.Contains(World + "|" + place);
    }

    /// <summary>
    ///     The places to find and their tiles.
    /// </summary>
    private static IEnumerable<(string Place, Rectangle Area)> Places()
    {
        if (BrockGymSystem.Exists)
        {
            yield return ("BrockGym", BrockGymSystem.Interior);
            var entrance = BrockGymSystem.SurfaceEntrance;
            if (entrance != Point.Zero)
                yield return ("BrockEntrance", BrockGymSystem.HasDungeon
                    ? BrockDigSite.Area(entrance.X, entrance.Y)
                    : new Rectangle(entrance.X - 8, entrance.Y - 10, 17, 12));
        }

        if (MistyGymSystem.Exists)
            yield return ("MistyGym", MistyGymSystem.Interior);
        if (PokemonCenterSystem.Exists)
            yield return ("PokemonCenter", PokemonCenterSystem.Interior);
        if (GymBuildingsSystem.GymPosition != Point.Zero)
            yield return ("Gym", new Rectangle(GymBuildingsSystem.GymPosition.X - 10, GymBuildingsSystem.GymPosition.Y - 10, 20, 14));
        if (GymBuildingsSystem.LeaguePosition != Point.Zero)
            yield return ("League", new Rectangle(GymBuildingsSystem.LeaguePosition.X - 10, GymBuildingsSystem.LeaguePosition.Y - 10, 20, 14));
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || ++_timer < 30)
            return;
        _timer = 0;
        var at = Player.Center.ToTileCoordinates();
        foreach (var (place, area) in Places())
        {
            // On screen: about 60 tiles across and 34 high around the player
            var seen = area;
            seen.Inflate(30, 17);
            if (!seen.Contains(at) || !_found.Add(World + "|" + place))
                continue;
            Main.NewText(Language.GetTextValue("Mods.Pokerraria.Discoveries." + place), new Color(255, 215, 90));
        }
    }

    public override void SaveData(TagCompound tag)
    {
        if (_found.Count > 0)
            tag["found"] = _found.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _found.Clear();
        foreach (var entry in tag.GetList<string>("found"))
            _found.Add(entry);
    }
}
