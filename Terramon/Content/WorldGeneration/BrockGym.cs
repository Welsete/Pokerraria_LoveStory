using Terramon.Content.NPCs.Trainers;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader.IO;
using Terraria.UI;
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

    /// <summary>
    ///     The top of the rope shaft at the surface (the first ground row), next to the gym's sign.
    /// </summary>
    public static Point SurfaceEntrance { get; internal set; }

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
        SurfaceEntrance = Point.Zero;
        _occupied = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists)
            return;
        tag["brockGym"] = new[]
        {
            Interior.X, Interior.Y, Interior.Width, Interior.Height, LeaderSpot.X, LeaderSpot.Y, SurfaceEntrance.X,
            SurfaceEntrance.Y
        };
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.TryGet<int[]>("brockGym", out var data) && data.Length >= 6)
        {
            Interior = new Rectangle(data[0], data[1], data[2], data[3]);
            LeaderSpot = new Point(data[4], data[5]);
            SurfaceEntrance = data.Length >= 8 ? new Point(data[6], data[7]) : Point.Zero;
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
        writer.Write(SurfaceEntrance.X);
        writer.Write(SurfaceEntrance.Y);
    }

    public override void NetReceive(BinaryReader reader)
    {
        Interior = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        LeaderSpot = new Point(reader.ReadInt32(), reader.ReadInt32());
        SurfaceEntrance = new Point(reader.ReadInt32(), reader.ReadInt32());
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
///     Shows the Boulder Badge on the map over Brock's Gym, and over its entrance at the surface.
/// </summary>
public class BrockGymMapLayer : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!BrockGymSystem.Exists)
            return;

        var icon = ModContent.Request<Texture2D>("Terramon/Assets/Items/Badges/BoulderBadge").Value;
        var gym = BrockGymSystem.Interior.Center.ToVector2();
        if (context.Draw(icon, gym, Color.White, new SpriteFrame(1, 1, 0, 0), 0.9f, 1.4f, Alignment.Center)
            .IsMouseOver)
            text = Language.GetTextValue("Mods.Terramon.BrockGym.MapName");

        if (BrockGymSystem.SurfaceEntrance == Point.Zero)
            return;
        if (context.Draw(icon, BrockGymSystem.SurfaceEntrance.ToVector2(), Color.White * 0.8f,
                new SpriteFrame(1, 1, 0, 0), 0.6f, 1f, Alignment.Bottom).IsMouseOver)
            text = Language.GetTextValue("Mods.Terramon.BrockGym.MapEntrance");
    }
}

/// <summary>
///     Tells each player once per world where Brock's Gym is.
/// </summary>
public class BrockGymPlayer : ModPlayer
{
    private readonly HashSet<string> _toldWorlds = [];

    public override void OnEnterWorld()
    {
        var world = Main.ActiveWorldFileData?.UniqueId.ToString();
        if (!BrockGymSystem.Exists || string.IsNullOrEmpty(world) || !_toldWorlds.Add(world))
            return;

        var entrance = BrockGymSystem.SurfaceEntrance == Point.Zero
            ? BrockGymSystem.Interior.Center
            : BrockGymSystem.SurfaceEntrance;
        var offset = entrance.X - Main.spawnTileX;
        var direction = Language.GetTextValue("Mods.Terramon.Pedestals." + (offset < 0 ? "West" : "East"));
        Main.NewText(Language.GetTextValue("Mods.Terramon.BrockGym.Announce", Math.Abs(offset), direction),
            new Color(200, 170, 120));
    }

    public override void SaveData(TagCompound tag)
    {
        if (_toldWorlds.Count > 0)
            tag["brockGymTold"] = _toldWorlds.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _toldWorlds.Clear();
        foreach (var world in tag.GetList<string>("brockGymTold"))
            _toldWorlds.Add(world);
    }
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
    public const int Width = 62;
    public const int Height = 26;
    private const int Shell = 2;
    private const int EntranceGap = 6; // Tunnel between the left door and the rope shaft
    private const int StageHeight = 3;
    private const int RopeAbove = 3; // How many rope tiles stick out of the ground at the entrance

    private const byte NoPaint = 0;

