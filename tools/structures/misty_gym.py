"""Builds Misty's Gym (beach aquarium gym) as a Blueprint Builder design (formatVersion 1), the file the mod builds
in new worlds (see Blueprint.cs). It can also be opened in the Blueprint Builder mod to look at it in game.
Objects use their top-left tile; names are Terraria item names, checked against terraria-ids.txt.
Run: python3 tools/structures/misty_gym.py Terramon/Assets/Structures/MistyGym.json tools/structures/terraria-ids.txt"""
import json, sys

W, H = 80, 40
FLOOR = 28           # row of the gym floor blocks
L, R = 6, 73         # glass posts of the hall
cells, objects = {}, []
BLACK, DEEPBLUE, DEEPSKY = 25, 21, 20


def cell(x, y):
    return cells.setdefault((x, y), {"x": x, "y": y})


def tile(x, y, name, **kw):
    c = cell(x, y)
    c["tile"] = name
    c.update(kw)


def wall(x, y, name, paint=0):
    c = cell(x, y)
    c["wall"] = name
    c.pop("wallPaint", None)
    if paint:
        c["wallPaint"] = paint


def obj(x, y, item, right=False):
    o = {"x": x, "y": y, "item": item}
    if right:
        o["facingRight"] = True
    objects.append(o)


def stand(x, item, height, right=False):
    """Furniture standing on the gym floor."""
    obj(x, FLOOR - height, item, right)


# --- Beach: sand on both sides and under the aquarium ---
for x in range(W):
    top = 30 if x in (0, 1, W - 1, W - 2) else 29
    for y in range(top, H):
        if 8 <= x <= 71 and y <= 38:
            continue  # aquarium
        tile(x, y, "SandBlock")
for x, item in [(2, "Starfish"), (4, "Coral"), (75, "Seashell"), (77, "Coral")]:
    obj(x, 28, item)

# --- Aquarium under the floor ---
for y in range(29, 39):
    tile(8, y, "Glass")
    tile(71, y, "Glass")
for x in range(8, 72):
    tile(x, 38, "Glass")
for x in range(9, 71):
    tile(x, 37, "SandBlock")
    for y in range(29, 38):
        wall(x, y, "BlueStainedGlass")
rocks = [(14, 36), (15, 36), (16, 36), (15, 35), (30, 36), (31, 36), (41, 36), (42, 36), (43, 36), (42, 35),
         (43, 35), (42, 34), (58, 36), (59, 36), (60, 36), (59, 35)]
for x, y in rocks:
    tile(x, y, "CoralstoneBlock")
for x in (11, 18, 22, 27, 34, 38, 46, 50, 54, 62, 66, 69):
    obj(x, 36, "Coral")
# (no candles in the aquarium: the mod fills it with water)
for x, item in [(25, "Starfish"), (36, "Seashell"), (48, "Starfish"), (56, "Seashell"), (20, "Seashell"),
                (40, "Starfish"), (64, "Starfish")]:
    obj(x, 36, item)

# --- Floor: palm wood deck over the aquarium ---
for x in range(L, R + 1):
    tile(x, FLOOR, "PalmWood")

# --- Glass frame and glass back wall ---
for y in range(4, 21):
    tile(L, y, "Glass")
    tile(R, y, "Glass")
for x in range(L, R + 1):
    tile(x, 4, "Glass")
for x in range(22, 58):
    tile(x, 2, "Glass")
for y in (2, 3):
    tile(22, y, "Glass")
    tile(57, y, "Glass")
for x in range(L + 1, R):
    for y in range(5, FLOOR):
        wall(x, y, "GlassWall")
for x in range(23, 57):
    wall(x, 3, "GlassWall")

# --- Marble columns ---
for left in (8, 18, 57, 67):
    for x in range(left, left + 5):
        tile(x, 5, "MarbleBlock")
        tile(x, 27, "MarbleBlock", halfBrick=True)
    for x in range(left + 1, left + 4):
        for y in range(6, 27):
            tile(x, y, "MarbleColumn")

