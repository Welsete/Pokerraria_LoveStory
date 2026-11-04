using Terraria.Localization;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The temple over the rope down to Brock's Gym (worlds with the dungeon): a two-tier stone pagoda on a rocky mound,
///     after Welsete's reference picture (Desktop/Pokerraria/referencias) and the temple entrances of A Link to Terraria
///     and The Story of Red Cloud. Boulder Badge in the upper gable, light marble columns, a dark hall with hanging
///     lanterns, stone lanterns on both sides, stairs up the mound and rubble around it. The design is
///     <see cref="Template" />, drawn by tools/structures/brock_temple.py.
/// </summary>
public static class BrockTemple
{
    /// <summary>
    ///     One character per tile; the rope is the middle column and the last row sits on the ground's surface.
    ///     K dark brick (lead), S stone slab, B gray brick, R stone, P marble column, O/o the badge (dark/light),
    ///     # dark back wall, L hanging lantern, T torch, t stone lantern light, | rope, = shaft wall, r rubble,
    ///     space: open air above the mound, the ground left as it is lower down.
    /// </summary>
    private static readonly string[] Template =
    [
        "                            K                            ",
        "                            K                            ",
        "                          KSSSK                          ",
        "                         KSSSSSK                         ",
        "                        KSSSSSSSK                        ",
        "                       KSSSSSSSSSK                       ",
        "                 KK   KSSSSSSSSSSSK   KK                 ",
        "                  KKKKKKKKKKKKKKKKKKKKK                  ",
        "                  KBBBBBBBBBBBBBBBBBBBK                  ",
        "                   KSSSSSSSSSSSSSSSSSK                   ",
        "                     KSSSSSOOOSSSSSK                     ",
        "                     KSSSSOoooOSSSSK                     ",
        "                     KSSSSOoOoOSSSSK                     ",
        "                     KSSSSOoooOSSSSK                     ",
        "                     KSSSSSOOOSSSSSK                     ",
        "            KK       KSSSSSSSSSSSSSK       KK            ",
        "             KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK             ",
        "             KBBBBBBBBBBBBBBBBBBBBBBBBBBBBBK             ",
        "              KSSSSSSSSSSSSSSSSSSSSSSSSSSSK              ",
        "                  KPK###############KPK                  ",
        "                  SPS###############SPS                  ",
        "                  SPS#L###########L#SPS                  ",
        "                  SPS###############SPS                  ",
        "                  SPS###############SPS                  ",
        "                  SPS###############SPS                  ",
        "             K    SPS###############SPS    K             ",
        "           KKKKK  SPS###T#######T###SPS  KKKKK           ",
        "            KtK   SPS#######|#######SPS   KtK            ",
        "             S    KPK#######|#######KPK    S             ",
        "           rrKK SSSSSSSSSSSS|SSSSSSSSSSSS KKK            ",
        "      rr  rrrBBBBBBBBBBBBBBB|BBBBBBBBBBBBBBB             ",
        "     rrr  BBBRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRBBB  rr      ",
        "       BBBRRRRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRRRRBrrr  rr  ",
        "    BBBRRRRRRRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRRRRRRRBBrrr  ",
        " BBBRRRRRRRRRRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRRRRRRRRRRBBB ",
        "BRRRRRRRRRRRRRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRRRRRRRRRRRRRB",
        "RRRRRRRRRRRRRRRRRRRRRRRRRRR=|=RRRRRRRRRRRRRRRRRRRRRRRRRRR",
    ];

    private static int Width => Template[0].Length;
    private static int Height => Template.Length;
    private static int Center => Width / 2;
    private const int MoundHeight = 7; // Bottom rows of the template that are the mound (its spaces keep the ground)

    /// <summary>
    ///     The temple's tiles, for a rope at <paramref name="shaftX" /> and the ground at <paramref name="surface" />.
    /// </summary>
    public static Rectangle Area(int shaftX, int surface)
        => new(shaftX - Center, surface - Height + 1, Width, Height);

