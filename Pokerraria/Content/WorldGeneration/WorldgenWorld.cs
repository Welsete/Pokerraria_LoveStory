using Terraria.WorldBuilding;

namespace Pokerraria.Core;

public partial class TerramonWorld
{
    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        var potsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Pots"));
        
        if (potsIndex != -1)
            tasks.Insert(potsIndex + 1, new TerramonItemPass($"{nameof(Pokerraria)} Items", 237.4298f));
    }
}