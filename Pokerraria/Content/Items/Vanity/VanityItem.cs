namespace Pokerraria.Content.Items;

public abstract class VanityItem : TerramonItem
{
    public override string Texture => "Pokerraria/Assets/Items/Vanity/" + GetType().Name;
}