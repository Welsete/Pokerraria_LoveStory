using Terramon.Content.Buffs;
using Terramon.Content.Configs;
using Terramon.ID;
using Terraria.Localization;

namespace Terramon.Core;

/// <summary>
///     Small bonuses the following (active) Pokémon gives its trainer, linking the Pokémon and Terraria worlds.
///     A few species have their own; every other Pokémon gets one per type (so dual types get two).
/// </summary>
public enum PokemonPassive : byte
{
    None,

    // Species
    AuraResonance, // Lucario

    // Types
    Pickup, // Normal
    FightingSpirit, // Fighting
    Featherfall, // Flying
    Antidote, // Poison
    Burrower, // Ground
    Sturdy, // Rock
    Swarm, // Bug
    SpiritSight, // Ghost
    SteelSkin, // Steel
    Flame, // Fire
    DeepBreath, // Water
    Photosynthesis, // Grass
    Static, // Electric
    Foresight, // Psychic
    IceSkater, // Ice
    DragonForce, // Dragon
    DarkVision, // Dark
    FairyLuck // Fairy
}

/// <summary>
///     Applies the passives of each player's active Pokémon. Runs for every player on every machine so stats agree;
///     only the local player sees the buff icon and the ore highlight of Lucario's aura.
/// </summary>
public sealed class PokemonPassivePlayer : ModPlayer
{
    /// <summary>
    ///     How far (in tiles) Lucario's aura finds ores and crystals.
    /// </summary>
    private const int AuraRadius = 22;

    private const int AuraScanInterval = 30;
    private const int MaxAuraTiles = 300;

    private static readonly List<Point> AuraTiles = [];
    private static int _auraScanTimer;

    /// <summary>
    ///     The passives active right now (empty with no Pokémon out, a fainted one, or passives turned off).
    /// </summary>
    public List<PokemonPassive> Active { get; } = [];

    /// <summary>
    ///     The Pokémon giving <see cref="Active" />.
    /// </summary>
    public PokemonData Source { get; private set; }

    /// <summary>
    ///     The passives a Pokémon gives while following its trainer.
    /// </summary>
    public static IEnumerable<PokemonPassive> For(PokemonData data)
    {
        if (data?.Schema == null)
            yield break;

        // Species with a passive of their own
        if (data.ID == NationalDexID.Lucario)
        {
            yield return PokemonPassive.AuraResonance;
            yield break;
        }

        foreach (var type in data.Schema.Types.Distinct())
        {
            var passive = ForType(type);
            if (passive != PokemonPassive.None)
                yield return passive;
        }
    }

    private static PokemonPassive ForType(PokemonType type) => type switch
    {
        PokemonType.Normal => PokemonPassive.Pickup,
        PokemonType.Fighting => PokemonPassive.FightingSpirit,
        PokemonType.Flying => PokemonPassive.Featherfall,
        PokemonType.Poison => PokemonPassive.Antidote,
        PokemonType.Ground => PokemonPassive.Burrower,
        PokemonType.Rock => PokemonPassive.Sturdy,
        PokemonType.Bug => PokemonPassive.Swarm,
        PokemonType.Ghost => PokemonPassive.SpiritSight,
        PokemonType.Steel => PokemonPassive.SteelSkin,
        PokemonType.Fire => PokemonPassive.Flame,
        PokemonType.Water => PokemonPassive.DeepBreath,
        PokemonType.Grass => PokemonPassive.Photosynthesis,
        PokemonType.Electric => PokemonPassive.Static,
        PokemonType.Psychic => PokemonPassive.Foresight,
        PokemonType.Ice => PokemonPassive.IceSkater,
        PokemonType.Dragon => PokemonPassive.DragonForce,
        PokemonType.Dark => PokemonPassive.DarkVision,
        PokemonType.Fairy => PokemonPassive.FairyLuck,
        _ => PokemonPassive.None
    };

    /// <summary>
    ///     The localized name of a passive.
    /// </summary>
    public static string PassiveName(PokemonPassive passive) =>
        Language.GetTextValue($"Mods.Terramon.Passives.{passive}.Name");

    /// <summary>
    ///     The localized description of what a passive does.
    /// </summary>
    public static string PassiveDescription(PokemonPassive passive) =>
        Language.GetTextValue($"Mods.Terramon.Passives.{passive}.Description");

    public override void ResetEffects()
    {
        Active.Clear();
        Source = null;

        if (!GameplayConfig.Instance.PokemonPassives || !Player.HasBuff(ModContent.BuffType<PokemonCompanion>()))
            return;
        var data = Player.Terramon().GetActivePokemon();
        if (data is not { HP: > 0 })
            return;

        Source = data;
        Active.AddRange(For(data));
    }