    // Lantern styles of TileID.HangingLanterns
    private const int ChainLantern = 0;
    private const int BrassLantern = 1;

    /// <summary>
    ///     Tries random spots underground, a short walk from spawn, and builds the gym in the first that fits. If
    ///     nothing fits, the rules are loosened step by step (farther away, more open caves, more liquid around) so
    ///     every world still gets its gym.
    /// </summary>
    public static bool TryPlaceNearSpawn()
    {
        (int MaxOffset, int SolidPercent, bool StrictStructures)[] tiers =
        [
            (200, 45, true),
            (350, 30, true),
            (500, 15, true),
            (Main.maxTilesX / 2, 0, false)
        ];

        var minDepth = (int)Main.worldSurface + 15;
        var maxDepth = Math.Max(minDepth + 10, (int)Main.rockLayer + 60);
        foreach (var (maxOffset, solidPercent, strictStructures) in tiers)
        {
            for (var attempt = 0; attempt < 1500; attempt++)
            {
                var offset = WorldGen.genRand.Next(50, Math.Max(51, maxOffset));
                var left = Main.spawnTileX + (WorldGen.genRand.NextBool() ? offset : -offset - Width);
                var top = WorldGen.genRand.Next(minDepth, maxDepth);
                if (!CanPlace(left, top, solidPercent, strictStructures))
                    continue;

                Build(left, top);
                GenVars.structures?.AddProtectedStructure(Footprint(left, top), 4);
                GenVars.structures?.AddProtectedStructure(
                    new Rectangle(left - EntranceGap - 4, BrockGymSystem.SurfaceEntrance.Y - 6, 12, 12), 2);
                Terramon.Instance.Logger.Info(
                    $"Built Brock's Gym at ({left}, {top}) (offset {maxOffset}, solid {solidPercent}%).");
                return true;
            }
        }

        return false;
    }

    private static Rectangle Footprint(int left, int top)
        => new(left - EntranceGap - 4, top - 2, Width + EntranceGap + 8, Height + 4);

    private static bool CanPlace(int left, int top, int solidPercent, bool strictStructures)
    {
        var area = Footprint(left, top);
        if (area.Left < 100 || area.Right > Main.maxTilesX - 100 || area.Bottom > Main.UnderworldLayer - 20)
            return false;
        if (Math.Abs(left + Width / 2 - Main.dungeonX) < 200)
            return false;
        if (strictStructures && GenVars.structures != null && !GenVars.structures.CanPlace(area))
            return false;

        // The shell is solid, so liquid can only get in through the tunnel and the rope shaft: keep those dry.
        // Liquid inside the footprint itself is drained when building.
        var shaftX = left - EntranceGap;
        for (var x = shaftX - 4; x <= left + 2; x++)
        {
            var fromY = x <= shaftX + 4 ? (int)(Main.worldSurface * 0.35) : top + Height - 9;
            for (var y = fromY; y < top + Height + 2; y++)
            {
                if (Main.tile[x, y].LiquidAmount > 0)
                    return false;
            }
        }

        // Mostly buried in natural ground (a cave running through it is fine), with nothing built in the way
        var solid = 0;
        for (var x = area.Left; x < area.Right; x++)
        {
            for (var y = area.Top; y < area.Bottom; y++)
            {
                var tile = Main.tile[x, y];
                if (!tile.HasTile)
                    continue;
                if (TileID.Sets.BasicChest[tile.TileType] || tile.TileType is TileID.LihzahrdBrick
                        or TileID.BlueDungeonBrick or TileID.GreenDungeonBrick or TileID.PinkDungeonBrick
                        or TileID.Hive or TileID.LivingWood)
                    return false;
                if (Main.tileSolid[tile.TileType])
                    solid++;
            }
        }

        return solid * 100 >= area.Width * area.Height * solidPercent;
    }

    /// <summary>
    ///     Builds the gym with its outer top-left corner at (left, top), plus the tunnel and the rope shaft up to
    ///     the surface, and remembers it.
    /// </summary>
    public static void Build(int left, int top)
    {
        // Rebuilding over a gym that's already there (dev panel) has to be able to change its tiles
        var bypass = ProtectedAreas.Bypass;
        ProtectedAreas.Bypass = true;
        try
        {
            BuildHall(left, top);
        }
        finally
        {
            ProtectedAreas.Bypass = bypass;
        }

        Protect(left, top, BrockGymSystem.SurfaceEntrance);
    }

