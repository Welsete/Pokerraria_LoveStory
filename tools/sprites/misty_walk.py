"""Builds Misty's walk cycle from her 14 walking frames, cut with cut_frames.py into an 18-frame sheet (standing,
walking 1-14, throwing, victory, sitting).
Her frames almost all have the near (light) leg in front, so the far leg never comes forward. The fix: a step with
the near leg in front (contact, down, legs passing) and the same three frames with the two legs' skin tones swapped,
which reads as the other leg stepping forward.
Run: python3 tools/sprites/misty_walk.py <18-frame sheet> <out.png>"""
import sys
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
a = np.array(Image.open(src).convert("RGBA")).astype(int)
H = 54
frames = [a[i * H:(i + 1) * H].copy() for i in range(a.shape[0] // H)]

LIGHT = np.array([245, 182, 148])
DARK = np.array([195, 121, 92])


def swap_legs(f):
    f = f.copy()
    # Only below the shorts, so the arms keep their shading
    rgb = f[..., :3]
    blue = (f[..., 3] > 0) & (rgb[..., 2] > rgb[..., 0] + 20) & (rgb[..., 2] > 90)
    rows = np.nonzero(blue[:int(H * 0.8)].sum(axis=1) >= 3)[0]  # the shorts (the soles are bluish grey too)
    top = rows.max() + 1 if len(rows) else H // 2
    for y in range(top, H):
        for x in range(f.shape[1]):
            if f[y, x, 3] == 0:
                continue
            r, g, b = (int(v) for v in f[y, x, :3])
            # Skin (not the red shoes, the dark outline or the white soles): split into the two tones by redness
            if not (r > 140 and 50 < r - g < 95 and g > b):
                continue
            light = r >= 222
            f[y, x, :3] = np.clip(f[y, x, :3] + (DARK - LIGHT if light else LIGHT - DARK), 0, 255)
    return f


step = [frames[1], frames[4], frames[2]]  # contact, down, legs passing (near leg in front)
walk = step + [swap_legs(f) for f in step]
sheet = [frames[0]] + walk + frames[15:18]
Image.fromarray(np.concatenate(sheet).astype(np.uint8)).save(out)
print(len(sheet), "frames")
