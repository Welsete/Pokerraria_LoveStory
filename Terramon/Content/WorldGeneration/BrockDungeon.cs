using Terramon.Content.Items;
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
    private enum Kind { Landing, Levers, SpikePit, Darts, Boulders, FalseFloor, LavaPit, Well, Chest }

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
        (Kind.Chest, 11, 8)
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
                    // Dart traps at foot height, each set off by a plate a few tiles before it
                    for (var k = 0; k < 4; k++)
                    {
                        var dx = room.From + 6 + k * 7;
                        Dart(dx, f - 1, facingRight: false);
                        var px = dx - 3;
                        Plate(px, f);
                        Line(px, f - 1, px, f - 2, WireColor.Blue);
                        Line(px, f - 2, dx, f - 2, WireColor.Blue);
                        Line(dx, f - 2, dx, f - 1, WireColor.Blue);
                    }

                    // And two facing the other way, shooting at your back once you've run past them
                    for (var k = 0; k < 2; k++)
                    {
                        var dx = room.From + 8 + k * 14;
                        Dart(dx, f - 1, facingRight: true);
                        var px = dx + 3;
                        Plate(px, f);
                        Line(px, f - 1, px, f - 5, WireColor.Green);
                        Line(dx, f - 5, px, f - 5, WireColor.Green);
                        Line(dx, f - 5, dx, f - 1, WireColor.Green);
                    }

                    break;

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
                {
                    // Stepping on the first pillar fires a dart across the lava from the far side
                    var pillar = room.From + 8;
                    var dx = room.To - 2;
                    Dart(dx, f - 1, facingRight: false);
                    Plate(pillar, f);
                    Line(pillar, f - 1, pillar, f - 3, WireColor.Blue);
                    Line(pillar, f - 3, dx, f - 3, WireColor.Blue);
                    Line(dx, f - 3, dx, f - 1, WireColor.Blue);
                    break;
                }

                case Kind.Well:
                {
                    // Down in the gallery: a dart trap at each end, set off by plates near the lava
                    var bottom = f + WellDepth;
                    int from = room.From + 3, to = room.To - 3;
                    var mid = (from + to) / 2;
                    Dart(from + 1, bottom - 1, facingRight: true);
                    Dart(to - 1, bottom - 1, facingRight: false);
                    foreach (var (px, dx) in new[] { (mid - 4, to - 1), (mid + 4, from + 1) })
                    {
                        Plate(px, bottom);
                        Line(px, bottom - 1, px, bottom - 4, WireColor.Yellow);
                        Line(Math.Min(px, dx), bottom - 4, Math.Max(px, dx), bottom - 4, WireColor.Yellow);
                        Line(dx, bottom - 4, dx, bottom - 1, WireColor.Yellow);
                    }

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
