using Terramon.Content.Items;
using Terraria.Localization;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The dungeon on the way to Brock (LoveStory chapter 1), between the bottom of the rope and the gym's left door, so
///     it can't be skipped. After the temples of the maps A Link to Terraria and The Story of Red Cloud (ideas only):
///     rooms of old stone joined by doorways, levers that open gates or drop traps, pits, a chest at the end.
///     <list type="number">
///         <item>Landing, where the rope comes down (the gym's dart trap watches it).</item>
///         <item>Lever hall: very tall and dark. Three levers that look the same: one opens the stone gate (actuators on
///         red wire), the other two drop a boulder from high up in the dark right on whoever pulled them. The right one
///         changes with every world, with no hint on purpose.</item>
///         <item>Spike pit: a wide pit with spikes at the bottom, crossed by jumping on stone pillars. A rope at its edge
///         to climb out, and a boulder waiting where you land.</item>
///         <item>Boulder corridor: tall and dark, pressure plates that drop boulders from the ceiling.</item>
///         <item>Lava pit: crossed on pillars one block wide.</item>
///         <item>Chest room: the diary page "Stone by stone", an evolution stone, Ultra Balls, Potions and Repels. Then the
///         gym's door.</item>
///     </list>
///     Cave bats spawn more often inside (see <see cref="BrockDungeonSpawns" />).
/// </summary>
public static class BrockDungeon
{
    /// <summary>
    ///     How many tiles the dungeon takes between the rope shaft and the gym (the shaft is at the gym's left minus this).
    /// </summary>
    public const int Length = 126;

    /// <summary>
    ///     How far the dungeon goes above and below the gym's floor row.
    /// </summary>
    public const int Above = 28;

    public const int Below = 11;

    private const int GrayPressurePlate = 2;

    private readonly record struct RoomSpan(int From, int To, int Ceiling);

    // Rooms, in tiles from the rope shaft
    private static RoomSpan R1(int s, int f) => new(s - 1, s + 10, f - 7);
    private static RoomSpan R2(int s, int f) => new(s + 13, s + 34, f - 24);
    private static RoomSpan R3(int s, int f) => new(s + 37, s + 60, f - 14);
    private static RoomSpan R4(int s, int f) => new(s + 63, s + 88, f - 20);
    private static RoomSpan R5(int s, int f) => new(s + 91, s + 112, f - 12);
    private static RoomSpan R6(int s, int f) => new(s + 115, s + Length - 1, f - 8);

    /// <summary>
    ///     The whole dungeon in tiles, for a gym whose rope shaft is at <paramref name="shaftX" />.
    /// </summary>
    public static Rectangle Area(int shaftX, int floorY)
        => new(shaftX - 3, floorY - Above, Length + 3, Above + Below + 1);

    /// <summary>
    ///     Builds the dungeon from the rope shaft at <paramref name="shaftX" /> to the gym's left wall, with its floor at
    ///     <paramref name="floorY" /> like the gym's.
    /// </summary>
    public static void Build(int shaftX, int floorY)
    {
        var area = Area(shaftX, floorY);
        var s = shaftX;
        var f = floorY;

        // Solid old stone all around: gray brick on the outside, stone inside
        for (var x = area.Left; x < area.Right; x++)
        {
            for (var y = area.Top; y < area.Bottom; y++)
            {
                Main.tile[x, y].ClearEverything();
                var outer = x == area.Left || y == area.Top || y == area.Bottom - 1;
                Block(x, y, outer ? TileID.GrayBrick : TileID.Stone);
            }
        }

        RoomSpan[] rooms = [R1(s, f), R2(s, f), R3(s, f), R4(s, f), R5(s, f), R6(s, f)];
        foreach (var room in rooms)
            Room(room, f);
        for (var i = 0; i < rooms.Length - 1; i++)
            Doorway(rooms[i].To + 1, rooms[i + 1].From - 1, f);
        Gate(R2(s, f).To + 1, R2(s, f).To + 2, f);

        SpikePit(s, f);
        LavaPit(s, f);

        // Signs and the chest first, so the decorations go around them
        PlaceSign(s + 6, f, "Mods.Terramon.BrockGym.DungeonSign1");
        PlaceSign(R2(s, f).From + 1, f, "Mods.Terramon.BrockGym.DungeonSign2");
        Chest(R6(s, f).From + 4, f);

        foreach (var room in rooms)
            Decorate(room, f);
    }

