using Terramon.Content.NPCs.Trainers;
using Terramon.Content.Tiles.Paintings;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     Brock's Gym: a stone hall dug into the caverns not far from spawn, the first stop of the Gym route.
///     Brock waits inside whenever a player walks in.
/// </summary>
public class BrockGymSystem : ModSystem
{
    /// <summary>
    ///     The inside of the gym in tile coordinates, or <see cref="Rectangle.Empty" /> if this world has none.
    /// </summary>
    public static Rectangle Interior { get; internal set; }

    /// <summary>
    ///     Where Brock stands: the tile his feet rest on.
    /// </summary>
    public static Point LeaderSpot { get; internal set; }

    public static bool Exists => Interior != Rectangle.Empty;

    // Whether a player was inside last time we looked, so Brock only shows up when someone walks in
    private static bool _occupied;
    private static int _checkTimer;

    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        // After caves and other structures are dug out, before the final cleanup
        var index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup"));
        if (index < 0)
            index = tasks.Count;
        tasks.Insert(index, new BrockGymPass("Terramon Brock Gym", 10f));
        totalWeight += 10f;
    }

    public override void ClearWorld()
    {
        Interior = Rectangle.Empty;
        LeaderSpot = Point.Zero;
        _occupied = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists)
            return;
        tag["brockGym"] = new[] { Interior.X, Interior.Y, Interior.Width, Interior.Height, LeaderSpot.X, LeaderSpot.Y };
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.TryGet<int[]>("brockGym", out var data) && data.Length == 6)
        {
            Interior = new Rectangle(data[0], data[1], data[2], data[3]);
            LeaderSpot = new Point(data[4], data[5]);
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Interior.X);
        writer.Write(Interior.Y);
        writer.Write(Interior.Width);
        writer.Write(Interior.Height);
        writer.Write(LeaderSpot.X);
        writer.Write(LeaderSpot.Y);
    }

    public override void NetReceive(BinaryReader reader)
    {
        Interior = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        LeaderSpot = new Point(reader.ReadInt32(), reader.ReadInt32());
    }

    public override void PostUpdateWorld()
    {
        // Runs on the server or in singleplayer
        if (!Exists || ++_checkTimer < 60)
            return;
        _checkTimer = 0;

        Player visitor = null;
        foreach (var player in Main.ActivePlayers)
        {
            if (!player.dead && Interior.Contains(player.Center.ToTileCoordinates()))
            {
                visitor = player;
                break;
            }
        }

        var walkedIn = visitor != null && !_occupied;
        _occupied = visitor != null;
        if (!walkedIn)
            return;

        var brock = ModContent.NPCType<Brock>();
        if (NPC.AnyNPCs(brock))
            return;

        var spot = LeaderSpot.ToWorldCoordinates(8f, 0f);
        var index = NPC.NewNPC(new EntitySource_WorldEvent(), (int)spot.X, (int)spot.Y, brock);
        if (index >= Main.maxNPCs)
            return;
        var npc = Main.npc[index];
        npc.direction = npc.spriteDirection = visitor.Center.X > npc.Center.X ? 1 : -1;

        var text = Language.GetTextValue("Mods.Terramon.BrockGym.Welcome", npc.GivenOrTypeName);
        if (Main.netMode == NetmodeID.Server)
            Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(200, 170, 120));
        else
            Main.NewText(text, new Color(200, 170, 120));
    }

    /// <summary>
    ///     Where to put a player teleporting into the gym (world coordinates of their bottom).
    /// </summary>
    public static Vector2 EntranceSpot => new Point(Interior.X + 6, Interior.Bottom).ToWorldCoordinates(8f, 0f);
}

/// <summary>
///     Finds a spot in the caverns close to spawn and builds Brock's Gym there.
/// </summary>
public class BrockGymPass(string name, float loadWeight) : GenPass(name, loadWeight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Language.GetTextValue("Mods.Terramon.WorldGen.BrockGymPass");
        BrockGymSystem.Interior = Rectangle.Empty;

        try
        {
            if (!BrockGymBuilder.TryPlaceNearSpawn())
                Terramon.Instance.Logger.Warn("Couldn't find a place for Brock's Gym, skipping it.");
        }
        catch (Exception e)
        {
            // A missing gym is better than a world that can't be generated
            Terramon.Instance.Logger.Error("Building Brock's Gym failed", e);
        }

        progress.Set(1);
    }
}

public static class BrockGymBuilder
{
    // Outer size, shell included
    public const int Width = 56;
    public const int Height = 24;
    private const int Shell = 2;
    private const int EntranceGap = 6; // Tunnel between the left door and the rope shaft

    private const ushort Brick = TileID.GrayBrick;

