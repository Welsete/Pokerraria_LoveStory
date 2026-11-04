using Terramon.Content.Buffs;
using Terramon.Content.LoveStory;
using Terramon.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Terramon.Content.GUI;

/// <summary>
///     The LoveStory journal (J): where to go next, what happened so far and the diary pages found.
/// </summary>
public sealed class JournalUI : SmartUIState
{
    private const float PanelWidth = 680f;
    private const float PanelHeight = 460f;
    private const float Gap = 8f;
    private const float ContentTop = 78f;
    private const float PageListWidth = 190f;

    private static readonly Color Gold = new(255, 215, 90);
    private static readonly Color Faded = new(170, 170, 190);
    private static readonly Color TabOn = new(90, 115, 200);
    private static readonly Color TabOff = new Color(44, 57, 105) * 0.9f;

    private enum Tab { Next, Story, Pages }

    private static Tab _tab = Tab.Next;
    private static string _selectedPage = Journal.Pages[0];

    private UIPanel _panel;
    private readonly List<UITextPanel<string>> _tabButtons = [];
    private readonly List<UIElement> _content = [];

    public override int InsertionIndex(List<GameInterfaceLayer> layers)
        => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

    private static JournalUI Instance => UILoader.GetUIState<JournalUI>();

    private static JournalPlayer Diary => Main.LocalPlayer.GetModPlayer<JournalPlayer>();

    public static void Toggle()
    {
        var ui = Instance;
        ui.Visible = !ui.Visible;
        SoundEngine.PlaySound(ui.Visible ? SoundID.MenuOpen : SoundID.MenuClose);
        if (ui.Visible)
            ui.Refresh();
    }

    /// <summary>
    ///     Opens the journal on the Pages tab, showing this page.
    /// </summary>
    public static void OpenPage(string id)
    {
        _tab = Tab.Pages;
        _selectedPage = id;
        var ui = Instance;
        if (!ui.Visible)
            SoundEngine.PlaySound(SoundID.MenuOpen);
        ui.Visible = true;
        ui.Refresh();
    }

    private static void Close()
    {
        Instance.Visible = false;
        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    public override void OnInitialize()
    {
        _panel = new UIPanel
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            BackgroundColor = new Color(40, 34, 60) * 0.95f,
            BorderColor = Gold * 0.8f
        };
        _panel.Width.Pixels = PanelWidth;
        _panel.Height.Pixels = PanelHeight;
        Append(_panel);

        _panel.Append(new UIText(Journal.Label("Title"), 0.6f, true) { HAlign = 0.5f, TextColor = Gold });

        var tabs = new[] { Tab.Next, Tab.Story, Tab.Pages };
        var inner = PanelWidth - _panel.PaddingLeft - _panel.PaddingRight;
        var closeWidth = 90f;
        var tabWidth = (inner - closeWidth - tabs.Length * Gap) / tabs.Length;
        for (var i = 0; i < tabs.Length; i++)
        {
            var tab = tabs[i];
            var button = new UITextPanel<string>("", 0.85f);
            button.Width.Pixels = tabWidth;
            button.Height.Pixels = 32f;
            button.Left.Pixels = i * (tabWidth + Gap);
            button.Top.Pixels = 36f;
            button.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _tab = tab;
                Refresh();
            };
            _tabButtons.Add(button);
            _panel.Append(button);
        }

