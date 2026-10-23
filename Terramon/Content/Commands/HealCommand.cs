using Terraria.Localization;

namespace Terramon.Content.Commands;

/// <summary>
///     Restores HP and cures (or revives) Pokémon in the party, for testing.
/// </summary>
public class HealCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "heal";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Heal.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Heal.Usage");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var party = caller.Player.Terramon().Party;
        var slots = Enumerable.Range(1, party.Length);
        if (args.Length > 0 && args[0] != "all")
        {
            if (!int.TryParse(args[0], out var slot))
            {
                caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Party.ParseErrorSlot"), ChatColorRed);
                return;
            }

            if (slot is < 1 or > 6)
            {
                caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Party.SlotOutOfRange"), ChatColorRed);
                return;
            }

            slots = [slot];
        }

        var healed = 0;
        foreach (var slot in slots)
        {
            var data = party[slot - 1];
            if (data == null)
                continue;
            data.HP = data.MaxHP;
            data.Status = NonVolatileStatus.None;
            data.RestorePP();
            healed++;
        }

        caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Heal.Success", healed), ChatColorYellow);
    }
}
