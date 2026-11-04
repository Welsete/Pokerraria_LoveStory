using Terramon.Content.Buffs;

namespace Terramon.Content.Items;

/// <summary>
///     Repel: a spray that keeps wild Pokémon away for a while (other creatures still come).
/// </summary>
public class Repel : TerramonItem
{
    public const int Duration = 60 * 60 * 3; // 3 minutes

    public override string Texture => "Terramon/Assets/Items/Miscellaneous/Repel";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 20;
        Item.height = 28;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 30;
        Item.consumable = true;
        Item.UseSound = SoundID.Item13;
        Item.buffType = ModContent.BuffType<RepelBuff>();
        Item.buffTime = Duration;
        Item.value = Item.buyPrice(silver: 15);
    }

    public override void AddRecipes()
    {
        CreateRecipe(2)
            .AddIngredient(ItemID.Bottle)
            .AddIngredient(ItemID.Deathweed)
            .AddIngredient(ItemID.Gel, 2)
            .AddTile(TileID.Bottles)
            .Register();
    }
}
