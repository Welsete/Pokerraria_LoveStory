using Terramon.Content.Buffs;
using Terramon.Content.Commands;
using Terramon.Content.GUI;
using Terramon.Content.Items;
using Terramon.Content.WorldGeneration;
using Terramon.Core.Systems;
using Terraria.GameInput;
using Terraria.ModLoader.IO;

namespace Terramon.Content.LoveStory;

/// <summary>
///     What a player has found of the LoveStory: the diary pages read so far. Saved with the character.
/// </summary>
public class JournalPlayer : ModPlayer
{
    /// <summary>
    ///     How long "Story time" (no enemies spawn) lasts after reading a page: one minute.
    /// </summary>
    public const int StoryTimeTicks = 60 * 60;

    /// <summary>
    ///     Ids (see <see cref="Journal.Pages" />) of the pages this player has read.
    /// </summary>
    public HashSet<string> Pages { get; } = [];

    private bool _prologueGiven;

    /// <summary>
    ///     Pages handed out in a Gym because its world has no dungeon to find them in (see <see cref="PostUpdate" />).
    /// </summary>
    private readonly HashSet<string> _handedOut = [];

    private int _checkTimer;

    /// <summary>
    ///     Reads a diary page: keeps it in the journal, starts Story time and opens the journal on it.
    /// </summary>
    public void ReadPage(string id)
    {
        var isNew = Pages.Add(id);
        Player.AddBuff(ModContent.BuffType<StoryTime>(), StoryTimeTicks);
        if (Player.whoAmI != Main.myPlayer)
            return;
        if (isNew)
            Main.NewText(Journal.Text("NewPage", Journal.PageTitle(id)), TerramonCommand.ChatColorYellow);
        JournalUI.OpenPage(id);
    }

    /// <summary>
    ///     Forgets every page and gives the prologue again next time (used by the test panel).
    /// </summary>
    public void Reset()
    {
        Pages.Clear();
        _handedOut.Clear();
        _prologueGiven = false;
    }

    /// <summary>
    ///     Worlds made before the Gym dungeons (and every world, for Misty, until hers exists) have nowhere to find the
    ///     Brock and Misty pages: walking into the Gym hands the page over once.
    /// </summary>
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || ++_checkTimer < 60)
            return;
        _checkTimer = 0;
        var feet = Player.Bottom.ToTileCoordinates();
        if (BrockGymSystem.Exists && !BrockGymSystem.HasDungeon && BrockGymSystem.Interior.Contains(feet))
            HandOut("Brock");
        if (MistyGymSystem.Exists && MistyGymSystem.Interior.Contains(feet))
            HandOut("Misty");
    }

    private void HandOut(string id)
    {
        if (Pages.Contains(id) || !_handedOut.Add(id))
            return;
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), DiaryPage.ItemType(id));
        Main.NewText(Journal.Text("PageInGym"), TerramonCommand.ChatColorYellow);
    }

    public override void OnEnterWorld()
    {
        // The first page is waiting in the bag of every new character
        if (_prologueGiven)
            return;
        _prologueGiven = true;
        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), DiaryPage.ItemType("Prologue"));
        Main.NewText(Journal.Text("FirstPageHint"), TerramonCommand.ChatColorYellow);
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (KeybindSystem.JournalKeybind.JustPressed)
            JournalUI.Toggle();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["pages"] = Pages.ToList();
        tag["prologueGiven"] = _prologueGiven;
        tag["handedOut"] = _handedOut.ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        Pages.Clear();
        foreach (var id in tag.GetList<string>("pages"))
            Pages.Add(id);
        _prologueGiven = tag.GetBool("prologueGiven");
        _handedOut.Clear();
        foreach (var id in tag.GetList<string>("handedOut"))
            _handedOut.Add(id);
    }
}
