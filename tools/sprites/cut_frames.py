"""Cuts a character's frames (one pose per PNG on a flat magenta background, named <prefix>_<pose>.png) into a
vertical sheet for the mod: same scale for every frame, feet on the bottom row, background and its fringe removed.
Run: python3 tools/sprites/cut_frames.py [--dark-pupils] <folder> <prefix> <out.png> <height> <pose> [<pose> ...]
--dark-pupils darkens the eye's colored column and the lid above it, for art whose thin black eye lines are smaller
than a pixel of the sheet and get lost.
e.g. python3 tools/sprites/cut_frames.py nurse_joy_frames nurse_joy Terramon/Assets/NPCs/NurseJoy.png 54 parada \
         cumprimento segurando_pokebola andando_1 andando_2 andando_3 andando_4 sentada"""
import sys
from collections import Counter
import numpy as np
from PIL import Image
from scipy import ndimage

args = sys.argv[1:]
DARK_PUPILS = "--dark-pupils" in args
if DARK_PUPILS:
    args.remove("--dark-pupils")
src, prefix, out_path, H = args[0], args[1], args[2], int(args[3])
ORDER = args[4:]

frames = []
for name in ORDER:
    a = np.array(Image.open(f"{src}/{prefix}_{name}.png").convert("RGB")).astype(int)
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
            # The color at the middle of the block: the source's own pixels are bigger than a block, so this keeps
            # every one of them (eyes, outlines), where the block's most common color could drop thin details
            cy, cx2 = min(int((Y0 + Y1) / 2), a.shape[0] - 1), min(int((X0 + X1) / 2), a.shape[1] - 1)
            if not fg[cy, cx2]:
                colors = a[Y0:Y1, X0:X1][blk]
                cy_c = colors[len(colors) // 2]
            else:
                cy_c = a[cy, cx2]
            c = tuple(int(v) for v in cy_c)
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
if DARK_PUPILS:
    for i in range(len(frames)):
        sl = px[i * H:i * H + H * 2 // 5]
        r, g, b = (sl[..., k].astype(int) for k in range(3))
        eye = (sl[..., 3] > 0) & (b > r + 15) & (b < 170)
        for y, x in zip(*np.where(eye)):
            sl[y, x, :3] = (24, 26, 40) if eye[min(y + 1, len(sl) - 1), x] else (60, 80, 120)
            if y > 0 and not eye[y - 1, x]:
                sl[y - 1, x - 1:x + 1, :3] = (24, 26, 40)
Image.fromarray(px).save(out_path)
print("scale", round(s, 2), "frame", W, "x", H)
