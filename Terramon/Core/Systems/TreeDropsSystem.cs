using MonoMod.Cil;
using Terramon.Content.Items;
using Terraria.Enums;
using Terraria.Utilities;

namespace Terramon.Core.Systems;

public class TreeDropsSystem : ModSystem
{
    public override void PostSetupContent()
    {
        var obtainable = ModContent.GetContent<ApricornItem>()
            .Where(item => !TerramonItemAPI.Sets.Unobtainable.Contains(item.Type)).Select(item => item.Type)
            .ToHashSet();

        int[] Pool(params int[] types) => types.Where(obtainable.Contains).ToArray();

        // Each kind of tree drops the Apricorns of its biome
        TreeDropsGlobalTile.ApricornsByTree = new Dictionary<TreeTypes, int[]>
        {
            [TreeTypes.Forest] = Pool(ModContent.ItemType<RedApricorn>(), ModContent.ItemType<BlueApricorn>(),
                ModContent.ItemType<YellowApricorn>(), ModContent.ItemType<BlackApricorn>()),
            [TreeTypes.Jungle] = Pool(ModContent.ItemType<GreenApricorn>(), ModContent.ItemType<GreenApricorn>(),
                ModContent.ItemType<YellowApricorn>()),
            [TreeTypes.Snow] = Pool(ModContent.ItemType<WhiteApricorn>(), ModContent.ItemType<WhiteApricorn>(),
                ModContent.ItemType<BlueApricorn>()),
            [TreeTypes.Hallowed] = Pool(ModContent.ItemType<PinkApricorn>(), ModContent.ItemType<PinkApricorn>(),
                ModContent.ItemType<WhiteApricorn>())
        };
    }
}

public class TreeDropsGlobalTile : GlobalTile
{
    private static bool _vanillaTreeShakeFailed;

    /// <summary>
    ///     The denominator (1/x) for the probability of Apricorns falling from a tree when it is shaken.
    ///     Constant value of 8, meaning a 1/8 or approximately 12.5% chance.
    /// </summary>
    /*private const int
        ApricornDropChanceDenominator = 8; // TODO: Make this configurable through a gameplay config option*/

    public static Dictionary<TreeTypes, int[]> ApricornsByTree { private get; set; }

    /// <summary>
    ///     Picks a random Apricorn for the given tree type, or returns false if that tree drops none.
    /// </summary>
    private static bool TryGetApricorn(TreeTypes treeType, out int apricorn)
    {
        apricorn = 0;
        if (ApricornsByTree == null || !ApricornsByTree.TryGetValue(treeType, out var pool) || pool.Length == 0)
            return false;
        apricorn = pool[WorldGen.genRand.Next(pool.Length)];
        return true;
    }

    public override void Load()
    {
        On_WorldGen.ShakeTree += WorldGenShakeTree_Detour;
        IL_WorldGen.ShakeTree += HookShakeTree;
    }

    public override void Unload()
    {
        ApricornsByTree = null;
    }

    /// <summary>
    ///     Handles dropping assorted apricorns from trees when they are cut down.
    /// </summary>
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || WorldGen.noTileActions || WorldGen.gen ||
            type != TileID.Trees || fail || noItem)
            return;

        WorldGen.GetTreeBottom(i, j, out var x, out var y);
        var treeType = WorldGen.GetTreeType(Main.tile[x, y].TileType);
        if (ApricornsByTree?.ContainsKey(treeType) != true) return;

        var shouldDrop = WorldGen.genRand.NextBool(5);
        if (!shouldDrop || !TryGetApricorn(treeType, out var randomApricorn)) return;

        Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 32, 32,
            randomApricorn,
            randomApricorn == ModContent.ItemType<RedApricorn>() && WorldGen.genRand.NextBool(6, 10)
                ? 2
                : 1); // TODO: Red Apricorns should be more common until Apricorn Trees are implemented
    }

    private static void HookShakeTree(ILContext il)
    {
        try
        {
            // Start the Cursor at the start
            var c = new ILCursor(il);

            // Try to find where massive if else chain ends to inject code
            c.GotoNext(i => i.MatchLdcI4(12), i => i.MatchCallvirt(typeof(UnifiedRandom), "Next"),
                i => i.MatchBrtrue(out _), i => i.MatchLdloc3(), i => i.MatchLdcI4(12));

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the result of the random number before the delegate consumes it
            c.EmitDup();

            // Check result of the random number
            c.EmitDelegate<Action<int>>(result =>
            {
                if (result == 0) return;
                _vanillaTreeShakeFailed = true;
            });

            // Move forwards a bit
            c.Index += 2;

            // Duplicate the tree type before the delegate consumes it
            c.EmitDup();

            // Check tree type
            c.EmitDelegate<Action<TreeTypes>>(type =>
            {
                if (type == TreeTypes.Ash) return;
                _vanillaTreeShakeFailed = true;
            });
        }
        catch
        {
            MonoModHooks.DumpIL(Terramon.Instance, il);
        }
    }

    private static void WorldGenShakeTree_Detour(On_WorldGen.orig_ShakeTree orig, int i, int j)
    {
        if (ApricornsByTree == null || ApricornsByTree.Count == 0)
        {
            orig(i, j);
            return;
        }

        WorldGen.GetTreeBottom(i, j, out var x, out var y);
        var numTreeShakes = WorldGen.numTreeShakes;
        var treeShakeX = WorldGen.treeShakeX;
        var treeShakeY = WorldGen.treeShakeY;
        for (var k = 0; k < numTreeShakes; k++)
        {
            if (treeShakeX[k] != x || treeShakeY[k] != y) continue;
            orig(i, j);
            return;
        }

        _vanillaTreeShakeFailed = false;
        orig(i, j); // Call the original method to shake the tree
        if (!_vanillaTreeShakeFailed) return;

        var treeType = WorldGen.GetTreeType(Main.tile[x, y].TileType);
        if (!WorldGen.genRand.NextBool(5) || ApricornsByTree?.ContainsKey(treeType) != true) return;
        y--;
        while (y > 10 && Main.tile[x, y].HasTile && TileID.Sets.IsShakeable[Main.tile[x, y].TileType]) y--;
        y++;
        if (!WorldGen.IsTileALeafyTreeTop(x, y) || Collision.SolidTiles(x - 2, x + 2, y - 2, y + 2))
            return;
        if (!TryGetApricorn(treeType, out var randomApricorn)) return;
        Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Rectangle(x * 16, y * 16, 16, 16),
            randomApricorn, WorldGen.genRand.Next(1, 3));
    }
}