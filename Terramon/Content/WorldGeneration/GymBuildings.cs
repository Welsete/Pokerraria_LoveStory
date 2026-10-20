using Terramon.Content.Tiles.Interactive;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     Builds a Pokémon Gym and a Pokémon League building on the surface of new worlds, and remembers where they are.
///     Worlds made before this existed simply have no buildings (players can craft the pedestals instead).
/// </summary>
public class GymBuildingsSystem : ModSystem
{
    /// <summary>
    ///     Tile position of the Gym pedestal (bottom-left tile), or <see cref="Point.Zero" /> if there's none.
    /// </summary>
    public static Point GymPosition { get; internal set; }

    /// <summary>
    ///     Tile position of the League pedestal (bottom-left tile), or <see cref="Point.Zero" /> if there's none.
    /// </summary>
    public static Point LeaguePosition { get; internal set; }

    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        // After the spawn point is known, but before trees, grass and plants decorate the surface
        var index = tasks.FindIndex(pass => pass.Name.Equals("Spawn Point"));
        if (index == -1)
            index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup")) - 1;
        if (index < 0)
            index = tasks.Count - 1;

        tasks.Insert(index + 1, new GymBuildingsPass("Terramon Gyms", 10f));
        totalWeight += 10f;
    }

    public override void ClearWorld()
    {
        GymPosition = Point.Zero;
        LeaguePosition = Point.Zero;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (GymPosition != Point.Zero)
        {
            tag["gymX"] = GymPosition.X;
            tag["gymY"] = GymPosition.Y;
        }

        if (LeaguePosition != Point.Zero)
        {
            tag["leagueX"] = LeaguePosition.X;
            tag["leagueY"] = LeaguePosition.Y;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        GymPosition = tag.ContainsKey("gymX") ? new Point(tag.GetInt("gymX"), tag.GetInt("gymY")) : Point.Zero;
        LeaguePosition = tag.ContainsKey("leagueX")
            ? new Point(tag.GetInt("leagueX"), tag.GetInt("leagueY"))
            : Point.Zero;
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(GymPosition.X);
        writer.Write(GymPosition.Y);
        writer.Write(LeaguePosition.X);
        writer.Write(LeaguePosition.Y);
    }

    public override void NetReceive(BinaryReader reader)
    {
        GymPosition = new Point(reader.ReadInt32(), reader.ReadInt32());
        LeaguePosition = new Point(reader.ReadInt32(), reader.ReadInt32());
    }
}

/// <summary>
///     Tells each player once per world where the Gym and the League were built.
/// </summary>
public class GymBuildingsPlayer : ModPlayer
{
    // Worlds (by unique id) this player was already told about
    private readonly HashSet<string> _toldWorlds = [];

    public override void OnEnterWorld()
    {
        var world = Main.ActiveWorldFileData?.UniqueId.ToString();
        if (string.IsNullOrEmpty(world) || _toldWorlds.Contains(world))
            return;

        var told = false;
        told |= Announce(GymBuildingsSystem.GymPosition, "Gym");
        told |= Announce(GymBuildingsSystem.LeaguePosition, "League");
        if (told)
            _toldWorlds.Add(world);
    }

    private static bool Announce(Point position, string kind)
    {
        if (position == Point.Zero)
            return false;

        var offset = position.X - Main.spawnTileX;
        var direction = Language.GetTextValue("Mods.Terramon.Pedestals." + (offset < 0 ? "West" : "East"));
        Main.NewText(Language.GetTextValue($"Mods.Terramon.Pedestals.{kind}Built", Math.Abs(offset), direction),
            new Color(255, 215, 90));
        return true;
    }

    public override void SaveData(TagCompound tag)
    {
        if (_toldWorlds.Count > 0)
            tag["gymBuildingsTold"] = _toldWorlds.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _toldWorlds.Clear();
        foreach (var world in tag.GetList<string>("gymBuildingsTold"))
            _toldWorlds.Add(world);
    }
}

/// <summary>
///     The world generation step that builds the Gym and the League.
/// </summary>
public class GymBuildingsPass(string name, float loadWeight) : GenPass(name, loadWeight)
{
    private const int Height = 12; // From the floor to the roof, both included
    private const int GymWidth = 23;
    private const int LeagueWidth = 27;
    private const int MinDistanceFromSpawn = 90;
    private const int MinDistanceBetween = 120;

    private readonly record struct Style(ushort Block, ushort Roof, ushort Wall, int TorchStyle);

