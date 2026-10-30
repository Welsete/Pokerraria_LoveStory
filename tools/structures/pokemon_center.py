"""Pokemon Center as a Blueprint Builder design (formatVersion 1): two floors, the healing counter downstairs and the
PC room upstairs, a giant Poke Ball on the roof. Objects use their top-left tile.
Run: python3 tools/structures/pokemon_center.py Terramon/Assets/Structures/PokemonCenter.json tools/structures/terraria-ids.txt [--preview]   (--preview leaves out the mod's own PCs, for the
Blueprint Builder mod, which only knows vanilla items)"""
import json, sys

W, H = 96, 52
GROUND = 51              # grass row outside
FLOOR = 50               # first floor
MID = 38                 # second floor (mezzanine)
CEIL = 28                # white fascia under the roof
L, R = 14, 81            # outer walls
COUNTER = 47             # counter top (half bricks)
cells, objects = {}, []
BLACK, WHITE = 25, 26
SIZES = {"GlassDoor": (1, 3), "HeartStatue": (2, 3), "HeartLantern": (1, 2), "CrystalChandelier": (3, 3),
         "MarbleSofa": (3, 2), "PottedForestTree": (2, 4), "PottedForestPalm": (2, 4), "MarbleBookcase": (3, 4),
         "TrashCan": (2, 2), "MarbleTable": (3, 2), "MarbleChair": (1, 2), "LampPost": (1, 6), "Bench": (3, 2),
         "PureWaterFountain": (2, 4), "Sunflower": (2, 4), "RedBanner": (1, 3), "CrystalLamp": (1, 3),
         "Terramon/PCItemRed": (2, 3), "Terramon/PCItemWhite": (2, 3), "MarbleWorkBench": (2, 1), "Book": (1, 1)}
PREVIEW = "--preview" in sys.argv


def cell(x, y):
    return cells.setdefault((x, y), {"x": x, "y": y})


def tile(x, y, name, paint=0, **kw):
    c = cell(x, y)
    c["tile"] = name
    for k in ("tilePaint", "slope", "halfBrick"):
        c.pop(k, None)
    if paint:
        c["tilePaint"] = paint
    c.update(kw)


def clear_tile(x, y):
    c = cells.get((x, y))
    if c:
        for k in ("tile", "tilePaint", "slope", "halfBrick"):
            c.pop(k, None)


def wall(x, y, name, paint=0):
    c = cell(x, y)
    c["wall"] = name
    c.pop("wallPaint", None)
    if paint:
        c["wallPaint"] = paint


def obj(x, y, item, right=False):
    if PREVIEW and item.startswith("Terramon/"):
        return
    o = {"x": x, "y": y, "item": item}
    if right:
        o["facingRight"] = True
    objects.append(o)


def stand(x, item, floor=FLOOR, right=False):
    obj(x, floor - SIZES[item][1], item, right)


# --- Ground: grass, stone slab paths to both doors ---
for x in range(W):
    tile(x, GROUND, "GrassSeeds")
for x in list(range(4, 13)) + list(range(83, 92)):
    tile(x, GROUND, "StoneSlab")

# --- Plinth and floors ---
for x in range(L - 2, R + 3):
    tile(x, FLOOR, "MarbleBlock")
tile(L - 2, FLOOR, "MarbleBlock", halfBrick=True)
tile(R + 2, FLOOR, "MarbleBlock", halfBrick=True)
for x in range(L + 1, R):
    tile(x, MID, "MarbleBlock")

# --- Outer walls, glass doors at the bottom, red stripes ---
for y in range(CEIL + 1, FLOOR):
    for x in (L, R):
        if y < FLOOR - 3:
            tile(x, y, "SnowBrick")
obj(L, FLOOR - 3, "GlassDoor")
obj(R, FLOOR - 3, "GlassDoor")
for x in (L, R):
    tile(x, 45, "RubyGemsparkBlock")
    tile(x, 34, "RubyGemsparkBlock")

# --- Curved red roof and white fascia ---
rows = {27: (11, 84), 26: (12, 83), 25: (14, 81), 24: (17, 78), 23: (21, 74), 22: (26, 69), 21: (32, 63)}
for y, (a, b) in rows.items():
    for x in range(a, b + 1):
        tile(x, y, "RedDynastyShingles")
    tile(a, y, "RedDynastyShingles", slope=2)
    tile(b, y, "RedDynastyShingles", slope=1)
for x in range(11, 85):
    tile(x, CEIL, "SnowBrick")

# --- Back walls: white marble, glowing red bands, big windows on both floors ---
for x in range(L + 1, R):
    for y in range(CEIL + 1, FLOOR):
        wall(x, y, "MarbleBlockWall", WHITE)
    for band in (45, 34):
        wall(x, band, "RubyGemsparkWall")
        wall(x, band + 1, "SnowBrickWall")
for left in (17, 26, 62, 71):
    for x in range(left, left + 7):
        for y in range(40, 44):
            wall(x, y, "GlassWall")
for left in (17, 26, 40, 49, 62, 71):
    for x in range(left, left + 7):
        for y in range(30, 33):
            wall(x, y, "GlassWall")

# --- Columns framing the counter, downstairs ---
for x in (36, 37, 58, 59):
    for y in range(MID + 1, FLOOR):
        tile(x, y, "MarbleColumn")

