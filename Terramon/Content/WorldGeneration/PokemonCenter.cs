using Terramon.Content.NPCs.Town;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The Pokémon Center next to spawn, built from a Blueprint Builder design: two floors under a red roof with a giant
///     Poké Ball, Nurse Joy healing Pokémon for free at the counter downstairs, Chansey and Blissey helping out, and a
///     room of PCs upstairs. Its surroundings are a safe zone where wild Pokémon stay weak (see
///     <see cref="Core.Systems.WildPokemonLevels" />).
/// </summary>
public class PokemonCenterSystem : ModSystem
{
    /// <summary>
    ///     The inside of the Center in tile coordinates, or <see cref="Rectangle.Empty" /> if this world has none.
    /// </summary>
    public static Rectangle Interior { get; internal set; }

    /// <summary>
    ///     Where Nurse Joy stands: the floor tile under her feet.
    /// </summary>
    public static Point JoySpot { get; internal set; }

    public static bool Exists => Interior != Rectangle.Empty;

    private static int _checkTimer;

    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        // Right after the Gym and League, so it doesn't overlap them, before trees and grass decorate the surface
        var index = tasks.FindIndex(pass => pass.Name.Equals("Terramon Gyms"));
        if (index < 0)
            index = tasks.FindIndex(pass => pass.Name.Equals("Spawn Point"));
        if (index < 0)
            index = tasks.Count - 1;
        tasks.Insert(index + 1, new PokemonCenterPass("Terramon Pokemon Center", 5f));
        totalWeight += 5f;
    }

    public override void ClearWorld()
    {
        Interior = Rectangle.Empty;
        JoySpot = Point.Zero;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (!Exists)
            return;
        tag["pokemonCenter"] = new[] { Interior.X, Interior.Y, Interior.Width, Interior.Height, JoySpot.X, JoySpot.Y };
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.TryGet<int[]>("pokemonCenter", out var data) && data.Length >= 6)
        {
            Interior = new Rectangle(data[0], data[1], data[2], data[3]);
            JoySpot = new Point(data[4], data[5]);
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Interior.X);
        writer.Write(Interior.Y);
        writer.Write(Interior.Width);
        writer.Write(Interior.Height);
        writer.Write(JoySpot.X);
        writer.Write(JoySpot.Y);
    }

    public override void NetReceive(BinaryReader reader)
    {
        Interior = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        JoySpot = new Point(reader.ReadInt32(), reader.ReadInt32());
    }

    /// <summary>
    ///     Whether this tile is inside the Center (where a PC can always be used).
    /// </summary>
    public static bool Contains(int i, int j) => Exists && Interior.Contains(i, j);

    public override void PostUpdateWorld()
    {
        // Runs on the server or in singleplayer: keeps Joy, Chansey and Blissey at the Center while someone is around
        if (!Exists || ++_checkTimer < 120)
            return;
        _checkTimer = 0;

        var center = Interior.Center.ToWorldCoordinates();
        var someoneNear = false;
        foreach (var player in Main.ActivePlayers)
        {
            if (player.DistanceSQ(center) < 200 * 16 * 200 * 16)
            {
                someoneNear = true;
                break;
            }
        }

        if (!someoneNear)
            return;

        var floor = Interior.Bottom * 16;
        Spawn(ModContent.NPCType<NurseJoy>(), JoySpot.X * 16 + 8, floor);
        Spawn(ModContent.NPCType<CenterChansey>(), (Interior.X + 6) * 16, floor);
        Spawn(ModContent.NPCType<CenterBlissey>(), (Interior.Right - 9) * 16, floor);
    }

    private static void Spawn(int type, int x, int y)
    {
        if (NPC.AnyNPCs(type))
            return;
        var index = NPC.NewNPC(new EntitySource_WorldEvent(), x, y, type);
        if (index < Main.maxNPCs && Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);
    }
}

/// <summary>
///     Plays the Pokémon Center music inside the Center.
/// </summary>
public class PokemonCenterScene : ModSceneEffect
{
    public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/PokeCenter");

