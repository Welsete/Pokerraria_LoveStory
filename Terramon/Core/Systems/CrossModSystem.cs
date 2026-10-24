namespace Terramon.Core.Systems;

public sealed class CrossModSystem : ModSystem
{
    public static Mod Wikithis { get; private set; }

    public override void OnModLoad()
    {
        if (Main.dedServ)
            return;

        // Wikithis compatibility is off: the only wiki is the original Terramon's, which doesn't match this mod.
        // Wikithis stays null until Pokerraria has a wiki of its own.
    }
}