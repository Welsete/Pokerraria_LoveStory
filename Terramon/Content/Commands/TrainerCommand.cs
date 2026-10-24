using Terramon.Content.NPCs.Trainers;
using Terraria.Localization;

namespace Terramon.Content.Commands;

public class TrainerCommand : DebugCommand
{
    public override CommandType Type => CommandType.World;

    public override string Command => "trainer";

    public override string Description => Language.GetTextValue("Mods.Terramon.Commands.Trainer.Description");

    public override string Usage => Language.GetTextValue("Mods.Terramon.Commands.Trainer.Usage");

    protected override int MinimumArgumentCount => 1;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        base.Action(caller, input, args);
        if (!Allowed) return;

        var trainer = Mod.GetContent<TrainerNPC>()
            .FirstOrDefault(t => t.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
        if (trainer == null)
        {
            caller.Reply(Language.GetTextValue("Mods.Terramon.Commands.Trainer.NotFound", args[0]), ChatColorRed);
            return;
        }

        // Spawn the trainer a few tiles in front of the player
        var player = caller.Player;
        var spawnPos = player.Bottom + new Vector2(player.direction * 64f, 0f);
        NPC.NewNPC(player.GetSource_FromThis(), (int)spawnPos.X, (int)spawnPos.Y, trainer.Type);
    }
}