    public override void PostUpdateEquips()
    {
        foreach (var passive in Active)
        {
            switch (passive)
            {
                case PokemonPassive.Pickup:
                    Player.treasureMagnet = true;
                    break;
                case PokemonPassive.FightingSpirit:
                    Player.GetDamage(DamageClass.Melee) += 0.05f;
                    break;
                case PokemonPassive.Featherfall:
                    Player.slowFall = true; // Hold down to fall normally
                    break;
                case PokemonPassive.Antidote:
                    Player.buffImmune[BuffID.Poisoned] = true;
                    break;
                case PokemonPassive.Burrower:
                    Player.pickSpeed -= 0.1f;
                    break;
                case PokemonPassive.Sturdy:
                    Player.statDefense += 3;
                    break;
                case PokemonPassive.Swarm:
                case PokemonPassive.Static:
                    Player.moveSpeed += 0.05f;
                    break;
                case PokemonPassive.SpiritSight:
                    Player.detectCreature = true;
                    break;
                case PokemonPassive.SteelSkin:
                    Player.endurance += 0.03f;
                    break;
                case PokemonPassive.DeepBreath:
                    Player.breathMax += 100;
                    break;
                case PokemonPassive.Photosynthesis:
                    if (Main.dayTime && Player.ZoneOverworldHeight)
                        Player.lifeRegen += 2;
                    break;
                case PokemonPassive.Foresight:
                    Player.dangerSense = true;
                    break;
                case PokemonPassive.IceSkater:
                    Player.iceSkate = true;
                    Player.buffImmune[BuffID.Chilled] = true;
                    break;
                case PokemonPassive.DragonForce:
                    Player.GetDamage(DamageClass.Generic) += 0.04f;
                    break;
                case PokemonPassive.DarkVision:
                    Player.nightVision = true;
                    break;
            }
        }
    }

    public override void ModifyLuck(ref float luck)
    {
        if (Active.Contains(PokemonPassive.FairyLuck))
            luck += 0.1f;
    }

    public override void PostUpdate()
    {
        if (Active.Count == 0)
            return;

        var isLocal = Player.whoAmI == Main.myPlayer;
        if (isLocal)
            Player.AddBuff(ModContent.BuffType<PokemonPassiveBuff>(), 2);

        if (Main.dedServ)
            return;

        var pet = Player.Terramon().ActivePetProjectile?.Projectile;
        var petCenter = pet is { active: true } ? pet.Center : Player.Center;

        foreach (var passive in Active)
        {
            switch (passive)
            {
                case PokemonPassive.Flame:
                    Lighting.AddLight(petCenter, 0.9f, 0.55f, 0.2f);
                    break;
                case PokemonPassive.Static:
                    Lighting.AddLight(petCenter, 0.55f, 0.55f, 0.25f);
                    break;
                case PokemonPassive.AuraResonance:
                    AuraDust(petCenter);
                    if (isLocal)
                        AuraHighlight();
                    break;
            }
        }
    }

    /// <summary>
    ///     A soft blue aura around Lucario.
    /// </summary>
    private static void AuraDust(Vector2 center)
    {
        Lighting.AddLight(center, 0.1f, 0.25f, 0.5f);
        if (!Main.rand.NextBool(4))
            return;
        var offset = Main.rand.NextVector2Circular(18f, 24f);
        var dust = Dust.NewDustPerfect(center + offset, DustID.BlueTorch,
            new Vector2(0f, Main.rand.NextFloat(-1f, -0.2f)), 100, default, Main.rand.NextFloat(0.6f, 1f));
        dust.noGravity = true;
    }

    /// <summary>
    ///     Makes ores, gems and life crystals near the player glow blue, a gentler and shorter-ranged Spelunker.
    /// </summary>
    private void AuraHighlight()
    {
        if (++_auraScanTimer >= AuraScanInterval)
        {
            _auraScanTimer = 0;
            AuraTiles.Clear();
            var center = Player.Center.ToTileCoordinates();
            for (var x = center.X - AuraRadius; x <= center.X + AuraRadius && AuraTiles.Count < MaxAuraTiles; x++)
            {
                for (var y = center.Y - AuraRadius; y <= center.Y + AuraRadius; y++)
                {
                    if (!WorldGen.InWorld(x, y, 2))
                        continue;
                    var tile = Main.tile[x, y];
                    if (!tile.HasTile || !Resonates(tile.TileType))
                        continue;
                    if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > AuraRadius * AuraRadius)
                        continue;
                    AuraTiles.Add(new Point(x, y));
                    if (AuraTiles.Count >= MaxAuraTiles)
                        break;
                }
            }
        }

        foreach (var point in AuraTiles)
            Lighting.AddLight(point.ToWorldCoordinates(), 0.12f, 0.28f, 0.55f);

        // Now and then, a spark rises from one of them
        if (AuraTiles.Count > 0 && Main.rand.NextBool(8))
        {
            var point = AuraTiles[Main.rand.Next(AuraTiles.Count)];
            var dust = Dust.NewDustPerfect(point.ToWorldCoordinates(Main.rand.NextFloat(16f), Main.rand.NextFloat(16f)),
                DustID.BlueTorch, new Vector2(0f, -0.6f), 120, default, 0.8f);
            dust.noGravity = true;
        }
    }

    /// <summary>
    ///     Ores, gems and crystals: what Spelunker shows, minus chests and pots.
    /// </summary>
    private static bool Resonates(ushort type) =>
        type == TileID.Heart ||
        (Main.tileSpelunker[type] && !TileID.Sets.BasicChest[type] && type != TileID.Pots);

    public override void OnEnterWorld()
    {
        AuraTiles.Clear();
        _auraScanTimer = 0;
    }
}