    /// <summary>
    ///     Tries random spots underground, a short walk from spawn, and builds the gym in the first that fits.
    /// </summary>
    public static bool TryPlaceNearSpawn()
    {
        var minDepth = (int)Main.worldSurface + 20;
        var maxDepth = Math.Max(minDepth + 10, (int)Main.rockLayer + 40);
        for (var attempt = 0; attempt < 600; attempt++)
        {
            var offset = WorldGen.genRand.Next(50, 200);
            var left = Main.spawnTileX + (WorldGen.genRand.NextBool() ? offset : -offset - Width);
            var top = WorldGen.genRand.Next(minDepth, maxDepth);
            if (!CanPlace(left, top))
                continue;

            Build(left, top);
            GenVars.structures?.AddProtectedStructure(
                new Rectangle(left - EntranceGap - 4, top - 2, Width + EntranceGap + 8, Height + 4), 4);
            Terramon.Instance.Logger.Info($"Built Brock's Gym at ({left}, {top}).");
            return true;
        }

        return false;
    }

    private static bool CanPlace(int left, int top)
    {
        var area = new Rectangle(left - EntranceGap - 4, top - 2, Width + EntranceGap + 8, Height + 4);
        if (area.Left < 100 || area.Right > Main.maxTilesX - 100 || area.Bottom > Main.UnderworldLayer - 20)
            return false;
        if (Math.Abs(left + Width / 2 - Main.dungeonX) < 200)
            return false;
        if (GenVars.structures != null && !GenVars.structures.CanPlace(area))
            return false;

        // Mostly buried in natural ground (a cave running through it is fine), with nothing built in the way
        var solid = 0;
        for (var x = area.Left; x < area.Right; x++)
        {
            for (var y = area.Top; y < area.Bottom; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.HasTile)
                {
                    if (TileID.Sets.BasicChest[tile.TileType] || tile.TileType is TileID.LihzahrdBrick
                            or TileID.BlueDungeonBrick or TileID.GreenDungeonBrick or TileID.PinkDungeonBrick
                            or TileID.Hive or TileID.LivingWood)
                        return false;
                    if (Main.tileSolid[tile.TileType])
                        solid++;
                }

                if (tile.LiquidType == LiquidID.Shimmer && tile.LiquidAmount > 0)
                    return false;
            }
        }

