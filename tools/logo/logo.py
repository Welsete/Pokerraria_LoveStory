"""Pokerraria: LoveStory logo, pixel art in the style of the Terramon logo."""
import math, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

# Usage: python3 logo.py <folder that contains Terramon/> ; then python3 gif.py
ROOT = sys.argv[1] if len(sys.argv) > 1 else "."
FONT = "/usr/share/fonts/opentype/inter/Inter-Black.otf"
rng = np.random.default_rng(7)

W, H = 540, 150
# grass palettes: outermost, outer rim, dark, mid, light, highlight, ring around letters
PALETTES = {
    "green": [(0, 36, 40), (0, 74, 68), (5, 130, 91), (16, 163, 95), (37, 186, 87), (67, 216, 95), (0, 74, 68)],
    "pink": [(58, 10, 40), (110, 24, 72), (170, 50, 112), (205, 78, 140), (232, 116, 170), (252, 168, 206), (110, 24, 72)],
    "purple": [(20, 24, 61), (43, 42, 84), (66, 58, 112), (83, 80, 143), (103, 100, 163), (149, 146, 214), (43, 42, 84)],
}
DIRT = dict(base=(153, 106, 79), light=(173, 123, 89), hi=(196, 143, 125), shade=(137, 92, 71),
            stones=[(99, 99, 99), (129, 128, 128), (165, 165, 165)])
METAL = {
    "red": dict(fill=(200, 38, 38), stripe=(228, 107, 68), shine=(242, 187, 154), shade=(161, 20, 54)),
    "white": dict(fill=(217, 227, 247), stripe=(255, 255, 255), shine=(255, 255, 255), shade=(175, 172, 220)),
    "pink": dict(fill=(232, 72, 140), stripe=(255, 140, 190), shine=(255, 214, 232), shade=(176, 30, 96)),
}
GOLD = dict(out=(107, 19, 36), band=(238, 143, 49), hi=(255, 199, 107), top=(251, 242, 54), low=(189, 100, 27),
            inner=(144, 41, 30))