    /// <summary>
    ///     The spike pit of room 3: 16 wide, 8 deep, spikes at the bottom, two stone pillars to jump on, a rope at its
    ///     left edge to climb back out.
    /// </summary>
    private static void SpikePit(int s, int f)
    {
        const int depth = 8;
        int from = s + 41, to = s + 56;
        for (var x = from; x <= to; x++)
        {
            for (var y = f; y < f + depth; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = WallID.RocksUnsafe1;
            }

            Block(x, f + depth, TileID.Stone);
            Block(x, f + depth - 1, TileID.Spikes);
        }

        foreach (var px in new[] { s + 45, s + 51 })
        {
            for (var x = px; x <= px + 1; x++)
            {
                for (var y = f; y < f + depth; y++)
                    Block(x, y, y == f ? TileID.GrayBrick : TileID.Stone);
            }
        }

        for (var y = f; y < f + depth - 1; y++)
            Rope(from, y);
    }

    /// <summary>
    ///     The lava pit of room 5: 14 wide, lava at the bottom, two pillars one block wide.
    /// </summary>
    private static void LavaPit(int s, int f)
    {
        const int depth = 7;
        int from = s + 95, to = s + 108;
        for (var x = from; x <= to; x++)
        {
            for (var y = f; y < f + depth; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = WallID.ObsidianBrickUnsafe;
                if (y >= f + depth - 3)
                {
                    tile.LiquidType = LiquidID.Lava;
                    tile.LiquidAmount = 255;
                }
            }

            Block(x, f + depth, TileID.Obsidian);
        }

        // The pit's sides in obsidian, so the lava stays put
        for (var y = f; y <= f + depth; y++)
        {
            Block(from - 1, y, TileID.Obsidian);
            Block(to + 1, y, TileID.Obsidian);
        }

        foreach (var px in new[] { s + 99, s + 104 })
        {
            for (var y = f; y < f + depth; y++)
            {
                Block(px, y, y == f ? TileID.GrayBrick : TileID.Obsidian);
                Main.tile[px, y].LiquidAmount = 0;
            }
        }

        // A short rope above the lava, a last chance to climb out
        for (var y = f; y < f + depth - 3; y++)
            Rope(from, y);
    }

    /// <summary>
    ///     Things that need the tiles framed first: the levers and their wires, the boulders in the ceilings, the plates
    ///     and the dart trap.
    /// </summary>
    public static void BuildTraps(int shaftX, int floorY)
    {
        var s = shaftX;
        var f = floorY;
        var r2 = R2(s, f);
        var gateX = r2.To + 1;

        // Three levers; one is right, picked for this world
        var levers = new[] { r2.From + 4, r2.From + 10, r2.From + 16 };
        var right = WorldGen.genRand.Next(levers.Length);
        for (var i = 0; i < levers.Length; i++)
        {
            var lx = levers[i];
            var parts = Lever(lx, f);
            if (parts.Count == 0)
                continue;
            if (i == right)
            {
                // Red wire under the floor to the gate
                foreach (var p in parts)
                    SetWire(p.X, p.Y, WireColor.Red);
                Line(lx, parts.Max(p => p.Y), lx, f + 1, WireColor.Red);
                Line(lx, f + 1, gateX, f + 1, WireColor.Red);
                Line(gateX, f + 1, gateX, f - 4, WireColor.Red);
                Line(gateX, f - 4, gateX + 1, f - 1, WireColor.Red);
            }
            else
            {
                // Its own wire color, straight up into a boulder high in the dark
                var color = i == (right + 1) % levers.Length ? WireColor.Blue : WireColor.Green;
                foreach (var p in parts)
                    SetWire(p.X, p.Y, color);
                Boulder(lx, r2.Ceiling - 1);
                Line(lx, parts.Min(p => p.Y), lx, r2.Ceiling - 1, color);
            }
        }

        // Spike pit room: right where you land after the pit, a plate drops a boulder
        var r3 = R3(s, f);
        Boulder(s + 58, r3.Ceiling - 1);
        Plate(s + 58, f);
        Line(s + 58, f - 1, s + 58, r3.Ceiling - 1, WireColor.Yellow);

        // Boulder corridor: a plate a little before each boulder
        var r4 = R4(s, f);
        foreach (var bx in new[] { r4.From + 5, r4.From + 13, r4.From + 21 })
        {
            Boulder(bx, r4.Ceiling - 1);
            var px = bx - 2;
            Plate(px, f);
            Line(px, f - 1, px, r4.Ceiling - 1, WireColor.Yellow);
            Line(px, r4.Ceiling - 1, bx, r4.Ceiling - 1, WireColor.Yellow);
        }
    }

