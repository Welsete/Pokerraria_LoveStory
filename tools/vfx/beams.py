"""Solar Beam and Ice Beam: Isa's sheets have pieces of different sizes instead of a grid, so each piece is cut by
hand and laid out in frames where a beam ends at the frame's center (drawn on the target, it reaches back to the
user) and a burst sits on the center."""
import sys
import numpy as np
from PIL import Image
sys.path.insert(0, sys.argv[3])
from convert import load, shrink

SRC, OUT = sys.argv[1], sys.argv[2]
K = 4          # downscale
REACH = 150    # how far back a beam reaches, in output pixels

# (box in the sheet, kind): "beam" ends at the center, "burst" is centered
SOLAR = ("solar_beam_spritesheet_1792854445400.jpg", "SolarBeam", [
    ((690, 5, 1185, 195), "beam"),     # arrow with rings
    ((0, 205, 675, 370), "beam"),      # beam with its head
    ((685, 205, 1376, 370), "beam"),   # full beam
    ((0, 385, 925, 575), "beam"),      # pulsing beam
    ((0, 580, 350, 762), "beam"),      # beam breaking up
    ((990, 385, 1100, 575), "burst"),  # ring
    ((350, 580, 615, 762), "burst"),   # ring and crescents
    ((1140, 385, 1280, 575), "burst"), # crescents fading
], dict(Travel=0, Ticks=5, Face=1))
ICE = ("ice_beam_vfx_spritesheet_1792855768122.jpg", "IceBeam", [
    ((385, 80, 670, 300), "beam"),     # spiral point
    ((1025, 70, 1376, 310), "beam"),   # beam with rings
    ((0, 450, 485, 680), "beam"),      # beam with chevrons, breaking
    ((485, 490, 785, 640), "beam"),    # ice blocks
    ((785, 430, 1030, 700), "burst"),  # rings
    ((70, 80, 275, 300), "burst"),     # crystal
    ((1160, 510, 1260, 620), "burst"), # sparkles
], dict(Travel=0, Ticks=5, Face=1))

for file, move, pieces, settings in (SOLAR, ICE):
    a = load(f"{SRC}/{file}")
    frames = []
    for (x0, y0, x1, y1), kind in pieces:
        p = shrink(a[y0:y1, x0:x1], K)
        if kind == "beam" and p.shape[1] > REACH:
            p = p[:, -REACH:]  # keep the end that hits
        frames.append((p, kind))
    h = max(p.shape[0] for p, _ in frames)
    h += h % 2
    w = 2 * REACH
    strip = np.zeros((h, w * len(frames), 4), np.uint8)
    for i, (p, kind) in enumerate(frames):
        y = (h - p.shape[0]) // 2
        x = REACH - p.shape[1] if kind == "beam" else REACH - p.shape[1] // 2
        strip[y:y + p.shape[0], i * w + x:i * w + x + p.shape[1]] = p.clip(0, 255).astype(np.uint8)
    Image.fromarray(strip, "RGBA").save(f"{OUT}/{move}.png", optimize=True)
    open(f"{OUT}/{move}.txt", "w").write("\n".join([f"Frames {len(frames)}"] + [f"{k} {v}" for k, v in settings.items()] + ["Scale 1"]) + "\n")
    print(move, len(frames), (w, h))
