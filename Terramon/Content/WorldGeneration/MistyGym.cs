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
///     Misty's Gym: a glass hall on a palm wood deck at the beach, with an aquarium under the floor. It's built from
///     <c>Assets/Structures/MistyGym.json</c> (see <see cref="Blueprint" />). Misty waits inside whenever a player walks in.
/// </summary>
public class MistyGymSystem : ModSystem
{
    /// <summary>
    ///     The inside of the hall in tile coordinates, or <see cref="Rectangle.Empty" /> if this world has none.
    /// </summary>
    public static Rectangle Interior { get; internal set; }

    /// <summary>
    ///     Where Misty stands: the tile her feet rest on.
    /// </summary>
    public static Point LeaderSpot { get; internal set; }

    public static bool Exists => Interior != Rectangle.Empty;

    private static bool _occupied;
    private static int _checkTimer;

    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        // After the beaches, trees and liquids are done, before the final cleanup
        var index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup"));
        if (index < 0)
            index = tasks.Count;
        tasks.Insert(index, new MistyGymPass("Terramon Misty Gym", 10f));
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
        if (Exists)
            tag["mistyGym"] = new[] { Interior.X, Interior.Y, Interior.Width, Interior.Height, LeaderSpot.X, LeaderSpot.Y };
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (!tag.TryGet<int[]>("mistyGym", out var data) || data.Length < 6)
            return;
        Interior = new Rectangle(data[0], data[1], data[2], data[3]);
        LeaderSpot = new Point(data[4], data[5]);
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

        var misty = ModContent.NPCType<Misty>();
        if (NPC.AnyNPCs(misty))
            return;

        var spot = LeaderSpot.ToWorldCoordinates(8f, 0f);
        var index = NPC.NewNPC(new EntitySource_WorldEvent(), (int)spot.X, (int)spot.Y, misty);
        if (index >= Main.maxNPCs)
            return;
        var npc = Main.npc[index];
        npc.direction = npc.spriteDirection = visitor.Center.X > npc.Center.X ? 1 : -1;

        var text = Language.GetTextValue("Mods.Terramon.MistyGym.Welcome", npc.GivenOrTypeName);
        if (Main.netMode == NetmodeID.Server)
            Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(110, 180, 255));
        else
            Main.NewText(text, new Color(110, 180, 255));
    }

    /// <summary>
    ///     Where to put a player teleporting into the gym (world coordinates of their bottom).
    /// </summary>
    public static Vector2 EntranceSpot => new Point(Interior.X + 2, Interior.Bottom).ToWorldCoordinates(8f, 0f);
}

/// <summary>
///     Shows the Cascade Badge on the map over Misty's Gym.
/// </summary>
public class MistyGymMapLayer : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!MistyGymSystem.Exists)
            return;
        var icon = ModContent.Request<Texture2D>("Terramon/Assets/Items/Badges/CascadeBadge").Value;
        if (context.Draw(icon, MistyGymSystem.Interior.Center.ToVector2(), Color.White, new SpriteFrame(1, 1, 0, 0),
                0.9f, 1.4f, Alignment.Center).IsMouseOver)
            text = Language.GetTextValue("Mods.Terramon.MistyGym.MapName");
    }
}

/// <summary>
///     Tells each player once per world where Misty's Gym is.
/// </summary>
public class MistyGymPlayer : ModPlayer
{
    private readonly HashSet<string> _toldWorlds = [];

    public override void OnEnterWorld()
    {
        var world = Main.ActiveWorldFileData?.UniqueId.ToString();
        if (!MistyGymSystem.Exists || string.IsNullOrEmpty(world) || !_toldWorlds.Add(world))
            return;

        var offset = MistyGymSystem.Interior.Center.X - Main.spawnTileX;
        var direction = Language.GetTextValue("Mods.Terramon.Pedestals." + (offset < 0 ? "West" : "East"));
        Main.NewText(Language.GetTextValue("Mods.Terramon.MistyGym.Announce", Math.Abs(offset), direction),
            new Color(110, 180, 255));
    }

