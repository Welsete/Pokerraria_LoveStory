using Terramon.Core.Systems.PokemonDirectUseSystem;
using Terraria.Localization;

namespace Terramon.Content.Items;

public abstract class RecoveryItem : TerramonItem, IPokemonDirectUse
{
    /// <summary>
    ///     What the item costs at the Pokémart, in copper coins.
    /// </summary>
    protected virtual int BuyPrice => 0;

    public override void SetStaticDefaults()
    {
        TerramonItemAPI.Sets.HeldItem.Add(Type);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = BuyPrice;
    }

    public virtual bool AffectedByPokemonDirectUse(PokemonData data)
    {
        return true;
    }
    
    public virtual int PokemonDirectUse(Player player, PokemonData data, int amount = 1)
    {
        return amount;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Insert(tooltips.FindIndex(t => t.Name == "Tooltip0"),
            new TooltipLine(Mod, "Recovery", Language.GetTextValue("Mods.Terramon.CommonTooltips.RecoveryItem")));
    }
}