    private enum WireColor { Red, Blue, Green, Yellow }

    private static void SetWire(int x, int y, WireColor color)
    {
        var tile = Main.tile[x, y];
        switch (color)
        {
            case WireColor.Red: tile.RedWire = true; break;
            case WireColor.Blue: tile.BlueWire = true; break;
            case WireColor.Green: tile.GreenWire = true; break;
            default: tile.YellowWire = true; break;
        }
    }

    /// <summary>
    ///     A wire covering the rectangle between two tiles (a straight line when they share a row or a column).
    /// </summary>
    private static void Line(int x1, int y1, int x2, int y2, WireColor color)
    {
        for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
                SetWire(x, y, color);
        }
    }

    private static void Block(int x, int y, ushort type)
    {
        var tile = Main.tile[x, y];
        tile.ClearTile();
        tile.HasTile = true;
        tile.TileType = type;
        tile.Slope = SlopeType.Solid;
        tile.IsHalfBlock = false;
    }

    private static void Rope(int x, int y)
    {
        var tile = Main.tile[x, y];
        tile.ClearTile();
        tile.HasTile = true;
        tile.TileType = TileID.Rope;
    }

    /// <summary>
    ///     Hollows a room: air with a stone slab back wall (rough rock near the edges and up in the dark), a gray brick
    ///     floor and ceiling band.
    /// </summary>
    private static void Room(RoomSpan room, int f)
    {
        for (var x = room.From; x <= room.To; x++)
        {
            for (var y = room.Ceiling; y < f; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.LiquidAmount = 0;
                var edge = Math.Min(x - room.From, room.To - x) < 2 || y < f - 9;
                tile.WallType = edge ? WallID.RocksUnsafe1 : WallID.StoneSlab;
            }

            Block(x, f, TileID.GrayBrick);
            Block(x, room.Ceiling - 1, TileID.GrayBrick);
        }

        foreach (var x in new[] { room.From, room.To })
            Block(x, room.Ceiling, TileID.Stone);
    }

    private static void Doorway(int from, int to, int f)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = f - 4; y < f; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = WallID.GrayBrick;
            }

            Block(x, f - 5, TileID.GrayBrick);
            Block(x, f, TileID.GrayBrick);
        }
    }

    /// <summary>
    ///     A stone gate filling a doorway: blocks with actuators, so the right lever makes them passable.
    /// </summary>
    private static void Gate(int from, int to, int f)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = f - 4; y < f; y++)
            {
                Block(x, y, TileID.StoneSlab);
                var tile = Main.tile[x, y];
                tile.HasActuator = true;
            }
        }
    }

    private static void Decorate(RoomSpan room, int f)
    {
        // Torches low on the back wall only: the ceilings stay dark
        for (var x = room.From + 3; x <= room.To - 2; x += 8)
        {
            if (!Main.tile[x, f - 4].HasTile && Main.tile[x, f].TileType == TileID.GrayBrick)
                WorldGen.PlaceTile(x, f - 4, TileID.Torches, true, true);
        }

        // Cobwebs in the top corners
        foreach (var x in new[] { room.From, room.From + 1, room.To, room.To - 1 })
        {
            if (!Main.tile[x, room.Ceiling + 1].HasTile)
                WorldGen.PlaceTile(x, room.Ceiling + 1, TileID.Cobweb, true, true);
        }

        // Rubble on the floor
        for (var x = room.From + 1; x < room.To; x += 5)
        {
            if (!Main.tile[x, f - 1].HasTile && Main.tile[x, f].TileType == TileID.GrayBrick && WorldGen.genRand.NextBool(2))
                WorldGen.PlaceSmallPile(x, f - 1, WorldGen.genRand.Next(6), 0);
        }
    }

    /// <summary>
    ///     A boulder sitting in a hollow of the ceiling, its bottom on row <paramref name="bottomY" />.
    /// </summary>
    private static void Boulder(int x, int bottomY)
    {
        for (var dx = 0; dx < 2; dx++)
        {
            for (var dy = 0; dy < 2; dy++)
            {
                var tile = Main.tile[x + dx, bottomY - 1 + dy];
                tile.ClearTile();
                tile.HasTile = true;
                tile.TileType = TileID.Boulder;
                tile.TileFrameX = (short)(dx * 18);
                tile.TileFrameY = (short)(dy * 18);
            }
        }
    }

    private static void Plate(int x, int f)
    {
        Main.tile[x, f - 1].ClearTile();
        WorldGen.PlaceTile(x, f - 1, TileID.PressurePlates, true, true, -1, GrayPressurePlate);
    }

    /// <summary>
    ///     A lever standing on the floor at <paramref name="x" />. Returns the tiles it takes (empty if it couldn't be
    ///     placed). If a lever doesn't fit, a switch on the back wall does the same job (the gate must always open).
    /// </summary>
    private static List<Point> Lever(int x, int f)
    {
        WorldGen.PlaceObject(x, f - 1, TileID.Lever, true);
        var parts = Find(x, f, TileID.Lever);
        if (parts.Count > 0)
            return parts;
        WorldGen.PlaceTile(x, f - 2, TileID.Switches, true, true);
        return Find(x, f, TileID.Switches);
    }

    private static List<Point> Find(int x, int f, ushort type)
    {
        var parts = new List<Point>();
        for (var i = x - 1; i <= x + 2; i++)
        {
            for (var j = f - 3; j < f; j++)
            {
                if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == type)
                    parts.Add(new Point(i, j));
            }
        }

        return parts;
    }

    private static void PlaceSign(int x, int f, string key)
    {
        if (!WorldGen.PlaceObject(x, f - 1, TileID.Signs, true))
            return;
        var sign = Terraria.Sign.ReadSign(x, f - 2, true);
        if (sign >= 0)
            Terraria.Sign.TextSign(sign, Language.GetTextValue(key));
    }

    /// <summary>
    ///     The chest at the end: the diary page and supplies for the Gym.
    /// </summary>
    private static void Chest(int x, int f)
    {
        var index = WorldGen.PlaceChest(x, f - 1, TileID.Containers, false, 1); // Gold Chest
        if (index < 0)
            return;
        var stones = ModContent.GetContent<EvolutionaryItem>()
            .Where(i => i is not LinkingCord)
            .Select(i => i.Type)
            .ToArray();
        var loot = new List<(int Type, int Stack)>
        {
            (ModContent.ItemType<DiaryPageBrock>(), 1),
            (stones.Length > 0 ? stones[WorldGen.genRand.Next(stones.Length)] : ItemID.None, 1),
            (ModContent.ItemType<Items.PokeBalls.UltraBallItem>(), 5),
            (ModContent.ItemType<Potion>(), 3),
            (ModContent.ItemType<Repel>(), 2)
        };
        var chest = Main.chest[index];
        var slot = 0;
        foreach (var (type, stack) in loot)
        {
            if (type <= ItemID.None)
                continue;
            chest.item[slot].SetDefaults(type);
            chest.item[slot].stack = stack;
            slot++;
        }
    }
}

/// <summary>
///     Cave bats spawn a lot more inside Brock's dungeon.
/// </summary>
public class BrockDungeonSpawns : GlobalNPC
{
    private static bool Inside(Player player)
        => BrockGymSystem.DungeonArea is { IsEmpty: false } area && area.Contains(player.Center.ToTileCoordinates());

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!Inside(player))
            return;
        spawnRate = Math.Max(1, spawnRate / 4);
        maxSpawns += 4;
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (Inside(spawnInfo.Player))
            pool[NPCID.CaveBat] = 2f;
    }
}

/// <summary>
///     Lets wired gates (blocks with actuators) inside protected buildings open: Terraria won't actuate a block when the
///     one above it can't be broken, and protected blocks never can.
/// </summary>
public class ProtectedActuators : ModSystem
{
    public override void Load() => On_Wiring.DeActive += DeActive;

    private static void DeActive(On_Wiring.orig_DeActive orig, int i, int j)
    {
        var bypass = ProtectedAreas.Bypass;
        ProtectedAreas.Bypass = true;
        try
        {
            orig(i, j);
        }
        finally
        {
            ProtectedAreas.Bypass = bypass;
        }
    }
}
