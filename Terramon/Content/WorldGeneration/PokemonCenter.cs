using Terramon.Content.NPCs.Town;
using Terramon.Content.Tiles.Interactive;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The Pokémon Center next to spawn: a white building with a red roof where Nurse Joy heals Pokémon for free, with
///     Chansey and Blissey helping out and a PC. Its surroundings are a safe zone where wild Pokémon stay weak (see
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
    public const int Width = 33;
    public const int Height = 12; // From the floor to the roof, both included

    // Columns from the left wall
    private const int JoyColumn = 16;

    /// <summary>
    ///     Tries flat ground close to spawn first, then a little farther.
    /// </summary>
    public static bool TryPlaceNearSpawn()
    {
        foreach (var (min, max) in new[] { (15, 90), (90, 250) })
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                var offset = WorldGen.genRand.Next(min, max);
                var left = Main.spawnTileX + (WorldGen.genRand.NextBool() ? offset : -offset - Width);
                if (!GymBuildingsPass.TryFindFloor(left, Width, out var floorY))
                    continue;

                Build(left, floorY);
                GenVars.structures?.AddProtectedStructure(
                    new Rectangle(left - 2, floorY - Height - 3, Width + 4, Height + 10), 4);
                Terramon.Instance.Logger.Info($"Built the Pokémon Center at ({left}, {floorY}).");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Builds the Center with its left wall at <paramref name="left" /> and its floor on row
    ///     <paramref name="floorY" />, and remembers it. The ground under it is filled in so it never floats.
    /// </summary>
    public static void Build(int left, int floorY)
    {
        var bypass = ProtectedAreas.Bypass;
        ProtectedAreas.Bypass = true;
        try
        {
            BuildShell(left, floorY);
        }
        finally
        {
            ProtectedAreas.Bypass = bypass;
        }
    }

    private static void BuildShell(int left, int floorY)
    {
        var right = left + Width - 1;
        var top = floorY - Height + 1;

        // Clear the inside and a little room around the roof
        for (var x = left - 2; x <= right + 2; x++)
        {
            for (var y = top - 4; y < floorY; y++)
                Main.tile[x, y].ClearEverything();
        }

        // Floor, with a foundation down to the ground
        var deepest = floorY;
        for (var x = left - 1; x <= right + 1; x++)
        {
            for (var y = floorY; y <= floorY + 25; y++)
            {
                var below = Main.tile[x, y];
                if (y > floorY && below.HasTile && Main.tileSolid[below.TileType])
                    break;
                Block(x, y, y == floorY ? TileID.MarbleBlock : TileID.Stone);
                deepest = Math.Max(deepest, y);
            }
        }

        // White walls, a red stripe and windows on the back wall
        for (var y = top + 1; y < floorY; y++)
        {
            Block(left, y, TileID.MarbleBlock);
            Block(right, y, TileID.MarbleBlock);
            for (var x = left + 1; x < right; x++)
            {
                var window = y >= top + 2 && y <= top + 4 &&
                             (x >= left + 3 && x <= left + 7 || x >= right - 7 && x <= right - 3);
                var wall = window ? WallID.Glass : y == floorY - 5 ? WallID.RedBrick : WallID.MarbleBlock;
                Main.tile[x, y].WallType = wall;
            }
        }

        // A red stepped roof
        for (var x = left - 1; x <= right + 1; x++)
            Block(x, top, TileID.RedBrick);
        for (var x = left + 1; x <= right - 1; x++)
            Block(x, top - 1, TileID.RedBrick);
        for (var x = left + 4; x <= right - 4; x++)
            Block(x, top - 2, TileID.RedBrick);
        // A white band with the Center's sign color in the middle of the roof
        for (var x = left + 12; x <= right - 12; x++)
            Block(x, top - 1, TileID.MarbleBlock);

        // Doors on both sides
        foreach (var x in new[] { left, right })
        {
            for (var y = floorY - 3; y < floorY; y++)
                Main.tile[x, y].ClearTile();
            WorldGen.PlaceTile(x, floorY - 1, TileID.ClosedDoor, true, true);
        }

        // Joy's counter: a table on each side of her
        WorldGen.PlaceObject(left + JoyColumn - 3, floorY - 1, TileID.Tables, true);
        WorldGen.PlaceObject(left + JoyColumn + 3, floorY - 1, TileID.Tables, true);

        // Waiting area: chairs and a bench by the left door, the PC by the right one
        WorldGen.PlaceObject(left + 5, floorY - 1, TileID.Chairs, true, 0, 0, -1, 1);
        WorldGen.PlaceObject(left + 7, floorY - 1, TileID.Benches, true);
        WorldGen.PlaceObject(left + 9, floorY - 1, TileID.Chairs, true, 0, 0, -1, -1);

        // Known before the PC goes in, since a PC can always be placed inside the Center
        PokemonCenterSystem.Interior = new Rectangle(left + 1, top + 1, Width - 2, Height - 2);
        PokemonCenterSystem.JoySpot = new Point(left + JoyColumn, floorY);

        var pcX = right - 6;
        if (WorldGen.PlaceObject(pcX, floorY - 1, ModContent.TileType<PCRed>(), true))
            ModContent.GetInstance<PCTileEntity>().Place(pcX, floorY - 3);

        // Light: torches on the back wall and a chandelier over the counter
        foreach (var x in new[] { left + 2, left + 11, right - 11, right - 2 })
            WorldGen.PlaceTile(x, floorY - 4, TileID.Torches, true, false, -1, 0);
        WorldGen.PlaceObject(left + JoyColumn, top + 1, TileID.Chandeliers, true);

        // The sign by the left door
        if (WorldGen.PlaceObject(left + 2, floorY - 1, TileID.Signs, true))
        {
            var sign = Sign.ReadSign(left + 2, floorY - 2, true);
            if (sign >= 0)
                Sign.TextSign(sign, Language.GetTextValue("Mods.Terramon.PokemonCenter.Sign"));
        }

        WorldGen.RangeFrame(left - 2, top - 4, right + 2, deepest + 1);

        ProtectedAreas.Add(new Rectangle(left - 1, top - 3, Width + 2, Height + 4));
    }

    private static void Block(int x, int y, ushort type)
    {
        var tile = Main.tile[x, y];
        tile.ClearTile();
        tile.HasTile = true;
        tile.TileType = type;
        tile.Slope = SlopeType.Solid;
        tile.IsHalfBlock = false;
        tile.LiquidAmount = 0;
    }
}
