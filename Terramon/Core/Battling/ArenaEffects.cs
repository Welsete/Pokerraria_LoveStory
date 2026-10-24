using ReLogic.Content;
using Terraria.GameContent;

namespace Terramon.Core.Battling;

/// <summary>
///     Shows a battle's weather and terrain around the battle itself, with Terraria's own effects: rain falling, a warm
///     sun, a sandstorm, snow and hail, flowers growing on Grassy Terrain, sparks on Electric Terrain, pink mist on
///     Misty Terrain and a purple glow on Psychic Terrain. Side conditions show on their own side: a gust of wind with
///     leaves for Tailwind, a shimmer in front of the Pokémon for Reflect, Light Screen, Safeguard and Mist, and rocks or
///     spikes on the ground for entry hazards. Only the area of the battle is touched and nothing in the
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
        public Vector2 PosA, PosB;
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
            arena.PosA = a.Center;
            arena.PosB = b.Center;

            var (weather, terrain) = BattlePlayback.ShownField(field);
            if (terrain == FieldCondition.GrassyTerrain)
                GrowFlowers(arena);
            else
                WiltFlowers(arena);

            if (!OnScreen(arena))
                continue;
            Weather(arena, weather);
            Terrain(arena, terrain);
            Side(arena, BattlePlayback.ShownSide(field, field.A), arena.PosA, arena.PosB);
            Side(arena, BattlePlayback.ShownSide(field, field.B), arena.PosB, arena.PosA);
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
            {
                // Terraria only lets as many raindrops exist as the clouds allow (none on a clear day), so the sky
                // counts as cloudy while the battle's drops are made. Nothing in the world's weather changes
                var clouds = Main.cloudAlpha;
                Main.cloudAlpha = 1f;
                for (var i = 0; i < (weather == BattleWeather.RainDance ? 5 : 9); i++)
                    Rain.NewRain(new Vector2(RandomX(arena), top + Main.rand.NextFloat(0f, 60f)),
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(11f, 14f)));
                Main.cloudAlpha = clouds;
                // Drops splashing around the Pokémon too, in case the rain is hidden
                for (var i = 0; i < 2; i++)
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), Main.rand.NextFloat(top, arena.Bottom - 20f)),
                        DustID.Water, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(6f, 8f)), 0,
                        default, 1.1f);
                    d.noGravity = true;
                }
                break;
            }

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
                // All over the battle's height, so it shows right away; a cold light so it's seen at night too
                for (var i = 0; i < 5; i++)
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), Main.rand.NextFloat(top, arena.Bottom)),
                        DustID.Snow, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(1.5f, 3f)), 0,
                        Color.White, Main.rand.NextFloat(0.9f, 1.3f));
                    d.noGravity = true;
                }

                if (weather == BattleWeather.Hail && Main.rand.NextBool(2))
                {
                    var d = Dust.NewDustPerfect(new Vector2(RandomX(arena), Main.rand.NextFloat(top, arena.Bottom - 60f)),
                        DustID.Ice, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(7f, 10f)), 0,
                        default, 1.1f);
                    d.noGravity = true;
                }

                for (var x = arena.Left; x < arena.Right; x += 48f)
                {
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 32f), 0.3f, 0.38f, 0.5f);
                    Lighting.AddLight(new Vector2(x, arena.Bottom - 160f), 0.3f, 0.38f, 0.5f);
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

    private static bool Has(uint conditions, SideCondition condition) => (conditions & (1u << (int)condition)) != 0;

    /// <summary>
    ///     The effects of one side's conditions; <paramref name="own" /> is that side's Pokémon and
    ///     <paramref name="foe" /> the one it faces.
    /// </summary>
    private static void Side(Arena arena, uint conditions, Vector2 own, Vector2 foe)
    {
        if (conditions == 0)
            return;
        var toward = foe.X >= own.X ? 1f : -1f;

        if (Has(conditions, SideCondition.Tailwind))
        {
            // Wind streaks blowing from behind the Pokémon towards the foe, carrying a few leaves
            var from = own.X - toward * Margin;
            for (var i = 0; i < 3; i++)
            {
                var d = Dust.NewDustPerfect(
                    new Vector2(from + toward * Main.rand.NextFloat(0f, 80f), own.Y + Main.rand.NextFloat(-90f, 40f)),
                    DustID.Cloud, new Vector2(toward * Main.rand.NextFloat(9f, 14f), Main.rand.NextFloat(-0.4f, 0.4f)), 150,
                    default, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }

            if (Main.rand.NextBool(3))
            {
                var d = Dust.NewDustPerfect(new Vector2(from, own.Y + Main.rand.NextFloat(-70f, 30f)), DustID.Grass,
                    new Vector2(toward * Main.rand.NextFloat(6f, 9f), Main.rand.NextFloat(-1.5f, 0.5f)), 0, default, 1.2f);
                d.noGravity = true;
            }
        }

        // Screens: a thin shimmering wall between the Pokémon and its foe
        Wall(conditions, SideCondition.Reflect, DustID.GoldFlame, own, toward, 0.6f, 0.5f, 0.15f);
        Wall(conditions, SideCondition.LightScreen, DustID.PinkTorch, own, toward, 0.6f, 0.3f, 0.5f);
        Wall(conditions, SideCondition.Safeguard, DustID.BlueTorch, own, toward, 0.2f, 0.4f, 0.7f);
        Wall(conditions, SideCondition.Mist, DustID.IceTorch, own, toward, 0.4f, 0.55f, 0.6f);

        // Entry hazards lie on the ground in front of the Pokémon
        Hazard(arena, conditions, SideCondition.StealthRock, DustID.Stone, own, toward, 1.3f);
        Hazard(arena, conditions, SideCondition.Spikes, DustID.Iron, own, toward, 1f);
        Hazard(arena, conditions, SideCondition.ToxicSpikes, DustID.Venom, own, toward, 1f);
        Hazard(arena, conditions, SideCondition.StickyWeb, DustID.Web, own, toward, 1f);
    }

    private static void Wall(uint conditions, SideCondition condition, int dust, Vector2 own, float toward, float r,
        float g, float b)
    {
        if (!Has(conditions, condition))
            return;
        var x = own.X + toward * 40f;
        if (Main.rand.NextBool(2))
        {
            var d = Dust.NewDustPerfect(new Vector2(x + Main.rand.NextFloat(-3f, 3f), own.Y + Main.rand.NextFloat(-56f, 32f)),
                dust, new Vector2(0f, Main.rand.NextFloat(-0.6f, -0.2f)), 100, default, 1.1f);
            d.noGravity = true;
        }

        Lighting.AddLight(new Vector2(x, own.Y - 16f), r, g, b);
        Lighting.AddLight(new Vector2(x, own.Y + 16f), r, g, b);
    }

    private static void Hazard(Arena arena, uint conditions, SideCondition condition, int dust, Vector2 own, float toward,
        float scale)
    {
        if (!Has(conditions, condition) || !Main.rand.NextBool(5))
            return;
        var x = own.X + toward * Main.rand.NextFloat(-24f, 72f);
        var d = Dust.NewDustPerfect(new Vector2(x, Ground(x, arena.Bottom) - 2f), dust,
            new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.8f, -0.2f)), 0, default, scale);
        d.noGravity = true;
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