def text_mask(text, size, wobble=0.0, seed=0, tracking=0):
    """Text rendered big, warped a little so it looks hand drawn, and brought down to pixel size."""
    S = 4
    font = ImageFont.truetype(FONT, size * S)
    widths = [font.getbbox(ch)[2] - font.getbbox(ch)[0] for ch in text]
    total = sum(widths) + tracking * S * (len(text) - 1) + 40 * S
    img = Image.new("L", (total, size * S * 2), 0)
    d = ImageDraw.Draw(img)
    x = 20 * S
    for ch, w in zip(text, widths):
        bb = font.getbbox(ch)
        d.text((x - bb[0], size * S // 3), ch, font=font, fill=255)
        x += w + tracking * S
    a = np.asarray(img, dtype=np.float32) / 255
    if wobble:
        r = np.random.default_rng(seed)
        yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]].astype(np.float32)
        ph = r.uniform(0, 6.28, 4)
        dx = wobble * S * (np.sin(yy / (9 * S) + ph[0]) + 0.5 * np.sin(yy / (4 * S) + ph[1]))
        dy = wobble * S * (np.sin(xx / (11 * S) + ph[2]) + 0.5 * np.sin(xx / (5 * S) + ph[3]))
        a = ndi.map_coordinates(a, [yy + dy, xx + dx], order=1)
    a = a[:a.shape[0] // S * S, :a.shape[1] // S * S]
    small = a.reshape(a.shape[0] // S, S, a.shape[1] // S, S).mean(axis=(1, 3)) > 0.5
    ys, xs = np.nonzero(small)
    return small[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def dil(m, r):
    if r <= 0:
        return m.copy()
    return ndi.distance_transform_edt(~m) <= r


def ero(m, r):
    return ndi.distance_transform_edt(m) > r


def paint(canvas, mask, color):
    canvas[mask] = (*color, 255)


def place(mask, x, y):
    out = np.zeros((H, W), bool)
    h, w = mask.shape
    out[y:y + h, x:x + w] = mask
    return out


def noise(scale, seed):
    r = np.random.default_rng(seed)
    n = r.random((H // scale + 2, W // scale + 2))
    return ndi.zoom(n, scale, order=1)[:H, :W]


def grass(canvas, letters, pal, seed):
    """The bushy grass the letters sit in, with tufts sticking up."""
    dist = ndi.distance_transform_edt(~letters)
    n = noise(3, seed)
    reach = 5 + n * 3.5
    blob = dist <= reach
    # tufts: thin spikes upwards from the top edge
    r = np.random.default_rng(seed + 1)
    tops = np.argmax(blob, axis=0)
    for x in range(W):
        if blob[:, x].any() and r.random() < 0.45:
            h = r.integers(2, 6)
            y0 = tops[x]
            blob[max(0, y0 - h):y0, x] = True
    blob = ndi.binary_closing(blob, iterations=1) | blob
    outer = dil(blob, 2) & ~blob
    rim = dil(blob, 1) & ~blob
    paint(canvas, outer, pal[0])
    paint(canvas, rim, pal[1])
    # shades: lighter towards the top of each column and with noise
    yy = np.mgrid[0:H, 0:W][0]
    top = np.where(blob.any(axis=0), np.argmax(blob, axis=0), 0)
    rel = (yy - top[None, :]) / 30.0
    n2 = noise(2, seed + 2)
    v = rel + (n2 - 0.5) * 0.6
    for thr, col in ((0.15, pal[5]), (0.45, pal[4]), (0.9, pal[3]), (99, pal[2])):
        sel = blob & (v < thr) & (canvas[..., 3] == 0)
        paint(canvas, sel, col)
    # scattered light blades
    blades = blob & (r.random((H, W)) < 0.06) & (rel < 1.2)
    paint(canvas, blades, pal[5])
    return blob


def dirt(canvas, mask, pal, seed):
    ring = dil(mask, 1) & ~mask
    paint(canvas, ring, pal[6])
    paint(canvas, mask, DIRT["base"])
    # light from the top left, shade at the bottom right
    up = mask & ~np.roll(mask, 2, axis=0)
    left = mask & ~np.roll(mask, 2, axis=1)
    down = mask & ~np.roll(mask, -2, axis=0)
    paint(canvas, mask & (ndi.distance_transform_edt(mask) > 2) & (noise(4, seed) > 0.55), DIRT["light"])
    paint(canvas, down, DIRT["shade"])
    paint(canvas, up | left, DIRT["hi"])
    # little stones
    r = np.random.default_rng(seed)
    inner = np.argwhere(ero(mask, 2))
    for _ in range(max(3, len(inner) // 140)):
        y, x = inner[r.integers(len(inner))]
        w, h = r.integers(2, 4), r.integers(2, 3)
        canvas[y:y + h, x:x + w] = (*DIRT["stones"][1], 255)
        canvas[y, x:x + w] = (*DIRT["stones"][2], 255)
        canvas[y + h - 1, x:x + w] = (*DIRT["stones"][0], 255)


def metal(canvas, mask, scheme, band=3, stripes=True):
    """Shiny letter with a gold rim, like the MON of the Terramon logo."""
    c = METAL[scheme]
    full = dil(mask, band + 1)
    paint(canvas, full & ~dil(mask, band), GOLD["out"])
    gold = dil(mask, band) & ~mask
    paint(canvas, gold, GOLD["band"])
    up = gold & ~np.roll(gold, 1, axis=0)
    paint(canvas, gold & ~np.roll(full, -1, axis=0) | (gold & ~np.roll(gold, -1, axis=0)), GOLD["low"])
    paint(canvas, up, GOLD["hi"])
    sparkle = up & (np.random.default_rng(3).random(mask.shape) < 0.25)
    paint(canvas, sparkle, GOLD["top"])
    paint(canvas, mask, c["fill"])
    edge = mask & ~ero(mask, 1)
    paint(canvas, edge, GOLD["inner"])
    core = ero(mask, 1)
    yy, xx = np.mgrid[0:H, 0:W]
    if stripes:
        diag = (xx + yy) % 14
        paint(canvas, core & (diag < 3), c["stripe"])
        paint(canvas, core & (diag == 0), c["shine"])
    low = core & ~np.roll(core, -2, axis=0)
    paint(canvas, low, c["shade"])


def ball(canvas, cx, cy, r):
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx - cx + 0.5, yy - cy + 0.5)
    disc = d <= r
    paint(canvas, (d <= r + 4) & ~(d <= r + 3), GOLD["out"])
    gold = (d <= r + 3) & ~disc
    paint(canvas, gold, GOLD["band"])
    paint(canvas, gold & (yy < cy - r * 0.4), GOLD["hi"])
    paint(canvas, gold & (yy > cy + r * 0.5), GOLD["low"])
    paint(canvas, disc & (yy < cy), METAL["red"]["fill"])
    paint(canvas, disc & (yy < cy) & (d > r - 2.5) & (xx > cx), METAL["red"]["shade"])
    paint(canvas, disc & (yy >= cy), METAL["white"]["fill"])
    paint(canvas, disc & (yy >= cy) & (d > r - 2.5) & (xx > cx - r * 0.2), METAL["white"]["shade"])
    hl = np.hypot(xx - (cx - r * 0.45), yy - (cy - r * 0.5)) <= r * 0.18
    paint(canvas, hl & disc, (255, 255, 255))
    paint(canvas, disc & (abs(yy - cy + 0.5) <= 2), (0, 0, 0))
    paint(canvas, d <= r * 0.36, (0, 0, 0))
    paint(canvas, d <= r * 0.36 - 2, (28, 34, 66))
    paint(canvas, d <= r * 0.36 - 3, METAL["white"]["fill"])
    paint(canvas, (d <= r * 0.36 - 3) & (yy < cy - 1) & (xx < cx), (255, 255, 255))
    paint(canvas, d > r + 4, (0, 0, 0)) if False else None
    return d <= r + 4


def heart_mask(size):
    t = np.linspace(0, 2 * math.pi, 400)
    x = 16 * np.sin(t) ** 3
    y = -(13 * np.cos(t) - 5 * np.cos(2 * t) - 2 * np.cos(3 * t) - np.cos(4 * t))
    S = 4
    img = Image.new("L", (size * S, size * S), 0)
    pts = [((xi + 17) / 34 * size * S, (yi + 17) / 34 * size * S) for xi, yi in zip(x, y)]
    ImageDraw.Draw(img).polygon(pts, fill=255)
    a = np.asarray(img, np.float32).reshape(size, S, size, S).mean(axis=(1, 3)) > 0.5
    ys, xs = np.nonzero(a)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def build(palette, icon=None):
    canvas = np.zeros((H, W, 4), np.uint8)
    x0 = 74
    base = 92  # baseline of the title
    # title pieces
    P = text_mask("P", 66, 0.6, 1)
    k = text_mask("k", 66, 0.6, 2)
    erraria = text_mask("erraria", 60, 1.1, 3, tracking=-1)
    love = text_mask("LoveStory", 30, 0.3, 4, tracking=1)
    heart = heart_mask(26)

    xP, yP = x0 + 4, base - P.shape[0]
    ball_r = 18
    bx = xP + P.shape[1] + 9 + ball_r
    by = base - ball_r - 2
    xk = bx + ball_r + 9
    yk = base - k.shape[0]
    xe = xk + k.shape[1] + 6
    ye = base - erraria.shape[0] + 1
    mP, mk, me = place(P, xP, yP), place(k, xk, yk), place(erraria, xe, ye)
    yy, xx = np.mgrid[0:H, 0:W]
    disc = np.hypot(xx - bx + 0.5, yy - by + 0.5) <= ball_r + 4
    xl = xe + (erraria.shape[1] - love.shape[1] - heart.shape[1] - 8) // 2 + heart.shape[1] + 8
    yl = base + 10
    ml = place(love, xl, yl)
    mh = place(heart, xl - heart.shape[1] - 7, yl + (love.shape[0] - heart.shape[0]) // 2 + 1)

    pal = PALETTES[palette]
    grass(canvas, dil(mP | mk | disc, 4) | me, pal, 11)
    dirt(canvas, me, pal, 12)
    metal(canvas, mP, "red")
    ball(canvas, bx, by, ball_r)
    metal(canvas, mk, "white")
    tx = xe + erraria.shape[1] // 3
    top = np.argmax(canvas[:, tx, 3] > 0)
    sprout(canvas, tx, top - 5)
    # subtitle on a dark ribbon so it reads over the grass
    rib = dil(ml | mh, 5)
    paint(canvas, dil(rib, 1) & ~rib, (40, 8, 30))
    paint(canvas, rib, (90, 20, 60))
    paint(canvas, rib & ~np.roll(rib, 2, axis=0), (130, 40, 90))
    metal(canvas, ml, "pink", band=2, stripes=False)
    metal(canvas, mh, "red", band=2, stripes=False)
    # shine on the heart
    hy, hx = np.argwhere(mh)[0]
    canvas[hy + 2:hy + 4, hx + 2:hx + 4] = (255, 214, 232, 255)
    img = Image.fromarray(canvas, "RGBA")
    if icon is not None:
        img.alpha_composite(icon, (0, (H - icon.height) // 2 - 4))
    bbox = img.getbbox()
    return img.crop((0, 0, bbox[2] + 2, H))


def recolor(icon, palette):
    """Swaps the green grass of the icon for another palette, by brightness."""
    a = np.array(icon)
    rgb = a[..., :3].astype(int)
    green = (rgb[..., 1] > rgb[..., 0] + 20) & (rgb[..., 1] >= rgb[..., 2]) & (a[..., 3] > 0)
    src = np.array(PALETTES["green"][:6])
    dst = np.array(PALETTES[palette][:6])
    idx = np.abs(rgb[..., None, :] - src[None, None]).sum(-1).argmin(-1)
    a[..., :3][green] = dst[idx][green]
    return Image.fromarray(a, "RGBA")


def sprout(canvas, x, y):
    """A little sprout with a heart on top, like the leaf sprout of the Terramon logo."""
    stem = (22, 120, 70)
    for i in range(6):
        canvas[y + i, x] = (*stem, 255)
    canvas[y + 3, x - 2:x] = (*stem, 255)
    canvas[y + 2, x - 3] = (*stem, 255)
    hm = heart_mask(11)
    h, w = hm.shape
    reg = canvas[y - h:y, x - w // 2:x - w // 2 + w]
    ring = dil(hm, 1)
    big = np.zeros((h + 2, w + 2), bool)
    reg2 = canvas[y - h - 1:y + 1, x - w // 2 - 1:x - w // 2 + w + 1]
    ringm = np.pad(hm, 1)
    ringm = dil(ringm, 1) & ~ringm
    reg2[ringm] = (90, 10, 30, 255)
    reg[hm] = (232, 50, 80, 255)
    reg[0:3, 1:3][hm[0:3, 1:3]] = (255, 170, 190, 255)


if __name__ == "__main__":
    src = Image.open(f"{ROOT}/Terramon/Assets/Misc/MenuLogo_0.png").convert("RGBA")
    src1 = Image.open(f"{ROOT}/Terramon/Assets/Misc/MenuLogo_1.png").convert("RGBA")
    icon0 = src.crop((0, 0, 72, 114))
    icon1 = src1.crop((0, 0, 72, 114))
    out = {}
    for name, icon in (("green", icon0), ("pink", recolor(icon0, "pink")), ("purple", icon1)):
        out[name] = build(name, icon)
        out[name].save(f"logo_{name}.png")
    prev = Image.new("RGBA", (out["green"].width, H * 3), (34, 36, 54, 255))
    for i, n in enumerate(out):
        prev.alpha_composite(out[n], (0, i * H))
    prev.resize((prev.width * 2, prev.height * 2), Image.NEAREST).save("preview.png")
    print({n: o.size for n, o in out.items()})
