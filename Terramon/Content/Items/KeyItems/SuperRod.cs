using Terramon.Content.Items.PokeBalls;

namespace Terramon.Content.Items;

public sealed class SuperRod : FishingRod
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        CatchTerramonChance = 1f;
        Item.fishingPole = 45;
        Item.shootSpeed = 16f;
    }
    public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
    {
        base.ModifyFishingLine(bobber, ref lineOriginOffset, ref lineColor);
        lineOriginOffset.X += 40f;
        lineOriginOffset.Y -= 34f;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient<GoodRod>()
            .AddIngredient(ItemID.SoulofLight, 5)
            .AddIngredient(ItemID.SoulofNight, 5)
            .AddIngredient<UltraBallItem>()
            .AddTile(TileID.MythrilAnvil)
            .AddCondition(Condition.Hardmode)
            .Register();
    }
}
