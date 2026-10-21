using System.Reflection;
using Newtonsoft.Json.Linq;
using ReLogic.Content;
using Terramon.Content.Commands;
using Terramon.Content.Configs;
using Terramon.Content.Dusts;
using Terramon.Content.GUI.Common;
using Terramon.Content.GUI.TurnBased;
using Terramon.Content.Items;
using Terramon.Content.Items.PokeBalls;
using Terramon.Content.Projectiles;
using Terramon.Core.Abstractions;
using Terramon.Core.Battling;
using Terramon.Core.Battling.BattlePackets;
using Terramon.Core.Battling.BattlePackets.Messages;
using Terramon.Core.Loaders;
using Terramon.Core.NPCComponents;
using Terramon.Core.Systems;
using Terramon.Helpers;
using Terramon.ID;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;

namespace Terramon.Content.NPCs;

[Autoload(false)]
public class PokemonNPC(ushort id, DatabaseV2.PokemonSchema schema) : ModNPC, IPokemonEntity, IBattleProvider
{
    /// <summary>
    ///     The index of the Pokémon NPC under the mouse cursor.
    /// </summary>
    private static int? _highlightedNPCIndex;

    private static readonly Vector2[] ShadowDirections =
    [
        -Vector2.UnitX,
        Vector2.UnitX,
        -Vector2.UnitY,
        Vector2.UnitY,
        Vector2.Normalize(new Vector2(-1, -1)),
        Vector2.Normalize(new Vector2(1, -1)),
        Vector2.Normalize(new Vector2(-1, 1)),
        Vector2.Normalize(new Vector2(1, 1))
    ];

    private int _cryTimer;

    /// <summary>
    ///     How long a fainted wild Pokémon lies on the ground after its battle, so it can still be caught.
    /// </summary>
    private const int KnockedOutTicks = 60 * 8;
    private int _faintTime;
    private int _knockedOutTime;
    private int _lastFoe = -1;
    private bool _isSpawnedFromRelease;
    private Asset<Texture2D> _mainTexture;
    private int _mouseHoverTimer;
    private int _plasmaStateTime;
    private Vector2 _plasmaStateVelocity;
    private bool _noGravityBeforeCapture;
    private int _shinySparkleTimer;

    static PokemonNPC()
    {
        On_Main.DrawItemTextPopups += (orig, scaleTarget) =>
        {
            orig(scaleTarget);

            if (_highlightedNPCIndex.HasValue)
            {
                if (BattleClient.LocalClient.Foe is PokemonNPC npc && npc.NPC.whoAmI == _highlightedNPCIndex)
                {
                    _highlightedNPCIndex = null;
                    return;
                }
            }
            else return;

            var highlightedNPC = Main.npc[_highlightedNPCIndex.Value];
            _highlightedNPCIndex = null;
            DrawLevelText(Main.spriteBatch, highlightedNPC);
        };
    }

    protected override bool CloneNewInstances => true;

    public override string Name { get; } = schema.Identifier + "NPC";

    public override LocalizedText DisplayName => DatabaseV2.GetLocalizedPokemonName(Schema);

    public bool PlasmaState { get; private set; }

    public override string Texture { get; } = "Terramon/Assets/Pokemon/" + schema.Identifier;

    public ushort ID { get; } = id;

    public DatabaseV2.PokemonSchema Schema { get; } = schema;

    public PokemonData Data
    {
        get;
        set
        {
            field = value;
            NPC.lifeMax = field.MaxHP;
            NPC.life = field.HP;
        }
    }

