using ReLogic.Content;
using ReLogic.Graphics;
using Terramon.Helpers;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terramon.Content.GUI.TurnBased;

public sealed class ParticipantPanel(Func<float> getPixelRatio = null) : UIElement
{
    private readonly float[] _ballSlotXPositions = new float[6];
    private readonly Func<float> _getPixelRatio = getPixelRatio;
    private float _hpVisual;
    private PokemonData _currentMon;
    public PokemonData CurrentMon
    {
        get => _currentMon;
        set
        {
            if (_currentMon == value)
                return;
            _currentMon = value;
            _hpVisual = 0f;
        }
    }
    public bool DrawBallSlots;
    public bool DrawEXPBar;

    /// <summary>
    ///     The team shown in the Poké Ball slots under the panel. Null shows the local player's party; a function that
    ///     returns null (e.g. against a wild Pokémon) hides the slots.
    /// </summary>
    public Func<IReadOnlyList<PokemonData>> BallParty;

    /// <summary>
    ///     The team whose Poké Balls are drawn right now, or null when no slots should be drawn.
    /// </summary>
    private IReadOnlyList<PokemonData> ShownParty
        => !DrawBallSlots ? null : BallParty == null ? TerramonPlayer.LocalPlayer.Party : BallParty();

    static ParticipantPanel()
    {
        HPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/HPBar");
        EXPBar = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/EXPBar");
        GenderIcon = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/Gender");
        BallSlots = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/BallSlots_Simple");
        PanelTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/TurnBased/PlayerPanel_Simple");
    }

    public static Asset<Texture2D> HPBar { get; }
    public static Asset<Texture2D> EXPBar { get; }
    public static Asset<Texture2D> GenderIcon { get; }
    public static Asset<Texture2D> BallSlots { get; }
    public static Asset<Texture2D> PanelTexture { get; }

    /// <summary>
    ///     Use instead of <see cref="UIElement.HAlign" />
    /// </summary>
    public float SideFactor
    {
        get => (HAlign - 0.5f) * 2f;
        set => HAlign = (value + 1f) * 0.5f;
    }

    public void Animate(int ticks)
    {
        const int animationStart = 190;
        const int ticksPerSlot = 6;
        const int ticksForWholeAnimation = ticksPerSlot * 6;
        const int animationEnd = animationStart + ticksForWholeAnimation;
        const float initialPosition = -42f;
        
        if (ticks == animationStart - 25)
            SoundEngine.PlaySound(in TerramonSoundID.BattleStart);

        var party = ShownParty;
        if (party == null || ticks > animationEnd || ticks < animationStart)
            return;

        for (int i = 0; i < _ballSlotXPositions.Length; i++)
        {
            int startTick = animationStart + (i * ticksPerSlot);

            // Only the player's own slots ping, so the sounds don't play twice
            if (ticks == startTick + ticksPerSlot - 1 && BallParty == null)
                SoundEngine.PlaySound(i < party.Count && party[i] != null ? TerramonSoundID.BattlePing : TerramonSoundID.BattlePingEmpty);

            int endTick = startTick + ticksPerSlot;
            float targetXPosition = -8f + (i * 26f);
            float lerp = Utils.GetLerpValue(startTick, endTick, ticks, true);

            _ballSlotXPositions[i] =
                float.Lerp(initialPosition, targetXPosition, Tween.ApplyEasing(Ease.OutBack, lerp));
        }
    }

    public void ResetAnimation() => Array.Fill(_ballSlotXPositions, -42f);
    
    private static readonly Color BackColor = new(10, 9, 22);

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        DynamicSpriteFont bigFont = FontAssets.DeathText.Value;
        DynamicSpriteFont smallFont = FontAssets.MouseText.Value;

        float zoom = _getPixelRatio?.Invoke() ?? 1f;
        var dims = GetDimensions();
        float third = dims.Height * 0.5f;
        float halfThird = third * 0.5f;
        float sideFactor = SideFactor;
        float sideShift = halfThird * sideFactor;
        bool right = sideFactor > 0f;
        float extra = 1.2f;
        // float the = (MathF.Sin((float)Main.timeForVisualEffects * 0.1f) + 1f) * 16f;
        var parallelogramRect = new Rectangle((int)(dims.X - halfThird + sideShift * extra), (int)dims.Y,
            (int)(dims.Width + third * extra), (int)dims.Height);
        DynamicPixelRatioElement.DrawAdjustableParallelogram(spriteBatch, PanelTexture.Value, parallelogramRect,
            Color.White, zoom);

        var monName = CurrentMon?.DisplayName ?? "???";
        var monGender = (int)(CurrentMon?.Gender ?? Gender.Unspecified) - 1;
        var monLevel = CurrentMon?.Level ?? 0;
        var monHpFactor = CurrentMon is null ? 0f : CurrentMon.HP / (float)CurrentMon.MaxHP;

