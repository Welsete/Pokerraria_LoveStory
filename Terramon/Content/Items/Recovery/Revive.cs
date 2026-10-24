using Terramon.Helpers;

namespace Terramon.Content.Items;

public class Revive : BaseReviveItem
{
    protected override float RestorationPercentage => 0.5f;

    protected override int BuyPrice => Item.buyPrice(gold: 1);

    protected override int UseRarity => ModContent.RarityType<ReviveRarity>();

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 20;
        Item.height = 30;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.FallenStar)
            .AddIngredient(ItemID.Daybloom, 2)
            .AddIngredient(ItemID.BottledWater)
            .AddTile(TileID.Bottles)
            .Register();

        // A use for spare Life Crystals once the player's own health is maxed out
        CreateRecipe(4)
            .AddIngredient(ItemID.LifeCrystal)
            .AddTile(TileID.Bottles)
            .Register();
    }
}

public class ReviveRarity : ModRarity
{
    public override Color RarityColor { get; } = ColorUtils.FromHexRGB(0xFFCA51);
}