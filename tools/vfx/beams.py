"""Solar Beam, Ice Beam and Flamethrower: Isa's sheets have pieces of different sizes instead of a grid, so each piece
is cut by hand. A "beam" piece is laid out with its tip on the frame's center and REACH pixels long, and the game
stretches it from the user to the target (Beam A B in the .txt); a "burst" piece sits on the center."""
import sys
import numpy as np
from PIL import Image
sys.path.insert(0, sys.argv[3])
from convert import load, shrink

SRC, OUT = sys.argv[1], sys.argv[2]
REACH = 150

SHEETS = [
    ("solar_beam_spritesheet_1792854445400.jpg", "SolarBeam", 4, [
        ((110, 5, 240, 195), "burst"),     # light gathering
        ((415, 5, 610, 195), "burst"),     # flash
        ((690, 5, 1185, 195), "burst"),    # arrow with rings, flying
        ((0, 205, 675, 370), "beam"),      # beam with its head
        ((685, 205, 1376, 370), "beam"),   # full beam
        ((0, 385, 925, 575), "beam"),      # pulsing beam
        ((0, 580, 350, 762), "beam"),      # beam breaking up
        ((990, 385, 1100, 575), "burst"),  # ring
        ((350, 580, 615, 762), "burst"),   # ring and crescents
        ((1140, 385, 1280, 575), "burst"), # crescents fading
    ], dict(Charge=2, Travel=1, Beam="3 6", Ticks=5, Face=1)),
    ("ice_beam_vfx_spritesheet_1792855768122.jpg", "IceBeam", 4, [
        ((70, 80, 275, 300), "burst"),     # crystal forming
        ((385, 80, 670, 300), "burst"),    # spiral point, flying
        ((1025, 70, 1376, 310), "beam"),   # beam with rings
        ((0, 450, 485, 680), "beam"),      # beam with chevrons, breaking
        ((485, 490, 785, 640), "beam"),    # ice blocks
        ((785, 430, 1030, 700), "burst"),  # rings
        ((1160, 510, 1260, 620), "burst"), # sparkles
    ], dict(Charge=1, Travel=1, Beam="2 4", Ticks=5, Face=1)),
    ("flamethrower_vfx_spritesheet_final_1792855053226.jpg", "Flamethrower", 3, [
        ((40, 70, 280, 280), "beam"),
        ((350, 70, 640, 280), "beam"),
        ((690, 70, 1010, 280), "beam"),
        ((1030, 70, 1345, 280), "beam"),
        ((40, 300, 630, 560), "beam"),     # the big stream
        ((690, 340, 1030, 500), "beam"),   # stream ending in a fireball
        ((1040, 360, 1185, 490), "burst"), # fireballs
        ((1190, 410, 1340, 540), "burst"),
        ((280, 580, 440, 720), "burst"),   # smoke and embers
        ((450, 570, 620, 750), "burst"),
        ((690, 570, 860, 750), "burst"),
        ((910, 570, 1110, 750), "burst"),
        ((1190, 600, 1345, 720), "burst"),
    ], dict(Travel=0, Beam="0 5", Ticks=4, Face=1)),
]

for file, move, k, pieces, settings in SHEETS:
    a = load(f"{SRC}/{file}")
    frames = []
    for (x0, y0, x1, y1), kind in pieces:
        p = shrink(a[y0:y1, x0:x1], k).clip(0, 255).astype(np.uint8)
        if kind == "beam":
            # Every beam the same length, so the game can stretch it to the distance between the Pokémon
            p = np.array(Image.fromarray(p, "RGBA").resize((REACH, p.shape[0]), Image.NEAREST))
        frames.append((p, kind))
    h = max(p.shape[0] for p, _ in frames)
    h += h % 2
    w = max([2 * REACH] + [p.shape[1] + p.shape[1] % 2 for p, _ in frames])
    strip = np.zeros((h, w * len(frames), 4), np.uint8)
    for i, (p, kind) in enumerate(frames):
        y = (h - p.shape[0]) // 2
        x = w // 2 - p.shape[1] if kind == "beam" else w // 2 - p.shape[1] // 2
        strip[y:y + p.shape[0], i * w + x:i * w + x + p.shape[1]] = p
    Image.fromarray(strip, "RGBA").save(f"{OUT}/{move}.png", optimize=True)
    lines = [f"Frames {len(frames)}"] + [f"{key} {val}" for key, val in settings.items()] + ["Scale 1"]
    open(f"{OUT}/{move}.txt", "w").write("\n".join(lines) + "\n")
    print(move, len(frames), (w, h))