    private static void BuildHall(int left, int top)
    {
        var right = left + Width - 1;
        var bottom = top + Height - 1;
        var floorY = bottom - Shell; // First row of the floor; the air ends just above it
        var inLeft = left + Shell;
        var inRight = right - Shell;
        var ceilingY = top + Shell;
        var centerLeft = left + 17;
        var centerRight = right - 17;
        var stageLeft = centerRight + 2;
        var stageTop = floorY - StageHeight;

        // Shell: gray brick outside, a layer of stone slab inside, hollow in the middle
        for (var x = left; x <= right; x++)
        {
            for (var y = top; y <= bottom; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearEverything();
                var inside = x >= inLeft && x <= inRight && y >= ceilingY && y < floorY;
                if (inside)
                    continue;
                var outer = x == left || x == right || y == top || y == bottom;
                PlaceBlock(x, y, outer ? TileID.GrayBrick : TileID.StoneSlab);
            }
        }

        // The side halls have a lower ceiling than the arena, and the outer corners are sloped
        for (var x = inLeft; x <= inRight; x++)
        {
            var sideHall = x < centerLeft || x > centerRight;
            var ceiling = ceilingY + (sideHall ? 3 : 0) + Math.Max(0, 3 - Math.Min(x - inLeft, inRight - x));
            for (var y = ceilingY; y < ceiling; y++)
                PlaceBlock(x, y, y == ceiling - 1 ? TileID.GrayBrick : TileID.StoneSlab);
        }

        BuildWalls(inLeft, inRight, ceilingY, floorY, centerLeft, centerRight);
        BuildArenaFloor(centerLeft - 3, stageLeft - 1, floorY);

        // Brock's raised stage: gray brick with a stone slab top, and steps down to the arena
        for (var x = stageLeft; x <= inRight; x++)
        {
            for (var y = stageTop; y < floorY; y++)
                PlaceBlock(x, y, y == stageTop ? TileID.StoneSlab : TileID.GrayBrick);
        }

        for (var step = 1; step < StageHeight; step++)
        {
            for (var y = floorY - step; y < floorY; y++)
                PlaceBlock(stageLeft - step, y, TileID.GrayBrick);
        }

        for (var step = 0; step < StageHeight; step++)
            WorldGen.SlopeTile(stageLeft - step, floorY - StageHeight + step, (int)SlopeType.SlopeDownRight);

        BrockGymSystem.Interior = new Rectangle(inLeft, ceilingY, inRight - inLeft + 1, floorY - ceilingY);
        BrockGymSystem.LeaderSpot = new Point(inRight - 6, stageTop);

        // The hall itself is done: a failing detail below shouldn't cost the whole gym
        TryStep("doors", () => BuildDoors(left, right, floorY, stageTop));
        TryStep("entrance", () => BuildEntrance(left, floorY));
        TryStep("mural", () => BuildMural((centerLeft + centerRight) / 2 - 15, ceilingY + 1));
        TryStep("decorations",
            () => Decorate(inLeft, inRight, ceilingY, floorY, centerLeft, centerRight, stageLeft, stageTop));

        WorldGen.RangeFrame(left - EntranceGap - 4, top - 2, right + 2, bottom + 2);
    }

    /// <summary>
    ///     Makes the gym, its tunnel and its rope shaft unbreakable (see <see cref="ProtectedAreas" />).
    /// </summary>
    private static void Protect(int left, int top, Point surfaceEntrance)
    {
        var floorY = top + Height - 1 - Shell;
        var shaftX = left - EntranceGap;
        ProtectedAreas.Add(new Rectangle(left, top, Width, Height));
        ProtectedAreas.Add(new Rectangle(shaftX - 1, floorY - 4, left - shaftX + 1, 5));
        if (surfaceEntrance != Point.Zero && surfaceEntrance.X == shaftX && surfaceEntrance.Y < floorY)
            ProtectedAreas.Add(new Rectangle(shaftX - 1, surfaceEntrance.Y - RopeAbove,
                3, floorY - surfaceEntrance.Y + RopeAbove + 1));
    }

