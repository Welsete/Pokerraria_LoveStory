namespace Terramon.Helpers;

/// <summary>
///     Colors and particles that show a Pokémon's status condition (frozen, burned, paralyzed...).
/// </summary>
public static class StatusVisuals
{
    /// <summary>
    ///     The color of a status condition, or null for none.
    /// </summary>
    public static Color? ColorOf(NonVolatileStatus status) => status switch
    {
        NonVolatileStatus.Frz => new Color(120, 225, 255),
        NonVolatileStatus.Brn => new Color(255, 140, 90),
        NonVolatileStatus.Par => new Color(255, 230, 90),
        NonVolatileStatus.Psn => new Color(200, 130, 255),
        NonVolatileStatus.Tox => new Color(170, 90, 240),
        NonVolatileStatus.Slp => new Color(165, 170, 205),
        _ => null
    };

    /// <inheritdoc cref="ColorOf(NonVolatileStatus)" />
    public static Color? ColorOf(string showdownStatus)
        => Enum.TryParse(showdownStatus, true, out NonVolatileStatus status) ? ColorOf(status) : null;

    /// <summary>
    ///     The color of a status condition as hex, for chat color tags.
    /// </summary>
    public static string HexOf(NonVolatileStatus status) => ColorOf(status)?.Hex3() ?? "ffffff";

    /// <summary>
    ///     Tints the color a Pokémon is drawn with. Frozen Pokémon turn icy blue, paralyzed ones flicker yellow,
    ///     sleeping ones slowly "breathe" darker.
    /// </summary>
    public static Color Tint(NonVolatileStatus status, Color drawColor)
    {
        if (ColorOf(status) is not { } tint)
            return drawColor;

        var time = (float)Main.timeForVisualEffects;
        var strength = status switch
        {
            NonVolatileStatus.Frz => 1f,
            NonVolatileStatus.Par => time % 40 < 6 ? 1f : 0.35f,
            NonVolatileStatus.Slp => 0.6f + 0.25f * MathF.Sin(time / 30f),
            _ => 0.55f + 0.2f * MathF.Sin(time / 12f)
        };

        var tinted = drawColor.MultiplyRGB(tint);
        return Color.Lerp(drawColor, tinted, strength) with { A = drawColor.A };
    }

    /// <summary>
    ///     Spawns the occasional particle around a Pokémon with a status condition. Call once per tick.
    /// </summary>
    public static void SpawnDust(NonVolatileStatus status, Rectangle hitbox)
    {
        if (Main.dedServ)
            return;

        var (dust, chance) = status switch
        {
            NonVolatileStatus.Frz => (DustID.IceTorch, 14),
            NonVolatileStatus.Brn => (DustID.Torch, 8),
            NonVolatileStatus.Par => (DustID.Electric, 12),
            NonVolatileStatus.Psn or NonVolatileStatus.Tox => (DustID.PurpleTorch, 10),
            _ => (-1, 0)
        };
        if (dust < 0 || !Main.rand.NextBool(chance))
            return;

        var d = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, dust, 0f, -1.5f, 100, default, 0.9f);
        d.noGravity = true;
        d.velocity *= 0.4f;
    }
}
