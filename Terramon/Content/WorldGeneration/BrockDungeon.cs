using Terramon.Content.Items;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Localization;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The dungeon on the way to Brock (LoveStory chapter 1), between the bottom of the rope and the gym's left door, so
///     it can't be skipped. After the temples of the maps A Link to Terraria and The Story of Red Cloud (ideas only):
///     rooms of old stone joined by doorways, levers, gates, pits and traps, a chest at the end. Room by room (see
///     <see cref="Kind" />): landing, lever hall, spike pit, dart gallery, boulder corridor, false floor, lava pit, the
///     well, chest room. Bats spawn a lot more inside (see <see cref="BrockDungeonSpawns" />).
/// </summary>
public static class BrockDungeon
{
    private enum Kind { Landing, Levers, SpikePit, Darts, Boulders, FalseFloor, LavaPit, Well, DartHall, Chest }

    // Width and height (floor to ceiling) of each room, in order
    private static readonly (Kind Kind, int Width, int Height)[] Plan =
    [
        (Kind.Landing, 12, 7),
        (Kind.Levers, 22, 24),
        (Kind.SpikePit, 24, 14),
        (Kind.Darts, 30, 9),
        (Kind.Boulders, 26, 20),
        (Kind.FalseFloor, 24, 10),
        (Kind.LavaPit, 22, 12),
        (Kind.Well, 22, 10),
        (Kind.DartHall, 60, 8),
        (Kind.Chest, 13, 16)
    ];

    private const int WallWidth = 2; // Between two rooms (a doorway goes through it)

    /// <summary>
    ///     How many tiles the dungeon takes between the rope shaft and the gym (the shaft is at the gym's left minus this).
    /// </summary>
    public static readonly int Length = Plan.Sum(r => r.Width) + WallWidth * (Plan.Length - 1) - 1;

    /// <summary>
    ///     How far the dungeon goes above and below the gym's floor row.
    /// </summary>
    public const int Above = 28;

    public const int Below = 17;

    private const int WellDepth = 14;
    private const int HallDepth = 7; // How far the dart hall's floor is sunk below the doorways
    private const int GrayPressurePlate = 2;

    private readonly record struct RoomSpan(Kind Kind, int From, int To, int Ceiling)
    {
        public int Width => To - From + 1;
    }

    /// <summary>
    ///     Where each room is, for a gym whose rope shaft is at <paramref name="s" /> and floor at <paramref name="f" />.
    /// </summary>
    private static List<RoomSpan> Rooms(int s, int f)
    {
        var rooms = new List<RoomSpan>();
        var x = s - 1; // The landing starts right left of the rope
        foreach (var (kind, width, height) in Plan)
        {
            rooms.Add(new RoomSpan(kind, x, x + width - 1, f - height));
            x += width + WallWidth;
        }

        return rooms;
    }

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

        var rooms = Rooms(shaftX, f);
        foreach (var room in rooms)
            Room(room, f);
        for (var i = 0; i < rooms.Count - 1; i++)
        {
            Doorway(rooms[i].To + 1, rooms[i + 1].From - 1, f);
            if (rooms[i].Kind == Kind.Levers)
                Gate(rooms[i].To + 1, rooms[i + 1].From - 1, f);
        }

