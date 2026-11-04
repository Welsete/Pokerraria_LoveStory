using Terramon.Content.Items;
using Terraria.DataStructures;
using Terraria.Localization;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The short dungeon on the way to Brock (LoveStory chapter 1), between the bottom of the rope and the gym's left
///     door, so it can't be skipped. After the temples of the maps A Link to Terraria and The Story of Red Cloud (ideas
///     only): rooms of old stone joined by doorways, levers that open gates or drop traps, a chest at the end.
///     <list type="number">
///         <item>Landing: where the rope comes down. A dart trap watches the way in.</item>
///         <item>Lever hall: three levers that look the same. One opens the stone gate (actuators on red wire); the other
///         two drop a boulder from the ceiling right on top of whoever pulled them. The right one changes with every world,
///         and there's no hint on purpose.</item>
///         <item>Boulder corridor: pressure plates that drop boulders from the ceiling.</item>
///         <item>Chest room: the diary page "Stone by stone", an evolution stone, Ultra Balls, Potions and Repels. Then the
///         gym's door.</item>
///     </list>
/// </summary>
public static class BrockDungeon
{
    /// <summary>
    ///     How many tiles the dungeon takes between the rope shaft and the gym (the shaft is at the gym's left minus this).
    /// </summary>
    public const int Length = 64;

    private const int GrayPressurePlate = 2;

    /// <summary>
    ///     Builds the dungeon from the rope shaft at <paramref name="shaftX" /> to the gym's left wall at
    ///     <paramref name="gymLeft" />, from <paramref name="top" /> to <paramref name="bottom" />, with its floor at
    ///     <paramref name="floorY" /> like the gym's.
    /// </summary>
    public static void Build(int shaftX, int gymLeft, int top, int bottom, int floorY)
    {
        var x0 = shaftX - 3;
        var x1 = gymLeft - 1;

        // Solid old stone all around: gray brick on the outside, stone inside
        for (var x = x0; x <= x1; x++)
        {
            for (var y = top; y <= bottom; y++)
            {
                Main.tile[x, y].ClearEverything();
                var outer = x == x0 || y == top || y == bottom;
                Block(x, y, outer ? TileID.GrayBrick : TileID.Stone);
            }
        }

        var r1 = (From: shaftX - 1, To: shaftX + 10, Ceiling: floorY - 7);
        var r2 = (From: shaftX + 13, To: shaftX + 32, Ceiling: floorY - 11);
        var r3 = (From: shaftX + 35, To: shaftX + 52, Ceiling: floorY - 7);
        var r4 = (From: shaftX + 55, To: x1, Ceiling: floorY - 8);
        foreach (var room in new[] { r1, r2, r3, r4 })
            Room(room.From, room.To, room.Ceiling, floorY);

        // Doorways between the rooms (4 tall), the one after the levers closed by the gate
        Doorway(r1.To + 1, r2.From - 1, floorY);
        Doorway(r2.To + 1, r3.From - 1, floorY);
        Doorway(r3.To + 1, r4.From - 1, floorY);
        Gate(r2.To + 1, r2.To + 2, floorY);

        // Signs and the chest first, so the decorations go around them
        PlaceSign(shaftX + 6, floorY, "Mods.Terramon.BrockGym.DungeonSign1");
        PlaceSign(r2.From + 1, floorY, "Mods.Terramon.BrockGym.DungeonSign2");
        Chest(r4.From + 3, floorY);

        Decorate(r1, floorY);
        Decorate(r2, floorY);
        Decorate(r3, floorY);
        Decorate(r4, floorY);
    }

