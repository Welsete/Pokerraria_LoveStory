using Terramon.Content.Projectiles;

namespace Terramon.Content.NPCs;

/// <summary>
///     Outside of turn-based battles, a wild Onix defends itself from Terraria's monsters by making stone spikes
///     burst out of the ground under them.
/// </summary>
public sealed class WildOnixAttack : GlobalNPC
{
    private const float Range = 16f * 30f;
    private const int Cooldown = 60 * 4;

    private int _timer = Cooldown / 2;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
        lateInstantiation && entity.ModNPC is PokemonNPC { Schema.Identifier: "Onix" };

    public override void PostAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.ModNPC is not PokemonNPC onix)
            return;
        if (onix.Data is not { HP: > 0 } || onix.PlasmaState || onix.BattleClient?.Foe != null)
            return;
        if (--_timer > 0)
            return;

        var target = FindTarget(npc);
        if (target == null)
        {
            _timer = 30;
            return;
        }

        _timer = Cooldown;
        npc.direction = npc.spriteDirection = target.Center.X > npc.Center.X ? 1 : -1;

        var ground = GroundBelow(target.Bottom);
        var size = new Vector2(180, 140);
        var damage = 15 + onix.Data.Level * 2;
        Projectile.NewProjectile(npc.GetSource_FromAI(), ground - new Vector2(0f, size.Y / 2f), Vector2.Zero,
            ModContent.ProjectileType<OnixRockSpikes>(), damage, 6f);
    }

    private static NPC FindTarget(NPC onix)
    {
        NPC best = null;
        var bestDistance = Range;
        foreach (var other in Main.ActiveNPCs)
        {
            if (other.friendly || other.townNPC || other.dontTakeDamage || other.lifeMax <= 5 ||
                other.ModNPC is PokemonNPC || !other.CanBeChasedBy())
                continue;
            var distance = onix.Distance(other.Center);
            if (distance >= bestDistance || !Collision.CanHitLine(onix.Center, 1, 1, other.Center, 1, 1))
                continue;
            best = other;
            bestDistance = distance;
        }

        return best;
    }

    /// <summary>
    ///     The top of the first solid ground under a point (up to 8 tiles down), so flying monsters still get hit
    ///     from the ground.
    /// </summary>
    private static Vector2 GroundBelow(Vector2 point)
    {
        var x = (int)(point.X / 16f);
        var y = (int)(point.Y / 16f);
        for (var i = 0; i < 8; i++, y++)
        {
            if (!WorldGen.InWorld(x, y))
                break;
            if (WorldGen.SolidTile(x, y))
                return new Vector2(point.X, y * 16f);
        }

        return point;
    }
}
