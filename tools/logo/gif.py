import sys
import numpy as np
from PIL import Image
names = ["green", "pink", "purple"]
imgs = [np.asarray(Image.open(f"logo_{n}.png").convert("RGBA")).astype(np.float32) for n in names]
frames, durs = [], []
for i in range(3):
    a, b = imgs[i], imgs[(i + 1) % 3]
    frames.append(a); durs.append(1600)
    for t in range(1, 9):
        k = t / 9
        frames.append(a * (1 - k) + b * k); durs.append(70)
def q(f):
    im = Image.fromarray(f.clip(0, 255).astype(np.uint8), "RGBA")
    alpha = im.getchannel("A")
    p = im.convert("RGB").quantize(255, method=Image.Quantize.MEDIANCUT)
    p.paste(255, mask=alpha.point(lambda v: 255 if v < 128 else 0))
    return p
for scale, out in ((1, "banner.gif"), (2, "banner@2x.gif")):
    fr = [q(f) for f in frames]
    if scale != 1:
        fr = [f.resize((f.width * scale, f.height * scale), Image.NEAREST) for f in fr]
    fr[0].save(out, save_all=True, append_images=fr[1:], duration=durs, loop=0, transparency=255, disposal=2)
print(len(frames))
