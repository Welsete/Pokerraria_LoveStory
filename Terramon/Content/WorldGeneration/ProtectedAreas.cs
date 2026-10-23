using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     Areas of the world nobody can change: the Gym and League buildings and Brock's Gym (with its tunnel, rope shaft
///     and sign). Their blocks, walls and furniture can't be mined, hammered, blown up, replaced or built over; doors and
///     signs still work. Each building adds its rectangle when it's built and the list is saved with the world. Worlds
///     made before this get the rectangles of the buildings they already have the first time they're loaded.
/// </summary>
public class ProtectedAreas : ModSystem
{
    private const int MaxAreas = 64;
    private static readonly List<Rectangle> Areas = [];

    // Whether this world's list still has to be made from the buildings it already has (worlds from before this)
    private static bool _needsOldBuildings;
    private static uint _lastMessage;

    /// <summary>
    ///     While true, nothing is protected: for the mod's own builders changing tiles inside an area (like rebuilding
    ///     Brock's Gym from the dev panel).
    /// </summary>
    public static bool Bypass { get; set; }

    /// <summary>
    ///     Protects a rectangle of tiles from now on. Rectangles already protected aren't added twice.
    /// </summary>
    public static void Add(Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0 || Areas.Contains(area) || Areas.Count >= MaxAreas)
            return;
        Areas.Add(area);
    }

    /// <summary>
    ///     Whether the tile at (i, j) is in a protected area and can't be changed right now.
    /// </summary>
    public static bool IsProtected(int i, int j)
    {
        if (Bypass || WorldGen.generatingWorld || Areas.Count == 0)
            return false;
        foreach (var area in Areas)
        {
            if (area.Contains(i, j))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Tells the local player (at most every few seconds) that the building they're working on can't be changed,
    ///     when it's them hitting or placing at that spot.
    /// </summary>
    internal static void Notify(int i, int j)
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        var player = Main.LocalPlayer;
        if (player == null || !player.active || (!player.controlUseItem && player.itemAnimation <= 0))
            return;
        if (Math.Abs(Player.tileTargetX - i) > 3 || Math.Abs(Player.tileTargetY - j) > 3)
            return;
        if (Main.GameUpdateCount - _lastMessage < 180)
            return;
        _lastMessage = Main.GameUpdateCount;
        Main.NewText(Language.GetTextValue("Mods.Terramon.ProtectedAreas.Blocked"), new Color(255, 190, 90));
    }

    public override void ClearWorld()
    {
        Areas.Clear();
        _needsOldBuildings = false;
        Bypass = false;
        BrockGymBuilder.NeedsArchRemoved = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        // Always saved (even empty), so a loaded world knows it doesn't need its old buildings looked up
        tag["protectedAreas"] = Areas.SelectMany(area => new[] { area.X, area.Y, area.Width, area.Height }).ToArray();
        tag["protectedBrockArch"] = true;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        Areas.Clear();
        if (tag.TryGet<int[]>("protectedAreas", out var data))
        {
            for (var k = 0; k + 3 < data.Length; k += 4)
                Add(new Rectangle(data[k], data[k + 1], data[k + 2], data[k + 3]));
            _needsOldBuildings = false;
        }
        else
            _needsOldBuildings = true;

        BrockGymBuilder.NeedsArchRemoved = !tag.ContainsKey("protectedBrockArch");
    }

    public override void PreUpdateWorld()
    {
        // Runs on the server or in singleplayer, once the other systems have loaded where their buildings are
        if (BrockGymBuilder.NeedsArchRemoved)
        {
            BrockGymBuilder.NeedsArchRemoved = false;
            BrockGymBuilder.RemoveOldArch();
        }

        if (!_needsOldBuildings)
            return;
        _needsOldBuildings = false;

        GymBuildingsPass.ProtectExisting();
        BrockGymBuilder.ProtectExisting();
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.WorldData);
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)Areas.Count);
        foreach (var area in Areas)
        {
            writer.Write(area.X);
            writer.Write(area.Y);
            writer.Write(area.Width);
            writer.Write(area.Height);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        Areas.Clear();
        int count = reader.ReadByte();
        for (var k = 0; k < count; k++)
            Add(new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
    }
}

/// <summary>
///     Keeps the blocks and furniture of <see cref="ProtectedAreas" /> from being mined, hammered, blown up, replaced or
///     built over.
/// </summary>
public class ProtectedAreasGlobalTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
    {
        if (!ProtectedAreas.IsProtected(i, j))
            return true;
        ProtectedAreas.Notify(i, j);
        return false;
    }

    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        // Anything that kills tiles without asking CanKillTile first (cutting with a sword, other mods...)
        if (!fail && ProtectedAreas.IsProtected(i, j))
            fail = true;
    }

    public override bool CanExplode(int i, int j, int type) => !ProtectedAreas.IsProtected(i, j);

    public override bool CanPlace(int i, int j, int type)
    {
        if (!ProtectedAreas.IsProtected(i, j))
            return true;
        ProtectedAreas.Notify(i, j);
        return false;
    }

    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
        => !ProtectedAreas.IsProtected(i, j);

    public override bool Slope(int i, int j, int type)
    {
        if (!ProtectedAreas.IsProtected(i, j))
            return true;
        ProtectedAreas.Notify(i, j);
        return false;
    }
}

/// <summary>
///     Keeps the walls of <see cref="ProtectedAreas" /> from being hammered, blown up or covered with other walls.
/// </summary>
public class ProtectedAreasGlobalWall : GlobalWall
{
    public override void KillWall(int i, int j, int type, ref bool fail)
    {
        if (fail || !ProtectedAreas.IsProtected(i, j))
            return;
        fail = true;
        ProtectedAreas.Notify(i, j);
    }

    public override bool CanExplode(int i, int j, int type) => !ProtectedAreas.IsProtected(i, j);

    public override bool CanPlace(int i, int j, int type)
    {
        if (!ProtectedAreas.IsProtected(i, j))
            return true;
        ProtectedAreas.Notify(i, j);
        return false;
    }
}
