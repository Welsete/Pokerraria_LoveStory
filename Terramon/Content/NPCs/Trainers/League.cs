using EasyPacketsLib;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Trainers;

/// <summary>
///     The Pokémon League: the order of the Gym Leaders and Elite Four, plus helpers shared by trainers and items.
/// </summary>
public static class League
{
    /// <summary>
    ///     Internal names of the eight Gym Leaders, in the order they must be beaten.
    /// </summary>
    public static readonly string[] GymLeaders =
        ["Brock", "Misty", "LtSurge", "Erika", "Koga", "Sabrina", "Blaine", "Giovanni"];

    /// <summary>
    ///     Internal names of the Elite Four followed by the Champion, in the order they're fought in one run.
    /// </summary>
    public static readonly string[] EliteFourAndChampion = ["Lorelei", "Bruno", "Agatha", "Lance", "Blue"];

    public static int BadgeCount(Player player)
    {
        var badges = player.GetModPlayer<GymPlayer>().Badges;
        return GymLeaders.Count(badges.Contains);
    }

    public static bool HasAllBadges(Player player) => BadgeCount(player) == GymLeaders.Length;

    public static int TrainerType(string name)
        => ModContent.TryFind<ModNPC>("Terramon", name, out var npc) ? npc.Type : 0;

    /// <summary>
    ///     Gives the player the given amount of copper coins, converted into the largest coins.
    /// </summary>
    public static void GiveMoney(Player player, long copper)
    {
        var source = player.GetSource_GiftOrReward();
        foreach (var (type, count) in SplitCoins(copper))
        {
            // Stack limit is 999 for platinum, more than any prize
            if (count > 0)
                player.QuickSpawnItem(source, type, count);
        }
    }

    /// <summary>
    ///     Formats an amount of copper coins with coin icons, e.g. "[i/s3:73] [i/s20:72]".
    /// </summary>
    public static string FormatMoney(long copper)
    {
        var parts = SplitCoins(copper)
            .Where(c => c.Count > 0)
            .Select(c => $"[i/s{c.Count}:{c.Type}]");
        return string.Join(" ", parts);
    }

    private static IEnumerable<(int Type, int Count)> SplitCoins(long copper)
    {
        copper = Math.Max(copper, 0);
        yield return (ItemID.PlatinumCoin, (int)(copper / 1000000));
        yield return (ItemID.GoldCoin, (int)(copper / 10000 % 100));
        yield return (ItemID.SilverCoin, (int)(copper / 100 % 100));
        yield return (ItemID.CopperCoin, (int)(copper % 100));
    }

    /// <summary>
    ///     Summons a trainer a few tiles in front of the given player, unless one of them is already in the world.
    ///     Can be called from any side: clients ask the server to do it.
    /// </summary>
    public static void RequestSpawn(Player player, int npcType)
    {
        if (npcType <= 0)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            Terramon.Instance.SendPacket(new SpawnTrainerRpc(npcType));
            return;
        }

        SpawnNear(player, npcType);
    }

    /// <summary>
    ///     Actually spawns the trainer. Only runs on the server or in singleplayer.
    /// </summary>
    internal static void SpawnNear(Player player, int npcType)
    {
        if (NPC.AnyNPCs(npcType))
            return;

        var spawnPos = player.Bottom + new Vector2(player.direction * 64f, 0f);
        var index = NPC.NewNPC(player.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, npcType);
        if (index >= Main.maxNPCs)
            return;

        var npc = Main.npc[index];
        npc.direction = npc.spriteDirection = player.Center.X > npc.Center.X ? 1 : -1;

        // A puff of smoke so the trainer doesn't just pop in (visual only, so skipped on the server)
        if (!Main.dedServ)
        {
            for (var i = 0; i < 20; i++)
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, 0f, -1f, 100, default, 1.4f);
        }

        var text = Language.GetTextValue("Mods.Terramon.NPCs.Trainers.Arrived", npc.GivenOrTypeName);
        if (Main.netMode == NetmodeID.Server)
            Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), Color.Orange);
        else
            Main.NewText(text, Color.Orange);
    }
}

/// <summary>
///     Sent by a client to ask the server to summon a trainer next to them.
/// </summary>
public struct SpawnTrainerRpc(int npcType) : IEasyPacket
{
    private int _npcType = npcType;

    public readonly void Serialise(BinaryWriter writer)
    {
        writer.Write(_npcType);
    }

    public void Deserialise(BinaryReader reader, in SenderInfo sender)
    {
        _npcType = reader.ReadInt32();
    }

    public readonly void Receive(in SenderInfo sender, ref bool handled)
    {
        handled = true;
        if (Main.netMode != NetmodeID.Server)
            return;

        // Only accept trainer types, and only next to the player who asked
        if (_npcType < NPCID.Count || _npcType >= NPCLoader.NPCCount ||
            NPCLoader.GetNPC(_npcType) is not TrainerNPC)
            return;

        var player = Main.player[sender.WhoAmI];
        if (player?.active == true)
            League.SpawnNear(player, _npcType);
    }
}
