namespace Pokerraria.Core.Systems;

public class RecipeSystem : ModSystem
{
    public override void AddRecipeGroups()
    {
        RecipeGroup.RegisterGroup($"{nameof(Pokerraria)}:SilverBar", new RecipeGroup(() => "Any Silver Bar", [
            ItemID.SilverBar,
            ItemID.TungstenBar
        ]));

        RecipeGroup.RegisterGroup($"{nameof(Pokerraria)}:GoldBar", new RecipeGroup(() => "Any Gold Bar", [
            ItemID.GoldBar,
            ItemID.PlatinumBar
        ]));
    }
}