using Terramon.Core.Battling;
using Terraria.Audio;
using Terraria.Localization;

namespace Terramon.Core.Systems;

/// <summary>
///     Heals the whole party like a Pokémon Center: by sleeping in a bed for a few seconds, or when the Nurse heals the player.
///     Party data lives on the owning client, the same as when a Potion is used outside battle.
/// </summary>
public class PartyHealingPlayer : ModPlayer
{
    private const int SleepTimeToHeal = 5 * 60;

    private int _sleepTimer;
    private bool _healedThisSleep;

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || Main.dedServ)
            return;

        if (!Player.sleeping.isSleeping || BattleClient.LocalBattleOngoing)
        {
            _sleepTimer = 0;
            _healedThisSleep = false;
            return;
        }

        if (_healedThisSleep || ++_sleepTimer < SleepTimeToHeal)
            return;

        _healedThisSleep = true;
        if (HealParty())
            Main.NewText(Language.GetTextValue("Mods.Terramon.Healing.Bed"), new Color(110, 224, 110));
    }

    public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
    {
        // The Nurse only offers a heal when the player needs one, so let a hurt party count too
        if (health <= 0 && PartyNeedsHealing())
            health = 1;
        return true;
    }

    public override void PostNurseHeal(NPC nurse, int health, bool removeDebuffs, int price)
    {
        if (HealParty())
            Main.NewText(Language.GetTextValue("Mods.Terramon.Healing.Nurse", nurse.GivenName),
                new Color(110, 224, 110));
    }

    private static bool NeedsHealing(PokemonData data)
        => data != null && (data.HP < data.MaxHP || data.Status != NonVolatileStatus.None || data.MissingPP);

    private bool PartyNeedsHealing()
        => !BattleClient.LocalBattleOngoing && Player.Terramon().Party.Any(NeedsHealing);

    /// <summary>
    ///     Whether someone in the party is hurt (or low on PP, or has a status) and a heal can be given right now.
    /// </summary>
    public bool CanHealParty => PartyNeedsHealing();

    /// <summary>
    ///     Whether the party can't be healed because a battle is going on.
    /// </summary>
    public bool PartyBattling => BattleClient.LocalBattleOngoing;

    /// <summary>
    ///     Restores every party Pokémon's HP and PP and cures its status. Returns false if nobody needed it.
    /// </summary>
    public bool HealParty()
    {
        if (!PartyNeedsHealing())
            return false;

        foreach (var data in Player.Terramon().Party)
        {
            if (!NeedsHealing(data))
                continue;
            data.Heal(data.MaxHP);
            data.Status = NonVolatileStatus.None;
            data.RestorePP();
        }

        SoundEngine.PlaySound(in SoundID.Item4);
        return true;
    }
}