    /// <summary>
    ///     Things that need the tiles framed first: the levers and their wires, and the boulders in the ceiling.
    /// </summary>
    public static void BuildTraps(int shaftX, int floorY)
    {
        var r2From = shaftX + 13;
        var r2Ceiling = floorY - 11;
        var gateX = shaftX + 33;

        // Three levers; one is right, picked for this world
        var levers = new[] { r2From + 4, r2From + 9, r2From + 14 };
        var right = WorldGen.genRand.Next(levers.Length);
        for (var i = 0; i < levers.Length; i++)
        {
            var lx = levers[i];
            var parts = Lever(lx, floorY);
            if (parts.Count == 0)
                continue;
            if (i == right)
            {
                // Red wire under the floor to the gate
                foreach (var p in parts)
                    SetWire(p.X, p.Y, WireColor.Red);
                var bottom = parts.Max(p => p.Y);
                Line(lx, bottom, lx, floorY + 1, WireColor.Red);
                Line(lx, floorY + 1, gateX, floorY + 1, WireColor.Red);
                Line(gateX, floorY + 1, gateX, floorY - 4, WireColor.Red);
                for (var y = floorY - 4; y < floorY; y++)
                    SetWire(gateX + 1, y, WireColor.Red);
            }
            else
            {
                // Its own wire color, straight up to a boulder over it
                var color = i == (right + 1) % levers.Length ? WireColor.Blue : WireColor.Green;
                foreach (var p in parts)
                    SetWire(p.X, p.Y, color);
                var topY = parts.Min(p => p.Y);
                Boulder(lx, r2Ceiling - 1);
                Line(lx, topY, lx, r2Ceiling - 1, color); // up into the boulder
            }
        }

        // Boulder corridor: a plate a little before each boulder
        var r3From = shaftX + 35;
        var r3Ceiling = floorY - 7;
        foreach (var bx in new[] { r3From + 5, r3From + 12 })
        {
            Boulder(bx, r3Ceiling - 1);
            var px = bx - 2;
            Main.tile[px, floorY - 1].ClearTile();
            WorldGen.PlaceTile(px, floorY - 1, TileID.PressurePlates, true, true, -1, GrayPressurePlate);
            Line(px, floorY - 1, px, r3Ceiling - 1, WireColor.Yellow);
            Line(px, r3Ceiling - 1, bx, r3Ceiling - 1, WireColor.Yellow); // along the ceiling into the boulder
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
    ///     A straight wire (vertical or horizontal) between two tiles.
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

    /// <summary>
    ///     Hollows a room: air with a stone slab back wall, a gray brick floor and ceiling band.
    /// </summary>
    private static void Room(int from, int to, int ceiling, int floorY)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = ceiling; y < floorY; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.LiquidAmount = 0;
                // Rough cave wall near the corners, cut stone in the middle (old temple dug into the rock)
                var edge = Math.Min(x - from, to - x) < 2 || y == ceiling;
                tile.WallType = edge ? WallID.RocksUnsafe1 : WallID.StoneSlab;
            }

            Block(x, floorY, TileID.GrayBrick);
            Block(x, ceiling - 1, TileID.GrayBrick);
        }

        // Rounded top corners
        foreach (var x in new[] { from, to })
            Block(x, ceiling, TileID.Stone);
    }

    private static void Doorway(int from, int to, int floorY)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = floorY - 4; y < floorY; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = WallID.GrayBrick;
            }

            Block(x, floorY - 5, TileID.GrayBrick);
        }
    }

    /// <summary>
    ///     A stone gate filling a doorway: blocks with actuators, so the right lever makes them passable.
    /// </summary>
    private static void Gate(int from, int to, int floorY)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = floorY - 4; y < floorY; y++)
            {
                Block(x, y, TileID.StoneSlab);
                var tile = Main.tile[x, y];
                tile.HasActuator = true;
            }
        }
    }

    private static void Decorate((int From, int To, int Ceiling) room, int floorY)
    {
        // Torches on the back wall, every 8 tiles
        for (var x = room.From + 3; x <= room.To - 2; x += 8)
            WorldGen.PlaceTile(x, floorY - 4, TileID.Torches, true, true);

        // A few cobwebs and rubble in the top corners
        foreach (var x in new[] { room.From, room.From + 1, room.To, room.To - 1 })
        {
            var tile = Main.tile[x, room.Ceiling + 1];
            if (!tile.HasTile)
                WorldGen.PlaceTile(x, room.Ceiling + 1, TileID.Cobweb, true, true);
        }

        for (var x = room.From + 1; x < room.To; x += 5)
        {
            if (WorldGen.genRand.NextBool(2))
                WorldGen.PlaceSmallPile(x, floorY - 1, WorldGen.genRand.Next(6), 0);
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

    /// <summary>
    ///     A lever standing on the floor at <paramref name="x" />. Returns the tiles it takes (empty if it couldn't be
    ///     placed).
    /// </summary>
    private static List<Point> Lever(int x, int floorY)
    {
        WorldGen.PlaceObject(x, floorY - 1, TileID.Lever, true);
        var parts = Find(x, floorY, TileID.Lever);
        if (parts.Count > 0)
            return parts;
        // If a lever doesn't fit, a switch on the back wall does the same job (the gate must always be openable)
        WorldGen.PlaceTile(x, floorY - 2, TileID.Switches, true, true);
        return Find(x, floorY, TileID.Switches);
    }

    private static List<Point> Find(int x, int floorY, ushort type)
    {
        var parts = new List<Point>();
        for (var i = x - 1; i <= x + 2; i++)
        {
            for (var j = floorY - 3; j < floorY; j++)
            {
                if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == type)
                    parts.Add(new Point(i, j));
            }
        }

        return parts;
    }

    private static void PlaceSign(int x, int floorY, string key)
    {
        if (!WorldGen.PlaceObject(x, floorY - 1, TileID.Signs, true))
            return;
        var sign = Terraria.Sign.ReadSign(x, floorY - 2, true);
        if (sign >= 0)
            Terraria.Sign.TextSign(sign, Language.GetTextValue(key));
    }

    /// <summary>
    ///     The chest at the end: the diary page and supplies for the Gym.
    /// </summary>
    private static void Chest(int x, int floorY)
    {
        var index = WorldGen.PlaceChest(x, floorY - 1, TileID.Containers, false, 1); // Gold Chest
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
