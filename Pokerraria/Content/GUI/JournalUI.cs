using ReLogic.Content;
using Pokerraria.Content.Buffs;
using Pokerraria.Content.LoveStory;
using Pokerraria.Core.Loaders.UILoading;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Pokerraria.Content.GUI;

/// <summary>
///     The LoveStory journal (J), drawn as an open book. The contents spread lists the diary pages (left) and where to
///     go next and what happened so far (right). Clicking a page opens it: its picture on the left (a black silhouette
///     while it's not found) and its text on the right, with arrows to turn the pages.
/// </summary>
public sealed class JournalUI : SmartUIState
{
    private const string Art = "Pokerraria/Assets/GUI/Journal/";

    // Where things go on the 720x460 book texture
    private const float LeftX = 44f;
    private const float RightX = 388f;
    private const float PageWidth = 290f;
    private const float PageTop = 28f;
    private const float Bottom = 412f;

    private static readonly Color Gold = new(255, 196, 60);
    private static readonly Color Ink = new(250, 242, 228);
    private static readonly Color Faded = new(200, 190, 175);
    private static readonly Color Missing = new(230, 120, 140);
    private static readonly Color Hover = new(255, 230, 140);

    // Ink on the pages' paper (the old book lettering, see OldFont)
    private static readonly Color PageInk = new(58, 38, 24);
    private static readonly Color TitleInk = new(110, 52, 18);

    /// <summary>
    ///     The page open on the book, or null for the contents.
    /// </summary>
    private static string _openPage;

    private UIImage _book;
    private readonly List<UIElement> _content = [];