        foreach (var room in rooms)
        {
            switch (room.Kind)
            {
                case Kind.Landing:
                    PlaceSign(shaftX + 6, f, "Mods.Terramon.BrockGym.DungeonSign1");
                    break;
                case Kind.Levers:
                    PlaceSign(room.From + 1, f, "Mods.Terramon.BrockGym.DungeonSign2");
                    break;
                case Kind.SpikePit:
                    Pit(room.From + 4, room.To - 4, f, 8, TileID.Spikes, false);
                    Pillar(room.From + 8, 2, f, 8, TileID.Stone);
                    Pillar(room.From + 14, 2, f, 8, TileID.Stone);
                    RopeDown(room.From + 4, f, f + 6);
                    break;
                case Kind.FalseFloor:
                    // The pit under the false floor, and a rope on its far side to climb out
                    Pit(room.From + 6, room.To - 5, f, 7, TileID.Spikes, false);
                    RopeDown(room.To - 5, f, f + 5);
                    for (var x = room.From + 6; x <= room.To - 6; x++)
                    {
                        Block(x, f, TileID.GrayBrick);
                        var tile = Main.tile[x, f];
                        tile.HasActuator = true;
                    }

                    break;
                case Kind.LavaPit:
                    Pit(room.From + 4, room.To - 4, f, 7, 0, true);
                    Pillar(room.From + 8, 1, f, 7, TileID.Obsidian);
                    Pillar(room.From + 13, 1, f, 7, TileID.Obsidian);
                    RopeDown(room.From + 4, f, f + 3);
                    break;
                case Kind.Well:
                    Well(room, f);
                    break;
                case Kind.DartHall:
                    DartHall(room, f);
                    break;
                case Kind.Chest:
                    Chest(room.From + 4, f);
                    break;
            }
        }

