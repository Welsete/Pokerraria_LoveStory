using Terramon.Content.Items;
using Terramon.Content.NPCs.Trainers;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace Terramon.Content.Tiles.Interactive;

/// <summary>
///     A pedestal inside a Pokémon Gym or the Pokémon League. Right-clicking it calls the next trainer the player
///     may challenge there, with the same rules as the challenge items.
/// </summary>
public abstract class PedestalTile : ModTile
{
    // A vanilla crystal ball on its stand, so no new sprite is needed
    public override string Texture => $"Terraria/Images/Tiles_{TileID.CrystalBall}";

    /// <summary>
    ///     The item that places this tile.
    /// </summary>
    protected abstract int ItemType { get; }

    /// <summary>
    ///     The color of the tile on the map and of the light it gives off.
    /// </summary>
    protected abstract Color GlowColor { get; }

    /// <summary>
    ///     Calls the next trainer for the local player, or tells them why nobody can come.
    /// </summary>
    protected abstract void Challenge(Player player);

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileLavaDeath[Type] = false;
        TileID.Sets.DisableSmartCursor[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.addTile(Type);

        DustType = DustID.Stone;
        RegisterItemDrop(ItemType);
        AddMapEntry(GlowColor, CreateMapEntryName());
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ItemType;
    }

    public override bool RightClick(int i, int j)
    {
        SoundEngine.PlaySound(SoundID.Item4, new Vector2(i * 16, j * 16));
        Challenge(Main.LocalPlayer);
        return true;
    }

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        var color = GlowColor.ToVector3() * 0.6f;
        r = color.X;
        g = color.Y;
        b = color.Z;
    }

    public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
    {
        // Tint the vanilla sprite so the two pedestals can be told apart
        drawData.finalColor = drawData.finalColor.MultiplyRGB(Color.Lerp(Color.White, GlowColor, 0.35f));
    }
}

/// <summary>
///     Calls the next Gym Leader the player doesn't have the badge of.
/// </summary>
public class GymPedestal : PedestalTile
{
    protected override int ItemType => ModContent.ItemType<GymPedestalItem>();

    protected override Color GlowColor => new(120, 190, 255);

    protected override void Challenge(Player player)
    {
        var next = League.NextGymLeader(player);
        if (next == null)
        {
            Main.NewText(Language.GetTextValue("Mods.Terramon.Pedestals.AllBadges"), Color.Orange);
            return;
        }

        League.Challenge(player, League.TrainerType(next), true);
    }
}

/// <summary>
///     Calls the next opponent of the player's Pokémon League run.
/// </summary>
public class LeaguePedestal : PedestalTile
{
    protected override int ItemType => ModContent.ItemType<LeaguePedestalItem>();

    protected override Color GlowColor => new(255, 215, 90);

    protected override void Challenge(Player player)
    {
        League.Challenge(player, League.NextLeagueOpponent(player), true);
    }
}

public abstract class PedestalItem : TerramonItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.CrystalBall}";

    protected override int UseRarity => ItemRarityID.Blue;

    protected abstract int TileType { get; }

    protected abstract Color Tint { get; }

    /// <summary>
    ///     The materials of the Work Bench recipe.
    /// </summary>
    protected abstract (int Type, int Stack)[] Materials { get; }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(TileType);
        base.SetDefaults();
        Item.width = 28;
        Item.height = 28;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.sellPrice(silver: 20);
        Item.color = Tint;
    }

    public override void AddRecipes()
    {
        var recipe = CreateRecipe();
        foreach (var (type, stack) in Materials)
            recipe.AddIngredient(type, stack);
        recipe.AddTile(TileID.WorkBenches).Register();
    }
}

public class GymPedestalItem : PedestalItem
{
    protected override int TileType => ModContent.TileType<GymPedestal>();
    protected override Color Tint => new(170, 210, 255);
    protected override (int Type, int Stack)[] Materials =>
        [(ItemID.StoneBlock, 20), (ItemID.Glass, 5), (ItemID.Torch, 2)];
}

public class LeaguePedestalItem : PedestalItem
{
    protected override int TileType => ModContent.TileType<LeaguePedestal>();
    protected override Color Tint => new(255, 225, 130);
    protected override (int Type, int Stack)[] Materials =>
        [(ItemID.StoneBlock, 20), (ItemID.Glass, 5), (ItemID.FallenStar, 1)];
}
