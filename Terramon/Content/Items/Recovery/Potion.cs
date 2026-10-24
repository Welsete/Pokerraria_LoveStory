using Terramon.Helpers;

namespace Terramon.Content.Items;

public class Potion : BasePotionItem
{
    protected override ushort HealAmount => 20;

    protected override int BuyPrice => Item.buyPrice(silver: 10);

    protected override int UseRarity => ModContent.RarityType<PotionRarity>();

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        Item.ResearchUnlockCount = 20;
    }

    public override void AddRecipes()
    {
        // A little cheaper than a Lesser Healing Potion, so early trainers can keep a few around
        CreateRecipe(2)
            .AddIngredient(ItemID.Mushroom)
            .AddIngredient(ItemID.Gel, 2)
            .AddIngredient(ItemID.Bottle)
            .AddTile(TileID.Bottles)
            .Register();
    }
}

public class PotionRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0x9A59DB);
}