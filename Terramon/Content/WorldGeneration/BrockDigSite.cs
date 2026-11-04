using Terraria.Localization;

namespace Terramon.Content.WorldGeneration;

/// <summary>
///     The way down to Brock's Gym (worlds with the dungeon): a fossil dig. Pewter City is the town of fossils, and
///     the dungeon below is older than the gym. A rocky outcrop with its layers showing (stone, granite, clay), the
///     skeleton of a huge ancient creature lying on it with its ribcage arching over the dig pit and its skull resting
///     on the cliff, a wooden scaffold with a crane over the pit, lanterns, barrels and the sign. The rope goes down the
///     pit. The design is <see cref="Template" />, drawn by tools/structures/brock_dig_site.py.
/// </summary>
public static class BrockDigSite
{
    /// <summary>
    ///     One character per tile; the last row sits on the ground's surface.
    ///     R stone, N granite, D clay (the rock's layers), b fossil bone, e the skull's eye, w wooden beam, - wooden
    ///     platform, W planks, | rope (the middle one goes down to the dungeon), ~ crane rope, L lantern, C barrel, s sign,
    ///     = shaft wall, . the dug-out pit. Spaces are open air up high and the ground left as it is lower down.
    /// </summary>
    private static readonly string[] Template =
    [
        "                            wwwwwwwwww                                     ",
        "                            w   bbbbb~bbb                 bbbbbbb          ",
        "                       -------bbb    ~b  bbbb           bbbbbbbbbbbbb      ",
        "                       Lw bbwb   b   ~   b  bbb        bbbebbbbbbbbbbbbb   ",
        "                        wbb w b  b   ~  b  b   bb      bbbbbbbbbbbbbbbbbb  ",
        "                      bbw |bw b  b   ~  b  b   b bb    bb b b b b b bbbbb  ",
        "                     b bw |bw b  b   ~  b  b  b  b b bb b             bbb  ",
        "                    b  -------b  b   ~  b  b  b  b  b   bb b b b b bbbbb   ",
        "                  bb    w |bwLb   b b~  b  b  b  b    RRRbbbbbbbbbbbbbb    ",
        "                 b b    w |bw  b  b b~ b  b   b  b    RRRRRRRRRRRRRRR      ",
        "                b  b    w | w  b  b b~ b  b   b  b   RRRRRRRRRRNNNNNNR     ",
        "               b   b    w | w  b  b bL b  b  b       RNNNNNNNNNRRRRRRR     ",
        "              b    b   -------         b     b      RDRRRRRRRRRDDDDDDDR    ",
        "             b     b    w | w   wwww|wwww           DDDDDDDDDDDDDDDDDDD    ",
        "           bb      b    w | w   w   |   w          RRRDDDDDDDDDRRRRRRRRR   ",
        "          b        b    w | CCs w   |   w  C       RRRRRRRRRRRRRRRRRRRRR   ",
        "         bRRRRRRR  bRR RRRR RRRRw ..|...wRRRR  R  RNNNRRRRRRRRRNNNNNNNNNR  ",
        "        bRRRRRRRRRRNNNRNNNNRRRRRw...|...wNNNNRRRRRRRRRNNNNNNNNNRRRRRRRRRN  ",
        "       b DDDDDDDDDRRRRRRRRRDDDDDw...|...wRRRRDDDDDDDDDRRRRRRRRRDDDDDDDDDR  ",
        "     bbRRDDDDDDDDDDDDDDDDDDDDDDDw...|...wDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD  ",
        "      RDDRRRRRRRRRDDDDDDDDDRRRRRw...|...wDDDDRRRRRRRRRDDDDDDDDDRRRRRRRRRD  ",
        "      RRRRRRRRRRRRRRRRRRRRRRRRRRw...|...wRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR  ",
        "      RRRNNNNNNNNNRRRRRRRRRNNNNNwWW=|=WWwRRRRNNNNNNNNNRRRRRRRRRNNNNNNNNNR  ",
    ];