    public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

    public override bool IsSceneEffectActive(Player player)
    {
        var tile = player.Center.ToTileCoordinates();
        return PokemonCenterSystem.Contains(tile.X, tile.Y);
    }
}

/// <summary>
///     Shows a Poké Ball on the map over the Pokémon Center.
/// </summary>
public class PokemonCenterMapLayer : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!PokemonCenterSystem.Exists)
            return;

        var icon = ModContent.Request<Texture2D>("Terramon/Assets/Items/PokeBalls/PokeBallItem").Value;
        if (context.Draw(icon, PokemonCenterSystem.Interior.Center.ToVector2(), Color.White,
                new SpriteFrame(1, 1, 0, 0), 0.9f, 1.4f, Alignment.Center).IsMouseOver)
            text = Language.GetTextValue("Mods.Terramon.PokemonCenter.MapName");
    }
}

/// <summary>
///     Tells each player once per world where the Pokémon Center is.
/// </summary>
public class PokemonCenterPlayer : ModPlayer
{
    private readonly HashSet<string> _toldWorlds = [];

    public override void OnEnterWorld()
    {
        var world = Main.ActiveWorldFileData?.UniqueId.ToString();
        if (!PokemonCenterSystem.Exists || string.IsNullOrEmpty(world) || !_toldWorlds.Add(world))
            return;

        var offset = PokemonCenterSystem.Interior.Center.X - Main.spawnTileX;
        var direction = Language.GetTextValue("Mods.Terramon.Pedestals." + (offset < 0 ? "West" : "East"));
        Main.NewText(Language.GetTextValue("Mods.Terramon.PokemonCenter.Announce", Math.Abs(offset), direction),
            new Color(255, 130, 150));
    }

    public override void SaveData(TagCompound tag)
    {
        if (_toldWorlds.Count > 0)
            tag["pokemonCenterTold"] = _toldWorlds.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        _toldWorlds.Clear();
        foreach (var world in tag.GetList<string>("pokemonCenterTold"))
            _toldWorlds.Add(world);
    }
}

/// <summary>
///     Builds the Pokémon Center on flat ground a short walk from spawn.
/// </summary>
public class PokemonCenterPass(string name, float loadWeight) : GenPass(name, loadWeight)
{
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = Language.GetTextValue("Mods.Terramon.WorldGen.PokemonCenterPass");
        PokemonCenterSystem.Interior = Rectangle.Empty;

        try
        {
            if (!PokemonCenterBuilder.TryPlaceNearSpawn())
                Terramon.Instance.Logger.Warn("Couldn't find a place for the Pokémon Center, skipping it.");
        }
        catch (Exception e)
        {
            // A missing Center is better than a world that can't be generated
            Terramon.Instance.Logger.Error("Building the Pokémon Center failed", e);
        }

        progress.Set(1);
    }
}

public static class PokemonCenterBuilder
{
    private const string Path = "Assets/Structures/PokemonCenter.json";

    public const int Width = 96;
    public const int Height = 52; // The whole design, from the Poké Ball on the roof down to the grass

    // Rows and columns in the design (tools/structures/pokemon_center.py)
    private const int GroundRow = 51; // The grass outside, level with the ground around it
    private const int FloorRow = 50;  // First floor
    private const int JoyColumn = 47; // The gap in the counter
    private static readonly Rectangle Inside = new(15, 29, 66, 21); // Both floors, between the outer walls

