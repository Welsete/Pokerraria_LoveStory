using Pokerraria.Content.Items;

namespace Pokerraria.Content.Tiles.Paintings;

public abstract class PaintingItem : TerramonItem
{
    public override string Texture => "Pokerraria/Assets/Tiles/Paintings/" + GetType().Name;
}