    private static readonly Style GymStyle = new(TileID.GrayBrick, TileID.RedBrick, WallID.GrayBrick, 0);
    private static readonly Style LeagueStyle = new(TileID.MarbleBlock, TileID.GoldBrick, WallID.MarbleBlock, 0);

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Language.GetTextValue("Mods.Terramon.WorldGen.GymsPass");
        GymBuildingsSystem.GymPosition = Point.Zero;
        GymBuildingsSystem.LeaguePosition = Point.Zero;

        try
        {
            if (TryBuild(GymWidth, GymStyle, ModContent.TileType<GymPedestal>(), null, out var gym))
                GymBuildingsSystem.GymPosition = gym;
            else
                Terramon.Instance.Logger.Warn("Couldn't find a place for the Pokémon Gym, skipping it.");

            progress.Set(0.5);

            int? avoid = gym == Point.Zero ? null : gym.X;
            if (TryBuild(LeagueWidth, LeagueStyle, ModContent.TileType<LeaguePedestal>(), avoid, out var league))
                GymBuildingsSystem.LeaguePosition = league;
            else
                Terramon.Instance.Logger.Warn("Couldn't find a place for the Pokémon League, skipping it.");
        }
        catch (Exception e)
        {
            // A missing building is better than a world that can't be generated
            Terramon.Instance.Logger.Error("Building the Pokémon Gym and League failed", e);
        }

