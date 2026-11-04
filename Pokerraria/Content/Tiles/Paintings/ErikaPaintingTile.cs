using Terraria.ObjectData;

namespace Pokerraria.Content.Tiles.Paintings;

public class ErikaPaintingTile : PaintingTile
{
    protected override void SetTileObjectData()
    {
        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.addTile(Type);
    }
}