    /// <summary>
    ///     Protects the Brock's Gym of a world made before <see cref="ProtectedAreas" /> existed, and its sign.
    /// </summary>
    internal static void ProtectExisting()
    {
        if (!BrockGymSystem.Exists)
            return;
        var interior = BrockGymSystem.Interior;
        var left = interior.X - Shell;
        Protect(left, interior.Y - Shell, BrockGymSystem.SurfaceEntrance);

        // The sign used to stand a few tiles right of the shaft
        var entrance = BrockGymSystem.SurfaceEntrance;
        if (entrance == Point.Zero)
            return;
        for (var x = entrance.X + 1; x <= entrance.X + 7; x++)
        {
            for (var y = entrance.Y - 8; y <= entrance.Y + 6; y++)
            {
                if (!WorldGen.InWorld(x, y, 10) || !Main.tile[x, y].HasTile || Main.tile[x, y].TileType != TileID.Signs)
                    continue;
                var sign = Main.tile[x, y];
                ProtectedAreas.Add(new Rectangle(x - sign.TileFrameX % 36 / 18, y - sign.TileFrameY % 36 / 18, 2,
                    3));
                return;
            }
        }
    }

    /// <summary>
    ///     Set when a world from before the entrance was only a sign and a rope is loaded: its stone arch is taken
    ///     down once (see <see cref="RemoveOldArch" />).
    /// </summary>
    internal static bool NeedsArchRemoved { get; set; }

    /// <summary>
    ///     Takes down the stone arch (and its torches) older versions built over the rope at the surface, if it's
    ///     still exactly there, so the entrance is just the sign and the rope. Runs on the server or in singleplayer.
    /// </summary>
    internal static void RemoveOldArch()
    {
        var entrance = BrockGymSystem.SurfaceEntrance;
        if (!BrockGymSystem.Exists || entrance == Point.Zero)
            return;
        var (x0, surface) = (entrance.X, entrance.Y);
        if (!WorldGen.InWorld(x0, surface - 8, 20) || !WorldGen.InWorld(x0, surface, 20))
            return;

        bool Is(int x, int y, ushort type) => Main.tile[x, y].HasTile && Main.tile[x, y].TileType == type;
        if (!Is(x0, surface - 7, TileID.GrayBrick) || !Is(x0, surface - 6, TileID.StoneSlab) ||
            !Is(x0 - 3, surface - 3, TileID.GrayBrick) || !Is(x0 + 3, surface - 3, TileID.GrayBrick))
            return;

        void Clear(int x, int y, ushort type)
        {
            if (Is(x, y, type))
                Main.tile[x, y].ClearTile();
        }

        for (var x = x0 - 3; x <= x0 + 3; x++)
        {
            Clear(x, surface - 6, TileID.StoneSlab);
            if (Math.Abs(x - x0) <= 2)
                Clear(x, surface - 7, TileID.GrayBrick);
        }

        foreach (var x in new[] { x0 - 3, x0 + 3 })
        {
            Clear(x, surface - 7, TileID.Torches);
            for (var y = surface - 5; y < surface; y++)
                Clear(x, y, TileID.GrayBrick);
        }

        // The rope went up into the arch: keep only the part sticking out of the ground
        for (var y = surface - 5; y < surface - RopeAbove; y++)
            Clear(x0, y, TileID.Rope);

        WorldGen.RangeFrame(x0 - 4, surface - 8, x0 + 4, surface + 1);
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendTileSquare(-1, x0 - 4, surface - 8, 9, 10);
        Terramon.Instance.Logger.Info($"Took down the old stone arch over Brock's Gym entrance at {entrance}.");
    }

    private static void TryStep(string name, Action step)
    {
        try
        {
            step();
        }
        catch (Exception e)
        {
            Terramon.Instance.Logger.Error($"Brock's Gym: building the {name} failed", e);
        }
    }

