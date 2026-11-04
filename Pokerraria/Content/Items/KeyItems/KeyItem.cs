using Pokerraria.Content.Rarities;
using Terraria.Localization;

namespace Pokerraria.Content.Items;

public abstract class KeyItem : TerramonItem
{
    public override string Texture => "Pokerraria/Assets/Items/KeyItems/" + GetType().Name;

    protected override int UseRarity => ModContent.RarityType<KeyItemRarity>();

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.maxStack = 1;
        Item.value = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "KeyItem", Language.GetTextValue("Mods.Pokerraria.CommonTooltips.KeyItem")));
    }
}

public class KeyItemRarity : DiscoRarity
{
    protected override Color[] Colors { get; } =
    [
        new Color(122, 255, 255),
        new Color(255, 86, 232),
        new Color(255, 192, 35)
    ];

    protected override float Time => 2.25f;
}