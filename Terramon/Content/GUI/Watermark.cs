using Terraria.GameContent;

namespace Terramon.Content.GUI;

/// <summary>
///     A faint "Pokerraria: LoveStory vX.Y.Z · Welsete" in the bottom right corner while playing, so
///     screenshots and videos of the mod show where it comes from and which version it is.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class Watermark : ModSystem
{
    public const string Authors = "Welsete";

    public static string Text => $"{Terramon.Instance.DisplayNameClean} v{Terramon.Instance.Version} · {Authors}";

    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        if (Main.gameMenu || Main.hideUI)
            return;

        const float scale = 0.75f;
        var text = Text;
        var size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        var position = new Vector2(Main.screenWidth - size.X - 10f, Main.screenHeight - size.Y - 4f);
        Utils.DrawBorderString(spriteBatch, text, position, Color.White * 0.45f, scale);
    }
}
