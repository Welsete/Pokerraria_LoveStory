using Terraria.Localization;

namespace Terramon.Content.Items;

/// <summary>
///     A held item with an effect in battle. The class name is the item's name in Pokémon Showdown, which applies the
///     effect; the sprite borrows a Terraria item's look until it gets its own, and it's crafted from that item.
/// </summary>
public abstract class BattleHeldItem : HeldItem
{
    /// <summary>
    ///     The Terraria item it looks like and is crafted from.
    /// </summary>
    protected abstract int LooksLike { get; }

    protected virtual int CraftAmount => 1;

    public override string Texture => $"Terraria/Images/Item_{LooksLike}";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Green;
        Item.value = Item.buyPrice(silver: 50);
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(LooksLike, CraftAmount)
            .AddIngredient(ItemID.FallenStar, 2)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}

public sealed class Leftovers : BattleHeldItem
{
    protected override int LooksLike => ItemID.Apple;
    protected override int CraftAmount => 3;
}

public sealed class OranBerry : BattleHeldItem
{
    protected override int LooksLike => ItemID.BlueBerries;
    protected override int CraftAmount => 3;
}

public sealed class SitrusBerry : BattleHeldItem
{
    protected override int LooksLike => ItemID.Lemon;
    protected override int CraftAmount => 3;
}

public sealed class Charcoal : BattleHeldItem
{
    protected override int LooksLike => ItemID.Coal;
}

public sealed class MysticWater : BattleHeldItem
{
    protected override int LooksLike => ItemID.BottledWater;
    protected override int CraftAmount => 3;
}

public sealed class MiracleSeed : BattleHeldItem
{
    protected override int LooksLike => ItemID.GrassSeeds;
    protected override int CraftAmount => 5;
}

public sealed class Magnet : BattleHeldItem
{
    protected override int LooksLike => ItemID.CelestialMagnet;
}

public sealed class NeverMeltIce : BattleHeldItem
{
    protected override int LooksLike => ItemID.IceBlock;
    protected override int CraftAmount => 20;
}

public sealed class HardStone : BattleHeldItem
{
    protected override int LooksLike => ItemID.StoneBlock;
    protected override int CraftAmount => 50;
}

public sealed class SoftSand : BattleHeldItem
{
    protected override int LooksLike => ItemID.SandBlock;
    protected override int CraftAmount => 50;
}

public sealed class BlackBelt : BattleHeldItem
{
    protected override int LooksLike => ItemID.BlackBelt;
}

public sealed class PoisonBarb : BattleHeldItem
{
    protected override int LooksLike => ItemID.Stinger;
    protected override int CraftAmount => 5;
}

public sealed class SharpBeak : BattleHeldItem
{
    protected override int LooksLike => ItemID.Feather;
    protected override int CraftAmount => 5;
}

public sealed class SpellTag : BattleHeldItem
{
    protected override int LooksLike => ItemID.Ectoplasm;
    protected override int CraftAmount => 3;
}

public sealed class MetalCoat : BattleHeldItem
{
    protected override int LooksLike => ItemID.IronBar;
    protected override int CraftAmount => 10;
}

public sealed class BlackGlasses : BattleHeldItem
{
    protected override int LooksLike => ItemID.Sunglasses;
}

public sealed class SilkScarf : BattleHeldItem
{
    protected override int LooksLike => ItemID.Silk;
    protected override int CraftAmount => 10;
}

public sealed class QuickClaw : BattleHeldItem
{
    protected override int LooksLike => ItemID.FeralClaws;
}

public sealed class FocusSash : BattleHeldItem
{
    protected override int LooksLike => ItemID.TatteredCloth;
    protected override int CraftAmount => 3;
}

public sealed class LifeOrb : BattleHeldItem
{
    protected override int LooksLike => ItemID.LifeCrystal;
}

public sealed class KingsRock : BattleHeldItem
{
    protected override int LooksLike => ItemID.GoldCrown;
}

public sealed class ShellBell : BattleHeldItem
{
    protected override int LooksLike => ItemID.Seashell;
    protected override int CraftAmount => 3;
}