    private static Asset<Texture2D> Tex(string name) => ModContent.Request<Texture2D>(Art + name);

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
        {
            _openPage = null;
            ui.Refresh();
        }
    }

    /// <summary>
    ///     Opens the journal on this page.
    /// </summary>
    public static void OpenPage(string id)
    {
        var ui = Instance;
        if (!ui.Visible)
            SoundEngine.PlaySound(SoundID.MenuOpen);
        ui.Visible = true;
        _openPage = id;
        ui.Refresh();
    }

    private static void Close()
    {
        Instance.Visible = false;
        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    public override void OnInitialize()
    {
        _book = new UIImage(Tex("Book")) { HAlign = 0.5f, VAlign = 0.5f };
        _book.Width.Pixels = 720f;
        _book.Height.Pixels = 460f;
        Append(_book);
    }

    private void Refresh()
    {
        if (_book == null)
            return;
        foreach (var element in _content)
            element.Remove();
        _content.Clear();

        if (_openPage == null)
            BuildContents();
        else
            BuildPage(_openPage);

        var close = Link("X", 0.9f, Close);
        close.Left.Pixels = 690f;
        close.Top.Pixels = 14f;
        Add(close);

        _book.Recalculate();
    }

    private T Add<T>(T element) where T : UIElement
    {
        _content.Add(element);
        _book.Append(element);
        return element;
    }

    private static UIText Text(string text, float scale, Color color, bool large = false)
        => new(text, scale, large) { TextColor = color };

    /// <summary>
    ///     Text that lights up under the mouse and does something when clicked.
    /// </summary>
    private static UIText Link(string text, float scale, Action onClick, Color? color = null)
    {
        var baseColor = color ?? Ink;
        var link = Text(text, scale, baseColor);
        link.OnMouseOver += (_, _) =>
        {
            link.TextColor = Hover;
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        link.OnMouseOut += (_, _) => link.TextColor = baseColor;
        link.OnLeftClick += (_, _) => onClick();
        return link;
    }

    /// <summary>
    ///     A scrolling column of wrapped text.
    /// </summary>
    private UIList Column(float left, float top, float height)
    {
        var list = new UIList { ListPadding = 8f };
        list.Left.Pixels = left;
        list.Top.Pixels = top;
        list.Width.Pixels = PageWidth - 16f;
        list.Height.Pixels = height;

        var scrollbar = new UIScrollbar();
        scrollbar.Left.Pixels = left + PageWidth - 12f;
        scrollbar.Top.Pixels = top + 4f;
        scrollbar.Height.Pixels = height - 8f;
        list.SetScrollbar(scrollbar);

        Add(list);
        Add(scrollbar);
        return list;
    }

    private static void Paragraph(UIList list, string text, Color color, float scale = 0.9f)
    {
        var line = new UIText(text, scale) { IsWrapped = true, TextColor = color, TextOriginX = 0f };
        line.Width.Set(0f, 1f);
        list.Add(line);
    }

    private static void Centered(UIElement element, float pageLeft, float top)
    {
        element.Left.Pixels = pageLeft;
        element.Width.Pixels = PageWidth;
        element.Top.Pixels = top;
        if (element is UIText text)
            text.TextOriginX = 0.5f;
    }

    // ------------------------------------------------------------------ contents
    private void BuildContents()
    {
        var player = Main.LocalPlayer;
        var found = Diary.Pages;

        var title = Add(Text(Journal.Text("Title"), 0.7f, Gold, true));
        Centered(title, LeftX, PageTop);
        var chapter = Add(Text(Journal.Text("Chapter1"), 0.8f, Faded));
        Centered(chapter, LeftX, PageTop + 44f);

        for (var i = 0; i < Journal.Pages.Length; i++)
        {
            var id = Journal.Pages[i];
            var has = found.Contains(id);
            var y = PageTop + 84f + i * 34f;

            var box = Add(new UIImage(Tex(has ? "Check" : "Unchecked")));
            box.Left.Pixels = LeftX + 8f;
            box.Top.Pixels = y + 2f;

            var name = $"{i + 1}. " + (has ? Journal.PageTitle(id).Value : "???");
            var link = Add(Link(name, 0.95f, () =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                _openPage = id;
                Refresh();
            }, has ? Gold : Missing));
            link.Left.Pixels = LeftX + 34f;
            link.Top.Pixels = y;
        }

        // Pages found, with a bar like the boss checklist
        var count = Journal.Pages.Count(found.Contains);
        var percent = Add(Text(Journal.Text("PagesFound", count, Journal.Pages.Length), 0.85f, Gold));
        Centered(percent, LeftX, Bottom - 50f);
        var bar = Add(new ProgressBar((float)count / Journal.Pages.Length));
        bar.Left.Pixels = LeftX + 10f;
        bar.Top.Pixels = Bottom - 24f;
        bar.Width.Pixels = PageWidth - 20f;
        bar.Height.Pixels = 14f;

        // Right page: next step and what happened so far
        var nextTitle = Add(Text(Journal.Text("TabNext"), 0.5f, Gold, true));
        Centered(nextTitle, RightX, PageTop);
        var list = Column(RightX, PageTop + 40f, Bottom - PageTop - 40f);
        var next = Journal.NextStep(player);
        Paragraph(list, next == null ? Journal.Text("ChapterEnd") : Journal.Text($"Steps.{next.Key}.Next"), Ink, 0.95f);
        Paragraph(list, Journal.Text("Progress", Journal.Chapter1.Count(s => s.Done(player)), Journal.Chapter1.Length),
            Faded, 0.8f);
        Paragraph(list, " ", Ink, 0.5f);
        Paragraph(list, Journal.Text("TabStory"), Gold, 1.05f);
        var any = false;
        foreach (var step in Journal.Chapter1.Where(s => s.Done(player)))
        {
            Paragraph(list, "- " + Journal.Text($"Steps.{step.Key}.Done"), Ink, 0.85f);
            any = true;
        }

        if (!any)
            Paragraph(list, Journal.Text("NothingYet"), Faded, 0.85f);
        Paragraph(list, " ", Ink, 0.5f);
        Paragraph(list, Journal.Text("PagesHint"), Faded, 0.75f);
    }

    // ------------------------------------------------------------------ one page
    private void BuildPage(string id)
    {
        var index = Array.IndexOf(Journal.Pages, id);
        var has = Diary.Pages.Contains(id);

        // A found page is written in the old book's ink; a missing one is just a red "???"
        if (has)
        {
            var title = Add(new InkText(Journal.PageTitle(id).Value, OldFont.Title, PageWidth, TitleInk, centered: true));
            title.Left.Pixels = LeftX;
            title.Top.Pixels = PageTop + 4f;
        }
        else
            Centered(Add(Text("???", 0.55f, Missing, true)), LeftX, PageTop);

        var picture = Add(new PagePicture(Tex("Page" + id), has));
        picture.Left.Pixels = LeftX + (PageWidth - 300f) / 2f;
        picture.Top.Pixels = PageTop + 56f;

        var number = Add(Text(Journal.Text("PageNumber", index + 1, Journal.Pages.Length), 0.8f, Faded));
        Centered(number, LeftX, PageTop + 280f);
        if (!has)
        {
            var hint = Add(Text(Journal.Text("PageMissingHint"), 0.75f, Faded));
            Centered(hint, LeftX, PageTop + 306f);
        }

        var list = Column(RightX, PageTop + 6f, Bottom - PageTop - 40f);
        if (has)
            list.Add(new InkText(Journal.PageText(id), OldFont.Text, PageWidth - 20f, PageInk));
        else
            Paragraph(list, Journal.Text("PageMissing"), Faded, 0.9f);

        // Turning the pages
        if (index > 0)
        {
            var prev = Add(Link("<", 1.2f, () => Turn(-1)));
            prev.Left.Pixels = LeftX - 6f;
            prev.Top.Pixels = Bottom - 18f;
        }

        if (index < Journal.Pages.Length - 1)
        {
            var nextLink = Add(Link(">", 1.2f, () => Turn(1)));
            nextLink.Left.Pixels = RightX + PageWidth - 6f;
            nextLink.Top.Pixels = Bottom - 18f;
        }

        var back = Add(Link(Journal.Text("BackToContents"), 0.85f, () =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            _openPage = null;
            Refresh();
        }));
        Centered(back, RightX, Bottom - 16f);
    }

    private void Turn(int direction)
    {
        var index = Array.IndexOf(Journal.Pages, _openPage) + direction;
        if (index < 0 || index >= Journal.Pages.Length)
            return;
        SoundEngine.PlaySound(SoundID.MenuTick);
        _openPage = Journal.Pages[index];
        Refresh();
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Visible || _book == null)
            return;

        var player = Main.LocalPlayer;

        // While the journal is open nothing spawns around the player
        player.AddBuff(ModContent.BuffType<StoryTime>(), 120);

        // Don't use items or click the world through the book
        if (_book.ContainsPoint(Main.MouseScreen))
            player.mouseInterface = true;

        // Esc (or the inventory key) closes it
        if (player.controlInv && player.releaseInventory)
        {
            player.releaseInventory = false;
            Close();
        }
    }

    /// <summary>
    ///     The picture of a page, or only its black silhouette while the page isn't found.
    /// </summary>
    private sealed class PagePicture : UIElement
    {
        private readonly Asset<Texture2D> _texture;
        private readonly bool _found;

        public PagePicture(Asset<Texture2D> texture, bool found)
        {
            _texture = texture;
            _found = found;
            Width.Pixels = 300f;
            Height.Pixels = 210f;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var position = GetDimensions().Position();
            spriteBatch.Draw(_texture.Value, position, _found ? Color.White : Color.Black);
            if (_found)
                return;
            var font = FontAssets.DeathText.Value;
            var size = font.MeasureString("?") * 1.2f;
            Utils.DrawBorderStringBig(spriteBatch, "?", position + new Vector2(150f, 105f) - size / 2f, Missing, 1.2f);
        }
    }

    /// <summary>
    ///     A thin bar that fills with the share of pages found.
    /// </summary>
    private sealed class ProgressBar(float fill) : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var r = GetDimensions().ToRectangle();
            var pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(pixel, r, new Color(20, 20, 24));
            var inner = new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
            spriteBatch.Draw(pixel, inner, new Color(110, 110, 130));
            spriteBatch.Draw(pixel, new Rectangle(inner.X, inner.Y, (int)(inner.Width * fill), inner.Height), Gold);
            spriteBatch.Draw(pixel, new Rectangle(inner.X, inner.Y, (int)(inner.Width * fill), 2), new Color(255, 236, 150));
        }
    }
}
