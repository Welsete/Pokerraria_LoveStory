namespace Pokerraria.Content.Items;

public abstract class Material : TerramonItem
{
    public override string Texture => "Pokerraria/Assets/Items/Materials/" + GetType().Name;
}