    /// <summary>
    ///     Tries flat ground close to spawn first, then farther, then rougher ground (it gets leveled).
    /// </summary>
    public static bool TryPlaceNearSpawn()
    {
        Rejections.Clear();
        foreach (var (min, max, bumps) in new[] { (15, 120, 6), (15, 250, 12), (15, 500, 20), (15, 800, 30) })
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                var offset = WorldGen.genRand.Next(min, max);
                var left = Main.spawnTileX + (WorldGen.genRand.NextBool() ? offset : -offset - Width);
                if (!TryFindGround(left, bumps, out var groundY))
                    continue;

                Build(left, groundY);
                GenVars.structures?.AddProtectedStructure(
                    new Rectangle(left - 2, groundY - Height, Width + 4, Height + 6), 4);
                Terramon.Instance.Logger.Info($"Built the Pokémon Center at ({left}, {groundY}).");
                return true;
            }
        }

        // Nothing passed every check: take the least bumpy stretch of surface near spawn anyway, since a world
        // without a Center is worse than one with a few leveled hills
        Terramon.Instance.Logger.Info("Pokémon Center: no ideal place (" +
                                      string.Join(", ", Rejections.Select(r => $"{r.Key} {r.Value}")) +
                                      "), using the flattest ground near spawn.");
        var best = -1;
        var bestBumps = int.MaxValue;
        var bestGround = 0;
        for (var left = Main.spawnTileX - 600 - Width; left <= Main.spawnTileX + 600; left += 3)
        {
            if (left < 350 || left + Width > Main.maxTilesX - 350 || Math.Abs(left + Width / 2 - Main.dungeonX) < 150)
                continue;
            var surface = new List<int>();
            for (var x = left; x < left + Width; x++)
                surface.Add(Surface(x, true));
            if (surface.Contains(-1))
                continue;
            var bumps = surface.Max() - surface.Min() + Math.Abs(left + Width / 2 - Main.spawnTileX) / 100;
            if (bumps >= bestBumps)
                continue;
            surface.Sort();
            // Never on top of the Gyms or anything else another pass protected
            var area = new Rectangle(left - 2, surface[surface.Count / 2] - Height, Width + 4, Height + 6);
            if (GenVars.structures != null && !GenVars.structures.CanPlace(area))
                continue;
            best = left;
            bestBumps = bumps;
            bestGround = surface[surface.Count / 2];
        }

        if (best < 0)
            return false;
        Build(best, bestGround);
        GenVars.structures?.AddProtectedStructure(new Rectangle(best - 2, bestGround - Height, Width + 4, Height + 6), 4);
        Terramon.Instance.Logger.Info($"Built the Pokémon Center at ({best}, {bestGround}).");
        return true;
    }

    private static readonly Dictionary<string, int> Rejections = [];

    private static bool Reject(string reason)
    {
        Rejections[reason] = Rejections.GetValueOrDefault(reason) + 1;
        return false;
    }

    /// <summary>
    ///     Whether the Center fits with its left edge here: surface ground (not ocean, Dungeon or water) that goes up
    ///     and down by at most <paramref name="bumps" /> tiles, with nothing built in the way. The Center is too wide to
    ///     wait for flat ground, so the ground is leveled when it's built.
    /// </summary>
    private static bool TryFindGround(int left, int bumps, out int groundY)
    {
        groundY = 0;
        var right = left + Width - 1;
        if (left < 350 || right > Main.maxTilesX - 350 || Math.Abs(left + Width / 2 - Main.dungeonX) < 150)
            return Reject("edge or Dungeon");

        var surface = new List<int>();
        for (var x = left; x <= right; x++)
        {
            var y = Surface(x);
            if (y < 0)
                return Reject("no surface");
            surface.Add(y);
        }

        if (surface.Max() - surface.Min() > bumps)
            return Reject("bumpy");
        surface.Sort();
        groundY = surface[surface.Count / 2];

        var area = new Rectangle(left - 2, groundY - Height, Width + 4, Height + 6);
        if (GenVars.structures != null && !GenVars.structures.CanPlace(area))
            return Reject("structure");

        for (var x = left - 2; x <= right + 2; x++)
        {
            for (var y = groundY - Height; y <= groundY + 4; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.LiquidAmount > 0 && y <= groundY)
                    return Reject("water");
                if (tile.HasTile && !GymBuildingsPass.IsNaturalGround(tile.TileType) && !IsPlant(tile.TileType))
                    return Reject("tile " + TileID.Search.GetName(tile.TileType));
            }
        }

        return true;
    }

    private static bool IsPlant(ushort type) =>
        type is TileID.Trees or TileID.PalmTree or TileID.Cactus or TileID.Sunflower or TileID.Vines
            or TileID.Plants or TileID.Plants2 or TileID.JunglePlants or TileID.Saplings;

    // The first solid ground from the top of the world, or -1 if there's none or it's a floating island.
    // With anyBlock, floating islands are looked past and any block counts as ground.
    private static int Surface(int x, bool anyBlock = false)
    {
        for (var y = 20; y < Main.worldSurface; y++)
        {
            var tile = Main.tile[x, y];
            if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                continue;
            if (anyBlock)
            {
                if (y < GenVars.worldSurfaceLow - 5)
                    continue;
                return y;
            }

            return GymBuildingsPass.IsNaturalGround(tile.TileType) && y >= GenVars.worldSurfaceLow - 5 ? y : -1;
        }

        return -1;
    }

    /// <summary>
    ///     Builds the Center with the left edge of its design at <paramref name="left" /> and its grass on row
    ///     <paramref name="groundY" />, and remembers it. The ground under it is filled in so it never floats.
    /// </summary>
    public static void Build(int left, int groundY)
    {
        var bypass = ProtectedAreas.Bypass;
        ProtectedAreas.Bypass = true;
        try
        {
            BuildFromBlueprint(left, groundY - GroundRow);
        }
        finally
        {
            ProtectedAreas.Bypass = bypass;
        }
    }

    private static void BuildFromBlueprint(int left, int top)
    {
        var blueprint = Blueprint.Load(Path);

        // Trees over it
        for (var x = left - 2; x < left + Width + 2; x++)
        {
            for (var y = Math.Max(10, top - 20); y < top; y++)
            {
                var tile = Main.tile[x, y];
                tile.ClearTile();
                tile.LiquidAmount = 0;
            }
        }

        // Known before the PCs go in, since a PC can always be placed inside the Center
        PokemonCenterSystem.Interior = new Rectangle(left + Inside.X, top + Inside.Y, Inside.Width, Inside.Height);
        PokemonCenterSystem.JoySpot = new Point(left + JoyColumn, top + FloorRow);

        var failed = blueprint.Build(left, top);
        if (failed > 0)
            Terramon.Instance.Logger.Warn($"Pokémon Center: {failed} pieces of furniture didn't fit.");

        // No floating Center: dirt down to the ground under it
        var deepest = top + Height;
        for (var x = left; x < left + Width; x++)
        {
            for (var y = top + Height; y < top + Height + 40 && y < Main.maxTilesY - 10; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.HasTile && Main.tileSolid[tile.TileType])
                    break;
                tile.ClearTile();
                tile.HasTile = true;
                tile.TileType = TileID.Dirt;
                tile.LiquidAmount = 0;
                deepest = Math.Max(deepest, y);
            }
        }

        // Ground beside it eases into the Center's level instead of leaving a cliff or a pit
        var groundY = top + GroundRow;
        for (var d = 1; d <= 14; d++)
        {
            foreach (var x in new[] { left - d, left + Width - 1 + d })
            {
                for (var y = groundY - 40; y < groundY - d; y++)
                {
                    var tile = Main.tile[x, y];
                    if (tile.HasTile && (GymBuildingsPass.IsNaturalGround(tile.TileType) || IsPlant(tile.TileType)))
                        tile.ClearTile();
                }

                for (var y = groundY + d - 1; y < groundY + 40; y++)
                {
                    var tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                        break;
                    tile.ClearTile();
                    tile.HasTile = true;
                    tile.TileType = TileID.Dirt;
                    tile.LiquidAmount = 0;
                    deepest = Math.Max(deepest, y);
                }
            }
        }

        WorldGen.RangeFrame(left - 16, top - 40, left + Width + 16, deepest + 1);
        ProtectedAreas.Add(new Rectangle(left, top, Width, Height));
    }
}