# --- Painting: Poke Ball and water drop in colored gemspark walls, wooden frame ---
for x in range(25, 55):
    for y in range(6, 23):
        wall(x, y, "LivingWoodWall")
for x in range(26, 54):
    for y in range(7, 22):
        wall(x, y, "DiamondGemsparkWall", DEEPSKY) if x < 40 else wall(x, y, "EmeraldGemsparkWall")
ball = ["....KKKK....",
        "..KKRRRRKK..",
        ".KRWRRRRRRK.",
        ".KRWRRRRRRK.",
        "KRRRRKKRRRRK",
        "KKKKKWWKKKKK",
        "KKKKKWWKKKKK",
        "KWWWWKKWWWWK",
        "KWWWWWWWWWWK",
        ".KWWWWWWWWK.",
        "..KKWWWWKK..",
        "....KKKK...."]
drop = ["....DD....",
        "....BB....",
        "...DBBD...",
        "...BBBB...",
        "..DBBBBD..",
        "..BBBBBB..",
        ".DBBBBBBD.",
        ".BBLBBBBB.",
        "DBLBBBBBBD",
        "DBLBBBBBBD",
        "DBBLBBBBBD",
        ".DBBBBBBD.",
        "..DDDDDD.."]
pixels = {"K": ("DiamondGemsparkWall", BLACK), "R": ("RubyGemsparkWall", 0), "W": ("DiamondGemsparkWall", 0),
          "B": ("SapphireGemsparkWall", 0), "D": ("SapphireGemsparkWall", DEEPBLUE), "L": ("DiamondGemsparkWall", 0)}
for art, left, top in ((ball, 27, 8), (drop, 42, 8)):
    for r, row in enumerate(art):
        for c, ch in enumerate(row):
            if ch != ".":
                wall(left + c, top + r, *pixels[ch])

# --- Lights ---
for x in (15, 64):
    for y in range(5, 9):
        tile(x, y, "Chain")
    tile(x, 9, "Glass")
    obj(x, 10, "MarbleLantern")
for x in (30, 49):
    obj(x, 5, "MarbleLantern")
obj(23, 5, "JellyfishBanner")
obj(56, 5, "JellyfishBanner")
obj(L + 1, 15, "CoralTorch")
obj(R - 1, 15, "CoralTorch")

# --- Wall trophies ---
obj(13, 15, "SwordfishTrophy")
obj(62, 15, "GoldfishTrophy")

# --- Furniture on the floor ---
stand(7, "ClayPot", 1)
stand(72, "ClayPot", 1)
stand(13, "SharkStatue", 3)
stand(16, "ClayPot", 1)
stand(24, "MarbleTable", 2)
obj(24, FLOOR - 4, "FishBowl")
obj(26, FLOOR - 3, "WaterCandle")
stand(28, "CrabStatue", 3)
stand(51, "PalmWoodTable", 2)
obj(51, FLOOR - 3, "Seashell")
obj(52, FLOOR - 3, "Starfish")
obj(53, FLOOR - 3, "WaterCandle")
stand(54, "PalmWoodBookcase", 4)
stand(62, "StarStatue", 3)
stand(64, "JellyfishStatue", 3)

design = {"formatVersion": 1, "name": "Misty Gym", "width": W, "height": H,
          "cells": sorted(cells.values(), key=lambda c: (c["y"], c["x"])), "objects": objects}

if __name__ == "__main__":
    ids = {l.split()[2] for l in open(sys.argv[2]) if l.startswith("IDDUMP ItemID")}
    names = {c.get(k) for c in design["cells"] for k in ("tile", "wall")} | {o["item"] for o in objects}
    bad = sorted(n for n in names if n and n not in ids)
    assert not bad, bad
    json.dump(design, open(sys.argv[1], "w", encoding="utf-8"), separators=(",", ":"), ensure_ascii=False)
    print(len(design["cells"]), "cells", len(objects), "objects")
