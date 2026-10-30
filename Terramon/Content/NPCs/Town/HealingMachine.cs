using ReLogic.Content;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terramon.Content.NPCs.Town;

/// <summary>
///     The healing machine on Nurse Joy's counter, like in the games: the party's Poké Balls go onto a tray one by one,
///     glow while they heal, and then Joy hands them back. Only the local player sees their own heal.
/// </summary>
public class HealingMachine : ModSystem
{
    private const int FirstBallTime = 12;
    private const int BallInterval = 14;
    private const int PopTime = 10;
    private const int GlowTime = 100;
    private const int HoldTime = 30;
    private const int FadeTime = 12;

    private static readonly Color HealPink = new(255, 120, 190);

    private static Asset<Texture2D> _glow;

    private static readonly List<BallID> Balls = [];
    private static int _joy = -1;
    private static int _side = 1;
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
    ///     Puts the local player's party on Joy's counter, on the side the player is standing, and heals it when the
    ///     balls light up.
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
        _side = player.Center.X >= joy.Center.X ? 1 : -1;
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
                    Tray(joy));
        }

        if (_age == GlowStart)
            Heal();

        if (_age >= GlowStart && _age < GlowStart + GlowTime)
        {
            var light = Glow(_age - GlowStart);
            Lighting.AddLight(Tray(joy) - new Vector2(0f, 8f), 0.9f * light, 0.35f * light, 0.6f * light);
        }

        if (_age == GlowStart + GlowTime)
            SoundEngine.PlaySound(TerramonSoundID.ShinySparkle with { Volume = 0.45f }, Tray(joy));

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
        Main.LocalPlayer.GetModPlayer<PartyHealingPlayer>().HealParty();
    }

    // Middle of the tray's bottom edge, on the counter or table beside Joy: the top of whatever stands there
    private static Vector2 Tray(NPC joy)
    {
        var x = joy.Bottom.X + _side * 56f;
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

    private static Vector2 Slot(int index)
    {
        // Two rows of three, like the machine's tray
        var column = index % 3;
        var row = index / 3;
        return new Vector2((column - 1) * 12f, -10f - row * 9f);
    }

    public override void PostDrawTiles()
    {
        if (!Busy)
            return;

        var joy = Main.npc[_joy];
        if (!joy.active)
            return;

        var fade = 1f - MathHelper.Clamp((_age - (EndTime - FadeTime)) / (float)FadeTime, 0f, 1f);
        var tray = Tray(joy) - Main.screenPosition;
        var spriteBatch = Main.spriteBatch;
        var transform = Main.GameViewMatrix.TransformationMatrix;
        var pixel = TextureAssets.MagicPixel.Value;
        var light = Lighting.GetColor(joy.Bottom.ToTileCoordinates());
        var shade = Color.Lerp(light, Color.White, 0.35f) * fade;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);

        // The machine: a grey base with a pink top where the balls sit
        var width = 40;
        var height = Balls.Count > 3 ? 22 : 13;
        var left = (int)(tray.X - width / 2f);
        var top = (int)tray.Y - height;
        spriteBatch.Draw(pixel, new Rectangle(left, top, width, height), new Color(70, 74, 86).MultiplyRGB(shade));
        spriteBatch.Draw(pixel, new Rectangle(left + 1, top + 1, width - 2, height - 4),
            new Color(232, 236, 242).MultiplyRGB(shade));
        spriteBatch.Draw(pixel, new Rectangle(left + 1, top + height - 4, width - 2, 2),
            new Color(236, 96, 140).MultiplyRGB(shade));

        for (var i = 0; i < Balls.Count; i++)
        {
            var since = _age - (FirstBallTime + i * BallInterval);
            if (since < 0)
                continue;

            var pop = MathHelper.Clamp(since / (float)PopTime, 0f, 1f);
            var drop = (1f - pop) * -6f;
            var texture = BallTexture(Balls[i]);
            var position = tray + Slot(i) + new Vector2(0f, drop);
            spriteBatch.Draw(texture, position.Floor(), null, shade * pop, 0f, texture.Size() / 2f, 0.75f,
                SpriteEffects.None, 0f);
        }

        spriteBatch.End();

        if (_age >= GlowStart && _age < GlowStart + GlowTime + HoldTime)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, transform);
            var t = _age - GlowStart;
            var glow = t < GlowTime ? Glow(t) : 0.15f * (1f - (t - GlowTime) / (float)HoldTime);
            for (var i = 0; i < Balls.Count; i++)
            {
                // Each ball lights a little after the one before it, so the light runs along the tray
                var wave = 0.75f + 0.25f * MathF.Sin(t * 0.3f - i * 0.9f);
                var position = tray + Slot(i);
                spriteBatch.Draw(_glow.Value, position, null, HealPink * (0.55f * glow * wave), 0f,
                    _glow.Size() / 2f, 0.22f, SpriteEffects.None, 0f);
                spriteBatch.Draw(BallTexture(Balls[i]), position.Floor(), null, Color.White * (0.6f * glow * wave),
                    0f, new Vector2(8f), 0.75f, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
        }
    }

    private static Texture2D BallTexture(BallID ball)
    {
        var path = $"Terramon/Assets/Items/PokeBalls/{ball}MiniItem";
        if (!ModContent.HasAsset(path))
            path = "Terramon/Assets/Items/PokeBalls/PokeBallMiniItem";
        return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
    }
}
