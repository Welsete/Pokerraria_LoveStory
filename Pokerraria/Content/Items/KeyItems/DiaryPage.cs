using Pokerraria.Content.LoveStory;
using Terraria.Localization;

namespace Pokerraria.Content.Items;

/// <summary>
///     A page of the diary of the player from the future, hidden in chests and dungeons. Using it reads it: it goes
///     into the journal (J) and starts Story time.
/// </summary>
public abstract class DiaryPage : KeyItem
{
    /// <summary>
    ///     Which page this is (see <see cref="Journal.Pages" />).
    /// </summary>
    public abstract string PageId { get; }

    public override string Texture => "Pokerraria/Assets/Items/KeyItems/DiaryPage";

    public override LocalizedText DisplayName =>
        Journal.Label("PageItem").WithFormatArgs(Journal.PageTitle(PageId));

    public override LocalizedText Tooltip => Journal.Label("PageItemTooltip");

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 22;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 30;
        Item.consumable = true;
        Item.UseSound = SoundID.Grab;
    }

    public override bool? UseItem(Player player)
    {
        player.GetModPlayer<JournalPlayer>().ReadPage(PageId);
        return true;
    }

    /// <summary>
    ///     The item type of the page with this id.
    /// </summary>
    public static int ItemType(string id)
        => ModContent.GetContent<DiaryPage>().First(p => p.PageId == id).Type;
}

public class DiaryPagePrologue : DiaryPage
{
    public override string PageId => "Prologue";
}

public class DiaryPageBrock : DiaryPage
{
    public override string PageId => "Brock";
}

public class DiaryPageMisty : DiaryPage
{
    public override string PageId => "Misty";
}
