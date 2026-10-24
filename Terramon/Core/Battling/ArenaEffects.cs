using ReLogic.Content;
using Terraria.GameContent;

namespace Terramon.Core.Battling;

/// <summary>
///     Shows a battle's weather and terrain around the battle itself, with Terraria's own effects: rain falling, a warm
///     sun, a sandstorm, snow and hail, flowers growing on Grassy Terrain, sparks on Electric Terrain, pink mist on
///     Misty Terrain and a purple glow on Psychic Terrain. Only the area of the battle is touched and nothing in the
///     world changes; the effects follow what the battle has shown so far (see <see cref="BattlePlayback.ShownField" />).
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class ArenaEffects : ModSystem
{
    private const int FlowerWidth = 10;
    private const int FlowerHeight = 16;
    private const int FlowerKinds = 5; // The last frame of the sheet is a grass tuft
    private const float Margin = 14 * 16f;

    private static Asset<Texture2D> _flowers;
    private static readonly List<Arena> Arenas = [];

    private sealed class Arena
    {
        public BattleField Field;
        public float Left, Right, Bottom;
        public bool Alive;
        public readonly List<Flower> Flowers = [];
    }

    private sealed class Flower
    {
        public Vector2 Position;
        public int Frame;
        public float Grow;
        public bool Leaving;
        public SpriteEffects Flip;
    }

    public override void Load()
    {
        _flowers = Mod.Assets.Request<Texture2D>("Assets/Misc/Battle/TerrainFlowers");
    }

    public override void Unload()
    {
        _flowers = null;
    }

    public override void ClearWorld()
    {
        Arenas.Clear();
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gamePaused)
            return;

        foreach (var arena in Arenas)
            arena.Alive = false;

        foreach (var player in Main.ActivePlayers)
        {
            var field = player.GetModPlayer<TerramonPlayer>().BattleClient?.Battle;
            var a = field?.A?.Provider?.SyncedEntity;
            var b = field?.B?.Provider?.SyncedEntity;
            if (a == null || b == null)
                continue;

            var arena = Arenas.Find(x => x.Field == field);
            if (arena == null)
            {
                arena = new Arena { Field = field };
                Arenas.Add(arena);
            }
            else if (arena.Alive)
                continue; // Already done for another player in the same battle

            arena.Alive = true;
            arena.Left = Math.Min(a.Center.X, b.Center.X) - Margin;
            arena.Right = Math.Max(a.Center.X, b.Center.X) + Margin;
            arena.Bottom = (a.Bottom.Y + b.Bottom.Y) * 0.5f;

            var (weather, terrain) = BattlePlayback.ShownField(field);
            if (terrain == FieldCondition.GrassyTerrain)
                GrowFlowers(arena);
            else
                WiltFlowers(arena);

            if (!OnScreen(arena))
                continue;
            Weather(arena, weather);
            Terrain(arena, terrain);
        }

        for (var i = Arenas.Count - 1; i >= 0; i--)
        {
            var arena = Arenas[i];
            if (!arena.Alive)
                WiltFlowers(arena);
            foreach (var flower in arena.Flowers)
                flower.Grow = flower.Leaving ? flower.Grow - 0.04f : Math.Min(1f, flower.Grow + 0.04f);
            arena.Flowers.RemoveAll(f => f.Leaving && f.Grow <= 0f);
            if (!arena.Alive && arena.Flowers.Count == 0)
                Arenas.RemoveAt(i);
        }
    }

    private static bool OnScreen(Arena arena)
    {
        return arena.Right > Main.screenPosition.X - 200f && arena.Left < Main.screenPosition.X + Main.screenWidth + 200f &&
               Math.Abs(arena.Bottom - (Main.screenPosition.Y + Main.screenHeight * 0.5f)) < Main.screenHeight;
    }

    private static float RandomX(Arena arena) => Main.rand.NextFloat(arena.Left, arena.Right);

    /// <summary>
    ///     The top of the ground under <paramref name="x" />, looking around the battle's height.
    /// </summary>
    private static float Ground(float x, float bottom)
    {
        var tileX = (int)(x / 16f);
        var startY = (int)(bottom / 16f) - 6;
        for (var y = startY; y < startY + 20; y++)
        {
            if (!WorldGen.InWorld(tileX, y, 2))
                break;
            var tile = Main.tile[tileX, y];
            if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
            {
                var above = Main.tile[tileX, y - 1];
                if (!(above.HasTile && Main.tileSolid[above.TileType]))
                    return y * 16f;
            }
        }

        return bottom;
    }

    private static void Weather(Arena arena, BattleWeather weather)
    {
        var top = Main.screenPosition.Y - 20f;
        switch (weather)
        {
            case BattleWeather.RainDance or BattleWeather.PrimordialSea or BattleWeather.StormSurge:
                for (var i = 0; i < (weather == BattleWeather.RainDance ? 5 : 9); i++)
                    Rain.NewRain(new Vector2(RandomX(arena), top + Main.rand.NextFloat(0f, 60f)),
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(11f, 14f)));
                break;

            case BattleWeather.SunnyDay or BattleWeather.DesolateLand:
                for (var x = arena.Left; x < arena.Right; x += 48f)
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 40f), 0.9f, 0.7f, 0.35f);
                if (Main.rand.NextBool(3))
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), arena.Bottom - Main.rand.NextFloat(40f, 220f)),
                        DustID.Enchanted_Gold, new Vector2(0f, Main.rand.NextFloat(0.3f, 0.8f)), 0, default, 1f);
                    d.noGravity = true;
                }
                break;

            case BattleWeather.Sandstorm or BattleWeather.DesertedDunes:
                for (var i = 0; i < 6; i++)
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), arena.Bottom - Main.rand.NextFloat(0f, 200f)),
                        DustID.Sand, new Vector2(Main.rand.NextFloat(5f, 9f), Main.rand.NextFloat(-0.4f, 0.4f)), 80,
                        default, Main.rand.NextFloat(0.9f, 1.4f));
                    d.noGravity = true;
                }
                break;

            case BattleWeather.Hail or BattleWeather.Snowscape:
                for (var i = 0; i < 4; i++)
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), top + Main.rand.NextFloat(0f, 80f)),
                        DustID.Snow, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(2f, 3.5f)), 0, default,
                        Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                }

                if (weather == BattleWeather.Hail && Main.rand.NextBool(2))
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), top), DustID.Ice,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(8f, 11f)), 0, default, 1.1f);
                    d.noGravity = true;
                }
                break;
        }
    }

    private static void Terrain(Arena arena, FieldCondition terrain)
    {
        switch (terrain)
        {
            case FieldCondition.GrassyTerrain:
                if (Main.rand.NextBool(4))
                {
                    var x = RandomX(arena);
                    var d = Dust.NewDustPerfect(new Vector2(x, Ground(x, arena.Bottom) - 4f), DustID.Grass,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.6f)), 0, default, 0.9f);
                    d.noGravity = true;
                }
                break;

            case FieldCondition.ElectricTerrain:
                for (var i = 0; i < 3; i++)
                {
                    var x = RandomX(arena);
                    var d = Dust.NewDustPerfect(new Vector2(x, Ground(x, arena.Bottom) - 2f), DustID.Electric,
                        new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-3f, -0.8f)), 0, default, 0.7f);
                    d.noGravity = true;
                }

                for (var x = arena.Left; x < arena.Right; x += 64f)
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 8f), 0.2f, 0.35f, 0.6f);
                break;

            case FieldCondition.MistyTerrain:
                for (var i = 0; i < 2; i++)
                {
                    var x = RandomX(arena);
                    var d = Dust.NewDustPerfect(new Vector2(x, Ground(x, arena.Bottom) - Main.rand.NextFloat(0f, 48f)),
                        DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -0.3f), 120, new Color(255, 150, 210),
                        Main.rand.NextFloat(1.6f, 2.4f));
                    d.noGravity = true;
                }

                for (var x = arena.Left; x < arena.Right; x += 64f)
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 16f), 0.5f, 0.25f, 0.4f);
                break;

            case FieldCondition.PsychicTerrain:
                for (var i = 0; i < 3; i++)
                {
                    var x = RandomX(arena);
                    var d = Dust.NewDustPerfect(new Vector2(x, Ground(x, arena.Bottom) - 2f), DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-2.2f, -0.8f)), 0, default, 1.2f);
                    d.noGravity = true;
                }

                for (var x = arena.Left; x < arena.Right; x += 64f)
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 16f), 0.45f, 0.15f, 0.6f);
                break;
        }
    }

    /// <summary>
    ///     Grass and flowers sprout along the ground of the battle, from the middle outwards.
    /// </summary>
    private static void GrowFlowers(Arena arena)
    {
        if (arena.Flowers.Exists(f => !f.Leaving))
            return;
        var middle = (arena.Left + arena.Right) * 0.5f;
        for (var x = arena.Left; x < arena.Right; x += 16f)
        {
            if (!Main.rand.NextBool(3, 5))
                continue;
            var px = x + Main.rand.NextFloat(0f, 12f);
            arena.Flowers.Add(new Flower
            {
                Position = new Vector2(px, Ground(px, arena.Bottom)),
                Frame = Main.rand.NextBool(2) ? FlowerKinds : Main.rand.Next(FlowerKinds),
                // A negative start makes the farther ones sprout a little later
                Grow = -Math.Abs(px - middle) / (Margin * 3f),
                Flip = Main.rand.NextBool() ? SpriteEffects.FlipHorizontally : SpriteEffects.None
            });
        }
    }

    private static void WiltFlowers(Arena arena)
    {
        foreach (var flower in arena.Flowers)
        {
            if (!flower.Leaving)
                flower.Grow = Math.Max(flower.Grow, 0f);
            flower.Leaving = true;
        }
    }

    public override void PostDrawTiles()
    {
        if (Arenas.Count == 0 || _flowers?.IsLoaded != true)
            return;

        var spriteBatch = Main.spriteBatch;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        var texture = _flowers.Value;
        foreach (var arena in Arenas)
        {
            foreach (var flower in arena.Flowers)
            {
                if (flower.Grow <= 0f)
                    continue;
                var grow = MathHelper.SmoothStep(0f, 1f, flower.Grow);
                var source = new Rectangle(flower.Frame * FlowerWidth, 0, FlowerWidth, FlowerHeight);
                var light = Lighting.GetColor((int)(flower.Position.X / 16f), (int)(flower.Position.Y / 16f) - 1);
                spriteBatch.Draw(texture, (flower.Position - Main.screenPosition).Floor(), source, light, 0f,
                    new Vector2(FlowerWidth * 0.5f, FlowerHeight), new Vector2(2f, 2f * grow), flower.Flip, 0f);
            }
        }

        spriteBatch.End();
    }
}
