using ReLogic.Content;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Town;

/// <summary>
///     The healing machine on Nurse Joy's counter, to her right, like in the games: it's always there, and when she heals
///     the party its Poké Balls go into the slots one by one, glow while the healing jingle plays, and then she hands
///     them back. Only the local player sees their own heal.
/// </summary>
public class HealingMachine : ModSystem
{
    private const int FirstBallTime = 12;
    private const int BallInterval = 14;
    private const int PopTime = 10;
    private const int GlowTime = 152; // The length of the jingle
    private const int HoldTime = 30;
    private const int FadeTime = 12;

    private static readonly Color HealPink = new(255, 120, 190);

    private static Asset<Texture2D> _glow;

    private static readonly List<BallID> Balls = [];
    private static int _joy = -1;
    private static int _age;
    private static bool _healed;

    /// <summary>
    ///     Whether the machine is healing the local player's party right now.
    /// </summary>
    public static bool Busy => _joy >= 0;

    private static int GlowStart => FirstBallTime + (Balls.Count - 1) * BallInterval + PopTime + 14;
    private static int EndTime => GlowStart + GlowTime + HoldTime + FadeTime;

    public override void Load()
    {
        if (!Main.dedServ)
            _glow = Mod.Assets.Request<Texture2D>("Assets/Misc/Evolution/Glow");
    }

    public override void Unload() => _glow = null;

    public override void OnWorldUnload() => Stop();

    private static void Stop()
    {
        Balls.Clear();
        _joy = -1;
    }

    /// <summary>
    ///     Puts the local player's party in Joy's machine, and heals it when the balls light up.
    /// </summary>
    public static void Start(NPC joy)
    {
        var player = Main.LocalPlayer;
        Balls.Clear();
        foreach (var data in player.Terramon().Party)
        {
            if (data != null)
                Balls.Add(data.Ball);
        }

        if (Balls.Count == 0)
            return;

        _joy = joy.whoAmI;
        _age = 0;
        _healed = false;
    }

    public override void PostUpdateEverything()
    {
        if (!Busy || Main.dedServ)
            return;

        var joy = Main.npc[_joy];
        if (!joy.active || joy.type != ModContent.NPCType<NurseJoy>())
        {
            Heal();
            Stop();
            return;
        }

        _age++;
        for (var i = 0; i < Balls.Count; i++)
        {
            if (_age == FirstBallTime + i * BallInterval)
                SoundEngine.PlaySound(TerramonSoundID.CatchClick with { Volume = 0.45f, Pitch = -0.2f + i * 0.08f },
                    Machine(joy));
        }

        if (_age == GlowStart)
        {
            SoundEngine.PlaySound(TerramonSoundID.PokeCenterHeal);
            Heal();
        }

        if (_age >= GlowStart && _age < GlowStart + GlowTime)
        {
            var light = Glow(_age - GlowStart);
            Lighting.AddLight(Machine(joy) - new Vector2(0f, 14f), 0.9f * light, 0.35f * light, 0.6f * light);
        }

        if (_age < EndTime)
            return;

        var player = Main.LocalPlayer;
        if (player.talkNPC == _joy)
            Main.npcChatText = Language.GetTextValue("Mods.Terramon.NPCs.NurseJoy.Dialogue.Healed");
        Stop();
    }

    private static void Heal()
    {
        if (_healed)
            return;
        _healed = true;
        Main.LocalPlayer.GetModPlayer<PartyHealingPlayer>().HealParty(false);
    }

    // Middle of the machine's bottom edge: on the counter (or table) to Joy's right, on top of whatever stands there
    private static Vector2 Machine(NPC joy)
    {
        var x = joy.Bottom.X + 64f;
        var column = (int)(x / 16f);
        var row = (int)(joy.Bottom.Y / 16f) - 1;
        var surface = joy.Bottom.Y - 32f;
        for (var y = row; y > row - 4 && WorldGen.InWorld(column, y); y--)
        {
            var tile = Main.tile[column, y];
            if (!tile.HasTile)
            {
                // Empty right next to the floor: keep looking up for a counter top
                if (y >= row - 1)
                    continue;
                break;
            }

            surface = y * 16f + (tile.IsHalfBlock ? 8f : 0f);
        }

        return new Vector2(x, surface);
    }

    // How bright the balls are during the glow: three pulses that fade out
    private static float Glow(int t)
    {
        var progress = t / (float)GlowTime;
        var pulse = 0.5f + 0.5f * MathF.Sin(progress * MathHelper.TwoPi * 3f - MathHelper.PiOver2);
        return pulse * (1f - progress * 0.4f);
    }

    // Where a ball sits, from the machine's bottom middle: three slots in the back row, three in the front
    private static Vector2 Slot(int index) => new((-17f + index % 3 * 11f) * Scale, (index < 3 ? -19f : -12f) * Scale);

    // The machine and the balls are drawn this much bigger than their 2-pixel design
    private const float Scale = 1.5f;
    private const float BallScale = 0.75f * Scale;

