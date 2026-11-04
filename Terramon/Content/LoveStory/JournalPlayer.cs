using Terramon.Content.Buffs;
using Terramon.Content.Commands;
using Terramon.Content.GUI;
using Terramon.Content.Items;
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
        _prologueGiven = false;
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
    }

    public override void LoadData(TagCompound tag)
    {
        Pages.Clear();
        foreach (var id in tag.GetList<string>("pages"))
            Pages.Add(id);
        _prologueGiven = tag.GetBool("prologueGiven");
    }
}