# --- Counter: white top, glowing red front; Nurse Joy stands at column 47 ---
for x in range(39, 57):
    if not 46 <= x <= 48:  # gap where Joy stands, with the healing machine on the counter beside her
        tile(x, COUNTER, "MarbleBlock", halfBrick=True)
    wall(x, COUNTER + 1, "RubyGemsparkWall")
    wall(x, COUNTER + 2, "SnowBrickWall")
for x in (39, 56):
    for y in (COUNTER + 1, COUNTER + 2):
        tile(x, y, "SnowBrick")

# --- Heart mural behind the counter ---
heart = [".RR...RR.",
         "RRRRRRRRR",
         "RRRRRRRRR",
         "RRRRRRRRR",
         ".RRRRRRR.",
         "..RRRRR..",
         "...RRR..."]
for r, row in enumerate(heart):
    for c, ch in enumerate(row):
        if ch == "R":
            wall(44 + c, 39 + r, "RubyGemsparkWall")
        elif ch == "W":
            wall(44 + c, 39 + r, "DiamondGemsparkWall")

# --- Stairs up to the second floor (left wing), with an opening in the floor above them ---
# 12 steps going up to the right; the last one sits in the floor row and joins the floor at x=36
for i in range(12):
    tile(24 + i, FLOOR - 1 - i, "MarblePlatform", slope=2)
for x in range(29, 35):
    clear_tile(x, MID)

# --- Poke Ball emblem crowning the roof ---
R2 = 7.5
for dy in range(15):
    for dx in range(15):
        ex, ey = dx - 7, dy - 7
        d = (ex * ex + ey * ey) ** 0.5
        if d > R2:
            continue
        x, y = 41 + dx, 6 + dy
        if d > R2 - 1.2 or (ey == 0 and abs(ex) > 2) or 1.5 < d <= 2.9:
            tile(x, y, "DiamondGemsparkBlock", BLACK)
        elif d <= 1.5:
            tile(x, y, "DiamondGemsparkBlock")
        elif ey < 0:
            tile(x, y, "RubyGemsparkBlock")
        else:
            tile(x, y, "DiamondGemsparkBlock")
tile(44, 9, "DiamondGemsparkBlock")
tile(45, 8, "DiamondGemsparkBlock")
for x in range(43, 53):
    tile(x, 20, "SnowBrick")
for x in range(45, 51):
    tile(x, 19, "SnowBrick") if (x, 19) not in cells else None

# --- First floor furniture ---
obj(42, MID + 1, "HeartLantern")
obj(53, MID + 1, "HeartLantern")
obj(19, MID + 1, "CrystalChandelier")
obj(70, MID + 1, "CrystalChandelier")
stand(16, "PottedForestTree")
stand(19, "MarbleSofa")
stand(23 - 2, "TrashCan") if False else None
stand(61, "MarbleTable")
stand(60, "MarbleChair")
stand(64, "MarbleChair", right=True)
stand(66, "MarbleSofa")
stand(70, "MarbleBookcase")
stand(74, "CrystalLamp")
stand(78, "PottedForestPalm")

# --- Second floor: the PC room ---
stand(40, "Terramon/PCItemRed", MID)
stand(44, "Terramon/PCItemWhite", MID)
stand(48, "Terramon/PCItemRed", MID)
stand(52, "Terramon/PCItemWhite", MID)
stand(17, "MarbleBookcase", MID)
stand(21, "MarbleSofa", MID)
stand(57, "MarbleTable", MID)
stand(56, "MarbleChair", MID)
stand(60, "MarbleChair", MID, right=True)
stand(64, "MarbleBookcase", MID)
stand(68, "MarbleSofa", MID)
stand(73, "TrashCan", MID)
stand(78, "PottedForestTree", MID)
obj(46, CEIL + 1, "CrystalChandelier")
obj(24, CEIL + 1, "HeartLantern")
obj(71, CEIL + 1, "HeartLantern")

# --- Outside: lamps, fountain, bench, flowers, banners under the eaves, hedges ---
stand(9, "LampPost", GROUND)
stand(86, "LampPost", GROUND)
stand(4, "PureWaterFountain", GROUND)
stand(89, "Bench", GROUND)
stand(1, "Sunflower", GROUND)
stand(93, "Sunflower", GROUND)
obj(12, CEIL + 1, "RedBanner")
obj(83, CEIL + 1, "RedBanner")
for x in list(range(6, 9)) + list(range(10, 12)) + list(range(84, 86)) + list(range(87, 89)):
    for y in (GROUND - 2, GROUND - 1):
        wall(x, y, "LivingLeafWall")

design = {"formatVersion": 1, "name": "Centro Pokemon", "width": W, "height": H,
          "cells": sorted(cells.values(), key=lambda c: (c["y"], c["x"])), "objects": objects}

if __name__ == "__main__":
    ids = {l.split()[2] for l in open(sys.argv[2]) if l.startswith("IDDUMP ItemID")}
    names = {c.get(k) for c in design["cells"] for k in ("tile", "wall")} | {o["item"] for o in objects}
    bad = sorted(n for n in names if n and n not in ids and not n.startswith("Terramon/"))
    assert not bad, bad
    json.dump(design, open(sys.argv[1], "w", encoding="utf-8"), separators=(",", ":"), ensure_ascii=False)
    print(len(design["cells"]), "cells", len(objects), "objects")