    /// <summary>
    ///     Builds the temple and returns the row where the rope starts (sticking out of the temple's floor).
    /// </summary>
    public static int Build(int shaftX, int surface)
    {
        var area = Area(shaftX, surface);
        var ropeTop = surface;
        var lanterns = new List<Point>();
        var rubble = new List<Point>();

        for (var row = 0; row < Height; row++)
        {
            var y = area.Y + row;
            var mound = row >= Height - MoundHeight;
            for (var col = 0; col < Width; col++)
            {
                var x = area.X + col;
                var ch = Template[row][col];
                if (ch == ' ' && mound)
                    continue; // The ground around the mound stays as it is

                var tile = Main.tile[x, y];
                if (tile.HasTile && TileID.Sets.IsATreeTrunk[tile.TileType])
                    WorldGen.KillTile(x, y, false, false, true); // Whole trees go, not just the bits in the way
                tile.ClearEverything();
                switch (ch)
                {
                    case 'K': Block(x, y, TileID.LeadBrick); break;
                    case 'S': Block(x, y, TileID.StoneSlab); break;
                    case 'B': Block(x, y, TileID.GrayBrick); break;
                    case 'R': Block(x, y, TileID.Stone); break;
                    case 'P': Block(x, y, TileID.MarbleBlock); break;
                    case 'O': Block(x, y, TileID.LeadBrick); break;
                    case 'o': Block(x, y, TileID.MarbleBlock); break;
                    case '#': tile.WallType = WallID.LeadBrick; break;
                    case 'L':
                        tile.WallType = WallID.LeadBrick;
                        lanterns.Add(new Point(x, y));
                        break;
                    case 'T':
                        tile.WallType = WallID.LeadBrick;
                        WorldGen.PlaceTile(x, y, TileID.Torches, true, true);
                        break;
                    case 't':
                        tile.WallType = WallID.StoneSlab;
                        WorldGen.PlaceTile(x, y, TileID.Torches, true, true);
                        break;
                    case '|':
                        ropeTop = Math.Min(ropeTop, y);
                        tile.WallType = mound ? WallID.GrayBrick : WallID.LeadBrick;
                        break;
                    case '=': tile.WallType = WallID.GrayBrick; break;
                    case 'r': rubble.Add(new Point(x, y)); break;
                }
            }
        }

        // No floating mound: fill under it with stone down to the ground
        for (var col = 0; col < Width; col++)
        {
            var x = area.X + col;
            if (Template[Height - 1][col] == ' ' || Math.Abs(col - Center) <= 1)
                continue; // (the shaft goes on down there)
            for (var y = surface + 1; y < surface + 40; y++)
            {
                var tile = Main.tile[x, y];
                if (tile.HasTile && Main.tileSolid[tile.TileType])
                    break;
                tile.ClearTile();
                Block(x, y, TileID.Stone);
            }
        }

        WorldGen.RangeFrame(area.Left - 1, area.Top - 1, area.Right + 1, area.Bottom + 1);

        // Lanterns on short chains from the roof's underside
        foreach (var p in lanterns)
        {
            var ceiling = p.Y;
            while (ceiling > area.Top && !Main.tile[p.X, ceiling - 1].HasTile)
                ceiling--;
            for (var y = ceiling; y < p.Y; y++)
            {
                var chain = Main.tile[p.X, y];
                chain.HasTile = true;
                chain.TileType = TileID.Chain;
            }

            if (WorldGen.PlaceObject(p.X, p.Y, TileID.HangingLanterns, true, 1))
                continue;
            for (var y = ceiling; y < p.Y; y++)
                Main.tile[p.X, y].ClearTile();
            WorldGen.PlaceTile(p.X, p.Y, TileID.Torches, true, true);
        }

        // Rubble: big stone piles on the mound
        foreach (var p in rubble)
        {
            if (!Main.tile[p.X, p.Y].HasTile)
                WorldGen.PlaceTile(p.X, p.Y, TileID.LargePiles, true, false, -1, WorldGen.genRand.Next(7));
        }

        // The sign inside, next to the rope
        var floor = surface - MoundHeight;
        if (WorldGen.PlaceObject(shaftX + 3, floor - 1, TileID.Signs, true))
        {
            var sign = Sign.ReadSign(shaftX + 3, floor - 2, true);
            if (sign >= 0)
                Sign.TextSign(sign, Language.GetTextValue("Mods.Terramon.BrockGym.Sign"));
        }

        return ropeTop;
    }

    private static void Block(int x, int y, ushort type)
    {
        var tile = Main.tile[x, y];
        tile.HasTile = true;
        tile.TileType = type;
        tile.Slope = SlopeType.Solid;
        tile.IsHalfBlock = false;
    }
}
