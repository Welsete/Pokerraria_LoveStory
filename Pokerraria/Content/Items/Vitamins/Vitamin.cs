using Terraria.Localization;

namespace Pokerraria.Content.Items;

public abstract class Vitamin : TerramonItem
{
    public override string Texture => "Pokerraria/Assets/Items/Vitamins/" + GetType().Name;

    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.HeldItem.Add(Type);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Vitamin", Language.GetTextValue("Mods.Pokerraria.CommonTooltips.Vitamin")));
    }
}