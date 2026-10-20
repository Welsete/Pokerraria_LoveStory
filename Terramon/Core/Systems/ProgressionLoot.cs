using Terramon.Content.Items;
using Terramon.Content.NPCs;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace Terramon.Core.Systems;

/// <summary>
///     A drop condition checked when the loot is rolled (world progression, biome of the closest player...).
/// </summary>
public sealed class ProgressionDropCondition(Func<DropAttemptInfo, bool> predicate, string descriptionKey = null)
    : IItemDropRuleCondition
{
    public bool CanDrop(DropAttemptInfo info) => predicate(info);

    public bool CanShowItemDropInUI() => true;

    public string GetConditionDescription()
        => descriptionKey == null ? null : Language.GetTextValue("Mods.Terramon.Loot." + descriptionKey);
}

/// <summary>
///     Exp. Candies from enemies by world progression and from bosses, plus a few evolution stones in their biomes.
/// </summary>
public sealed class ProgressionLootGlobalNPC : GlobalNPC
{
    private static bool IsRegularEnemy(DropAttemptInfo info)
    {
        var npc = info.npc;
        return npc != null && !npc.friendly && !npc.townNPC && npc.lifeMax > 5 && npc.value > 0f &&
               !npc.SpawnedFromStatue && !npc.boss && npc.ModNPC is not PokemonNPC;
    }

    private static IItemDropRuleCondition Enemy(Func<bool> progression, string key)
        => new ProgressionDropCondition(info => IsRegularEnemy(info) && progression(), key);

    public override void ModifyGlobalLoot(GlobalLoot globalLoot)
    {
        globalLoot.Add(ItemDropRule.ByCondition(Enemy(() => !Main.hardMode, "PreHardmode"),
            ModContent.ItemType<ExpCandyXS>(), 40));
        globalLoot.Add(ItemDropRule.ByCondition(Enemy(() => NPC.downedBoss2 && !NPC.downedPlantBoss, "AfterEvilBoss"),
            ModContent.ItemType<ExpCandyS>(), 70));
        globalLoot.Add(ItemDropRule.ByCondition(Enemy(() => Main.hardMode, "Hardmode"),
            ModContent.ItemType<ExpCandyM>(), 90));
        globalLoot.Add(ItemDropRule.ByCondition(Enemy(() => NPC.downedPlantBoss, "AfterPlantera"),
            ModContent.ItemType<ExpCandyL>(), 120));
        globalLoot.Add(ItemDropRule.ByCondition(Enemy(() => NPC.downedMoonlord, "AfterMoonLord"),
            ModContent.ItemType<ExpCandyXL>(), 200));

        // Evolution stones that have no other source in worlds created before this update
        globalLoot.Add(ItemDropRule.ByCondition(
            new ProgressionDropCondition(info => IsRegularEnemy(info) && info.player is { ZoneSnow: true }, "InSnow"),
            ModContent.ItemType<IceStone>(), 150));
        globalLoot.Add(ItemDropRule.ByCondition(
            new ProgressionDropCondition(info => IsRegularEnemy(info) && !Main.dayTime &&
                                                 info.player is { } p &&
                                                 (p.ZoneCorrupt || p.ZoneCrimson || p.ZoneGraveyard), "AtNightEvil"),
            ModContent.ItemType<DuskStone>(), 150));
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (!TryGetBossCandy(npc.type, out var candy, out var min, out var max))
            return;

        // The Lunatic Cultist has no treasure bag, so it drops candies in every mode
        if (npc.type == NPCID.CultistBoss)
        {
            npcLoot.Add(ItemDropRule.Common(candy, 1, min, max));
            return;
        }

        var notExpert = new LeadingConditionRule(new Conditions.NotExpert());
        if (npc.type is NPCID.Retinazer or NPCID.Spazmatism)
            notExpert.OnSuccess(ItemDropRule.ByCondition(new Conditions.MissingTwin(), candy, 1, min, max));
        else if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
            notExpert.OnSuccess(ItemDropRule.ByCondition(new Conditions.LegacyHack_IsABoss(), candy, 1, min, max));
        else
            notExpert.OnSuccess(ItemDropRule.Common(candy, 1, min, max));
        npcLoot.Add(notExpert);
    }

    private static bool TryGetBossCandy(int npcType, out int candy, out int min, out int max)
    {
        (candy, min, max) = npcType switch
        {
            NPCID.KingSlime or NPCID.EyeofCthulhu or NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody
                or NPCID.EaterofWorldsTail or NPCID.BrainofCthulhu or NPCID.QueenBee or NPCID.Deerclops
                or NPCID.SkeletronHead => (ModContent.ItemType<ExpCandyS>(), 2, 4),
            NPCID.WallofFlesh or NPCID.QueenSlimeBoss or NPCID.TheDestroyer or NPCID.Retinazer or NPCID.Spazmatism
                or NPCID.SkeletronPrime => (ModContent.ItemType<ExpCandyM>(), 2, 4),
            NPCID.Plantera or NPCID.Golem or NPCID.DukeFishron or NPCID.HallowBoss or NPCID.CultistBoss
                => (ModContent.ItemType<ExpCandyL>(), 2, 3),
            NPCID.MoonLordCore => (ModContent.ItemType<ExpCandyXL>(), 2, 4),
            _ => (0, 0, 0)
        };
        return candy != 0;
    }
}

/// <summary>
///     Exp. Candies inside boss treasure bags.
/// </summary>
public sealed class ProgressionLootGlobalItem : GlobalItem
{
    public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
    {
        var (candy, min, max) = item.type switch
        {
            ItemID.KingSlimeBossBag or ItemID.EyeOfCthulhuBossBag or ItemID.EaterOfWorldsBossBag
                or ItemID.BrainOfCthulhuBossBag or ItemID.QueenBeeBossBag or ItemID.DeerclopsBossBag
                or ItemID.SkeletronBossBag => (ModContent.ItemType<ExpCandyS>(), 3, 5),
            ItemID.WallOfFleshBossBag or ItemID.QueenSlimeBossBag or ItemID.DestroyerBossBag or ItemID.TwinsBossBag
                or ItemID.SkeletronPrimeBossBag => (ModContent.ItemType<ExpCandyM>(), 3, 5),
            ItemID.PlanteraBossBag or ItemID.GolemBossBag or ItemID.FishronBossBag or ItemID.FairyQueenBossBag
                => (ModContent.ItemType<ExpCandyL>(), 2, 4),
            ItemID.MoonLordBossBag => (ModContent.ItemType<ExpCandyXL>(), 3, 5),
            _ => (0, 0, 0)
        };
        if (candy != 0)
            itemLoot.Add(ItemDropRule.Common(candy, 1, min, max));
    }
}