        foreach (var room in rooms)
            Decorate(room, f);
    }

    /// <summary>
    ///     A pit in the floor from <paramref name="from" /> to <paramref name="to" />: spikes (or lava) at the bottom.
    /// </summary>
    private static void Pit(int from, int to, int f, int depth, ushort bottomTile, bool lava)
    {
        for (var x = from; x <= to; x++)
        {
            for (var y = f; y < f + depth; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = lava ? WallID.ObsidianBrickUnsafe : WallID.RocksUnsafe1;
                if (lava && y >= f + depth - 3)
                {
                    tile.LiquidType = LiquidID.Lava;
                    tile.LiquidAmount = 255;
                }
            }

            Block(x, f + depth, lava ? TileID.Obsidian : TileID.Stone);
            if (!lava)
                Block(x, f + depth - 1, bottomTile);
        }

        if (!lava)
            return;
        // The sides in obsidian, so the lava stays put
        for (var y = f; y <= f + depth; y++)
        {
            Block(from - 1, y, TileID.Obsidian);
            Block(to + 1, y, TileID.Obsidian);
        }
    }

    private static void Pillar(int x, int width, int f, int depth, ushort type)
    {
        for (var px = x; px < x + width; px++)
        {
            for (var y = f; y < f + depth; y++)
            {
                Block(px, y, y == f ? TileID.GrayBrick : type);
                Main.tile[px, y].LiquidAmount = 0;
            }
        }
    }

    private static void RopeDown(int x, int fromY, int toY)
    {
        for (var y = fromY; y <= toY; y++)
            Rope(x, y);
    }

    /// <summary>
    ///     The well: the floor gives way to a deep shaft. A rope goes down to a dark gallery under the dungeon (a puddle
    ///     of lava to jump, dart traps) and another rope climbs back up on the far side.
    /// </summary>
    private static void Well(RoomSpan room, int f)
    {
        int from = room.From + 3, to = room.To - 3;
        var bottom = f + WellDepth;
        for (var x = from; x <= to; x++)
        {
            for (var y = f; y < bottom; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = y < bottom - 6 ? WallID.RocksUnsafe1 : WallID.StoneSlab;
            }

            Block(x, bottom, TileID.GrayBrick);
        }

        RopeDown(from, f, bottom - 1);
        RopeDown(to, f, bottom - 1);

        // Lava puddle in the middle of the gallery
        var mid = (from + to) / 2;
        for (var x = mid - 1; x <= mid + 1; x++)
        {
            var tile = Main.tile[x, bottom];
            tile.ClearTile();
            tile.LiquidType = LiquidID.Lava;
            tile.LiquidAmount = 255;
            Block(x, bottom + 1, TileID.Obsidian);
        }

        Block(mid - 2, bottom + 1, TileID.Obsidian);
        Block(mid + 2, bottom + 1, TileID.Obsidian);
    }

    /// <summary>
    ///     The dart hall: a long hall sunk below the doorways, with dart traps in both end walls at three heights (feet,
    ///     head, jump), firing on a timer, one side then the other. Pairs of stone blocks on the floor make shelters that
    ///     cover from both sides: run from one to the next between volleys. A rope on the far side climbs out.
    /// </summary>
    private static void DartHall(RoomSpan room, int f)
    {
        int from = room.From + 3, to = room.To - 3;
        var floor = f + HallDepth;
        for (var x = from; x <= to; x++)
        {
            for (var y = f; y < floor; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.WallType = WallID.StoneSlab;
            }

            Block(x, floor, TileID.GrayBrick);
        }

        // Shelters: two stone blocks 2 wide and 4 tall with room for one player between them
        for (var x = from + 6; x + 6 <= to - 4; x += 12)
        {
            foreach (var px in new[] { x, x + 5 })
            {
                for (var dx = 0; dx < 2; dx++)
                {
                    for (var y = floor - 4; y < floor; y++)
                        Block(px + dx, y, y == floor - 4 ? TileID.GrayBrick : TileID.Stone);
                }
            }
        }

        RopeDown(to, f, floor - 1);

        // A few torches low on the back wall, under the darts' way
        for (var x = from + 8; x < to; x += 20)
            WorldGen.PlaceTile(x, floor - 6, TileID.Torches, true, true);
    }

    /// <summary>
    ///     The chest room's tiles, for <see cref="BrockDungeonChestTrap" />.
    /// </summary>
    public static Rectangle ChestRoom(int shaftX, int floorY)
    {
        var room = Rooms(shaftX, floorY).First(r => r.Kind == Kind.Chest);
        return new Rectangle(room.From, room.Ceiling, room.Width, floorY - room.Ceiling);
    }

    /// <summary>
    ///     Things that need the tiles framed first: the levers and their wires, the boulders in the ceilings, the plates
    ///     and the dart traps.
    /// </summary>
    public static void BuildTraps(int shaftX, int floorY)
    {
        var f = floorY;
        var rooms = Rooms(shaftX, f);
        for (var i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            switch (room.Kind)
            {
                case Kind.Levers:
                    LeverTraps(room, rooms[i + 1].From - 2, f);
                    break;

                case Kind.SpikePit:
                    // Right where you land after the pit, a plate drops a boulder
                    BoulderTrap(room.To - 2, room.To - 2, room, f);
                    break;

                case Kind.Darts:
                    // Dart traps at foot height (they fire on a timer, see BrockDungeonDarts): four shooting at you as
                    // you come, two at your back once you've run past them
                    for (var k = 0; k < 4; k++)
                        Dart(room.From + 6 + k * 7, f - 1, facingRight: false);
                    for (var k = 0; k < 2; k++)
                        Dart(room.From + 8 + k * 14, f - 1, facingRight: true);
                    break;

                case Kind.DartHall:
                {
                    var floor = f + HallDepth;
                    foreach (var y in new[] { floor - 1, floor - 3, floor - 5 })
                    {
                        Dart(room.From + 2, y, facingRight: true);
                        Dart(room.To - 2, y, facingRight: false);
                    }

                    break;
                }

                case Kind.Boulders:
                    foreach (var bx in new[] { room.From + 5, room.From + 13, room.From + 21 })
                        BoulderTrap(bx - 2, bx, room, f);
                    break;

                case Kind.FalseFloor:
                {
                    // A plate right before the false floor opens it under your feet
                    var px = room.From + 5;
                    Plate(px, f);
                    Line(px, f - 1, px, f, WireColor.Yellow);
                    Line(px, f, room.To - 6, f, WireColor.Yellow);
                    // ...and a boulder for whoever stops to look
                    BoulderTrap(room.From + 3, room.From + 10, room, f);
                    break;
                }

                case Kind.LavaPit:
                    // A dart flying across the lava now and then, from the far side
                    Dart(room.To - 2, f - 1, facingRight: false);
                    break;

                case Kind.Well:
                {
                    // Down in the gallery: a dart trap at each end
                    var bottom = f + WellDepth;
                    int from = room.From + 3, to = room.To - 3;
                    Dart(from + 1, bottom - 1, facingRight: true);
                    Dart(to - 1, bottom - 1, facingRight: false);

                    // Both walls of the shaft, at alternating heights: they shoot across while you climb the ropes
                    for (var y = f + 2; y < bottom - 2; y += 4)
                        Dart(from - 1, y, facingRight: true);
                    for (var y = f + 4; y < bottom - 2; y += 4)
                        Dart(to + 1, y, facingRight: false);
                    break;
                }
            }
        }
    }

    /// <summary>
    ///     Three levers that look the same; one is right, picked for this world. It opens the gate (red wire under the
    ///     floor); the other two drop a boulder from high up in the dark right on whoever pulled them.
    /// </summary>
    private static void LeverTraps(RoomSpan room, int gateX, int f)
    {
        var levers = new[] { room.From + 4, room.From + 10, room.From + 16 };
        var right = WorldGen.genRand.Next(levers.Length);
        for (var i = 0; i < levers.Length; i++)
        {
            var lx = levers[i];
            var parts = Lever(lx, f);
            if (parts.Count == 0)
                continue;
            if (i == right)
            {
                foreach (var p in parts)
                    SetWire(p.X, p.Y, WireColor.Red);
                Line(lx, parts.Max(p => p.Y), lx, f + 1, WireColor.Red);
                Line(lx, f + 1, gateX, f + 1, WireColor.Red);
                Line(gateX, f + 1, gateX, f - 4, WireColor.Red);
                Line(gateX, f - 4, gateX + 1, f - 1, WireColor.Red);

                // A switch on the other side of the gate, to open it again on the way back
                var back = gateX + 3;
                WorldGen.PlaceTile(back, f - 2, TileID.Switches, true, true);
                Line(gateX, f + 1, back, f + 1, WireColor.Red);
                Line(back, f + 1, back, f - 2, WireColor.Red);
            }
            else
            {
                var color = i == (right + 1) % levers.Length ? WireColor.Blue : WireColor.Green;
                foreach (var p in parts)
                    SetWire(p.X, p.Y, color);
                Boulder(lx, room.Ceiling - 1);
                Line(lx, parts.Min(p => p.Y), lx, room.Ceiling - 1, color);
            }
        }
    }

    /// <summary>
    ///     A plate at <paramref name="plateX" /> that drops a boulder from the ceiling at <paramref name="boulderX" />.
    /// </summary>
    private static void BoulderTrap(int plateX, int boulderX, RoomSpan room, int f)
    {
        Boulder(boulderX, room.Ceiling - 1);
        Plate(plateX, f);
        Line(plateX, f - 1, plateX, room.Ceiling - 1, WireColor.Yellow);
        Line(Math.Min(plateX, boulderX), room.Ceiling - 1, Math.Max(plateX, boulderX), room.Ceiling - 1,
            WireColor.Yellow);
    }

    /// <summary>
    ///     Puts every challenge back the way it was built (see <see cref="BrockDungeonReset" />): the gate and the false
    ///     floor closed, the levers and switches off, and a boulder back in every hollow of the ceilings.
    /// </summary>
    public static void ResetTraps(int shaftX, int floorY)
    {
        var f = floorY;
        var rooms = Rooms(shaftX, f);
        for (var i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            switch (room.Kind)
            {
                case Kind.Levers:
                {
                    foreach (var lx in new[] { room.From + 4, room.From + 10, room.From + 16 })
                    {
                        var parts = Find(lx, f, TileID.Lever);
                        parts.AddRange(Find(lx, f, TileID.Switches));
                        foreach (var p in parts)
                            LeverOff(p.X, p.Y);
                        // The levers with blue or green wire are the wrong ones, with a boulder over them
                        if (parts.Any(p => Main.tile[p.X, p.Y].BlueWire || Main.tile[p.X, p.Y].GreenWire))
                            RestoreBoulder(lx, room.Ceiling - 1);
                    }

                    var gateX = room.To + 1;
                    for (var x = gateX; x < rooms[i + 1].From; x++)
                    {
                        for (var y = f - 4; y < f; y++)
                            Actuate(x, y, false);
                    }

                    LeverOff(gateX + 3, f - 2);
                    break;
                }

                case Kind.SpikePit:
                    RestoreBoulder(room.To - 2, room.Ceiling - 1);
                    break;

                case Kind.Boulders:
                    foreach (var bx in new[] { room.From + 5, room.From + 13, room.From + 21 })
                        RestoreBoulder(bx, room.Ceiling - 1);
                    break;

                case Kind.FalseFloor:
                    for (var x = room.From + 6; x <= room.To - 6; x++)
                        Actuate(x, f, false);
                    RestoreBoulder(room.From + 10, room.Ceiling - 1);
                    break;
            }
        }

        // No RangeFrame here: framing a boulder hanging from the ceiling makes Terraria try to drop it, which the
        // protection refuses, over and over (a stack overflow). The gate and false floor are framed one by one instead.
        if (Main.netMode != NetmodeID.Server)
            return;
        foreach (var room in rooms)
            NetMessage.SendTileSquare(-1, room.From - 2, room.Ceiling - 3, room.Width + 4, f - room.Ceiling + 6);
    }

    private static void LeverOff(int x, int y)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasTile)
            return;
        if (tile.TileType == TileID.Lever)
            tile.TileFrameX = (short)(tile.TileFrameX % 36);
        else if (tile.TileType == TileID.Switches)
            tile.TileFrameY = 0;
    }

    private static void Actuate(int x, int y, bool open)
    {
        var tile = Main.tile[x, y];
        if (!tile.HasActuator || tile.IsActuated == open)
            return;
        tile.IsActuated = open;
        WorldGen.SquareTileFrame(x, y, false);
    }

    private static void RestoreBoulder(int x, int bottomY)
    {
        if (Main.tile[x, bottomY].HasTile && Main.tile[x, bottomY].TileType == TileID.Boulder)
            return;
        Boulder(x, bottomY);
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
        // Few torches, low on the back wall only: the ceilings and most of the floor stay dark
        for (var x = room.From + 4; x <= room.To - 2; x += 16)
        {
            if (!Main.tile[x, f - 4].HasTile && Main.tile[x, f].HasTile && Main.tile[x, f].TileType == TileID.GrayBrick)
                WorldGen.PlaceTile(x, f - 4, TileID.Torches, true, true);
        }

        // Cobwebs in the top corners
        foreach (var x in new[] { room.From, room.From + 1, room.To, room.To - 1 })
        {
            if (!Main.tile[x, room.Ceiling + 1].HasTile)
                WorldGen.PlaceTile(x, room.Ceiling + 1, TileID.Cobweb, true, true);
        }

        // Rubble on the floor (never on the false floor or near the traps' plates)
        if (room.Kind is Kind.Darts or Kind.FalseFloor)
            return;
        for (var x = room.From + 1; x < room.To; x += 5)
        {
            var floor = Main.tile[x, f];
            if (!Main.tile[x, f - 1].HasTile && floor.HasTile && floor.TileType == TileID.GrayBrick && !floor.HasActuator &&
                WorldGen.genRand.NextBool(2))
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
    ///     A dart trap block (it's solid: a step of one tile in the floor, or set into a wall).
    /// </summary>
    private static void Dart(int x, int y, bool facingRight)
    {
        var tile = Main.tile[x, y];
        tile.ClearTile();
        tile.HasTile = true;
        tile.TileType = TileID.Traps;
        tile.TileFrameX = (short)(facingRight ? 18 : 0);
        tile.TileFrameY = 0;
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
    ///     The chest at the end, a generous one: the diary page, supplies and held items for the fight with Brock,
    ///     treasure to sell and things for the way.
    /// </summary>
    private static void Chest(int x, int f)
    {
        var index = WorldGen.PlaceChest(x, f - 1, TileID.Containers, false, 1); // Gold Chest
        if (index >= 0)
            FillLoot(index);
    }

    /// <summary>
    ///     Puts the loot in a chest (empties it first). Also used when a player opens the chest for the first time and
    ///     finds it empty (see <see cref="BrockDungeonChestTrap" />), so everyone gets it once.
    /// </summary>
    public static void FillLoot(int index)
    {
        var chest = Main.chest[index];
        if (chest == null)
            return;
        foreach (var item in chest.item)
            item.TurnToAir();
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
            (ModContent.ItemType<Repel>(), 2),
            // For the fight with Brock right after
            (ModContent.ItemType<HardStone>(), 1),
            (ModContent.ItemType<FocusSash>(), 1),
            (ModContent.ItemType<Revive>(), 2),
            (ModContent.ItemType<RareCandy>(), 2),
            (ModContent.ItemType<ExpCandyS>(), 3),
            (ModContent.ItemType<ExpCandyM>(), 2),
            (ModContent.ItemType<OranBerry>(), 3),
            (ModContent.ItemType<SitrusBerry>(), 2),
            // Treasure to sell
            (ModContent.ItemType<Nugget>(), 2),
            (ModContent.ItemType<RelicCopper>(), 1),
            // For whoever made it through the lava and the pits
            (ItemID.HealingPotion, 5),
            (ItemID.ObsidianSkinPotion, 3),
            (ItemID.Compass, 1),
            (ItemID.Rope, 100)
        };
        var slot = 0;
        foreach (var (type, stack) in loot)
        {
            if (type <= ItemID.None)
                continue;
            chest.item[slot].SetDefaults(type);
            chest.item[slot].stack = Math.Clamp(stack, 1, chest.item[slot].maxStack);
            slot++;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;
        for (var i = 0; i < chest.item.Length; i++)
            NetMessage.SendData(MessageID.SyncChestItem, -1, -1, null, index, i);
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
        spawnRate = Math.Max(1, spawnRate / 8);
        maxSpawns += 8;
    }

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (!Inside(spawnInfo.Player))
            return;
        pool[NPCID.CaveBat] = 3f;
        pool[NPCID.Hellbat] = 0.6f; // Drawn by the lava
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

/// <summary>
///     The dart traps of Brock's dungeon fire on their own, on a timer (no plates): every two seconds, the ones facing
///     right first and the ones facing left a second later, so there's a gap to run in. Only while someone is close.
/// </summary>
public class BrockDungeonDarts : ModSystem
{
    private const int Period = 120;
    private static List<(Point Tile, int Direction)> _traps;

    /// <summary>
    ///     Makes the trap list be looked up again (the dungeon was just built).
    /// </summary>
    public static void Reset() => _traps = null;

    public override void ClearWorld() => _traps = null;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !BrockGymSystem.HasDungeon)
            return;
        var area = BrockGymSystem.DungeonArea;
        if (area.IsEmpty || !Main.player.Any(p => p.active && !p.dead && Inflate(area).Contains(p.Center.ToTileCoordinates())))
            return;

        _traps ??= FindTraps(area);
        var time = (int)(Main.GameUpdateCount % Period);
        foreach (var ((x, y), direction) in _traps)
        {
            if (time != (direction > 0 ? 0 : Period / 2))
                continue;
            var tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType != TileID.Traps)
                continue;
            var position = new Vector2(x * 16 + 8 + direction * 12, y * 16 + 8);
            Projectile.NewProjectile(new EntitySource_Wiring(x, y), position, new Vector2(direction * 12f, 0f),
                ProjectileID.PoisonDartTrap, 20, 2f, Main.myPlayer);
        }
    }

    private static Rectangle Inflate(Rectangle area)
    {
        area.Inflate(60, 40);
        return area;
    }

    /// <summary>
    ///     Every dart trap in the dungeon, past the landing (the gym's own trap there still waits for its plate).
    /// </summary>
    private static List<(Point, int)> FindTraps(Rectangle area)
    {
        var traps = new List<(Point, int)>();
        for (var x = area.Left + 16; x < area.Right; x++)
        {
            for (var y = area.Top; y < area.Bottom; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.HasTile && tile.TileType == TileID.Traps && tile.TileFrameY == 0)
                    traps.Add((new Point(x, y), tile.TileFrameX == 0 ? -1 : 1));
            }
        }

        return traps;
    }
}