    public override void PostDrawTiles()
    {
        var joy = Busy ? Main.npc[_joy] : null;
        if (joy == null)
        {
            var type = ModContent.NPCType<NurseJoy>();
            var index = NPC.FindFirstNPC(type);
            if (index < 0)
                return;
            joy = Main.npc[index];
        }

        if (!joy.active)
            return;

        var origin = Machine(joy);
        if (!new Rectangle((int)Main.screenPosition.X - 64, (int)Main.screenPosition.Y - 64, Main.screenWidth + 128,
                Main.screenHeight + 128).Contains(origin.ToPoint()))
            return;

        var spriteBatch = Main.spriteBatch;
        var transform = Main.GameViewMatrix.TransformationMatrix;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        // A little lower, so its dark base overlaps the counter's top edge instead of floating over it
        var bottom = (origin - Main.screenPosition).Floor() + new Vector2(0f, 6f);
        var shade = Lighting.GetColor(origin.ToTileCoordinates() - new Point(0, 1));
        var glowing = Busy && _age >= GlowStart && _age < GlowStart + GlowTime + HoldTime;
        DrawMachine(spriteBatch, bottom, shade, glowing ? Glow(Math.Min(_age - GlowStart, GlowTime - 1)) : 0f);

        if (Busy)
        {
            var fade = 1f - MathHelper.Clamp((_age - (EndTime - FadeTime)) / (float)FadeTime, 0f, 1f);
            var ballShade = Color.Lerp(shade, Color.White, 0.35f) * fade;
            for (var i = 0; i < Balls.Count; i++)
            {
                var since = _age - (FirstBallTime + i * BallInterval);
                if (since < 0)
                    continue;

                var pop = MathHelper.Clamp(since / (float)PopTime, 0f, 1f);
                var position = bottom + Slot(i) + new Vector2(0f, (1f - pop) * -8f * Scale);
                spriteBatch.Draw(BallTexture(Balls[i]), position.Floor(), null, ballShade * pop, 0f,
                    new Vector2(8f, 16f), BallScale, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.End();

        if (!glowing)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);
        var t = _age - GlowStart;
        var glow = t < GlowTime ? Glow(t) : 0.15f * (1f - (t - GlowTime) / (float)HoldTime);
        for (var i = 0; i < Balls.Count; i++)
        {
            // Each ball lights a little after the one before it, so the light runs along the slots
            var wave = 0.75f + 0.25f * MathF.Sin(t * 0.3f - i * 0.9f);
            var position = bottom + Slot(i);
            spriteBatch.Draw(_glow.Value, position - new Vector2(0f, 6f * Scale), null, HealPink * (0.55f * glow * wave), 0f,
                _glow.Size() / 2f, 0.22f * Scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(BallTexture(Balls[i]), position.Floor(), null, Color.White * (0.6f * glow * wave), 0f,
                new Vector2(8f, 16f), BallScale, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    // The machine, drawn in 2-pixel blocks like the tiles: a white body with two rows of ball slots, a pink stripe and
    // a dark base, and a small monitor on the right that lights up green while it heals
    private static void DrawMachine(SpriteBatch spriteBatch, Vector2 bottom, Color shade, float glow)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        var outline = new Color(58, 62, 74).MultiplyRGB(shade);
        void Box(int x, int y, int w, int h, Color color) => spriteBatch.Draw(pixel,
            new Rectangle((int)(bottom.X + x * Scale), (int)(bottom.Y + y * Scale), (int)(w * Scale),
                (int)(h * Scale)), color);

        // Monitor on its stand
        Box(13, -40, 16, 14, outline);
        var screen = Color.Lerp(new Color(40, 70, 80), new Color(120, 255, 170), glow);
        Box(15, -38, 12, 10, screen.MultiplyRGB(Color.Lerp(shade, Color.White, glow)));
        Box(17, -34, 2, 2, new Color(255, 140, 180).MultiplyRGB(shade)); // a little heart on the screen
        Box(19, -34, 2, 2, new Color(255, 140, 180).MultiplyRGB(shade));
        Box(18, -32, 2, 2, new Color(255, 140, 180).MultiplyRGB(shade));
        Box(20, -26, 2, 4, outline);

        // Body
        Box(-26, -22, 52, 22, outline);
        Box(-24, -20, 48, 18, new Color(236, 238, 244).MultiplyRGB(shade));
        Box(-24, -20, 48, 2, new Color(255, 255, 255).MultiplyRGB(shade));
        Box(-24, -8, 48, 3, new Color(232, 90, 140).MultiplyRGB(shade));
        Box(-26, -2, 52, 2, new Color(40, 42, 52).MultiplyRGB(shade));

        // Slots, darker where a ball isn't sitting
        var hole = new Color(110, 116, 130).MultiplyRGB(shade);
        for (var i = 0; i < 6; i++)
        {
            var slot = Slot(i);
            Box((int)(slot.X / Scale) - 5, (int)(slot.Y / Scale) - 1, 10, 3, hole);
        }

        // A green light on the front that blinks while it heals
        Box(16, -16, 4, 4, outline);
        Box(17, -15, 2, 2, (glow > 0.5f ? new Color(120, 255, 140) : new Color(60, 120, 70)).MultiplyRGB(shade));
    }

    private static Texture2D BallTexture(BallID ball)
    {
        var path = $"Terramon/Assets/Items/PokeBalls/{ball}MiniItem";
        if (!ModContent.HasAsset(path))
            path = "Terramon/Assets/Items/PokeBalls/PokeBallMiniItem";
        return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
    }
}
