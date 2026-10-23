using System.Reflection;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Buffs;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Core.Abstractions;
using Terramon.Core.Battling;
using Terramon.Core.Loaders;
using Terramon.Core.ProjectileComponents;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;

namespace Terramon.Content.Projectiles;

[Autoload(false)]
public sealed class PokemonPet(ushort id, DatabaseV2.PokemonSchema schema) : ModProjectile, IPokemonEntity
{
    public delegate void CustomFindFrame(PokemonPet proj);

    public const float DistanceFromFoe = 128f;

    private static readonly Asset<Texture2D> ResourceBarStartTexture;
    private static readonly Asset<Texture2D> ResourceBarMiddleTexture;
    private static readonly Asset<Texture2D> ResourceBarEndTexture;

    private static readonly HashSet<int> PetsNeedingHPBars = [];
    private int _activeAttackTimer;
    private int _attackCooldown = 60;

    private Vector2? _attackDirection;

    private ushort _cachedID;
    private int _cryTimer;

    private int _lastHPState = -1;
    private Asset<Texture2D> _mainTexture;
    private bool _regeneratingHealth;
    private int _regenStartTarget;
    private int _regenStartTimer;
    private float _regenTimer;
    private int _shinySparkleTimer;

    private NPC _target;

    private int _visualImmunityFrames;
    public int CustomFrameCounter;
    public int? CustomSpriteDirection;
    public Vector2? CustomTargetPosition;
    public CustomFindFrame FindFrame;

    /// <summary>Whether this pet flies around (<see cref="ProjectileFlyingPet" />) instead of walking.</summary>
    public bool Flying;

    // Coming out of the ball, like in the games: the ball flies from the trainer to the spot and pops open, then the
    // Pokémon grows out of a white flash. Counted on every client from when the pet appears there, so other players
    // see it too (pets are synced on their own).
    private const int BallFlight = 14;
    private const int GrowTime = 20;
    private int _sendOutTime;
    private bool _skipSendOut;
    private Vector2 _ballStart;

    // The last frame drawn, for the recall effect once the pet is gone (see PokeBallVisuals)
    private Texture2D _lastTexture;
    private Rectangle _lastSource;
    private Vector2 _lastOrigin;
    private float _lastScale;
    private SpriteEffects _lastEffects;
    private Vector2 _lastFeet;

    static PokemonPet()
    {
        // Don't run this on the server
        if (Main.dedServ) return;

        // Load resource bar textures
        ResourceBarStartTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarStart");
        ResourceBarMiddleTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarMiddle");
        ResourceBarEndTexture = ModContent.Request<Texture2D>("Terramon/Assets/GUI/Realtime/ResourceBarEnd");

        // Draw HP bars on proper layer
        On_Main.DrawInterface_14_EntityHealthBars += (orig, self) =>
        {
            orig(self);

            // Draw HP bars for all marked pets
            foreach (var projIndex in PetsNeedingHPBars)
            {
                var proj = Main.projectile[projIndex];
                if (!proj.active || proj.ModProjectile is not PokemonPet pet) continue;

                DrawHealthBar(pet, proj);
            }

            // Clear the collection for next frame
            PetsNeedingHPBars.Clear();
        };

        // Draw the player's active Pokémon pet in the character select UI
        On_UICharacter.ctor += (orig, self, player, animated, panel, scale, clone) =>
        {
            orig(self, player, animated, panel, scale, clone);

            // Get the pet projectiles from the UICharacter
            ref var petProjectiles = ref self._petProjectiles;
            if (petProjectiles.Length > 0) return;

            var modPlayer = player.Terramon();
            var activePokemon = modPlayer.GetActivePokemon();

            if (activePokemon == null) return;

            // Create a dummy pet projectile and assign it to the UICharacter
            var projectile = new Projectile();
            projectile.SetDefaults(PokemonEntityLoader.IDToPetType[activePokemon.ID]);
            projectile.isAPreviewDummy = true;
            ((PokemonPet)projectile.ModProjectile).Data =
                activePokemon; // Set the pet's data to the player's active Pokémon (hacky)
            petProjectiles = [projectile];
        };
    }


    protected override bool CloneNewInstances => true;

    public override string Name { get; } = schema.Identifier + "Pet";

    public override LocalizedText DisplayName => DatabaseV2.GetLocalizedPokemonName(Schema);

    public override string Texture { get; } = "Terramon/Assets/Pokemon/" + schema.Identifier;

    public ushort ID { get; } = id;

    public DatabaseV2.PokemonSchema Schema { get; } = schema;

    public PokemonData Data { get; set; }

