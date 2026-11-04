using Terraria.Localization;

namespace Pokerraria.Content.Commands;

/// <summary>
///     Raises the level of a Pokémon in the party, for testing.
/// </summary>
public class LevelCommand : DebugCommand
{
    public override CommandType Type => CommandType.Chat;

    public override string Command => "level";

    public override string Description => Language.GetTextValue("Mods.Pokerraria.Commands.Level.Description");

    public override string Usage => Language.GetTextValue("Mods.Pokerraria.Commands.Level.Usage");

    protected override int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        if (!int.TryParse(args[0], out var slot))
        {
            caller.Reply(Language.GetTextValue("Mods.Pokerraria.Commands.Party.ParseErrorSlot"), ChatColorRed);
            return;
        }

        if (slot is < 1 or > 6)
        {
            caller.Reply(Language.GetTextValue("Mods.Pokerraria.Commands.Party.SlotOutOfRange"), ChatColorRed);
            return;
        }

        var amount = 1;
        if (args.Length > 1 && (!int.TryParse(args[1], out amount) || amount < 1))
        {
            caller.Reply($"Usage: {Usage}", ChatColorRed);
            return;
        }

        var data = caller.Player.Pokerraria().Party[slot - 1];
        if (data == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Pokerraria.Commands.PartyClear.NoPokemonInSlot", slot), ChatColorRed);
            return;
        }

        for (var i = 0; i < amount && data.LevelUp(); i++)
        {
        }

        caller.Reply(Language.GetTextValue("Mods.Pokerraria.Misc.RareCandyUse", data.DisplayName, data.Level),
            ChatColorYellow);
    }
}
