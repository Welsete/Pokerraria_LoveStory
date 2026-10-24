using ReLogic.Content;

namespace Terramon.Content.Menus;

[Autoload(Side = ModSide.Client)]
public class TerramonMenu : ModMenu
{
    // The animated Pokerraria: LoveStory logo (the same animation as the README banner): 114 frames of 418x150 in
    // rows of 6, 70 ms each. It cycles through green, pink and purple, with the heart beating and "LoveStory" waving.
    private const int FrameWidth = 418;
    private const int FrameHeight = 150;
    private const int Columns = 6;
    private const int FrameCount = 114;
    private const double FrameSeconds = 0.07;

    private static readonly Asset<Texture2D> LogoAnimation;

    static TerramonMenu()
    {
        if (Main.dedServ) return;

        LogoAnimation = ModContent.Request<Texture2D>("Terramon/Assets/Misc/MenuLogoAnim");
    }

    public override string DisplayName => "Pokerraria: LoveStory";

    public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TitleTheme");

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation,
        ref float logoScale,
        ref Color drawColor)
    {
        // Offsets the logo draw position a bit for alignment purposes
        logoDrawCenter.Y += 16;

        // Real seconds, so the animation plays at the same speed at any frame rate
        var frame = (int)(Main.GlobalTimeWrappedHourly / FrameSeconds) % FrameCount;
        var source = new Rectangle(frame % Columns * FrameWidth, frame / Columns * FrameHeight, FrameWidth,
            FrameHeight);
        var origin = new Vector2(FrameWidth * 0.5f, FrameHeight * 0.5f);

        spriteBatch.Draw(LogoAnimation.Value, logoDrawCenter, source, drawColor, logoRotation, origin, logoScale,
            SpriteEffects.None, 0f);

        return false;
    }

    /// <summary>
    ///     Spoofs the last selected mod menu and sets the current one to Terramon's mod menu.
    /// </summary>
    public void ForceSwitchToThis()
    {
        MenuLoader.switchToMenu = this;
        MenuLoader.LastSelectedModMenu = FullName;
    }
}
