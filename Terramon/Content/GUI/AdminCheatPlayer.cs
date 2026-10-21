using Terraria.DataStructures;

namespace Terramon.Content.GUI;

/// <summary>
///     Test panel cheats that have to run every tick. Not saved: they reset when the player leaves the world.
/// </summary>
public sealed class AdminCheatPlayer : ModPlayer
{
    public bool GodMode;

    /// <summary>
    ///     The next left click in the world (not on a menu) teleports the player there.
    /// </summary>
    public bool TeleportOnClick;

    public override void PostUpdate()
    {
        if (Player.whoAmI == Main.myPlayer && TeleportOnClick && Main.mouseLeft && Main.mouseLeftRelease &&
            !Player.mouseInterface && !Player.dead)
        {
            TeleportOnClick = false;
            AdminPanelUI.TeleportPlayerTo(Main.MouseWorld);
        }

        if (!GodMode)
            return;
        Player.statLife = Player.statLifeMax2;
        Player.breath = Player.breathMax;
    }

    // Nothing hurts while God Mode is on
    public override bool FreeDodge(Player.HurtInfo info) => GodMode;

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
        ref PlayerDeathReason damageSource) => !GodMode;
}
