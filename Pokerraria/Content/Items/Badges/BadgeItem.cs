using Pokerraria.Content.NPCs.Trainers;
using Pokerraria.Core.Systems;
using Terraria.Localization;

namespace Pokerraria.Content.Items;

/// <summary>
///     A Gym badge, given the first time a Gym Leader is beaten. Hovering one shows the whole Badge Case.
/// </summary>
public abstract class BadgeItem : KeyItem
{
    public override string Texture => "Pokerraria/Assets/Items/Badges/" + GetType().Name;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.width = 24;
        Item.height = 24;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);

        var player = Main.LocalPlayer;
        tooltips.Add(new TooltipLine(Mod, "BadgeCase",
            Language.GetTextValue("Mods.Pokerraria.CommonTooltips.BadgeCase", League.BadgeCount(player),
                League.GymLeaders.Length)));

        var cap = LevelCap.Compute(player);
        tooltips.Add(new TooltipLine(Mod, "LevelCap", LevelCap.HasLimit(cap)
            ? Language.GetTextValue("Mods.Pokerraria.CommonTooltips.LevelCap", cap)
            : Language.GetTextValue("Mods.Pokerraria.CommonTooltips.NoLevelCap")));

        var hallOfFame = player.GetModPlayer<GymPlayer>().HallOfFame;
        if (hallOfFame.Count > 0)
        {
            tooltips.Add(new TooltipLine(Mod, "HallOfFame",
                Language.GetTextValue("Mods.Pokerraria.CommonTooltips.HallOfFame", hallOfFame[0].Date.ToShortDateString()))
            {
                OverrideColor = Color.Gold
            });
        }
    }
}