    /// <summary>
    ///     Draws the NPC's level text above its sprite.
    /// </summary>
    private static void DrawLevelText(SpriteBatch spriteBatch, NPC npc)
    {
        var modNPC = npc.Pokemon();
        var text = modNPC.Mod.GetLocalization("GUI.Party.LevelDisplay").Format(modNPC.Data.Level);
        var font = FontAssets.DeathText.Value;

        const float scale = 0.375f;
        var size = font.MeasureString(text) * scale;

        var textXPos = npc.position.X - Main.screenPosition.X + 8f;
        var textYPos = npc.position.Y - Main.screenPosition.Y + npc.gfxOffY;
        if (Math.Abs(Main.LocalPlayer.gravDir - -1f) < 0.001f)
            textYPos = Main.screenHeight - textYPos;

        var textDrawPos = new Vector2(textXPos, textYPos) - size;

        using (spriteBatch.Override(sampler: SamplerState.LinearClamp))
        {
            var baseColor = Main.MouseTextColorReal;
            const float rotation = 0f;
            var origin = Vector2.Zero;
            var baseScale = new Vector2(scale);
            const float spread = 1.85f;
            const float maxWidth = -1f;

            var snippets = ChatManager.ParseMessage(text, baseColor).ToArray();
            ChatManager.ConvertNormalSnippets(snippets);
            foreach (var d in BetterUIText.ShadowDirections)
                ChatManager.DrawColorCodedString(spriteBatch, font, snippets, textDrawPos + d * spread,
                    new Color(0, 0, 0, baseColor.A), rotation, origin, baseScale, out _, maxWidth, true);

            _ = ChatManager.DrawColorCodedString(spriteBatch, font, snippets, textDrawPos, Color.White, rotation,
                origin, baseScale, out _, maxWidth);
        }
    }

