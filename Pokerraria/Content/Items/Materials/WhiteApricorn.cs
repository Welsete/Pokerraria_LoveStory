using Pokerraria.Content.Items.PokeBalls;

namespace Pokerraria.Content.Items;

public class WhiteApricorn : ApricornItem
{
    protected override int UseRarity { get; } = ModContent.RarityType<PremierBallRarity>();
    
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }
}