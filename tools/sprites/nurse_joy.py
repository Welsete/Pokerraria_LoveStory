"""Cuts Nurse Joy's frames (one pose per PNG, magenta background) into a vertical sheet for the mod.
Run: python3 tools/sprites/nurse_joy.py <folder with the PNGs> Terramon/Assets/NPCs/NurseJoy.png [height]"""
import sys
from collections import Counter
import numpy as np
from PIL import Image
from scipy import ndimage

ORDER = ["parada", "cumprimento", "segurando_pokebola", "andando_1", "andando_2", "andando_3", "andando_4", "sentada"]
src, out_path = sys.argv[1], sys.argv[2]
H = int(sys.argv[3]) if len(sys.argv) > 3 else 44

frames = []
for name in ORDER:
    a = np.array(Image.open(f"{src}/nurse_joy_{name}.png").convert("RGB")).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    fg = ~((r > 170) & (b > 170) & (g < 120))
    lab, n = ndimage.label(fg, structure=np.ones((3, 3)))
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    fg = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s > 80])
    ys, xs = np.where(fg)
    frames.append((a, fg, ys.min(), ys.max(), xs.min(), xs.max()))

s = max(f[3] - f[2] + 1 for f in frames if True) / H
W = max(int(np.ceil((f[5] - f[4] + 1) / s)) for f in frames) + 2
sheet = Image.new("RGBA", (W, H * len(frames)), (0, 0, 0, 0))
# Every frame centered on the same column (the middle of the standing pose's body), feet on the bottom row
cx_all = [(f[4] + f[5]) / 2 for f in frames]
for i, (a, fg, ya, yb, xa, xb) in enumerate(frames):
    cx = cx_all[i]
    for oy in range(H):
        for ox in range(W):
            Y0 = int(round(yb + 1 - (H - oy) * s)); Y1 = int(round(yb + 1 - (H - oy - 1) * s))
            X0 = int(round(cx + (ox - W / 2) * s)); X1 = int(round(cx + (ox + 1 - W / 2) * s))
            if Y1 <= 0 or X1 <= 0:
                continue
            Y0, X0 = max(Y0, 0), max(X0, 0)
            blk = fg[Y0:Y1, X0:X1]
            if blk.size == 0 or blk.mean() < 0.5:
                continue
            colors = a[Y0:Y1, X0:X1][blk] // 8 * 8 + 4
            cols = [tuple(c) for c in colors]
            c = Counter(cols).most_common(1)[0][0]
            sheet.putpixel((ox, i * H + oy), (int(c[0]), int(c[1]), int(c[2]), 255))

# Leftover background tint at the edges, and specks
px = np.array(sheet)
for i in range(len(frames)):
    sl = px[i * H:(i + 1) * H]
    r, g, b = (sl[..., k].astype(int) for k in range(3))
    pink = (sl[..., 3] > 0) & (b > 50) & (b - g > 40) & (r - g > 40) & (abs(r - b) < 90)
    sl[pink] = 0
    fg = sl[..., 3] > 0
    lab, n = ndimage.label(fg, structure=np.ones((3, 3)))
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    for j, size in enumerate(sizes):
        if size < 6:
            sl[lab == j + 1] = 0
Image.fromarray(px).save(out_path)
print("scale", round(s, 2), "frame", W, "x", H)
