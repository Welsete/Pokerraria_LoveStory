using Terraria.DataStructures;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Paintings;

/// <summary>
///     The big painting in Brock's Gym: a Poké Ball next to the Boulder Badge.
/// </summary>
public class BrockPaintingTile : PaintingTile
{
    protected override void SetTileObjectData()
    {
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width = 8;
        TileObjectData.newTile.Height = 5;
        TileObjectData.newTile.Origin = Point16.Zero;
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16];
        TileObjectData.addTile(Type);
    }
}