        Vector2 drawTextPosition = new(dims.X + 64f + sideShift, dims.Y + 16f);
        float textDrawScale = 0.7f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monName, drawTextPosition, Color.White, 0f,
            Vector2.Zero, new Vector2(0.7f));

        if (monGender != -1)
        {
            Vector2 drawGenderPosition = drawTextPosition;
            drawGenderPosition.X += bigFont.MeasureString(monName + " ").X * textDrawScale;
            
            // Remove floating points from draw position
            drawGenderPosition = drawGenderPosition.Floor();
            
            spriteBatch.Draw(GenderIcon.Value, drawGenderPosition, GenderIcon.Frame(2, 1, monGender), Color.White);
        }

        string lv = "Lv. ";
        float levelNumberScale = 0.85f;
        Vector2 levelNumberSize = bigFont.MeasureString(monLevel.ToString()) * levelNumberScale;
        Vector2 levelLabelSize = smallFont.MeasureString(lv);
        Vector2 drawLevelPosition = new(dims.X + dims.Width - 64f, dims.Y + levelNumberSize.Y + 6f);
        Vector2 drawLevelLabelPosition = drawLevelPosition;

        drawLevelPosition.Y -= levelNumberSize.Y;
        drawLevelLabelPosition.X -= levelNumberSize.X + 8f;
        drawLevelLabelPosition.Y -= levelLabelSize.Y * 1.4f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, bigFont, monLevel.ToString(), drawLevelPosition,
            Color.White, 0f, Vector2.Zero, new Vector2(levelNumberScale));
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, lv, drawLevelLabelPosition, Color.White, 0f,
            Vector2.Zero, Vector2.One);

        var hpBarHeight = HPBar.Height();

        float hpWidth = dims.Width - 84f;
        float drawHpY = dims.Y + dims.Height - (DrawEXPBar ? 68f : 52f);
        float parallelogramCenterForHp = GetParallelogramCenter(drawHpY + hpBarHeight * 0.5f, in parallelogramRect);
        Vector2 drawHpPosition = new(parallelogramCenterForHp - (hpWidth * 0.5f) - (sideShift * 0.75f), drawHpY);
        
        // Remove floating points from draw position
        drawHpPosition = drawHpPosition.Floor();
        
        if (monHpFactor < _hpVisual)
            Step(ref _hpVisual, monHpFactor, 0.01f);
        else
            _hpVisual = monHpFactor;

        var colorShader = ShaderAssets.FadeToColor.Shader;
        var colorParam = colorShader.Parameters["uColor"];
        colorShader.Parameters["uOpacity"].SetValue(1f);

        colorParam.SetValue(BackColor.ToVector3());

        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth, BackColor,
            zoom, colorShader);
        if (_hpVisual != monHpFactor)
        {
            colorParam.SetValue(Color.Red.ToVector3());
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * _hpVisual,
                Color.Red, zoom, colorShader);
        }
        DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, HPBar.Value, drawHpPosition, hpWidth * monHpFactor,
            Color.White, zoom);

        string hpDisplay = CurrentMon is null ? "??? / ???" : $"HP: {CurrentMon.HP} / {CurrentMon.MaxHP}";
        Vector2 hpDisplaySize = smallFont.MeasureString(hpDisplay);
        Vector2 hpDisplayPosition = drawHpPosition + new Vector2(hpWidth * 0.5f, (hpBarHeight + 8) * 0.5f * zoom) -
                                    hpDisplaySize * 0.5f;

        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, smallFont, hpDisplay, hpDisplayPosition, Color.White,
            0f, Vector2.Zero, Vector2.One);

        if (DrawEXPBar)
        {
            float xDifference = 128f;
            Vector2 expDrawPos = drawHpPosition + new Vector2(xDifference, hpBarHeight);
            float expWidth = hpWidth - xDifference - HPBar.Width() / 3;
            float expFactor = CurrentMon is null ? 0f :
                monLevel == Terramon.MaxPokemonLevel ? 1f :
                CurrentMon.TotalEXP /
                (float)ExperienceLookupTable.GetLevelTotalExp(monLevel + 1, CurrentMon.Schema.GrowthRate);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth, BackColor,
                zoom);
            DynamicPixelRatioElement.DrawAdjustableBar(spriteBatch, EXPBar.Value, expDrawPos, expWidth * expFactor,
                Color.White, zoom);
        }

        DrawStatusBadge(spriteBatch, drawHpPosition + new Vector2(16f, hpBarHeight * zoom), zoom);

        if (ShownParty is { } party)
        {
            Texture2D ballSlots = BallSlots.Value;
            Texture2D bola = Terramon.Instance.Assets.Request<Texture2D>("Assets/Items/PokeBalls/PokeBallMiniItem")
                .Value;
            var slotWidth = ballSlots.Width / 3f * zoom;

            for (int i = _ballSlotXPositions.Length - 1; i >= 0; i--)
            {
                float slotPosition = _ballSlotXPositions[i];
                if (i != 0 && slotPosition == -8)
                    continue;

                // The first slot is the square end at the panel's outer edge; on the right side everything is
                // mirrored so the slots come out from the right edge of the screen
                Rectangle frame = ballSlots.Frame(3, 1, i == 0 ? right ? 2 : 0 : 1);
                Vector2 drawPosition = right
                    ? new(dims.X + dims.Width - slotPosition - slotWidth, dims.Y + dims.Height)
                    : new(dims.X + slotPosition, dims.Y + dims.Height);

                spriteBatch.Draw(ballSlots, drawPosition, frame, Color.White, 0f, Vector2.Zero, zoom,
                    SpriteEffects.None, 0f);

                var mon = i < party.Count ? party[i] : null;
                if (mon is null)
                    continue;

                // Knocked out Pokémon get a grey ball, like in the main games
                var fainted = mon.HP == 0 || mon.Status == NonVolatileStatus.Fnt;
                drawPosition.X += 12f;
                drawPosition.Y += 6f;
                spriteBatch.Draw(bola, drawPosition, null, fainted ? new Color(70, 70, 80) : Color.White, 0f,
                    Vector2.Zero, zoom, SpriteEffects.None, 0f);
            }
        }
        // spriteBatch.Draw(TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.White * 0.5f);
    }

    /// <summary>
    ///     A short colored tag for the status condition of <see cref="CurrentMon" /> (SLP, PSN, BRN...), drawn under
    ///     the HP bar like in the main games. Nothing is drawn for a healthy Pokémon.
    /// </summary>
    private void DrawStatusBadge(SpriteBatch spriteBatch, Vector2 position, float zoom)
    {
        if (CurrentMon is not { } mon)
            return;
        var status = mon.HP == 0 ? NonVolatileStatus.Fnt : mon.Status;
        if (status == NonVolatileStatus.None)
            return;
        var key = $"Mods.Terramon.GUI.BattleLog.StatusShort.{status}";
        if (!Language.Exists(key))
            return;

        var label = Language.GetTextValue(key);
        var color = StatusVisuals.ColorOf(status) ?? new Color(200, 70, 70);
        const float textScale = 0.75f;
        var font = FontAssets.MouseText.Value;
        var textSize = font.MeasureString(label) * textScale;
        var box = new Rectangle((int)position.X, (int)position.Y, (int)(textSize.X + 14f * zoom),
            (int)(16f * zoom));
        var pixel = TextureAssets.MagicPixel.Value;
        var source = new Rectangle(0, 0, 1, 1);

        // A pill: dark outline, the status color, and a lighter top half
        spriteBatch.Draw(pixel, box with { X = box.X + 1, Width = box.Width - 2 }, source, BackColor);
        spriteBatch.Draw(pixel, box with { Y = box.Y + 1, Height = box.Height - 2 }, source, BackColor);
        var inner = new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2);
        var fill = Color.Lerp(color, Color.Black, 0.35f);
        spriteBatch.Draw(pixel, inner with { X = inner.X + 1, Width = inner.Width - 2 }, source, fill);
        spriteBatch.Draw(pixel, inner with { Y = inner.Y + 1, Height = inner.Height - 2 }, source, fill);
        spriteBatch.Draw(pixel, new Rectangle(inner.X + 2, inner.Y + 1, inner.Width - 4, inner.Height / 2 - 1),
            source, Color.Lerp(color, Color.Black, 0.15f));

        var textPosition = new Vector2(box.Center.X, box.Center.Y + 2f * textScale);
        Utils.DrawBorderString(spriteBatch, label, textPosition, Color.White, textScale, 0.5f, 0.5f);
    }

    public static float GetParallelogramCenter(float y, in Rectangle bounds)
    {
        if (y < bounds.Y || y > bounds.Y + bounds.Height)
            return y;
        y -= bounds.Y; // makes it relative, so we don't need to use bounds.Y or bounds.X anymore
        y /= bounds.Height; // makes it range from 0f - 1f
        float spaceAtTop = bounds.Height * 0.5f;
        float space = spaceAtTop * (1f - y);
        return bounds.X + space + ((bounds.Width - spaceAtTop) * 0.5f);
    }

    public static void Step(ref float f, float target, float step)
    {
        if (f > target)
            f = Math.Max(f - step, target);
        else if (f < target)
            f = Math.Min(f + step, target);
    }
}