    public override void SetStaticDefaults()
    {
        Main.projPet[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        foreach (var component in PokemonEntityLoader.PetSchemaCache[ID].Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.Projectiles.Projectile{component.Name}");
            if (componentType == null)
                // Remove the component from the schema if it doesn't exist.
                // _schemaCache[ID].First(x => x.Path == component.Path).Remove();
                continue;
            var enableComponentRef =
                ProjectileComponentExtensions.EnableComponentMethod.MakeGenericMethod(componentType);
            var instancedComponent = enableComponentRef.Invoke(null, [Projectile, null]);
            foreach (var prop in component.Value.Children<JProperty>())
            {
                var fieldInfo = componentType.GetRuntimeField(prop.Name);
                if (fieldInfo == null) continue;
                fieldInfo.SetValue(instancedComponent, prop.Value.ToObject(fieldInfo.FieldType));
            }
        }
    }

    public override void OnSpawn(IEntitySource source)
    {
        var owningPlayer = Main.player[Projectile.owner];
        var modPlayer = owningPlayer.Terramon();

        // Move ahead of player
        var direction = owningPlayer.direction;
        CustomSpriteDirection = direction;
        Projectile.position.X += direction * (MathF.Abs(Main.player[Projectile.owner].velocity.X) < 1.5f ? 40 : -30);

        // The dust cloud comes when its ball pops open (see UpdateSendOut)
        ConfrontFoe(modPlayer.BattleClient);

        Data = modPlayer.GetActivePokemon();
        modPlayer.ActivePetProjectile = this;
        _cachedID = ID;
    }

    /// <summary>
    ///     Who battle animations (attacks, getting hurt) are played on for this pet: its owner, since that's who battles.
    /// </summary>
    private object AnimOwner => Projectile.isAPreviewDummy
        ? this
        : Main.player[Projectile.owner].GetModPlayer<TerramonPlayer>();

    public override bool PreDraw(ref Color lightColor)
    {
        lightColor = StatusVisuals.Tint(Data?.Status ?? NonVolatileStatus.None, lightColor);
        _mainTexture ??= PokemonEntityLoader.RequestTexture(this);

        if (Projectile.isAPreviewDummy)
            // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
            FindFrame?.Invoke(this);

        if (InBallFlight)
        {
            DrawBallFlight();
            return false;
        }

        if (_visualImmunityFrames > 0 && _visualImmunityFrames % 6 < 3)
            return false; // Skip draw to create flashing

        var projFrameCount = Main.projFrames[Type];
        var drawPos = Projectile.Center - Main.screenPosition +
                      new Vector2(0f,
                          Projectile.gfxOffY + DrawOriginOffsetY + (int)Math.Ceiling(Projectile.height / 2f) + 4);

        // Apply attack animation offset
        if (_activeAttackTimer > 0)
        {
            _attackDirection ??= _target == null ? Vector2.Zero : Vector2.Normalize(_target.Center - Projectile.Center);

            var normalizedTime = (16 - _activeAttackTimer) / 16f;
            float offsetIntensity;

            if (normalizedTime < 0.3f)
            {
                // Fast attack movement
                var attackTime = normalizedTime / 0.3f;
                offsetIntensity = (float)Math.Sin(attackTime * Math.PI * 0.5f);
            }
            else
            {
                // Slow return with easing
                var returnTime = (normalizedTime - 0.3f) / 0.7f;
                offsetIntensity = (float)Math.Cos(returnTime * Math.PI * 0.5f);
            }

            const float maxOffset = 16f;

            var attackOffset = _attackDirection.Value * (offsetIntensity * maxOffset);
            drawPos += attackOffset;
        }
        else
        {
            _attackDirection = null;
        }

        var sourceRect = _mainTexture.Frame(1, projFrameCount, frameY: Projectile.frame);
        var frameSize = sourceRect.Size();
        var origin = frameSize / new Vector2(2, 1);
        var texture = _mainTexture.Value;

        // Pokémon with a full animation set pick their frame from what they're doing instead
        var animSet = PokemonAnimations.Get(Schema.Identifier, Data);
        var shiny = Data?.IsShiny ?? false;
        var shownID = ID;

        // Transformed in battle (Transform or Imposter): looks like the Pokémon it copied, from its animation set or,
        // for Pokémon without one, from the first frame of its sprite sheet
        var transformedSheet = false;
        if (!Projectile.isAPreviewDummy && Projectile.owner is >= 0 and < Main.maxPlayers &&
            BattleSide.TryGetTransformed(Main.player[Projectile.owner].Terramon(), out var transformedInto,
                out var transformedShiny))
        {
            if (PokemonAnimations.Get(Terramon.DatabaseV2.GetPokemon(transformedInto)?.Identifier) is
                { } transformedSet)
            {
                animSet = transformedSet;
                shiny = transformedShiny;
                shownID = transformedInto;
            }
            else if (PokemonAnimations.TryGetSheetFrame(transformedInto, transformedShiny, out var sheet,
                         out var sheetFrame))
            {
                animSet = null;
                shownID = transformedInto;
                transformedSheet = true;
                texture = sheet;
                sourceRect = sheetFrame;
                frameSize = sheetFrame.Size();
                origin = frameSize / new Vector2(2, 1);
            }
        }

        if (animSet != null)
        {
            var battling = AnimOwner is TerramonPlayer { BattleClient.Foe: not null };
            PokemonAnimations.GetFrame(animSet, AnimOwner, shiny,
                Math.Abs(Projectile.velocity.X) > 0.2f, !battling && Math.Abs(Projectile.velocity.Y) > 1f, battling,
                Data is { Status: NonVolatileStatus.Slp }, Data is { HP: 0 }, out texture, out sourceRect,
                out origin, Flying);
            frameSize = sourceRect.Size() * animSet.Scale;
            drawPos.Y -= 3f;
        }
        var effects = CustomSpriteDirection.HasValue
            ? CustomSpriteDirection.Value == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None
            : Projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

        // Draw name on mouse hover
        if (!Projectile.isAPreviewDummy && ClientConfig.Instance.ShowPetNameOnHover)
        {
            var originOffsetDrawPos = drawPos - origin * (animSet?.Scale ?? 1f);
            var drawRect = new Rectangle((int)originOffsetDrawPos.X, (int)originOffsetDrawPos.Y, (int)frameSize.X,
                (int)frameSize.Y);
            if (drawRect.Contains(Main.mouseX, Main.mouseY))
            {
                var mouseTextMult = Main.mouseTextColor / 255f;
                var subColor = new Color((byte)(182f * mouseTextMult), (byte)(187f * mouseTextMult),
                    (byte)(203f * mouseTextMult));
                Main.instance.MouseText(
                    $"{Data.DisplayName}: {Data.HP}/{Data.MaxHP}\n[c/{subColor.ToHexString()}:Following]");
                Main.LocalPlayer.cursorItemIconEnabled = false;
                Main.mouseText = true;
            }
        }

        // Desaturate the lightColor for Gastly
        var adjustedColor = shownID == NationalDexID.Gastly
            ? GrayscaleColor(lightColor)
            : lightColor;

        // Growing out of the ball's flash: smaller and white at first
        var grow = SendOutProgress;
        var fullScale = Projectile.scale * (animSet?.Scale ?? 1f);
        var drawScale = fullScale * (0.25f + 0.75f * (1f - (1f - grow) * (1f - grow)));
        Main.EntitySpriteDraw(texture,
            drawPos,
            sourceRect, adjustedColor,
            Projectile.rotation,
            origin, drawScale, effects);

        if (grow < 1f)
            DrawWhiteOverlay(texture, drawPos, sourceRect, origin, drawScale, effects, 1f - grow);

        if (!Projectile.isAPreviewDummy)
        {
            _lastTexture = texture;
            _lastSource = sourceRect;
            _lastOrigin = origin;
            _lastScale = fullScale;
            _lastEffects = effects;
            _lastFeet = drawPos + Main.screenPosition;
        }

        var glowCache = Data?.IsShiny ?? false
            ? PokemonEntityLoader.ShinyGlowTextureCache
            : PokemonEntityLoader.GlowTextureCache;
        if (animSet != null || transformedSheet || !glowCache.TryGetValue(ID, out var glowTexture)) return false;

        // Draw the glowmask texture for the pet
        lightColor = Color.White;
        if (ID == NationalDexID.Gastly) lightColor.A = 128;
        Main.EntitySpriteDraw(glowTexture.Value,
            drawPos,
            sourceRect, lightColor,
            Projectile.rotation,
            origin, drawScale, effects);

        return false;
    }

    /// <summary>Whether the ball is still flying out to where this pet comes out.</summary>
    private bool InBallFlight => !Projectile.isAPreviewDummy && !_skipSendOut && _sendOutTime < BallFlight;

    /// <summary>0 when the ball pops open, 1 once the Pokémon is all the way out.</summary>
    private float SendOutProgress => Projectile.isAPreviewDummy || _skipSendOut
        ? 1f
        : Math.Clamp((_sendOutTime - BallFlight) / (float)GrowTime, 0f, 1f);

    /// <summary>
    ///     The ball in the air, from the trainer to the pet's feet in an arc, spinning.
    /// </summary>
    private void DrawBallFlight()
    {
        var ball = BallAssets.GetBallIcon(Data?.Ball ?? BallID.PokeBall)?.Value;
        if (ball == null)
            return;
        var t = _sendOutTime / (float)BallFlight;
        var end = Projectile.Bottom + new Vector2(0f, Projectile.gfxOffY - 8f);
        var position = Vector2.Lerp(_ballStart, end, t) - new Vector2(0f, MathF.Sin(t * MathHelper.Pi) * 36f);
        var light = Lighting.GetColor((int)(position.X / 16f), (int)(position.Y / 16f));
        Main.EntitySpriteDraw(ball, position - Main.screenPosition, null, Color.Lerp(light, Color.White, 0.5f),
            t * MathHelper.TwoPi * 1.5f * -Main.player[Projectile.owner].direction, ball.Size() / 2f, 1.25f,
            SpriteEffects.None);
    }

    /// <summary>
    ///     Draws the frame again as a flat silhouette of <paramref name="color" /> (white coming out, red going back).
    /// </summary>
    internal static void DrawSilhouette(Texture2D texture, Vector2 position, Rectangle source, Vector2 origin,
        float scale, SpriteEffects effects, Color color, float opacity, Matrix transform)
    {
        var spriteBatch = Main.spriteBatch;
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);
        ShaderAssets.FadeToColor.UseColor(color).UseOpacity(opacity).Apply();
        spriteBatch.Draw(texture, position, source, Color.White, 0f, origin, scale, effects, 0f);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, transform);
    }

    private void DrawWhiteOverlay(Texture2D texture, Vector2 drawPos, Rectangle source, Vector2 origin, float scale,
        SpriteEffects effects, float opacity)
    {
        if (Projectile.isAPreviewDummy || Main.dedServ)
            return;
        DrawSilhouette(texture, drawPos, source, origin, scale, effects, Color.White, opacity,
            Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    ///     Advances the send-out on this client and pops the ball open when it lands.
    /// </summary>
    private void UpdateSendOut(Player owner)
    {
        if (Projectile.isAPreviewDummy || _sendOutTime >= BallFlight + GrowTime)
            return;

        if (_sendOutTime == 0)
        {
            _ballStart = owner.MountedCenter + new Vector2(owner.direction * 8f, -6f);
            // An evolution swaps the pet for its new form: it doesn't go back into its ball for that
            _skipSendOut = PokeBallVisuals.IsEvolving(Projectile.owner);
        }

        _sendOutTime++;
        if (_sendOutTime == BallFlight && !_skipSendOut && !Main.dedServ)
            PokeBallVisuals.PopOpen(Projectile.Bottom + new Vector2(0f, -8f), Data?.Ball ?? BallID.PokeBall);
    }

    public override void OnKill(int timeLeft)
    {
        // Going back into its ball (not for the preview in the character menu, or an evolution swapping forms)
        if (Main.dedServ || Projectile.isAPreviewDummy || _lastTexture == null ||
            PokeBallVisuals.IsEvolving(Projectile.owner))
            return;
        PokeBallVisuals.AddRecall(Projectile.owner, _lastTexture, _lastSource, _lastOrigin, _lastScale, _lastEffects,
            _lastFeet, Projectile.owner != Main.myPlayer);
    }

    public override void PostDraw(Color lightColor)
    {
        if (InBallFlight)
            return;
        if (!Projectile.isAPreviewDummy && Data is { Status: NonVolatileStatus.Slp })
            StatusVisuals.DrawSleepZ(Main.spriteBatch, Projectile.Top, Main.screenPosition, Projectile.whoAmI);

        // Marks this pet for HP bar rendering if it needs one
        if (!Projectile.isAPreviewDummy && !BattleClient.LocalBattleOngoing && Data != null && Data.HP != Data.MaxHP)
            PetsNeedingHPBars.Add(Projectile.whoAmI);
    }

    private static void DrawHealthBar(PokemonPet pet, Projectile proj)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(default, null, Main.DefaultSamplerState, null, Main.Rasterizer, null,
            Main.GameViewMatrix.TransformationMatrix);

        // Get the main texture to determine Pokémon width (and subsequently bar width)
        const int barMinWidth = 28;
        const int barMaxWidth = 48;

        var pokemonWidth = pet._mainTexture.Value.Width;
        var barWidth = Math.Clamp(pokemonWidth, barMinWidth, barMaxWidth);
        var barXOffset = (barWidth - pokemonWidth) / -2;

        // Calculate health bar position
        var drawPos = proj.Center - Main.screenPosition +
                      new Vector2(barXOffset,
                          proj.gfxOffY + pet.DrawOriginOffsetY + (int)Math.Ceiling(proj.height / 2f) + 4);
        var sourceRect = pet._mainTexture.Frame(1, Main.projFrames[pet.Type], frameY: proj.frame);
        var frameSize = sourceRect.Size();
        var origin = frameSize / new Vector2(2, 1);

        // Health bar dimensions
        var healthPercent = (float)pet.Data.HP / pet.Data.MaxHP;
        var regenHealthPercent = (float)pet.Data.RegenHP / pet.Data.MaxHP;

        // Draw health bar background (full width)
        var barStartPos = drawPos - origin + new Vector2(0, frameSize.Y + 6);
        barStartPos.X = (int)barStartPos.X;
        barStartPos.Y = (int)barStartPos.Y;

        const float lightMultiplier = 3.75f;
        var lightColor = Lighting.GetColor((int)(proj.Center.X / 16f), (int)(proj.Center.Y / 16f));
        var barColor = new Color(
            Math.Min((int)(lightColor.R * lightMultiplier), 255),
            Math.Min((int)(lightColor.G * lightMultiplier), 255),
            Math.Min((int)(lightColor.B * lightMultiplier), 255),
            255
        );

        // Draw start cap
        Main.spriteBatch.Draw(ResourceBarStartTexture.Value, barStartPos, null, barColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

        // Draw middle section (stretched to fill the width)
        var middleWidth = barWidth - ResourceBarStartTexture.Value.Width - ResourceBarEndTexture.Value.Width;
        var middlePos = barStartPos + new Vector2(ResourceBarStartTexture.Value.Width, 0);

        // ReSharper disable once PossibleLossOfFraction
        Main.spriteBatch.Draw(ResourceBarMiddleTexture.Value, middlePos, null, barColor, 0f, Vector2.Zero,
            new Vector2(middleWidth / ResourceBarMiddleTexture.Value.Width, 1f), SpriteEffects.None, 0);

        // Draw end cap
        Main.spriteBatch.Draw(ResourceBarEndTexture.Value,
            barStartPos + new Vector2(barWidth - ResourceBarEndTexture.Value.Width, 0),
            null, barColor,
            0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

        var healthFillRect = new Rectangle((int)barStartPos.X + 4, (int)barStartPos.Y + 4, barWidth - 8, 4);
        var originalHealthFillRectWidth = healthFillRect.Width;

        // Draw health fill background
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
            ColorUtils.MultiplyBlend(barColor, new Color(30, 40, 43)));

        // Draw regen health fill
        if (regenHealthPercent > 0f)
        {
            var pulseProgress = (float)Main.timeForVisualEffects / 60f % 1f;
            var pulseOpacity = 1f - pulseProgress; // Fade from full opacity to 0

            var regenColor = new Color(120, 150, 156) * pulseOpacity;

            healthFillRect.Width = (int)(originalHealthFillRectWidth * regenHealthPercent);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
                ColorUtils.MultiplyBlend(barColor, regenColor));
        }

        // Draw health fill
        if (!(healthPercent > 0f)) return;

        var currentHpState = healthPercent switch
        {
            > 0.5f => 2,
            > 0.2f => 1,
            _ => 0
        };

        if (currentHpState != pet._lastHPState)
        {
            if (pet._lastHPState >= 0 && currentHpState < pet._lastHPState)
                pet._cryTimer = 30;
            pet._lastHPState = currentHpState;
        }

        var healthColor = currentHpState switch
        {
            2 => new Color(0x36, 0xFF, 0x66),
            1 => new Color(0xFF, 0xE6, 0x43),
            _ => new Color(0xF0, 0x29, 0x29)
        };

        healthFillRect.Width = (int)(originalHealthFillRectWidth * healthPercent);
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, healthFillRect,
            ColorUtils.MultiplyBlend(barColor, healthColor));
    }

    public static Color GrayscaleColor(Color color)
    {
        // Compute luminance using the weighted average formula
        var grayValue = (int)(0.299f * color.R + 0.587f * color.G + 0.114f * color.B);
        return new Color(grayValue, grayValue, grayValue, color.A); // Maintain original alpha
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        Data.NetWrite(writer, PokemonData.BitIsShiny | PokemonData.BitPersonalityValue | PokemonData.BitVariant);
        writer.Write(CustomSpriteDirection.HasValue);
        if (CustomSpriteDirection.HasValue) writer.Write(CustomSpriteDirection.Value);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Data ??= PokemonData.Create(ID).Build();
        Data.NetRead(reader);
        CustomSpriteDirection = reader.ReadBoolean() ? reader.ReadInt32() : null;
    }

    public override void AI()
    {
        var owningPlayer = Main.player[Projectile.owner];
        var modPlayer = owningPlayer.Terramon();
        var activePokemon = modPlayer.GetActivePokemon();
        UpdateSendOut(owningPlayer);

        if (Data != null && !Projectile.isAPreviewDummy)
            StatusVisuals.SpawnDust(Data.Status, Projectile.Hitbox);

        var isShiny = Data is { IsShiny: true };

        if (ID == NationalDexID.Gastly)
        {
            var lightColor = isShiny
                ? new Vector3(63 / 255f, 109 / 255f, 239 / 255f)
                : new Vector3(169 / 255f, 124 / 255f, 226 / 255f);
            Lighting.AddLight(Projectile.Center, lightColor * 1.25f * (Main.raining || Projectile.wet ? 1 - 0.5f : 1));
        }

        // Handles keeping the pet alive until it should despawn
        if (!owningPlayer.dead && owningPlayer.HasBuff(ModContent.BuffType<PokemonCompanion>()) &&
            activePokemon == Data && activePokemon.ID == _cachedID) Projectile.timeLeft = 2;

        // Attacking NPCs (disabled during battles)
        if (!modPlayer.BattleClient.BattleOngoing)
        {
            const float maxDetectRadius = 400f;
            const float maxAttackRadius = 500f;

            if (_target != null && _activeAttackTimer <= 0)
            {
                var distanceToTarget = Vector2.Distance(_target.Center, Projectile.Center);

                // Retargets if current target is too far away or no longer valid
                if (distanceToTarget > maxAttackRadius || !IsValidTarget(_target))
                    _target = null;
            }

            _target ??= FindClosestNPC(maxDetectRadius);
        }
        else
        {
            // Clear target and attack state when a battle starts
            _target = null;
            _activeAttackTimer = 0;
            _attackCooldown = 60;
        }

        if (Data != null && _target != null)
        {
            var dir = (Projectile.position.X < _target.position.X).ToDirectionInt();
            CustomSpriteDirection = dir;

            if (_attackCooldown == 0)
            {
                _activeAttackTimer = 16;
                PokemonAnimations.Play(AnimOwner, PokemonAnimState.Attack);
                _attackCooldown = 120;
                SoundEngine.PlaySound(in SoundID.Item7);
            }
            else if (_activeAttackTimer == 8)
            {
                var moves = Data.Moves;
                var nonStatusMoves = moves.Where(m => m.Schema.Category != MoveCategory.Status).ToArray();

                if (nonStatusMoves.Length == 0)
                {
                    CombatText.NewText(Projectile.Hitbox, Color.White, "No usable moves!", dot: true);
                }
                else
                {
                    var selectedMove = nonStatusMoves[Main.rand.Next(nonStatusMoves.Length)];

                    /*Main.NewText($"{Data.DisplayName} used {selectedMove.ID} (Max PP: {selectedMove.Schema.PP})!");
                    Main.NewText(selectedMove.Schema.Category);*/

                    var moveName = Language.GetTextValue($"Mods.Terramon.Moves.{selectedMove.ID}.DisplayName");
                    CombatText.NewText(Projectile.Hitbox, Color.White, moveName, dot: true);

                    var damage = Data.CompanionMoveDamage(selectedMove.Schema);
                    var kb = CalculateKnockback();

                    switch (selectedMove.Schema.Category)
                    {
                        case MoveCategory.Status:
                        case MoveCategory.Dynamic:
                            // Currently no status or dynamic moves implemented
                            break;
                        case MoveCategory.Physical:
                        {
                            _target.SimpleStrikeNPC(damage, dir, false, kb);
                            CreateHitEffect(_target.Center);
                            break;
                        }
                        case MoveCategory.Special:
                        {
                            var proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                Vector2.Normalize(_target.Center - Projectile.Center) * 4f,
                                ModContent.ProjectileType<PokemonPetGenericAttackProjectile>(),
                                damage,
                                kb,
                                Projectile.owner,
                                ID);
                            var randomType = Data.Schema.Types[Main.rand.Next(Data.Schema.Types.Count)];
                            var modProj = (PokemonPetGenericAttackProjectile)proj.ModProjectile;
                            modProj.AttackType = randomType;
                            modProj.HomingTarget = _target;
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        if (_attackCooldown > 0) _attackCooldown--;
        _activeAttackTimer--;

        // Health regen
        if (Data != null && Projectile.owner == Main.myPlayer)
        {
            if (Data.HP < Data.RegenHP)
            {
                if (_regenStartTarget == 0)
                    _regenStartTarget = Main.rand.Next(6, 1021); // Between 1/10 of a second and 17 seconds

                _regenStartTimer++;

                if (_regenStartTimer == _regenStartTarget)
                {
                    _regenStartTimer = 0;
                    _regenStartTarget = 0;
                    _regeneratingHealth = true;
                }

                if (_regeneratingHealth)
                {
                    // Regen rate scales with MaxHP (20 is baseline), so more MaxHP = faster regen
                    var regenScale = 20f / Data.MaxHP;
                    var regenIncrement = Math.Abs(owningPlayer.velocity.X) == 0 ? 1.25f : 0.5f;
                    _regenTimer += regenIncrement / regenScale;

                    if (_regenTimer >= 30f)
                    {
                        Data.Heal(1);
                        _regenTimer = 0f;
                    }
                }
            }
            else
            {
                CancelRegen();
            }
        }

        // Handle decreasing of visual immunity frames
        if (_visualImmunityFrames > 0) _visualImmunityFrames--;

        // Handle disabling custom sprite direction
        if (CustomSpriteDirection.HasValue && Projectile.velocity.X != 0 && _activeAttackTimer <= 0)
        {
            CustomSpriteDirection = null;
            Projectile.netUpdate = true;
        }

        // Play cry sound effect if queued
        if (_cryTimer > 0)
        {
            _cryTimer--;
            if (_cryTimer == 0 && Data != null && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(Data.GetCry(), Projectile.position);
            }
        }

        if (isShiny) ShinyEffect();
    }

    private NPC FindClosestNPC(float maxDetectDistance)
    {
        NPC closestNPC = null;

        var sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

        foreach (var target in Main.ActiveNPCs)
        {
            if (!IsValidTarget(target)) continue;

            var sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

            if (!(sqrDistanceToTarget < sqrMaxDetectDistance)) continue;
            sqrMaxDetectDistance = sqrDistanceToTarget;
            closestNPC = target;
        }

        return closestNPC;
    }

    private bool IsValidTarget(NPC target)
    {
        return target.CanBeChasedBy() &&
               Collision.CanHit(Projectile.Center, 1, 1, target.position, target.width, target.height);
    }

    public void RealtimeHit()
    {
        CancelRegen();
        _visualImmunityFrames = 24;
    }

    private void CancelRegen()
    {
        _regeneratingHealth = false;
        _regenTimer = 0f;
        _regenStartTimer = 0;

        // Only set new target if we don't already have one from recent damage
        if (_regenStartTarget == 0)
            _regenStartTarget = Main.rand.Next(300, 600); // 5-10 seconds
    }

    private static void CreateHitEffect(Vector2 hitPosition)
    {
        var particleCount = Main.rand.Next(12, 18);

        for (var i = 0; i < particleCount; i++)
        {
            var angle = (float)(i * 2 * Math.PI / particleCount);

            angle += Main.rand.NextFloat(-0.3f, 0.3f);

            var speed = Main.rand.NextFloat(1.5f, 2.5f);
            var velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed
            );

            var dustColor = Main.rand.Next(6) switch
            {
                0 => Color.Red,
                1 => Color.Orange,
                2 => Color.Yellow,
                3 => Color.OrangeRed,
                4 => Color.Crimson,
                _ => Color.Gold
            };

            var spawnPos = hitPosition + new Vector2(
                Main.rand.NextFloat(-3, 3),
                Main.rand.NextFloat(-3, 3)
            );

            var dust = Dust.NewDustDirect(
                spawnPos,
                0, 0,
                DustID.PortalBolt,
                velocity.X,
                velocity.Y,
                100,
                dustColor,
                Main.rand.NextFloat(0.8f, 1.3f) // Varied scale for depth
            );

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.2f, 0.5f);

            if (i % 3 == 0)
            {
                dust.velocity *= 0.6f;
                dust.scale *= 1.2f;
            }
        }

        for (var i = 0; i < Main.rand.Next(3, 6); i++)
        {
            var flashDust = Dust.NewDustDirect(
                hitPosition + new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                0, 0,
                DustID.PortalBolt,
                Main.rand.NextFloat(-0.5f, 0.5f),
                Main.rand.NextFloat(-0.5f, 0.5f),
                100,
                Color.LightYellow,
                Main.rand.NextFloat(1.0f, 1.5f)
            );

            flashDust.noGravity = true;
            flashDust.fadeIn = 0.1f;
        }

        for (var i = 0; i < Main.rand.Next(4, 8); i++)
        {
            var sparkAngle = Main.rand.NextFloat(0, (float)(2 * Math.PI));
            var sparkSpeed = Main.rand.NextFloat(1f, 2f);

            var sparkVelocity = new Vector2(
                (float)Math.Cos(sparkAngle) * sparkSpeed,
                (float)Math.Sin(sparkAngle) * sparkSpeed
            );

            var spark = Dust.NewDustDirect(
                hitPosition,
                0, 0,
                DustID.PortalBolt,
                sparkVelocity.X,
                sparkVelocity.Y,
                100,
                Color.Orange,
                Main.rand.NextFloat(0.4f, 0.8f)
            );

            spark.noGravity = false;
            spark.fadeIn = Main.rand.NextFloat(0.4f, 0.8f);
        }
    }

    private float CalculateKnockback()
    {
        // Base attack of 255 = 2.5x multiplier
        var multiplier = 1f + Schema.BaseStats.Attack / 255f * 1.5f;
        return 2f * multiplier;
    }

    public override void PostAI()
    {
        // Call FindFrame delegate to determine the frame of the pet (set by behaviour components)
        FindFrame?.Invoke(this);
    }

    public void ConfrontFoe(BattleClient battle)
    {
        if (battle is null)
        {
            CustomTargetPosition = null;
            return;
        }

        Vector2 foePos;
        int foeDir;

        var foeEntity = battle.Foe?.SyncedEntity;
        if (foeEntity is null)
            return;
        if (foeEntity is Player plr)
        {
            var pet = plr.Terramon().ActivePetProjectile;
            if (pet != null)
                foeEntity = pet.Projectile;
        }

        foePos = foeEntity.Center;
        foeDir = foeEntity.direction;

        var xTarget = foePos.X + (foeDir * DistanceFromFoe);
        CustomTargetPosition = new Vector2(xTarget, foePos.Y);
    }

    private void ShinyEffect()
    {
        if (_mainTexture == null) return;

        // Disable shiny effect lighting for Haunter and Gengar
        if (ID != NationalDexID.Haunter && ID != NationalDexID.Gengar)
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);

        _shinySparkleTimer++;

        if (_shinySparkleTimer < 15) return;

        for (var i = 0; i < 2; i++)
        {
            // Spoof projectile width and height to match the texture size
            var oldWidth = Projectile.width;
            var oldHeight = Projectile.height;
            Projectile.width = (int)(_mainTexture.Value.Width * 0.8f);
            Projectile.height = (int)((float)_mainTexture.Value.Height / Main.projFrames[Type] * 0.8f);

            var dust = Dust.NewDustDirect(
                Projectile.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)) +
                new Vector2(oldWidth - Projectile.width, oldHeight - Projectile.height),
                Projectile.width,
                Projectile.height, DustID.TreasureSparkle);
            dust.velocity = Projectile.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);

            // Reset projectile width and height to original values
            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
        }

        _shinySparkleTimer = 0;
    }
}

