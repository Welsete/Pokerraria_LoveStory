using Terramon.Content.Items.PokeBalls;
using Terramon.Content.NPCs.Trainers;

namespace Terramon.Content.Items;

/// <summary>
///     A reusable item that summons a Gym Leader (or the Elite Four) next to the player.
///     Crafted at a Work Bench once the trainer's gate boss is defeated.
/// </summary>
public abstract class ChallengeItem : TerramonItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.Book}";

    protected override int UseRarity => ItemRarityID.Orange;

    /// <summary>
    ///     The color the vanilla texture is tinted with.
    /// </summary>
    protected abstract Color Tint { get; }

    /// <summary>
    ///     The condition (usually a boss being defeated) for the recipe to be available.
    /// </summary>
    protected abstract Condition RecipeCondition { get; }

    /// <summary>
    ///     The thematic materials of the recipe. A Poké Ball is always added on top.
    /// </summary>
    protected abstract (int Type, int Stack)[] Materials { get; }

    /// <summary>
    ///     The trainer this item summons right now for the given player, or 0 for none.
    /// </summary>
    protected abstract int GetTrainerType(Player player);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 28;
        Item.height = 30;
        Item.maxStack = 1;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 45;
        Item.UseSound = SoundID.Item4;
        Item.consumable = false;
        Item.value = Item.sellPrice(silver: 50);
        Item.color = Tint;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
            return true;

        League.Challenge(player, GetTrainerType(player));
        return true;
    }

    public override void AddRecipes()
    {
        var recipe = CreateRecipe();
        foreach (var (type, stack) in Materials)
            recipe.AddIngredient(type, stack);
        recipe.AddIngredient(ModContent.ItemType<PokeBallItem>())
            .AddTile(TileID.WorkBenches)
            .AddCondition(RecipeCondition)
            .Register();
    }
}

/// <summary>
///     A <see cref="ChallengeItem" /> that always summons the same Gym Leader.
/// </summary>
public abstract class GymChallenge<T> : ChallengeItem where T : GymLeader
{
    protected override int GetTrainerType(Player player) => ModContent.NPCType<T>();
}

public sealed class BrockChallenge : GymChallenge<Brock>
{
    protected override Color Tint => new(170, 170, 180);
    protected override Condition RecipeCondition => Condition.DownedEyeOfCthulhu;
    protected override (int Type, int Stack)[] Materials => [(ItemID.StoneBlock, 25)];
}

public sealed class MistyChallenge : GymChallenge<Misty>
{
    protected override Color Tint => new(90, 160, 255);
    protected override Condition RecipeCondition => Condition.DownedEowOrBoc;
    protected override (int Type, int Stack)[] Materials =>
        [(ItemID.Coral, 3), (ItemID.Seashell, 3), (ItemID.BottledWater, 1)];
}

public sealed class LtSurgeChallenge : GymChallenge<LtSurge>
{
    protected override Color Tint => new(255, 200, 60);
    protected override Condition RecipeCondition => Condition.DownedSkeletron;
    protected override (int Type, int Stack)[] Materials => [(ItemID.Wire, 25), (ItemID.Bone, 10)];
}

public sealed class ErikaChallenge : GymChallenge<Erika>
{
    protected override Color Tint => new(110, 220, 110);
    protected override Condition RecipeCondition => Condition.Hardmode;
    protected override (int Type, int Stack)[] Materials => [(ItemID.Daybloom, 5), (ItemID.JungleSpores, 8)];
}

public sealed class KogaChallenge : GymChallenge<Koga>
{
    protected override Color Tint => new(200, 90, 230);
    protected override Condition RecipeCondition => Condition.DownedMechBossAny;
    protected override (int Type, int Stack)[] Materials => [(ItemID.Stinger, 8), (ItemID.SoulofNight, 5)];
}

public sealed class SabrinaChallenge : GymChallenge<Sabrina>
{
    protected override Color Tint => new(255, 210, 90);
    protected override Condition RecipeCondition => Condition.DownedMechBossAll;
    protected override (int Type, int Stack)[] Materials => [(ItemID.CrystalShard, 10), (ItemID.FallenStar, 5)];
}

public sealed class BlaineChallenge : GymChallenge<Blaine>
{
    protected override Color Tint => new(255, 100, 60);
    protected override Condition RecipeCondition => Condition.DownedPlantera;
    protected override (int Type, int Stack)[] Materials => [(ItemID.HellstoneBar, 10), (ItemID.Obsidian, 10)];
}

public sealed class GiovanniChallenge : GymChallenge<Giovanni>
{
    protected override Color Tint => new(120, 200, 100);
    protected override Condition RecipeCondition => Condition.DownedGolem;
    protected override (int Type, int Stack)[] Materials => [(ItemID.GoldCoin, 10), (ItemID.LihzahrdBrick, 20)];
}

/// <summary>
///     Summons the next opponent of the player's Elite Four run (Lorelei when no run is going).
/// </summary>
public sealed class LeagueInvitation : ChallengeItem
{
    protected override Color Tint => new(255, 230, 120);
    protected override Condition RecipeCondition => Condition.DownedCultist;

    protected override (int Type, int Stack)[] Materials =>
    [
        (ItemID.Ectoplasm, 10), (ItemID.FragmentSolar, 1), (ItemID.FragmentVortex, 1),
        (ItemID.FragmentNebula, 1), (ItemID.FragmentStardust, 1)
    ];

    protected override int GetTrainerType(Player player) => League.NextLeagueOpponent(player);
}