/// <summary>
///     The chest at the end of Brock's dungeon opens once per player and world: the first time, it's filled with the
///     loot if someone else already emptied it, and boulders come down from the dark ceiling; after that it stays shut
///     for that player, so nobody piles up items.
/// </summary>
public class BrockDungeonChestTrap : ModPlayer
{
    private int _lastChest = -1;
    private readonly HashSet<string> _opened = []; // World ids

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;
        var opened = Player.chest >= 0 && Player.chest != _lastChest;
        _lastChest = Player.chest;
        if (!opened || !BrockGymSystem.HasDungeon)
            return;
        var index = Player.chest;
        var chest = Main.chest[index];
        if (chest == null)
            return;
        var room = BrockDungeon.ChestRoom(BrockGymSystem.SurfaceEntrance.X, BrockGymSystem.Interior.Bottom);
        if (!room.Contains(chest.x, chest.y))
            return;

        var world = Main.ActiveWorldFileData?.UniqueId.ToString() ?? "";
        if (!_opened.Add(world))
        {
            // Already opened once: it stays shut
            Player.chest = -1;
            _lastChest = -1;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncPlayerChestIndex, -1, -1, null, Player.whoAmI, -1);
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.NewText(Language.GetTextValue("Mods.Terramon.BrockGym.ChestDone"), new Color(200, 170, 120));
            return;
        }

        if (chest.item.All(i => i.IsAir))
            BrockDungeon.FillLoot(index);
        Main.NewText(Language.GetTextValue("Mods.Terramon.BrockGym.ChestOnce"), new Color(200, 170, 120));

        SoundEngine.PlaySound(SoundID.Item14, Player.Center);
        foreach (var dx in new[] { -6, -2, 2, 6 })
        {
            var position = new Vector2((chest.x + 1 + dx) * 16f, (room.Top + 1) * 16f);
            Projectile.NewProjectile(Player.GetSource_FromThis(), position, new Vector2(0f, 2f), ProjectileID.Boulder,
                60, 10f, Main.myPlayer);
        }
    }

    public override void SaveData(TagCompound tag)
    {
        if (_opened.Count > 0)
            tag["brockChest"] = _opened.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _opened.Clear();
        foreach (var world in tag.GetList<string>("brockChest"))
            _opened.Add(world);
    }
}

/// <summary>
///     Brock's dungeon starts over whenever nobody is left inside (everyone went on to the gym, left, or died): the
///     challenges are put back (see <see cref="BrockDungeon.ResetTraps" />), so dying means doing it all again. Also once
///     when the world is loaded.
/// </summary>
public class BrockDungeonReset : ModSystem
{
    private static bool _wasOccupied;
    private static int _timer;

    public override void OnWorldLoad() => _wasOccupied = true;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !BrockGymSystem.HasDungeon || ++_timer < 30)
            return;
        _timer = 0;
        var area = BrockGymSystem.DungeonArea;
        if (area.IsEmpty)
            return;
        var occupied = Main.player.Any(p => p.active && !p.dead && area.Contains(p.Center.ToTileCoordinates()));
        if (_wasOccupied && !occupied)
            BrockDungeon.ResetTraps(BrockGymSystem.SurfaceEntrance.X, BrockGymSystem.Interior.Bottom);
        _wasOccupied = occupied;
    }
}
