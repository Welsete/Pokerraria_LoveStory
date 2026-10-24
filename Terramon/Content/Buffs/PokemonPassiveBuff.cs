using Terramon.Content.Configs;
using Terraria.Localization;

namespace Terramon.Content.Buffs;

/// <summary>
///     Shows the passives the active Pokémon is giving (see <see cref="PokemonPassivePlayer" />) and what it could
///     do in the field. Only an icon: the bonuses themselves are applied by <see cref="PokemonPassivePlayer" />.
/// </summary>
public class PokemonPassiveBuff : ModBuff
{
    public override string Texture => "Terramon/Assets/Buffs/PokemonPassiveBuff";

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        var passives = Main.LocalPlayer.GetModPlayer<PokemonPassivePlayer>();
        if (passives.Source == null)
            return;

        var lines = new List<string>
        {
            Language.GetTextValue("Mods.Terramon.Passives.From", passives.Source.DisplayName)
        };
        foreach (var passive in passives.Active)
            lines.Add($"{PokemonPassivePlayer.PassiveName(passive)}: {PokemonPassivePlayer.PassiveDescription(passive)}");

        var field = FieldAbilities.Describe(passives.Source);
        if (!string.IsNullOrEmpty(field))
            lines.Add(Language.GetTextValue("Mods.Terramon.Passives.FieldAbilities", field));

        tip = string.Join("\n", lines);
        if (ClientConfig.Instance.RainbowBuffText)
            rare = ItemRarityID.Expert;
    }

    // Goes away on its own when the Pokémon is put back
    public override bool RightClick(int buffIndex) => false;
}
