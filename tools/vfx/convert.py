"""Turns Isa's generated move sheets (magenta background) into the frame strips MoveVisuals reads."""
import sys
import numpy as np
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]

# file, move, layout (cols, rows) or list of boxes, downscale, settings for the .txt
SHEETS = [
    ("aurora_beam_vfx_spritesheet_v3_1792871463131.jpg", "AuroraBeam", (4, 2), 3, dict(Travel=2, Ticks=5, Face=1)),
    ("blizzard_vfx_spritesheet_v2_1792871597489.jpg", "Blizzard", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("Bullet Punch.png", "BulletPunch",
     [(0, 0, 85, 85), (85, 0, 170, 85), (170, 0, 256, 85), (0, 85, 128, 170), (128, 85, 256, 170),
      (0, 185, 66, 256), (66, 185, 128, 256), (128, 185, 192, 256)], 1, dict(Travel=0, Ticks=3, Face=1)),
    ("ember_vfx_spritesheet_1792854135105.jpg", "Ember", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("flamethrower_vfx_spritesheet_final_1792855053226.jpg", "Flamethrower",
     [(40, 70, 280, 280), (350, 70, 640, 280), (690, 70, 1010, 280), (1030, 70, 1345, 280),
      (40, 300, 630, 560), (690, 340, 1030, 500), (1040, 360, 1185, 490), (1190, 410, 1340, 540),
      (40, 600, 280, 720), (280, 580, 440, 720), (450, 570, 620, 750), (690, 570, 860, 750),
      (910, 570, 1110, 750), (1190, 600, 1345, 720)], 3, dict(Travel=0, Ticks=4, Face=1, At="middle")),
    ("freeze_dry_vfx_spritesheet_1792871714552.jpg", "FreezeDry", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("haze_vfx_spritesheet_1792876842170.jpg", "Haze", (4, 2), 3, dict(Travel=0, Ticks=6)),
    ("ice_fang_vfx_spritesheet_1792856044803.jpg", "IceFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_punch_vfx_spritesheet_1792856062413.jpg", "IcePunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_shard_vfx_spritesheet_1792859337124.jpg", "IceShard", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("icicle_crash_vfx_spritesheet_1792871266443.jpg", "IcicleCrash", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("icicle_spear_vfx_spritesheet_1792871321463.jpg", "IcicleSpear", (4, 2), 3, dict(Travel=1, Ticks=4, Face=1)),
    ("icy_wind_vfx_spritesheet_1792876366324.jpg", "IcyWind", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1)),
    ("powder_snow_vfx_spritesheet_v2_1792876584939.jpg", "PowderSnow", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("sheer_cold_vfx_spritesheet_1792876713818.jpg", "SheerCold", (4, 4), 2, dict(Travel=0, Ticks=4)),
]


def load(path):
    im = Image.open(path)
    if path.endswith(".png"):
        a = np.array(im.convert("RGBA")).astype(float)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        a[..., 3][(r - g > 50) & (b - g > 50)] = 0  # magenta fringe left by the generator
        return a
    a = np.array(im.convert("RGB")).astype(float)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = (r - g > 60) & (b - g > 60)
    # one more pixel around the background goes too: it's the pink halo of the jpeg
    from scipy import ndimage
    bg = ndimage.binary_dilation(bg, iterations=2)
    alpha = np.where(bg, 0, 255.0)
    return np.dstack([a, alpha])


def shrink(cell, k):
    """Averages k x k blocks (only the opaque pixels count for the color), then makes the edge hard again."""
    if k == 1:
        out = cell.copy()
        out[..., 3] = np.where(out[..., 3] > 127, 255, 0)
        return out
    h, w = cell.shape[0] // k * k, cell.shape[1] // k * k
    c = cell[:h, :w].reshape(h // k, k, w // k, k, 4)
    al = c[..., 3:4] / 255
    rgb = (c[..., :3] * al).sum((1, 3)) / np.maximum(al.sum((1, 3)), 1e-6)
    cov = al.mean((1, 3))[..., 0]
    return np.dstack([rgb, np.where(cov > 0.45, 255, 0)])


def frames_of(a, layout):
    if isinstance(layout, tuple):
        cols, rows = layout
        fw, fh = a.shape[1] // cols, a.shape[0] // rows
        return [a[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw] for r in range(rows) for c in range(cols)]
    return [a[y0:y1, x0:x1] for x0, y0, x1, y1 in layout]


for file, move, layout, k, settings in SHEETS:
    a = load(f"{SRC}/{file}")
    fr = [shrink(f, k) for f in frames_of(a, layout)]
    w = max(f.shape[1] for f in fr)
    h = max(f.shape[0] for f in fr)
    w += w % 2
    h += h % 2
    strip = np.zeros((h, w * len(fr), 4), np.uint8)
    for i, f in enumerate(fr):
        y, x = (h - f.shape[0]) // 2, (w - f.shape[1]) // 2
        strip[y:y + f.shape[0], i * w + x:i * w + x + f.shape[1]] = f.clip(0, 255).astype(np.uint8)
    Image.fromarray(strip, "RGBA").save(f"{OUT}/{move}.png", optimize=True)
    lines = [f"Frames {len(fr)}"] + [f"{key} {val}" for key, val in settings.items()] + ["Scale 1"]
    open(f"{OUT}/{move}.txt", "w").write("\n".join(lines) + "\n")
    print(move, len(fr), (w, h))