        return solid > area.Width * area.Height / 2;
    }

    /// <summary>
    ///     Builds the gym with its outer top-left corner at (left, top), plus the tunnel and the rope shaft up to
    ///     the surface, and remembers it.
    /// </summary>
    public static void Build(int left, int top)
    {
        var right = left + Width - 1;
        var bottom = top + Height - 1;
        var floorY = bottom - Shell; // First row of the floor; the air ends just above it
        var inLeft = left + Shell;
        var inRight = right - Shell;
        var centerLeft = left + 19;
        var centerRight = right - 19;

        // Shell of stone bricks, hollow inside
        for (var x = left; x <= right; x++)
        {
            for (var y = top; y <= bottom; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearEverything();
                var inside = x >= inLeft && x <= inRight && y >= top + Shell && y < floorY;
                if (!inside)
                    PlaceBlock(x, y, Brick);
            }
        }

        // The side halls have a lower ceiling than the middle one, and the outer corners are sloped
        for (var x = inLeft; x <= inRight; x++)
        {
            var sideHall = x < centerLeft || x > centerRight;
            var ceiling = top + Shell + (sideHall ? 3 : 0) + Math.Max(0, 3 - Math.Min(x - inLeft, inRight - x));
            for (var y = top + Shell; y < ceiling; y++)
                PlaceBlock(x, y, Brick);
        }

        // Back walls: rough stone, gray brick around the beams, and wooden bands
        var beams = new[] { inLeft + 7, centerLeft - 1, centerRight + 1, inRight - 7 };
        for (var x = inLeft; x <= inRight; x++)
        {
            for (var y = top + Shell; y < floorY; y++)
            {
                ushort wall = WallID.Stone;
                if (beams.Any(b => Math.Abs(b - x) <= 1))
                    wall = WallID.GrayBrick;
                if (y == top + Shell + 6 || y == floorY - 5)
                    wall = WallID.Wood;
                Main.tile[x, y].WallType = wall;
            }
        }

        // Wooden beams from floor to ceiling (they're background tiles, so they don't block anyone)
        foreach (var x in beams)
        {
            for (var y = top + Shell; y < floorY; y++)
            {
                if (!Main.tile[x, y].HasTile)
                    PlaceBlock(x, y, TileID.WoodenBeam);
            }
        }

        // A raised stage on the right where Brock stands, with a step
        var stageLeft = centerRight - 4;
        for (var x = stageLeft; x <= inRight; x++)
            PlaceBlock(x, floorY - 1, Brick);
        Main.tile[stageLeft, floorY - 1].IsHalfBlock = true;

        // Doors on both sides
        foreach (var x in new[] { left + 1, right - 1 })
        {
            var outer = x == left + 1 ? left : right;
            var doorFloor = x == right - 1 ? floorY - 1 : floorY;
            for (var y = doorFloor - 3; y < doorFloor; y++)
            {
                Main.tile[x, y].ClearTile();
                Main.tile[outer, y].ClearTile();
            }

            WorldGen.PlaceTile(x, doorFloor - 2, TileID.ClosedDoor, true, true);
        }

        // The painting: a Poké Ball and the Boulder Badge
        var paintingX = (centerLeft + centerRight) / 2 - 3;
        WorldGen.PlaceObject(paintingX, top + Shell + 2, ModContent.TileType<BrockPaintingTile>(), true);

        // Lanterns under the ceiling of every hall
        foreach (var x in new[] { centerLeft + 2, (centerLeft + centerRight) / 2, centerRight - 2 })
            WorldGen.PlaceObject(x, top + Shell, TileID.HangingLanterns, true, 0);
        foreach (var x in new[] { inLeft + 4, inRight - 4 })
            WorldGen.PlaceObject(x, top + Shell + 3, TileID.HangingLanterns, true, 0);

        // Torches along the walls
        foreach (var x in new[] { inLeft, inRight, beams[0] - 2, beams[3] + 2, centerLeft + 1, centerRight - 1 })
            WorldGen.PlaceTile(x, floorY - 4, TileID.Torches, true, false, -1, 0);

        // Some furniture: pots, a bookcase, a table and chairs
        foreach (var x in new[] { inLeft + 2, inLeft + 10, centerLeft + 3, centerRight - 6 })
            WorldGen.PlaceTile(x, floorY - 1, TileID.Pots, true, false, -1, WorldGen.genRand.Next(4));
        WorldGen.PlaceObject(inLeft + 13, floorY - 1, TileID.Bookcases, true, 0);
        WorldGen.PlaceObject(centerLeft + 8, floorY - 1, TileID.Tables, true, 0);
        WorldGen.PlaceObject(centerLeft + 6, floorY - 1, TileID.Chairs, true, 0, direction: 1);
        WorldGen.PlaceObject(centerLeft + 10, floorY - 1, TileID.Chairs, true, 0, direction: -1);

        BuildEntrance(left, floorY);

        WorldGen.RangeFrame(left - EntranceGap - 4, top - 2, right + 2, bottom + 2);

        BrockGymSystem.Interior = new Rectangle(inLeft, top + Shell, inRight - inLeft + 1, floorY - top - Shell);
        BrockGymSystem.LeaderSpot = new Point(inRight - 5, floorY - 1);
    }

    /// <summary>
    ///     A short brick tunnel out of the left door, then a shaft with a rope all the way up to the surface.
    /// </summary>
    private static void BuildEntrance(int left, int floorY)
    {
        var shaftX = left - EntranceGap;

        // Tunnel
        for (var x = shaftX - 1; x < left; x++)
        {
            for (var y = floorY - 4; y <= floorY; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                if (y == floorY || y == floorY - 4)
                    PlaceBlock(x, y, Brick);
                else
                    tile.WallType = WallID.GrayBrick;
            }
        }

        // Shaft up to the first solid ground from the top of the world
        var surface = floorY - 4;
        for (var y = 40; y < floorY - 4; y++)
        {
            var tile = Main.tile[shaftX, y];
            if (tile.HasTile && Main.tileSolid[tile.TileType] && y > Main.worldSurface * 0.35)
            {
                surface = y;
                break;
            }
        }

        for (var y = surface - 1; y < floorY; y++)
        {
            for (var x = shaftX - 1; x <= shaftX + 1; x++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                if (y > surface + 2)
                    tile.WallType = WallID.GrayBrick;
            }

            // Ropes are set directly, top to bottom, so each one hangs from the one above
            var rope = Main.tile[shaftX, y];
            rope.HasTile = true;
            rope.TileType = TileID.Rope;
        }

        // A little brick arch marking the entrance at the surface
        for (var x = shaftX - 2; x <= shaftX + 2; x++)
            PlaceBlock(x, surface - 5, Brick);
        foreach (var x in new[] { shaftX - 2, shaftX + 2 })
        {
            for (var y = surface - 4; y < surface; y++)
                PlaceBlock(x, y, Brick);
            WorldGen.PlaceTile(x, surface - 6, TileID.Torches, true, false, -1, 0);
        }

        for (var y = surface - 4; y < surface - 1; y++)
        {
            var rope = Main.tile[shaftX, y];
            rope.ClearTile();
            rope.HasTile = true;
            rope.TileType = TileID.Rope;
        }
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