    public override void SaveData(TagCompound tag)
    {
        if (_toldWorlds.Count > 0)
            tag["mistyGymTold"] = _toldWorlds.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _toldWorlds.Clear();
        foreach (var world in tag.GetList<string>("mistyGymTold"))
            _toldWorlds.Add(world);
    }
}

/// <summary>
///     Builds Misty's Gym on one of the two beaches.
/// </summary>
public class MistyGymPass(string name, float loadWeight) : GenPass(name, loadWeight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Language.GetTextValue("Mods.Terramon.WorldGen.MistyGymPass");
        MistyGymSystem.Interior = Rectangle.Empty;

        try
        {
            if (!MistyGymBuilder.TryPlaceOnBeach())
                Terramon.Instance.Logger.Warn("Couldn't find a beach for Misty's Gym, skipping it.");
        }
        catch (Exception e)
        {
            // A missing gym is better than a world that can't be generated
            Terramon.Instance.Logger.Error("Building Misty's Gym failed", e);
        }

        progress.Set(1);
    }
}

public static class MistyGymBuilder
{
    private const string Path = "Assets/Structures/MistyGym.json";

    // Rows and columns of the blueprint (see tools/structures/misty_gym.py)
    private const int GroundRow = 29;     // First sand row next to the hall: the ground level of the beach
    private const int FloorRow = 28;      // The palm wood deck
    private const int ShoreGap = 6;       // Columns of beach between the sea and the blueprint
    private static readonly Rectangle Hall = new(7, 5, 66, 23);
    private static readonly Rectangle Aquarium = new(9, 29, 62, 8);
    private const int LeaderColumn = 44;

    public static int Width => Blueprint.Load(Path).Width;
    public static int Height => Blueprint.Load(Path).Height;

    /// <summary>
    ///     Builds the gym on the beach of a random side of the world (the other side if that one doesn't work), right
    ///     where the sea ends.
    /// </summary>
    public static bool TryPlaceOnBeach()
    {
        var width = Width;
        var leftFirst = WorldGen.genRand.NextBool();
        foreach (var leftSide in new[] { leftFirst, !leftFirst })
        {
            var shore = FindShore(leftSide);
            if (shore < 0)
                continue;
            var left = leftSide ? shore + ShoreGap : shore - ShoreGap - width;
            if (Math.Abs(left + width / 2 - Main.dungeonX) < 120 || left < 60 || left + width > Main.maxTilesX - 60)
                continue;

            var ground = GroundLevel(left, width);
            if (ground < 0)
                continue;
            Build(left, ground - GroundRow);
            GenVars.structures?.AddProtectedStructure(new Rectangle(left, ground - GroundRow, width, Height), 4);
            Terramon.Instance.Logger.Info($"Built Misty's Gym at ({left}, {ground - GroundRow}).");
            return true;
        }

        return false;
    }

    /// <summary>
    ///     The first column, coming in from the edge of the world, whose surface is dry ground instead of sea.
    /// </summary>
    private static int FindShore(bool leftSide)
    {
        for (var step = 60; step < Main.maxTilesX / 3; step++)
        {
            var x = leftSide ? step : Main.maxTilesX - 1 - step;
            var y = SurfaceY(x);
            if (y < 0)
                continue;
            if (Main.tile[x, y - 1].LiquidAmount == 0 && Main.tileSolid[Main.tile[x, y].TileType])
                return x;
        }

        return -1;
    }

    /// <summary>
    ///     The first row from the top with a block or liquid, or -1.
    /// </summary>
    private static int SurfaceY(int x)
    {
        for (var y = 40; y < Main.worldSurface + 50 && y < Main.maxTilesY - 10; y++)
        {
            var tile = Main.tile[x, y];
            if (tile.LiquidAmount > 0 || (tile.HasTile && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType]
                                          && tile.TileType is not (TileID.Trees or TileID.PalmTree or TileID.Cactus)))
                return y;
        }