public class PokemonPetGenericAttackProjectile : ModProjectile
{
    public PokemonType AttackType
    {
        get;
        set
        {
            field = value;
            if (AttackType == PokemonType.Fire)
            {
                _color = ColorUtils.FromHexRGB(0xFF490F);
            }
            else
            {
                _color = AttackType.GetColor();
            }
            _color.A = 0; // Draw additive, so set alpha to 0
        }
    }

    public NPC HomingTarget;
    private Color _color;

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DiamondBolt;

    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.DiamondBolt);
        Projectile.aiStyle = 0;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Generic;
    }

    public override void AI()
    {
        Projectile.oldPosition = Projectile.position;
        Projectile.oldVelocity = Projectile.velocity;

        Projectile.position += Projectile.velocity;

        var useColor = _color;
        for (var num260 = 0; num260 < 2; num260++)
        {
            if (AttackType == PokemonType.Fire)
            {
                useColor = _color.HueShift(Main.rand.NextFloat(0.02f, 0.08f), Main.rand.NextFloat(-0.08f, -0.08f), 1f);
                useColor.A = 0;
            }
            var num261 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width,
                Projectile.height, ModContent.DustType<ColorableDust>(), Projectile.velocity.X, Projectile.velocity.Y,
                50, useColor, 1.2f);
            Main.dust[num261].noGravity = true;
            var dust2 = Main.dust[num261];
            dust2.velocity *= 0.3f;
        }

        if (Projectile.ai[1] == 0f)
        {
            Projectile.ai[1] = 1f;
            SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
        }
        
        // Homing behaviour
        if (HomingTarget is not { active: true }) return;
        
        var length = Projectile.velocity.Length();
        var targetAngle = Projectile.AngleTo(HomingTarget.Center);
        Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(6)).ToRotationVector2() * length;
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    public override void ModifyDamageHitbox(ref Rectangle hitbox)
    {
        hitbox.Inflate(4, 4);
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y);
        for (var num615 = 0; num615 < 15; num615++)
        {
            var num616 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width,
                Projectile.height, ModContent.DustType<ColorableDust>(), Projectile.oldVelocity.X,
                Projectile.oldVelocity.Y, 50, _color, 1.2f);
            Main.dust[num616].noGravity = true;
            var dust2 = Main.dust[num616];
            dust2.scale *= 1.25f;
            dust2 = Main.dust[num616];
            dust2.velocity *= 0.5f;
        }
    }
}