    /// <summary>
    ///     Carved cave walls, stone slab and palladium column pillars, and a slab band under the ceiling.
    /// </summary>
    private static void BuildWalls(int inLeft, int inRight, int ceilingY, int floorY, int centerLeft,
        int centerRight)
    {
        var cave = WallByName("Cave8Unsafe", WallID.Stone);
        var dirt = WallByName("Cave6Unsafe", WallID.DirtUnsafe);
        var pillars = new[] { inLeft + 7, centerLeft - 1, centerRight + 1, inRight - 7 };
        for (var x = inLeft; x <= inRight; x++)
        {
            for (var y = ceilingY; y < floorY; y++)
            {
                var tile = Main.tile[x, y];
                tile.WallType = WorldGen.genRand.NextBool(5) ? dirt : cave;
                tile.WallColor = NoPaint;

                var pillar = pillars.Select(p => Math.Abs(p - x)).Min();
                if (pillar == 0)
                    tile.WallType = WallID.StoneSlab;
                else if (pillar == 1)
                {
                    tile.WallType = WallID.PalladiumColumn;
                    tile.WallColor = PaintID.GrayPaint;
                }
                else if (y <= ceilingY + 3 && (x < centerLeft || x > centerRight) || y == ceilingY)
                    tile.WallType = WallID.StoneSlab;
            }
        }
    }

    /// <summary>
    ///     The middle of the arena: uneven ground of stone, mudstone and dirt, with ramps between the steps.
    /// </summary>
    private static void BuildArenaFloor(int from, int to, int floorY)
    {
        var mud = WorldGen.genRand.NextBool() ? TileID.Mud : TileID.Dirt;
        var heights = new int[to - from + 1];
        var height = 0;
        for (var i = 0; i < heights.Length; i++)
        {
            // A gentle random walk, flat near both ends so the halls join up
            var nearEnd = i < 3 || i > heights.Length - 4;
            if (nearEnd)
                height = 0;
            else if (WorldGen.genRand.NextBool(3))
                height = Math.Clamp(height + WorldGen.genRand.Next(-1, 2), 0, 2);
            heights[i] = height;
        }

        for (var i = 0; i < heights.Length; i++)
        {
            var x = from + i;
            for (var y = floorY - heights[i]; y <= floorY; y++)
            {
                var roll = WorldGen.genRand.Next(10);
                var type = roll < 6 ? TileID.Stone : roll < 8 ? TileID.StoneSlab : (ushort)mud;
                PlaceBlock(x, y, type);
            }
        }

        // Ramps wherever the ground steps up or down, so walking across never needs a jump
        for (var i = 1; i < heights.Length - 1; i++)
        {
            var x = from + i;
            var y = floorY - heights[i];
            if (heights[i] <= 0)
                continue;
            if (heights[i - 1] < heights[i])
                WorldGen.SlopeTile(x, y, (int)SlopeType.SlopeDownRight);
            else if (heights[i + 1] < heights[i])
                WorldGen.SlopeTile(x, y, (int)SlopeType.SlopeDownLeft);
        }
    }

    /// <summary>
    ///     The mural of a Poké Ball and the Boulder Badge, drawn tile by tile with gemspark walls (turned off) and paint.
    /// </summary>
    private static void BuildMural(int left, int top)
    {
        const int w = 31;
        const int h = 15;
        var white = WallByName("DiamondGemsparkOff", WallID.MarbleBlock);
        var red = WallByName("RubyGemsparkOff", WallID.MarbleBlock);
        var amber = WallByName("AmberGemsparkOff", WallID.MarbleBlock);
        var marble = WallByName("MarbleBlock", WallID.MarbleBlock);

        for (var i = 0; i < w; i++)
        {
            for (var j = 0; j < h; j++)
            {
                var tile = Main.tile[left + i, top + j];
                ushort wall;
                byte paint = NoPaint;

                if (i == 0 || j == 0 || i == w - 1 || j == h - 1 || i == w / 2)
                    (wall, paint) = (WallID.StoneSlab, NoPaint); // Frame and the line between the halves
                else if (i < w / 2)
                    (wall, paint) = PokeBallPixel(i - 8, j - 7, white, red, marble);
                else
                    (wall, paint) = BadgePixel(i - 23, j - 7, white, amber);

                tile.WallType = wall;
                tile.WallColor = paint;
            }
        }
    }

