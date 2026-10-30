using Newtonsoft.Json.Linq;
using Terraria.ObjectData;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     A building saved in the JSON format of the Blueprint Builder mod (formatVersion 1), so buildings can be designed
///     in game with it and then built by this mod in new worlds, without needing it installed.
///     <para />
///     <c>cells</c> are blocks and walls by the name of the item that places them (<c>tile</c>, <c>wall</c>), with
///     <c>tilePaint</c>, <c>wallPaint</c>, <c>slope</c>, <c>halfBrick</c> and <c>wires</c>. <c>objects</c> are
///     furniture by item name, at their top-left tile, with <c>facingRight</c>. Names this version of Terraria doesn't
///     know are skipped and logged.
/// </summary>
public sealed class Blueprint
{
    private readonly record struct Cell(int X, int Y, int Tile, int Wall, byte TilePaint, byte WallPaint, int Slope,
        bool HalfBrick, int Wires);

    private readonly record struct Furniture(int X, int Y, int Tile, int Style, bool FacingRight, string Name);

    private static readonly Dictionary<string, Blueprint> Cache = [];

    public int Width { get; private init; }
    public int Height { get; private init; }
    private Cell[] _cells = [];
    private Furniture[] _objects = [];

    /// <summary>
    ///     Reads a blueprint from the mod's files, e.g. <c>Assets/Structures/MistyGym.json</c>.
    /// </summary>
    public static Blueprint Load(string path)
    {
        if (Cache.TryGetValue(path, out var cached))
            return cached;

        var json = JObject.Parse(System.Text.Encoding.UTF8.GetString(Terramon.Instance.GetFileBytes(path)));
        var missing = new SortedSet<string>();
        var blueprint = new Blueprint
        {
            Width = (int)json["width"],
            Height = (int)json["height"]
        };

        var cells = new List<Cell>();
        foreach (var c in json["cells"] ?? new JArray())
        {
            var tile = ItemOf((string)c["tile"], missing)?.createTile ?? -1;
            var wall = ItemOf((string)c["wall"], missing)?.createWall ?? -1;
            if (tile < 0 && wall <= 0)
                continue;
            cells.Add(new Cell((int)c["x"], (int)c["y"], tile, wall, (byte)((int?)c["tilePaint"] ?? 0),
                (byte)((int?)c["wallPaint"] ?? 0), (int?)c["slope"] ?? 0, (bool?)c["halfBrick"] ?? false,
                (int?)c["wires"] ?? 0));
        }

        var objects = new List<Furniture>();
        foreach (var o in json["objects"] ?? new JArray())
        {
            var name = (string)o["item"];
            if (ItemOf(name, missing) is not { createTile: >= 0 } item)
                continue;
            objects.Add(new Furniture((int)o["x"], (int)o["y"], item.createTile, item.placeStyle,
                (bool?)o["facingRight"] ?? false, name));
        }

        blueprint._cells = cells.ToArray();
        blueprint._objects = objects.ToArray();
        if (missing.Count > 0)
            Terramon.Instance.Logger.Warn($"{path}: unknown items skipped: {string.Join(", ", missing)}");
        Cache[path] = blueprint;
        return blueprint;
    }

    private static Item ItemOf(string name, ISet<string> missing)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        if (ItemID.Search.TryGetId(name, out var id))
            return ContentSamples.ItemsByType[id];
        missing.Add(name);
        return null;
    }

    /// <summary>
    ///     Builds it with its top-left corner at (left, top). Everything inside its rectangle is replaced: blocks,
    ///     walls and liquid where the blueprint has nothing are cleared. Returns how many objects didn't fit.
    /// </summary>
    public int Build(int left, int top)
    {
        for (var x = left; x < left + Width; x++)
        {
            for (var y = top; y < top + Height; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearEverything();
            }
        }

        foreach (var c in _cells)
        {
            var tile = Main.tile[left + c.X, top + c.Y];
            if (c.Tile >= 0)
            {
                tile.HasTile = true;
                tile.TileType = (ushort)c.Tile;
                tile.TileColor = c.TilePaint;
                tile.IsHalfBlock = c.HalfBrick;
                tile.Slope = c.HalfBrick ? SlopeType.Solid : (SlopeType)Math.Clamp(c.Slope, 0, 4);
            }

            if (c.Wall > 0)
            {
                tile.WallType = (ushort)c.Wall;
                tile.WallColor = c.WallPaint;
            }

            tile.RedWire = (c.Wires & 1) != 0;
            tile.BlueWire = (c.Wires & 2) != 0;
            tile.GreenWire = (c.Wires & 4) != 0;
            tile.YellowWire = (c.Wires & 8) != 0;
        }

        WorldGen.RangeFrame(left, top, left + Width, top + Height);

        var failed = 0;
        foreach (var o in _objects)
        {
            var x = left + o.X;
            var y = top + o.Y;
            var data = TileObjectData.GetTileData(o.Tile, o.Style);
            var origin = data?.Origin ?? Terraria.DataStructures.Point16.Zero;
            var direction = o.FacingRight ? 1 : -1;
            if (WorldGen.PlaceObject(x + origin.X, y + origin.Y, o.Tile, true, o.Style, 0, -1, direction))
                continue;
            WorldGen.PlaceTile(x + origin.X, y + origin.Y, o.Tile, true, true, -1, o.Style);
            if (Main.tile[x + origin.X, y + origin.Y].HasTile && Main.tile[x + origin.X, y + origin.Y].TileType == o.Tile)
                continue;
            failed++;
            Terramon.Instance.Logger.Debug($"Blueprint: couldn't place {o.Name} at ({o.X}, {o.Y})");
        }

        return failed;
    }
}
