namespace Terramon.Content.Tiles.Paintings;

public class BrockPaintingItem : PaintingItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<BrockPaintingTile>());
        base.SetDefaults();
        Item.width = 32;
        Item.height = 24;
    }
}