        return -1;
    }

    /// <summary>
    ///     The usual ground row under where the gym goes (the median, so a dip or a hill doesn't move it much).
    /// </summary>
    private static int GroundLevel(int left, int width)
    {
        var rows = new List<int>();
        for (var x = left; x < left + width; x += 2)
        {
            var y = SurfaceY(x);
            if (y > 0)
                rows.Add(y);
        }

        if (rows.Count == 0)
            return -1;
        rows.Sort();
        return rows[rows.Count / 2];
    }

    /// <summary>
    ///     Builds the gym with its top-left corner at (left, top): clears the space above it, builds the blueprint,
    ///     fills the ground under it with sand, fills the aquarium with water, plants two palm trees and protects it.
    /// </summary>
    public static void Build(int left, int top)
    {
        var blueprint = Blueprint.Load(Path);
        var bypass = ProtectedAreas.Bypass;
        ProtectedAreas.Bypass = true;
        try
        {
            // Trees and hills over the gym
            for (var x = left - 2; x < left + blueprint.Width + 2; x++)
            {
                for (var y = Math.Max(10, top - 30); y < top; y++)
                {
                    var tile = Main.tile[x, y];
                    tile.ClearTile();
                    tile.LiquidAmount = 0;
                }
            }

            var failed = blueprint.Build(left, top);
            if (failed > 0)
                Terramon.Instance.Logger.Warn($"Misty's Gym: {failed} pieces of furniture didn't fit.");

            // No floating gym: sand down to the ground under it
            for (var x = left; x < left + blueprint.Width; x++)
            {
                for (var y = top + blueprint.Height; y < top + blueprint.Height + 40 && y < Main.maxTilesY - 10; y++)
                {
                    var tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                        break;
                    tile.ClearTile();
                    tile.HasTile = true;
                    tile.TileType = TileID.Sand;
                    tile.LiquidAmount = 0;
                }
            }

            for (var x = left + Aquarium.Left; x < left + Aquarium.Right; x++)
            {
                for (var y = top + Aquarium.Top; y < top + Aquarium.Bottom; y++)
                {
                    var tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                        continue;
                    tile.LiquidType = LiquidID.Water;
                    tile.LiquidAmount = 255;
                }
            }

            PlantPalm(left - 3, top + GroundRow);
            PlantPalm(left + blueprint.Width + 2, top + GroundRow);
            WorldGen.RangeFrame(left - 2, top - 30, left + blueprint.Width + 2, top + blueprint.Height + 40);
        }
        finally
        {
            ProtectedAreas.Bypass = bypass;
        }

        MistyGymSystem.Interior = new Rectangle(left + Hall.X, top + Hall.Y, Hall.Width, Hall.Height);
        MistyGymSystem.LeaderSpot = new Point(left + LeaderColumn, top + FloorRow);
        ProtectedAreas.Add(new Rectangle(left, top, blueprint.Width, blueprint.Height));
    }

    /// <summary>
    ///     A palm tree on the beach next to the gym, if it grows there (outside the protected area, so players can cut it).
    /// </summary>
    private static void PlantPalm(int x, int groundY)
    {
        try
        {
            var ground = groundY - 3;
            while (ground < groundY + 8 && !(Main.tile[x, ground].HasTile && Main.tileSolid[Main.tile[x, ground].TileType]))
                ground++;
            if (Main.tile[x, ground].TileType != TileID.Sand || Main.tile[x, ground - 1].LiquidAmount > 0)
                return;
            WorldGen.PlaceTile(x, ground - 1, TileID.Saplings, true);
            WorldGen.GrowPalmTree(x, ground - 1);
        }
        catch (Exception e)
        {
            Terramon.Instance.Logger.Debug($"Misty's Gym: no palm tree at {x}: {e.Message}");
        }
    }
}
