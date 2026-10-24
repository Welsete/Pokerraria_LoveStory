using Terramon.Content.Commands;
using Terramon.Content.Items;
using Terraria.Localization;

namespace Terramon.Core.Systems.PokemonDirectUseSystem;

public class PokemonDirectUseGlobalItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return entity.ModItem is IPokemonDirectUse;
    }

    public override void SetDefaults(Item item)
    {
        item.useTime = 30;
        item.useAnimation = 30;
        item.useStyle = ItemUseStyleID.HoldUp;
        item.consumable = true;
    }

    /// <summary>
    ///     The Pokémon an item used from the hand goes to: the active one, or for Revives (a fainted Pokémon can't be
    ///     out) the first fainted one in the party.
    /// </summary>
    private static PokemonData Target(Item item, Player player)
    {
        var modPlayer = player.Terramon();
        var active = modPlayer.GetActivePokemon();
        var directUseItem = (IPokemonDirectUse)item.ModItem;
        if (active != null && directUseItem.AffectedByPokemonDirectUse(active))
            return active;
        if (item.ModItem is BaseReviveItem)
            return modPlayer.Party.FirstOrDefault(p => p != null && directUseItem.AffectedByPokemonDirectUse(p));
        return null;
    }

    public override bool CanUseItem(Item item, Player player)
    {
        // Mid-battle, Revives go through the Bag so the battle knows the Pokémon is back
        if (item.ModItem is BaseReviveItem && player.Terramon().BattleClient is { BattleOngoing: true })
        {
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.UseBagInBattle"), TerramonCommand.ChatColorYellow);
            return false;
        }

        if (Target(item, player) != null)
            return true;

        var activePokemonData = player.Terramon().GetActivePokemon();
        if (item.ModItem is BaseReviveItem)
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoFaintedPokemon"),
                TerramonCommand.ChatColorYellow);
        else if (activePokemonData == null)
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.NoActivePokemon"),
                TerramonCommand.ChatColorYellow);
        else
            player.NewText(Language.GetTextValue("Mods.Terramon.Misc.ItemNoEffect", activePokemonData.DisplayName),
                TerramonCommand.ChatColorYellow);
        return false;
    }

    public override bool? UseItem(Item item, Player player)
    {
        var directUseItem = (IPokemonDirectUse)item.ModItem;
        directUseItem.PokemonDirectUse(player, Target(item, player) ?? player.Terramon().GetActivePokemon());
        return true;
    }
}