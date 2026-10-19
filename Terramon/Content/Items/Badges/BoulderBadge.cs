namespace Terramon.Content.Items;

public sealed class BoulderBadge : KeyItem
{
    public override string Texture => "Terramon/Assets/Items/Badges/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 24;
    }
}