    public override void SetStaticDefaults()
    {
        // Hide from the bestiary (they will appear in the Pokédex instead)
        var drawModifier = new NPCID.Sets.NPCBestiaryDrawModifiers
        {
            Hide = true
        };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifier);
    }

    public override void SetDefaults()
    {
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.value = 0f;
        NPC.knockBackResist = 0.75f;
        NPC.npcSlots = 0.2f;
        NPC.despawnEncouraged = GameplayConfig.Instance.EncourageDespawning;
        NPC.friendly = true;

        // Start a stopwatch to measure the time it takes to apply all components.
        // var stopwatch = Stopwatch.StartNew();

        foreach (var component in PokemonEntityLoader.NPCSchemaCache[ID].Children<JProperty>())
        {
            var componentType = Mod.Code.GetType($"Terramon.Content.NPCs.NPC{component.Name}");
            if (componentType == null)
                // Remove the component from the schema if it doesn't exist.
                // _schemaCache[ID].First(x => x.Path == component.Path).Remove();
                continue;
            var enableComponentRef = NPCComponentExtensions.EnableComponentMethod.MakeGenericMethod(componentType);
            var instancedComponent = enableComponentRef.Invoke(null, [NPC, null]);
            foreach (var prop in component.Value.Children<JProperty>())
            {
                var fieldInfo = componentType.GetRuntimeField(prop.Name);
                if (fieldInfo == null) continue;
                fieldInfo.SetValue(instancedComponent, prop.Value.ToObject(fieldInfo.FieldType));
            }
        }

        // Stop the stopwatch and log the time taken to apply all components.
        // stopwatch.Stop();
        // Mod.Logger.Debug("Time taken to apply components: " + stopwatch.Elapsed + "ms");

        BattleClient = new BattleClient(this);
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (source is { Context: "PokemonRelease" })
        {
            SpawnReleaseEffect();
            _cryTimer = 30;
            _isSpawnedFromRelease = true;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        var spawningPlayer = Player.FindClosest(NPC.Center, NPC.width, NPC.height);
        // Released Pokémon get their data from the ball right after spawning; wild ones scale with world progression
        var level = _isSpawnedFromRelease
            ? (byte)5
            : WildPokemonLevels.GetSpawnLevel(ID, NPC.Center == Vector2.Zero ? Main.player[spawningPlayer].Center : NPC.Center,
                source);
        Data = PokemonData.Create(ID, level).ForPlayer(Main.player[spawningPlayer]).Build();
        NPC.netUpdate = true;
    }

    private void SpawnReleaseEffect()
    {
        var dust = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 4; i++)
        {
            var angle = MathHelper.PiOver2 * i;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x / 2, y / 2);
            Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, dust, x, y);
        }
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (PlasmaState)
            return;

        if (Data is { HP: 0 })
            StatusVisuals.DrawDizzyStars(spriteBatch, NPC.Bottom - new Vector2(0f, NPC.frame.Width + 2f), screenPos,
                NPC.whoAmI);
        else if (Data is { Status: NonVolatileStatus.Slp })
            StatusVisuals.DrawSleepZ(spriteBatch, NPC.Top, screenPos, NPC.whoAmI);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        drawColor = StatusVisuals.Tint(Data?.Status ?? NonVolatileStatus.None, drawColor);
        _mainTexture ??= PokemonEntityLoader.RequestTexture(this);
        var mainTextureValue = _mainTexture.Value;

        var frameSize = NPC.frame.Size();
        var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var drawPos = NPC.Center - screenPos +
                      new Vector2(0f, NPC.gfxOffY + DrawOffsetY - (frameSize.Y - NPC.height) / 2f + 4);
        var rotation = NPC.rotation;

        // Pokémon with a full animation set pick their frame from what they're doing instead
        var animSet = PokemonAnimations.Get(Schema.Identifier);
        Texture2D animTexture = null;
        var animSource = Rectangle.Empty;
        var animOrigin = Vector2.Zero;
        if (animSet != null)
        {
            var battling = BattleClient?.Foe != null;
            PokemonAnimations.GetFrame(animSet, this, Data?.IsShiny ?? false, Math.Abs(NPC.velocity.X) > 0.2f,
                !battling && Math.Abs(NPC.velocity.Y) > 1f, battling, Data is { Status: NonVolatileStatus.Slp },
                Data is { HP: 0 }, out animTexture, out animSource, out animOrigin);
            drawPos = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 1f);
        }

        if (Data is { HP: 0 } && !PlasmaState)
        {
            // Knocked out: tips over backwards onto its side, darkens, and blinks just before leaving
            var fall = Math.Min(_faintTime / 15f, 1f);
            if (animSet == null)
            {
                rotation = fall * MathHelper.PiOver2 * (NPC.spriteDirection == 1 ? -1 : 1);
                drawPos = Vector2.Lerp(drawPos, NPC.Bottom - screenPos - new Vector2(0f, frameSize.X / 2f - 2f), fall);
            }

            drawColor = Color.Lerp(drawColor, drawColor.MultiplyRGB(new Color(150, 150, 170)), fall);
            if (KnockedOutTicks - _knockedOutTime < 90 && _knockedOutTime % 10 < 5)
                drawColor *= 0.3f;
        }

        var isHighlighted = NPC.whoAmI == _highlightedNPCIndex;

        if (!PlasmaState && BattleClient.LocalClient.Foe == null)
        {
            if (isHighlighted && NPC.DistanceSQ(Main.LocalPlayer.Center) < 300f * 300f)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                // Apply outline shader
                var highlightColor = Data.IsShiny
                    ? ModContent.GetInstance<KeyItemRarity>().RarityColor
                    : ClientConfig.Instance.HighlightColor;
                var outlineShader = ShaderAssets.Outline;
                outlineShader.Shader.Parameters["uThickOutline"].SetValue(ClientConfig.Instance.ThickHighlights);
                outlineShader.Shader.Parameters["uImageSize0"].SetValue(animTexture?.Size() ?? _mainTexture.Size());
                outlineShader
                    .UseColor(highlightColor.MultiplyRGB(drawColor))
                    .UseSecondaryColor(highlightColor.HueShift(0.035f, -0.08f).MultiplyRGB(drawColor))
                    .Apply();
            }
        }

        if (_plasmaStateTime <= 20)
        {
            // Desaturate the lightColor for Gastly
            var adjustedColor = ID == NationalDexID.Gastly
                ? PokemonPet.GrayscaleColor(drawColor)
                : drawColor;

            if (animTexture != null)
                spriteBatch.Draw(animTexture, drawPos, animSource, adjustedColor, rotation, animOrigin,
                    NPC.scale * animSet.Scale, effects, 0f);
            else
                spriteBatch.Draw(mainTextureValue, drawPos, NPC.frame, adjustedColor, rotation,
                    frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);

            if (isHighlighted)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            var glowCache = Data?.IsShiny ?? false
                ? PokemonEntityLoader.ShinyGlowTextureCache
                : PokemonEntityLoader.GlowTextureCache;
            if (glowCache.TryGetValue(ID, out var glowTexture) && !PlasmaState)
            {
                drawColor = Color.White;
                if (ID == NationalDexID.Gastly) drawColor.A = 128;
                spriteBatch.Draw(glowTexture.Value, drawPos, NPC.frame, drawColor, rotation,
                    frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);
            }
        }

        if (!PlasmaState) return false;

        // Draw the Pokémon with the fade shader
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None,
            Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        // Apply fade shader
        ShaderAssets.FadeToColor
            .UseColor(drawColor)
            .UseOpacity(_plasmaStateTime <= 20 ? _plasmaStateTime / 7.5f : NPC.Opacity)
            .Apply();

        if (animTexture != null)
            spriteBatch.Draw(animTexture, drawPos, animSource, drawColor, NPC.rotation, animOrigin,
                NPC.scale * animSet.Scale, effects, 0f);
        else
            spriteBatch.Draw(mainTextureValue, drawPos, NPC.frame, drawColor, NPC.rotation,
                frameSize / new Vector2(2, 2), NPC.scale, effects, 0f);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }

    /// <summary>
    ///     What clients need to know about a wild Pokémon: how it looks, its level, HP and status (shown above it and in
    ///     the catch rate), and everything kept when it's caught (the catching client adds its own copy to the party).
    /// </summary>
    private const int NetSyncFields = PokemonData.BitIsShiny | PokemonData.BitPersonalityValue | PokemonData.BitVariant |
                                      PokemonData.BitLevel | PokemonData.BitEXP | PokemonData.BitHP |
                                      PokemonData.BitStatus | PokemonData.BitIVs | PokemonData.BitEVs |
                                      PokemonData.BitMoves;

    public override void SendExtraAI(BinaryWriter writer)
    {
        Data.NetWrite(writer, NetSyncFields);
        writer.WriteFlags(PlasmaState, _isSpawnedFromRelease);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var isFirstSync = Data == null;
        Data ??= PokemonData.Create(ID, 5).Build();

        Data.NetRead(reader);
        reader.ReadFlags(out var plasmaState, out var isSpawnedFromRelease);
        if (PlasmaState && !plasmaState && !isFirstSync)
        {
            // Already inside the ball here, so the server let it break free (mid-battle): come back out too
            if (NPC.scale < 0.02f)
                RestoreFromBall();
            // Otherwise it's a sync sent before the server learned about the ball hit, so keep going into the ball
            else
                plasmaState = true;
        }

        PlasmaState = plasmaState;
        _isSpawnedFromRelease = isSpawnedFromRelease;

        if (!isFirstSync)
            return;

        _mainTexture = PokemonEntityLoader.RequestTexture(this);

        if (!_isSpawnedFromRelease) return;
        SpawnReleaseEffect();
        _cryTimer = 30;
    }

    public override void AI()
    {
        if (_cryTimer > 0)
        {
            _cryTimer--;
            if (_cryTimer == 0 && Data != null && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(Data.GetCry(), NPC.position);
            }
        }

        if (Data != null && Data.HP != 0) NPC.life = Data.HP;

        if (Data != null && !PlasmaState)
            StatusVisuals.SpawnDust(Data.Status, NPC.Hitbox);

        if (Data is { HP: 0 } && !PlasmaState)
        {
            UpdateKnockedOut();
            if (!NPC.active)
                return;
        }
        else
        {
            _faintTime = 0;
        }
        if (PlasmaState)
        {
            var lightIntensity = 0.57f - _plasmaStateTime / 70f;
            if (lightIntensity > 0)
                Lighting.AddLight(NPC.Center, 178f / 255f * lightIntensity, 223f / 255f * lightIntensity,
                    lightIntensity);
            if (_plasmaStateTime > 20)
            {
                NPC.velocity = _plasmaStateVelocity;
                NPC.scale *= 0.85f;
                NPC.alpha += 25;
                if (NPC.alpha >= 255) NPC.alpha = 255;
            }
            else
            {
                NPC.velocity = Vector2.Zero;
            }

            if (NPC.scale < 0.02f)
            {
                // Mid-battle, stay hidden inside the ball until it decides; the battle needs this NPC if it breaks free
                if (BattleClient?.Foe != null)
                {
                    NPC.velocity = Vector2.Zero;
                    return;
                }

                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            _plasmaStateTime++;
            return;
        }

        if (Data is not { IsShiny: true }) return;

        if (_mainTexture == null)
            SoundEngine.PlaySound(in SoundID.Item30, NPC.position);

        ShinyEffect();
    }

    /// <summary>
    ///     A fainted wild Pokémon falls over and lies there. Once its battle is over, the player has a few seconds to
    ///     throw a Poké Ball at it before it leaves.
    /// </summary>
    private void UpdateKnockedOut()
    {
        if (_faintTime++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(Data.GetCry() with { Pitch = -0.5f }, NPC.position);

        NPC.velocity.X *= 0.8f;

        if (BattleClient?.Foe != null)
        {
            if (BattleClient.Foe is TerramonPlayer foe)
                _lastFoe = foe.Player.whoAmI;
            return;
        }

        if (_knockedOutTime++ == 0 && _lastFoe == Main.myPlayer && !Main.dedServ)
            Main.NewText(Language.GetTextValue("Mods.Terramon.Battle.KnockedOutHint", Data.DisplayName),
                new Color(255, 215, 94));

        if (_knockedOutTime < KnockedOutTicks || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        for (var i = 0; i < 20; i++)
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke);
        NPC.active = false;
        if (Main.dedServ)
            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
    }

    /// <summary>
    ///     A knocked out Pokémon that broke out of a Poké Ball wakes up and runs off shortly after.
    /// </summary>
    public void LeaveSoon() => _knockedOutTime = Math.Max(_knockedOutTime, KnockedOutTicks - 30);

    /// <summary>
    ///     Chance that a knocked out Pokémon breaking out of a Poké Ball comes to and flees for good.
    /// </summary>
    private const float FleeAfterFailedCatchChance = 0.35f;

    /// <summary>
    ///     A knocked out Pokémon just broke out of a Poké Ball: it either comes to and flees, or stays knocked out
    ///     (lying down already, with the full time left) so the player can try again. Runs on the server or in
    ///     singleplayer.
    /// </summary>
    public void AfterFailedCatchWhileKnockedOut()
    {
        _faintTime = 15; // Already lying down
        var flees = Main.rand.NextFloat() < FleeAfterFailedCatchChance;
        if (flees)
            LeaveSoon();
        else
            _knockedOutTime = 1; // Full time left, and no repeated hint

        var text = Language.GetTextValue(
            "Mods.Terramon.Battle." + (flees ? "FledAfterCatch" : "StillKnockedOut"), Data.DisplayName);
        var color = flees ? new Color(255, 140, 110) : new Color(255, 215, 94);
        if (Main.netMode == NetmodeID.Server)
            Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        else
            Main.NewText(text, color);
    }

    private void ShinyEffect()
    {
        // Disable shiny effect lighting for Haunter and Gengar
        if (ID != NationalDexID.Haunter && ID != NationalDexID.Gengar) Lighting.AddLight(NPC.Center, 0.5f, 0.5f, 0.5f);
        _shinySparkleTimer++;
        if (_shinySparkleTimer < 15) return;
        for (var i = 0; i < 2; i++)
        {
            var dust = Dust.NewDustDirect(
                NPC.position + new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)), NPC.width,
                NPC.height, DustID.TreasureSparkle);
            dust.velocity = NPC.velocity;
            dust.noGravity = true;
            dust.scale *= 1f + Main.rand.NextFloat(-0.03f, 0.03f);
        }

        _shinySparkleTimer = 0;
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        if (projectile.ModProjectile is not BasePkballProjectile || PlasmaState)
            return false;
        if (!BattleClient.BattleOngoing)
            return true;

        // Mid-battle, only the player fighting this Pokémon can throw a ball at it, and only when choosing an action
        // (the throw then takes their turn, see BasePkballProjectile)
        if (BattleClient.Foe is not TerramonPlayer thrower || thrower.Player.whoAmI != projectile.owner)
            return false;
        return projectile.owner != Main.myPlayer || BattleClient.LocalClient.CurrentRequest == ShowdownRequest.Any;
    }
    
    public override bool CanBeHitByNPC(NPC attacker)
    {
        return !PlasmaState && !BattleClient.BattleOngoing;
    }

    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (projectile.ModProjectile is BasePkballProjectile)
            modifiers.HideCombatText();
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public override bool PreHoverInteract(bool mouseIntersects)
    {
        if (_highlightedNPCIndex == null)
        {
            _highlightedNPCIndex = NPC.whoAmI;
            if (NPC.DistanceSQ(Main.LocalPlayer.Center) < 300f * 300f)
            {
                var modPlayer = TerramonPlayer.LocalPlayer;
                if (modPlayer.BattleClient.Foe == null && Main.mouseRight && Main.mouseRightRelease)
                {
                    if (!modPlayer.HasChosenStarter)
                    {
                        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.RequireStarter"),
                            TerramonCommand.ChatColorYellow);
                    }
                    else if (modPlayer.GetActivePokemon() == null)
                    {
                        Main.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"),
                            TerramonCommand.ChatColorYellow);
                    }
                    else
                    {
                        SoundEngine.PlaySound(in SoundID.MenuTick);
                        modPlayer.StartBattle(this);
                    }
                }
            }
        }

        if (_mouseHoverTimer == -1)
            return true;

        if (mouseIntersects)
        {
            if (++_mouseHoverTimer !=
                60) // 1 second assuming 60 FPS. TODO: Make independent of framerate, but only if it matters
                return true;
            // Register as seen in the player's Pokédex
            TerramonPlayer.LocalPlayer.UpdatePokedex(ID, PokedexEntryStatus.Seen,
                shiny: Data?.IsShiny ?? false);
            _mouseHoverTimer = -1;
        }
        else
        {
            _mouseHoverTimer = 0;
        }

        return true;
    }

    public void Encapsulate(Vector2 pokeballPos)
    {
        if (PlasmaState) return;

        var dust = ModContent.DustType<SummonCloud>();
        for (var i = 0; i < 4; i++)
        {
            var angle = MathHelper.PiOver2 * i;
            var (y, x) = MathF.SinCos(angle);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x / 2, y / 2);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, dust, x, y);
        }

        // Enable plasma state
        PlasmaState = true;
        _noGravityBeforeCapture = NPC.noGravity;

        // Set velocity to move towards pokeball
        _plasmaStateVelocity = pokeballPos - NPC.Center;
        if (_plasmaStateVelocity != Vector2.Zero) _plasmaStateVelocity = Vector2.Normalize(_plasmaStateVelocity) * 2f;

        NPC.noGravity = true; // Disable gravity
        NPC.ShowNameOnHover = false; // Disable showing name on hover
        NPC.netUpdate = true;
    }

    #region IBattleProvider

    public BattleProviderType ProviderType => BattleProviderType.PokemonNPC;

    public BattleClient BattleClient { get; private set; }

    public Entity SyncedEntity => NPC;

    public string BattleName => Language.GetTextValue("Mods.Terramon.Battle.WildName", Data.DisplayName);

    public PokemonData[] GetBattleTeam() => [Data];

    public void StartBattleEffects(bool before)
    {
        // Turn towards the player and disable hover behaviour
        NPC.spriteDirection = NPC.direction = BattleClient.Foe.SyncedEntity.position.X > NPC.position.X ? 1 : -1;
        NPC.ShowNameOnHover = false;
    }

    public void StopBattleEffects()
    {
        NPC.ShowNameOnHover = !PlasmaState;
    }

    /// <summary>
    ///     Brings this Pokémon back out of a Poké Ball it escaped from in the middle of a battle.
    /// </summary>
    public void BreakFree(Vector2 position)
    {
        if (!PlasmaState)
            return;
        PlasmaState = false;
        RestoreFromBall();
        NPC.Center = position;
        NPC.netUpdate = true;
    }

    private void RestoreFromBall()
    {
        _plasmaStateTime = 0;
        NPC.scale = 1f;
        NPC.alpha = 0;
        NPC.velocity = Vector2.Zero;
        NPC.noGravity = _noGravityBeforeCapture;
        SpawnReleaseEffect();
    }

    /// <summary>
    ///     Ends the current battle because this Pokémon was caught.
    /// </summary>
    public void EndBattleForCapture()
    {
        if (BattleClient?.Foe == null)
            return;
        var forfeit = new ForfeitOrder
        {
            Sender = this
        };
        forfeit.Send();
    }

    public void SetActiveSlot(byte newSlot)
    {
        if (BattleClient.LocalClient.Foe == this)
            TestBattleUI.FoePanel.CurrentMon = Data;
    }

    public void Reply(BattleMessage m)
    {
        switch (m)
        {
            case ChallengeQuestion:

                // Accept immediately
                m.Return(new ChallengeAnswer(yes: true));
                break;
            case ChallengeAnswer c: // Wild Pokémon in tall grass?

                if (c.Yes)
                {
                    // Imbalanced operation: Will send battle pick to foe here,
                    // and the other one's pick will be sent in SlotChoice to server
                    m.Return(new SlotChoice(slot: 1));
                }

                break;
            case SlotChoice: // Challenge to this mon
                var pick = new SlotChoice(slot: 1)
                {
                    Sender = this
                };
                pick.Send();
                break;
            case TeamQuestion:
                m.Return(new TeamAnswer(this.GetNetTeam()));
                break;
            default:
                Main.NewText(m.GetType());
                break;
        }
    }

    public void Witness(BattleMessage message)
    {
    }

    public void AutoBattleChoice()
    {
        BattleClient.MakeChoice(BattleChoice.Default);
    }

    #endregion
}