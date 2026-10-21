using Terraria.Localization;

namespace Terramon.Content.Items;

/// <summary>
///     A held item with an effect in battle. The class name is the item's name in Pokémon Showdown, which applies the
///     effect. Its sprite is Assets/Items/HeldItems/&lt;class name&gt;.png (Isa's 32x32 art), and it's crafted from the
///     Terraria item in <see cref="LooksLike" />.
/// </summary>
public abstract class BattleHeldItem : HeldItem
{
    /// <summary>
    ///     The Terraria item it's crafted from (and looked like before it had a sprite of its own).
    /// </summary>
    protected abstract int LooksLike { get; }

    protected virtual int CraftAmount => 1;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = Item.height = 32;
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
