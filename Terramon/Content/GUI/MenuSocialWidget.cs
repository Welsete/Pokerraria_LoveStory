using ReLogic.Graphics;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader.UI;

namespace Terramon.Content.GUI;

internal static class MenuSocialWidget
{
    private static readonly bool[] LastHoveringInteractableText = new bool[5];

    public static void Setup()
    {
        On_Main.DrawVersionNumber += MainDrawVersionNumber_Detour;
    }

    private static void MainDrawVersionNumber_Detour(On_Main.orig_DrawVersionNumber orig, Color menucolor, float upbump)
    {
        orig(menucolor, upbump);

        // Wait until the mod is loaded by TML
        var mod = Terramon.Instance;
        if (mod == null) return;

        var drawPos = new Vector2(15, 15);
        if (Main.showFrameRate)
            drawPos.Y += 22;
        if (ModLoader.HasMod("TerrariaOverhaul"))
            drawPos.Y = Main.screenHeight / 2f - 74;
        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value,
            $"{mod.DisplayNameClean} v{mod.Version}", drawPos, Color.White, 0f, Vector2.Zero,
            1.07f, SpriteEffects.None, 0f, alphaMult: 0.76f);

        // Draw Mod Config link text
        const string configText = "Mod Config";
        var configTextSize = FontAssets.MouseText.Value.MeasureString(configText);
        configTextSize.Y *= 0.9f;
        drawPos.Y += 30;
        var hoveredConfig = Main.MouseScreen.Between(drawPos, drawPos + configTextSize);
        if (hoveredConfig)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (!LastHoveringInteractableText[4])
                SoundEngine.PlaySound(in SoundID.MenuTick);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                SoundEngine.PlaySound(in SoundID.MenuOpen);
                Main.mouseLeftRelease = false;

                var modConfigList = Interface.modConfigList;
                modConfigList.ModToSelectOnOpen = Terramon.Instance;
                Main.menuMode = Interface.modConfigListID;
            }

            LastHoveringInteractableText[4] = true;
        }
        else
        {
            LastHoveringInteractableText[4] = false;
        }

        DrawOutlinedStringOnMenu(Main.spriteBatch, FontAssets.MouseText.Value, configText, drawPos,
            hoveredConfig ? new Color(237, 246, 255) : new Color(173, 173, 198), 0f, Vector2.Zero, 1.02f,
            SpriteEffects.None, 0f, alphaMult: 0.76f);
    }

    private static void DrawOutlinedStringOnMenu(SpriteBatch spriteBatch, DynamicSpriteFont font, string text,
        Vector2 position, Color drawColor, float rotation, Vector2 origin, float scale, SpriteEffects effects,
        float layerDepth, bool special = false, float alphaMult = 0.3f)
    {
        for (var i = 0; i < 5; i++)
        {
            var color = Color.Black;
            if (i == 4)
            {
                color = drawColor;
                if (special)
                {
                    color.R = (byte)((255 + color.R) / 2);
                    color.G = (byte)((255 + color.R) / 2);
                    color.B = (byte)((255 + color.R) / 2);
                }
            }

            color.A = (byte)(color.A * alphaMult);

            var offX = 0;
            var offY = 0;
            switch (i)
            {
                case 0:
                    offX = -2;
                    break;
                case 1:
                    offX = 2;
                    break;
                case 2:
                    offY = -2;
                    break;
                case 3:
                    offY = 2;
                    break;
            }

            spriteBatch.DrawString(font, text, position + new Vector2(offX, offY), color, rotation, origin, scale,
                effects, layerDepth);
        }
    }

    private static Color BrightenColor(Color color, float brightenFactor)
    {
        return new Color(
            (byte)(color.R + (255 - color.R) * brightenFactor),
            (byte)(color.G + (255 - color.G) * brightenFactor),
            (byte)(color.B + (255 - color.B) * brightenFactor),
            color.A
        );
    }
}