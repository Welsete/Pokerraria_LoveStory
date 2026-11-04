namespace Pokerraria.Content.Items;

public class YellowApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<ReviveRarity>();
}