        var close = new UITextPanel<LocalizedText>(Journal.Label("Close"), 0.85f);
        close.Width.Pixels = closeWidth;
        close.Height.Pixels = 32f;
        close.Left.Pixels = inner - closeWidth;
        close.Top.Pixels = 36f;
        close.OnLeftClick += (_, _) => Close();
        _panel.Append(close);
    }

    /// <summary>
    ///     Builds the open tab again (pages and progress may have changed).
    /// </summary>
    private void Refresh()
    {
        if (_panel == null)
            return;
        foreach (var element in _content)
            element.Remove();
        _content.Clear();

        var found = Diary.Pages.Count;
        var labels = new[]
        {
            Journal.Text("TabNext"), Journal.Text("TabStory"),
            Journal.Text("TabPages", found, Journal.Pages.Length)
        };
        for (var i = 0; i < _tabButtons.Count; i++)
        {
            _tabButtons[i].SetText(labels[i]);
            _tabButtons[i].BackgroundColor = i == (int)_tab ? TabOn : TabOff;
            _tabButtons[i].BorderColor = i == (int)_tab ? Gold : Color.Black;
        }

        switch (_tab)
        {
            case Tab.Next: BuildNext(); break;
            case Tab.Story: BuildStory(); break;
            case Tab.Pages: BuildPages(); break;
        }

        _panel.Recalculate();
    }

    /// <summary>
    ///     A scrollable column of wrapped text, from <paramref name="left" /> to the right edge of the panel.
    /// </summary>
    private UIList TextColumn(float left)
    {
        var list = new UIList { ListPadding = 10f };
        list.Left.Pixels = left;
        list.Top.Pixels = ContentTop;
        list.Width.Set(-left - 26f, 1f);
        list.Height.Set(-ContentTop, 1f);

        var scrollbar = new UIScrollbar();
        scrollbar.Left.Set(-20f, 1f);
        scrollbar.Top.Pixels = ContentTop + 6f;
        scrollbar.Height.Set(-ContentTop - 12f, 1f);
        list.SetScrollbar(scrollbar);

        _content.Add(list);
        _content.Add(scrollbar);
        _panel.Append(list);
        _panel.Append(scrollbar);
        return list;
    }

    private static void Paragraph(UIList list, string text, Color color, float scale = 0.95f)
    {
        var line = new UIText(text, scale) { IsWrapped = true, TextColor = color, TextOriginX = 0f };
        line.Width.Set(0f, 1f);
        list.Add(line);
    }

    private void BuildNext()
    {
        var list = TextColumn(0f);
        var player = Main.LocalPlayer;
        Paragraph(list, Journal.Text("Chapter1"), Gold, 1.1f);
        var next = Journal.NextStep(player);
        Paragraph(list, next == null ? Journal.Text("ChapterEnd") : Journal.Text($"Steps.{next.Key}.Next"),
            Color.White);
        var done = Journal.Chapter1.Count(s => s.Done(player));
        Paragraph(list, Journal.Text("Progress", done, Journal.Chapter1.Length), Faded, 0.85f);
        Paragraph(list, Journal.Text("PagesHint"), Faded, 0.85f);
    }

    private void BuildStory()
    {
        var list = TextColumn(0f);
        var player = Main.LocalPlayer;
        var any = false;
        foreach (var step in Journal.Chapter1.Where(s => s.Done(player)))
        {
            Paragraph(list, "- " + Journal.Text($"Steps.{step.Key}.Done"), Color.White);
            any = true;
        }

        foreach (var id in Journal.Pages.Where(Diary.Pages.Contains))
        {
            Paragraph(list, "- " + Journal.Text("FoundPage", Journal.PageTitle(id)), Gold);
            any = true;
        }

        if (!any)
            Paragraph(list, Journal.Text("NothingYet"), Faded);
    }

    private void BuildPages()
    {
        var found = Diary.Pages;
        for (var i = 0; i < Journal.Pages.Length; i++)
        {
            var id = Journal.Pages[i];
            var has = found.Contains(id);
            var button = new UITextPanel<string>(has ? Journal.PageTitle(id).Value : "???", 0.85f)
            {
                BackgroundColor = id == _selectedPage ? TabOn : TabOff,
                BorderColor = id == _selectedPage ? Gold : Color.Black,
                TextColor = has ? Color.White : Faded
            };
            button.Width.Pixels = PageListWidth;
            button.Height.Pixels = 32f;
            button.Top.Pixels = ContentTop + i * (32f + Gap);
            button.OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _selectedPage = id;
                Refresh();
            };
            _content.Add(button);
            _panel.Append(button);
        }

        var list = TextColumn(PageListWidth + 2 * Gap);
        if (found.Contains(_selectedPage))
        {
            Paragraph(list, Journal.PageTitle(_selectedPage).Value, Gold, 1.1f);
            Paragraph(list, Journal.PageText(_selectedPage), Color.White);
        }
        else
            Paragraph(list, Journal.Text("PageMissing"), Faded);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Visible || _panel == null)
            return;

        var player = Main.LocalPlayer;

        // While the journal is open nothing spawns around the player
        player.AddBuff(ModContent.BuffType<StoryTime>(), 120);

        // Don't use items or click the world through the journal
        if (_panel.ContainsPoint(Main.MouseScreen))
            player.mouseInterface = true;

        // Esc (or the inventory key) closes it
        if (player.controlInv && player.releaseInventory)
        {
            player.releaseInventory = false;
            Close();
        }
    }
}