    private const int Center = 36; // The rope's column
    private const int Open = 7; // The bottom rows where spaces keep the ground as it is

    private static int Width => Template[0].Length;
    private static int Height => Template.Length;

    /// <summary>
    ///     The dig site's tiles, for a rope at <paramref name="shaftX" /> and the ground at <paramref name="surface" />.
    /// </summary>
    public static Rectangle Area(int shaftX, int surface)
        => new(shaftX - Center, surface - Height + 1, Width, Height);

    /// <summary>
    ///     Builds the dig site and returns the row where the rope down to the dungeon starts.
    /// </summary>
    public static int Build(int shaftX, int surface)
    {
        var area = Area(shaftX, surface);
        var ropeTop = surface;
        var later = new List<(Point At, char What)>();

        for (var row = 0; row < Height; row++)
        {
            var y = area.Y + row;
            var low = row >= Height - Open;
            for (var col = 0; col < Width; col++)
            {
                var x = area.X + col;
                var ch = Template[row][col];
                if (ch == ' ' && low)
                    continue; // The ground around stays as it is

                var tile = Main.tile[x, y];
                if (tile.HasTile && TileID.Sets.IsATreeTrunk[tile.TileType])
                    WorldGen.KillTile(x, y, false, false, true); // Whole trees go, not just the bits in the way
                tile.ClearEverything();
                switch (ch)
                {
                    case 'R': Block(x, y, TileID.Stone); break;
                    case 'N': Block(x, y, TileID.Granite); break;
                    case 'D': Block(x, y, TileID.ClayBlock); break;
                    case 'b': Block(x, y, TileID.BoneBlock); break;
                    case 'W': Block(x, y, TileID.WoodBlock); break;
                    case 'e': tile.WallType = WallID.Stone; break;
                    case '.': tile.WallType = WallID.Dirt; break;
                    case '=': tile.WallType = WallID.Dirt; break;
                    case 'w':
                        tile.HasTile = true;
                        tile.TileType = TileID.WoodenBeam;
                        break;
                    case '-':
                    case '|':
                    case '~':
                    case 'L':
                    case 'C':
                    case 's':
                        if (ch == '|' && col == Center)
                            ropeTop = Math.Min(ropeTop, y);
                        if (low)
                            tile.WallType = WallID.Dirt;
                        later.Add((new Point(x, y), ch));
                        break;
                }
            }
        }

        // Nothing floating: under the outcrop, stone down to the ground (but not in the shaft)
        for (var col = 0; col < Width; col++)
        {
            var x = area.X + col;
            if (Template[Height - 1][col] == ' ' || Math.Abs(col - Center) <= 1)
                continue;
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

        // Platforms and ropes (top to bottom, each rope hangs from the one above), then what hangs or stands on them
        foreach (var (p, what) in later.Where(l => l.What is '-' or '|' or '~').OrderBy(l => l.At.Y))
        {
            var tile = Main.tile[p.X, p.Y];
            if (what == '-')
                WorldGen.PlaceTile(p.X, p.Y, TileID.Platforms, true, true);
            else
            {
                tile.HasTile = true;
                tile.TileType = TileID.Rope;
            }
        }

        foreach (var (p, what) in later.Where(l => l.What is 'L' or 'C' or 's'))
        {
            switch (what)
            {
                case 'L':
                    if (!WorldGen.PlaceObject(p.X, p.Y, TileID.HangingLanterns, true, 1))
                        WorldGen.PlaceTile(p.X, p.Y, TileID.Torches, true, true);
                    break;
                case 'C':
                    WorldGen.PlaceChest(p.X, p.Y, TileID.Containers, false, 5); // Barrel
                    break;
                case 's':
                    if (WorldGen.PlaceObject(p.X, p.Y, TileID.Signs, true))
                    {
                        var sign = Sign.ReadSign(p.X, p.Y - 1, true);
                        if (sign >= 0)
                            Sign.TextSign(sign, Language.GetTextValue("Mods.Terramon.BrockGym.Sign"));
                    }

                    break;
            }
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
