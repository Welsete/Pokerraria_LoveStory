"""Cuts a character's frames (one pose per PNG on a flat magenta background, named <prefix>_<pose>.png) into a
vertical sheet for the mod: same scale for every frame, feet on the bottom row, background and its fringe removed.
Run: python3 tools/sprites/cut_frames.py [--dark-pupils] [--wide=1.15] <folder> <prefix> <out.png> <height> <pose> [<pose> ...]
--dark-pupils darkens the iris next to the eye's white and the lid above it, for art whose thin black eye lines are smaller
than a pixel of the sheet and get lost. --own-scale=<pose prefix> scales the poses starting with it on their own (for
example walk frames drawn bigger than the rest), so they come out just as tall as the others, and lines them up by the torso instead of the bounding box. --wide stretches the frames sideways by that much, since the art is a bit
slimmer than Terraria's characters.
e.g. python3 tools/sprites/cut_frames.py nurse_joy_frames nurse_joy Pokerraria/Assets/NPCs/NurseJoy.png 54 parada \
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
WIDE = 1.0
for arg in list(args):
    if arg.startswith("--wide="):
        WIDE = float(arg.split("=", 1)[1])
        args.remove(arg)
OWN = None
for arg in list(args):
    if arg.startswith("--own-scale="):
        OWN = arg.split("=", 1)[1]
        args.remove(arg)
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

own = [OWN is not None and name.startswith(OWN) for name in ORDER]
def group_scale(flag):
    heights = [f[3] - f[2] + 1 for f, o in zip(frames, own) if o == flag]
    return max(heights) / H if heights else None
scales = [group_scale(o) for o in own]  # source pixels per output row, per frame
W = max(int(np.ceil((f[5] - f[4] + 1) / (sc / WIDE))) for f, sc in zip(frames, scales)) + 2
sheet = Image.new("RGBA", (W, H * len(frames)), (0, 0, 0, 0))
# Every frame centered on the same column (the middle of the standing pose's body), feet on the bottom row
def torso_x(f):
    # The middle of the torso (40-60% down the figure): steady while the legs, arms and hair swing, so walk frames
    # don't jitter sideways the way their bounding boxes would
    a, fg, ya, yb = f[0], f[1], f[2], f[3]
    rows = fg[ya + int((yb - ya) * 0.4):ya + int((yb - ya) * 0.6)]
    return float(np.nonzero(rows)[1].mean())
cx_all = [torso_x(f) if o else (f[4] + f[5]) / 2 for f, o in zip(frames, own)]
for i, (a, fg, ya, yb, xa, xb) in enumerate(frames):
    cx = cx_all[i]
    s = scales[i]
    sx = s / WIDE  # source pixels per output column
    for oy in range(H):
        for ox in range(W):
            Y0 = int(round(yb + 1 - (H - oy) * s)); Y1 = int(round(yb + 1 - (H - oy - 1) * s))
            X0 = int(round(cx + (ox - W / 2) * sx)); X1 = int(round(cx + (ox + 1 - W / 2) * sx))
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
        if size < 12:
            sl[lab == j + 1] = 0
if DARK_PUPILS:
    # The eye is a white pixel with the iris (bluish or purplish, unlike skin) on the side the art faces: the iris
    # becomes a dark pupil and the lid above both is darkened
    for i in range(len(frames)):
        sl = px[i * H:i * H + H * 2 // 5]
        rgb = sl[..., :3].astype(int)
        solid = sl[..., 3] > 0
        white = solid & (rgb.min(2) > 215)
        iris = solid & (rgb[..., 2] >= rgb[..., 1]) & (rgb.min(2) <= 235) & (rgb.max(2) - rgb.min(2) >= 25) & (rgb[..., 2] >= rgb[..., 0] - 40)
        for y, x in zip(*np.where(white)):
            if x + 1 >= sl.shape[1] or not iris[y, x + 1]:
                continue
            for yy in (y, y + 1):
                if yy < len(sl) and iris[yy, x + 1]:
                    sl[yy, x + 1, :3] = (28, 22, 44)
            if y > 0:
                sl[y - 1, x:x + 2, :3] = (28, 22, 44)
Image.fromarray(px).save(out_path)
print("scale", round(s, 2), "frame", W, "x", H)
