using Pokerraria.Content.Configs;
using Pokerraria.Content.WorldGeneration;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.UI;

namespace Pokerraria.Content.GUI;

/// <summary>
///     Test panel map tools: with "Teleport on map" on, a right click on the full screen map teleports the player
///     there. Only works in Debug Mode.
/// </summary>
public sealed class AdminMapSystem : ModSystem
{
    public static bool TeleportOnMap;

    public override void ClearWorld() => TeleportOnMap = false;

    public override void PostDrawFullscreenMap(ref string mouseText)
    {
        if (!TeleportOnMap || !GameplayConfig.Instance.DebugMode)
            return;

        // The full screen map is centered on mapFullscreenPos (in tiles), scaled by mapFullscreenScale
        var screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
        var tile = (new Vector2(Main.mouseX, Main.mouseY) - screenCenter) / Main.mapFullscreenScale +
                   Main.mapFullscreenPos;
        var x = (int)tile.X;
        var y = (int)tile.Y;
        if (!WorldGen.InWorld(x, y, 10))
            return;

        mouseText = Language.GetTextValue("Mods.Pokerraria.GUI.AdminPanel.MapTeleportHint");
        if (!Main.mouseRight || !Main.mouseRightRelease)
            return;

        Main.mouseRightRelease = false;
        Main.mapFullscreen = false;
        AdminPanelUI.TeleportPlayerTo(SafeSpot(x, y).ToWorldCoordinates(8f, 0f));
    }

    /// <summary>
    ///     The clicked tile, or the first spot above it with room for the player (three tiles of air), so a click on
    ///     solid ground doesn't bury them.
    /// </summary>
    private static Point SafeSpot(int x, int y)
    {
        for (var top = y; top > 20; top--)
        {
            if (!Blocked(x, top) && !Blocked(x, top - 1) && !Blocked(x, top - 2))
                return new Point(x, top + 1);
        }

        return new Point(x, y);
    }

    private static bool Blocked(int x, int y)
    {
        var tile = Main.tile[x, y];
        return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
    }
}

/// <summary>
///     In Debug Mode the map also marks the Gym and League pedestals, which have no icon of their own.
/// </summary>
public sealed class AdminMapLayer : ModMapLayer
{
    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (!GameplayConfig.Instance.DebugMode)
            return;
        Mark(ref context, ref text, GymBuildingsSystem.GymPosition, ItemID.GoldCrown, "DebugGymPedestal");
        Mark(ref context, ref text, GymBuildingsSystem.LeaguePosition, ItemID.PlatinumCrown, "DebugLeaguePedestal");
    }

    private static void Mark(ref MapOverlayDrawContext context, ref string text, Point position, int item, string key)
    {
        if (position == Point.Zero)
            return;
        Main.instance.LoadItem(item);
        var icon = TextureAssets.Item[item].Value;
        if (context.Draw(icon, position.ToVector2() + new Vector2(1f, -1f), Color.White, new SpriteFrame(1, 1, 0, 0),
                0.9f, 1.3f, Alignment.Center).IsMouseOver)
            text = Language.GetTextValue("Mods.Pokerraria.GUI.AdminPanel." + key);
    }
}
