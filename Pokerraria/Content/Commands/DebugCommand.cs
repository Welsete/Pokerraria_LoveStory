using Pokerraria.Content.Configs;
using Terraria.Localization;

namespace Pokerraria.Content.Commands;

public abstract class DebugCommand : TerramonCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;
        if (GameplayConfig.Instance.DebugMode) return;
        caller.Reply(Language.GetTextValue("Mods.Pokerraria.Commands.RequiresDebugMode"), ChatColorRed);
        Allowed = false;
    }
}