    private static (ushort, byte) PokeBallPixel(int dx, int dy, ushort white, ushort red, ushort background)
    {
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist > 6.2)
            return (background, PaintID.GrayPaint);
        if (dist > 5.3 || Math.Abs(dy) == 0 || dist <= 2.2 && dist > 1.2)
            return (white, PaintID.BlackPaint); // Outline, middle band and the button's ring
        if (dist <= 1.2)
            return (white, NoPaint); // Button
        return dy < 0 ? (red, NoPaint) : (white, NoPaint);
    }

    private static (ushort, byte) BadgePixel(int dx, int dy, ushort white, ushort amber)
    {
        // An octagon, like the Boulder Badge
        var size = Math.Max(Math.Abs(dx), Math.Abs(dy)) + Math.Min(Math.Abs(dx), Math.Abs(dy)) / 2.0;
        if (Math.Abs(dx) > 6 || Math.Abs(dy) > 6 || size > 7.5)
            return (amber, NoPaint);
        if (Math.Abs(dx) == 6 || Math.Abs(dy) == 6 || size > 6.5)
            return (white, PaintID.BlackPaint);
        if (dx is -3 or -2 && dy is -3 or -2)
            return (white, NoPaint); // Shine
        return (white, PaintID.GrayPaint);
    }

    private static void BuildDoors(int left, int right, int floorY, int stageTop)
    {
        foreach (var (x, outer, doorFloor) in new[] { (left + 1, left, floorY), (right - 1, right, stageTop) })
        {
            for (var y = doorFloor - 3; y < doorFloor; y++)
            {
                Main.tile[x, y].ClearTile();
                Main.tile[outer, y].ClearTile();
            }

            // For doors, PlaceTile takes the bottom tile of the opening and builds the door upwards from it
            WorldGen.PlaceTile(x, doorFloor - 1, TileID.ClosedDoor, true, true);
        }
    }

    private static void Decorate(int inLeft, int inRight, int ceilingY, int floorY, int centerLeft, int centerRight,
        int stageLeft, int stageTop)
    {
        // Brass and chain lanterns hanging on chains
        var arenaMid = (centerLeft + centerRight) / 2;
        foreach (var (x, length, style) in new[]
                 {
                     (centerLeft + 2, 4, BrassLantern), (arenaMid - 9, 2, ChainLantern),
                     (arenaMid + 9, 2, ChainLantern), (centerRight - 2, 4, BrassLantern)
                 })
            HangLantern(x, ceilingY, length, style);
        foreach (var x in new[] { inLeft + 4, inLeft + 11, inRight - 4 })
            HangLantern(x, ceilingY + 3, 2, BrassLantern);

        // Torches on the pillars
        foreach (var x in new[] { inLeft + 7, centerLeft - 1, centerRight + 1, inRight - 7 })
            WorldGen.PlaceTile(x, x > centerRight ? stageTop - 4 : floorY - 4, TileID.Torches, true, false, -1, 0);

        // Weapon racks on the side walls
        WorldGen.PlaceObject(inLeft + 2, floorY - 5, TileID.WeaponsRack2, true);
        WorldGen.PlaceObject(inRight - 2, stageTop - 5, TileID.WeaponsRack2, true);

        // Clay pots along the walls and on the stage
        foreach (var x in new[] { inLeft + 1, inLeft + 9, inLeft + 13 })
            WorldGen.PlaceTile(x, floorY - 1, TileID.Pots, true, false, -1, WorldGen.genRand.Next(4));
        foreach (var x in new[] { inRight - 1, stageLeft + 1 })
            WorldGen.PlaceTile(x, stageTop - 1, TileID.Pots, true, false, -1, WorldGen.genRand.Next(4));

        // Boulders resting on flat spots of the arena (they only roll if someone digs under them)
        var placed = 0;
        for (var x = centerLeft + 2; x < stageLeft - 4 && placed < 3; x += WorldGen.genRand.Next(5, 9))
        {
            var ground = GroundBelow(x, ceilingY, floorY);
            if (ground == GroundBelow(x + 1, ceilingY, floorY) && Main.tile[x, ground].Slope == SlopeType.Solid &&
                Main.tile[x + 1, ground].Slope == SlopeType.Solid &&
                WorldGen.PlaceObject(x, ground - 1, TileID.Boulder, true))
                placed++;
        }
    }

    private static int GroundBelow(int x, int fromY, int floorY)
    {
        for (var y = fromY; y <= floorY; y++)
        {
            if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                return y;
        }

        return floorY;
    }

    /// <summary>
    ///     A chain from the ceiling with a lantern at its end. If the lantern doesn't hold on the chain, it goes right
    ///     under the ceiling instead.
    /// </summary>
    private static void HangLantern(int x, int ceilingY, int chainLength, int style)
    {
        for (var y = ceilingY; y < ceilingY + chainLength; y++)
        {
            var chain = Main.tile[x, y];
            chain.HasTile = true;
            chain.TileType = TileID.Chain;
        }

        if (WorldGen.PlaceObject(x, ceilingY + chainLength, TileID.HangingLanterns, true, style))
            return;

        for (var y = ceilingY; y < ceilingY + chainLength; y++)
            Main.tile[x, y].ClearTile();
        WorldGen.PlaceObject(x, ceilingY, TileID.HangingLanterns, true, style);
    }

    /// <summary>
    ///     A short brick tunnel out of the left door, then a shaft with a rope all the way up to the surface, where the
    ///     entrance is just the rope sticking out of the ground and a sign next to it.
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
                tile.ClearEverything();
                if (y == floorY || y == floorY - 4)
                    PlaceBlock(x, y, TileID.GrayBrick);
                else
                    tile.WallType = WallID.StoneSlab;
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

        // Clear the air over the hole too, so trees or grass don't hide the rope
        for (var y = surface - RopeAbove - 1; y < floorY; y++)
        {
            for (var x = shaftX - 1; x <= shaftX + 1; x++)
            {
                var tile = Main.tile[x, y];
                // A tree in the way is cut down whole instead of leaving its top floating
                if (y < surface && tile.HasTile)
                    WorldGen.KillTile(x, y, false, false, true);
                tile.ClearTile();
                tile.LiquidAmount = 0;
                if (y > surface + 2)
                    tile.WallType = WallID.GrayBrick;
            }
        }

        // Ropes are set directly, top to bottom, so each one hangs from the one above. The top few stick out of the
        // ground so the entrance is easy to spot and to grab.
        for (var y = surface - RopeAbove; y < floorY; y++)
        {
            var rope = Main.tile[shaftX, y];
            rope.HasTile = true;
            rope.TileType = TileID.Rope;
        }

        // The sign right next to the hole, on the ground
        var signX = shaftX + 3;
        var signGround = surface;
        for (var y = surface - 6; y < surface + 6; y++)
        {
            if (Main.tile[signX, y].HasTile && Main.tileSolid[Main.tile[signX, y].TileType] &&
                Main.tile[signX + 1, y].HasTile && Main.tileSolid[Main.tile[signX + 1, y].TileType])
            {
                signGround = y;
                break;
            }
        }

        for (var x = signX; x <= signX + 1; x++)
        {
            if (!Main.tile[x, signGround].HasTile || !Main.tileSolid[Main.tile[x, signGround].TileType])
                PlaceBlock(x, signGround, TileID.GrayBrick);
        }

        if (WorldGen.PlaceObject(signX, signGround - 1, TileID.Signs, true))
        {
            var sign = Sign.ReadSign(signX, signGround - 2, true);
            if (sign >= 0)
                Sign.TextSign(sign, Language.GetTextValue("Mods.Terramon.BrockGym.Sign"));
            ProtectedAreas.Add(new Rectangle(signX, signGround - 2, 2, 3));
        }

        BrockGymSystem.SurfaceEntrance = new Point(shaftX, surface);
    }

    /// <summary>
    ///     Looks up a wall by its internal name, so a wall missing from this tModLoader version doesn't break the
    ///     build.
    /// </summary>
    private static ushort WallByName(string name, ushort fallback)
        => WallID.Search.TryGetId(name, out var id) ? (ushort)id : fallback;

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