        progress.Set(1);
    }

    private static bool TryBuild(int width, Style style, int pedestalType, int? avoidX, out Point pedestal)
    {
        pedestal = Point.Zero;
        var maxOffset = Math.Max(MinDistanceFromSpawn + 50, Main.maxTilesX / 3);
        for (var attempt = 0; attempt < 400; attempt++)
        {
            // Not too far from spawn so players come across them, on a random side
            var offset = WorldGen.genRand.Next(MinDistanceFromSpawn, maxOffset);
            var left = Main.spawnTileX + (WorldGen.genRand.NextBool() ? offset : -offset - width);
            if (avoidX is { } other && Math.Abs(left + width / 2 - other) < MinDistanceBetween)
                continue;

            if (!TryFindFloor(left, width, out var floorY))
                continue;

            pedestal = Build(left, floorY, width, style, pedestalType);
            GenVars.structures?.AddProtectedStructure(
                new Rectangle(left - 2, floorY - Height - 3, width + 4, Height + 10), 4);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks that the ground under [left, left + width) is flat, dry, natural surface, and returns the floor row.
    /// </summary>
    private static bool TryFindFloor(int left, int width, out int floorY)
    {
        floorY = 0;
        var right = left + width - 1;

        // Stay away from the oceans at both ends of the world and from the Dungeon
        const int oceanMargin = 400;
        if (left < oceanMargin || right > Main.maxTilesX - oceanMargin)
            return false;
        if (Math.Abs(left + width / 2 - Main.dungeonX) < 150)
            return false;

        var min = int.MaxValue;
        var max = int.MinValue;
        var sum = 0;
        for (var x = left - 1; x <= right + 1; x++)
        {
            var y = SurfaceY(x);
            if (y < 0)
                return false;
            min = Math.Min(min, y);
            max = Math.Max(max, y);
            sum += y;
        }

        if (max - min > 4)
            return false;

        floorY = (int)Math.Round(sum / (double)(width + 2));

        // Don't overlap structures placed by other passes (or mods)
        var area = new Rectangle(left - 2, floorY - Height - 3, width + 4, Height + 10);
        if (GenVars.structures != null && !GenVars.structures.CanPlace(area))
            return false;

        // Make sure nothing important (chests, temples, houses, living trees, water...) is in the way
        for (var x = left - 2; x <= right + 2; x++)
        {
            for (var y = floorY - Height - 3; y <= floorY + 6; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.LiquidAmount > 0 && y <= floorY)
                    return false;
                if (tile.HasTile && !IsNaturalGround(tile.TileType))
                    return false;
                if (tile.WallType != WallID.None && y < floorY - 2 && !IsNaturalWall(tile.WallType))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     The first solid tile from the top of the world in this column, or -1 if it isn't surface ground.
    /// </summary>
    private static int SurfaceY(int x)
    {
        for (var y = 20; y < Main.worldSurface; y++)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                continue;

            // Floating islands are made of clouds, and lie well above the rest of the surface
            if (!IsNaturalGround(tile.TileType) || y < GenVars.worldSurfaceLow - 5)
                return -1;
            return y;
        }

        return -1;
    }

    private static bool IsNaturalGround(ushort type)
    {
        if (TileID.Sets.Ore[type] || Main.tileCut[type])
            return true;

        return type is TileID.Dirt or TileID.Stone or TileID.Grass or TileID.Sand or TileID.Mud
            or TileID.JungleGrass or TileID.ClayBlock or TileID.SnowBlock or TileID.IceBlock or TileID.Ebonstone
            or TileID.CorruptGrass or TileID.Crimstone or TileID.CrimsonGrass or TileID.Ebonsand
            or TileID.Crimsand or TileID.Silt or TileID.Slush or TileID.HardenedSand or TileID.Sandstone
            or TileID.Pearlstone or TileID.HallowedGrass or TileID.Pearlsand;
    }

    private static bool IsNaturalWall(ushort type)
    {
        return type is WallID.DirtUnsafe or WallID.Dirt or WallID.GrassUnsafe or WallID.FlowerUnsafe
            or WallID.Stone or WallID.MudUnsafe or WallID.SnowWallUnsafe or WallID.IceUnsafe;
    }

    /// <summary>
    ///     Builds one building with its floor on row floorY, and returns where its pedestal is.
    /// </summary>
    private static Point Build(int left, int floorY, int width, Style style, int pedestalType)
    {
        var right = left + width - 1;
        var top = floorY - Height + 1;

        // Clear the inside and a little room around the roof
        for (var x = left - 2; x <= right + 2; x++)
        {
            for (var y = top - 4; y < floorY; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearEverything();
            }
        }

        // Floor, with a foundation down to the ground so it never floats
        for (var x = left - 1; x <= right + 1; x++)
        {
            for (var y = floorY; y <= floorY + 8; y++)
            {
                var solidBelow = y > floorY && Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType];
                if (solidBelow)
                    break;
                PlaceBlock(x, y, style.Block);
            }
        }

        // Side walls and back wall
        for (var y = top + 1; y < floorY; y++)
        {
            PlaceBlock(left, y, style.Block);
            PlaceBlock(right, y, style.Block);
            for (var x = left + 1; x < right; x++)
                WorldGen.PlaceWall(x, y, style.Wall, true);
        }

        // A stepped roof
        for (var x = left - 1; x <= right + 1; x++)
            PlaceBlock(x, top, style.Roof);
        for (var x = left + 1; x <= right - 1; x++)
            PlaceBlock(x, top - 1, style.Roof);
        for (var x = left + 4; x <= right - 4; x++)
            PlaceBlock(x, top - 2, style.Roof);

        // Doors on both sides: clear a 3 tiles tall opening and put a door in it
        foreach (var x in new[] { left, right })
        {
            for (var y = floorY - 3; y < floorY; y++)
                Main.tile[x, y].ClearTile();
            WorldGen.PlaceTile(x, floorY - 2, TileID.ClosedDoor, true, true);
        }

        // A balcony of platforms along both side walls
        var balconyY = floorY - 6;
        for (var x = left + 1; x <= left + 4; x++)
            WorldGen.PlaceTile(x, balconyY, TileID.Platforms, true, true);
        for (var x = right - 4; x <= right - 1; x++)
            WorldGen.PlaceTile(x, balconyY, TileID.Platforms, true, true);

        // Torches
        foreach (var x in new[] { left + 2, right - 2, left + width / 2 - 4, left + width / 2 + 4 })
            WorldGen.PlaceTile(x, floorY - 3, TileID.Torches, true, false, -1, style.TorchStyle);
        foreach (var x in new[] { left + 2, right - 2 })
            WorldGen.PlaceTile(x, balconyY - 2, TileID.Torches, true, false, -1, style.TorchStyle);

        // The pedestal in the middle, on a little stage
        var pedestalX = left + width / 2 - 1;
        for (var x = pedestalX - 2; x <= pedestalX + 3; x++)
            WorldGen.PlaceTile(x, floorY - 1, TileID.Platforms, true, true);
        WorldGen.PlaceObject(pedestalX, floorY - 2, pedestalType, true);

        WorldGen.RangeFrame(left - 2, top - 4, right + 2, floorY + 8);

        var pedestal = new Point(pedestalX, floorY - 2);
        if (Main.tile[pedestal.X, pedestal.Y].TileType != pedestalType || !Main.tile[pedestal.X, pedestal.Y].HasTile)
            Terramon.Instance.Logger.Warn($"The pedestal at {pedestal} couldn't be placed.");
        else
            Terramon.Instance.Logger.Info($"Built a Pokémon building at {pedestal}.");

        return pedestal;
    }

    private static void PlaceBlock(int x, int y, ushort type)
    {
        var tile = Main.tile[x, y];
        tile.ClearTile();
        tile.HasTile = true;
        tile.TileType = type;
        tile.Slope = SlopeType.Solid;
        tile.IsHalfBlock = false;
    }
}
