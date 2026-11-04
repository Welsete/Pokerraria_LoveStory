"""Desenha em código a arte do diário da LoveStory: o livro aberto, as caixinhas e a ilustração de cada página.

Pixel art em escala 2x (livro, caixinhas) e 3x (ilustrações), sem suavizar.
Rodar da raiz do repo: python3 tools/lovestory/draw_journal.py
Grava em Pokerraria/Assets/GUI/Journal/.
"""
import math
import random

import numpy as np
from PIL import Image

OUT = "Pokerraria/Assets/GUI/Journal/"


def save(arr, name, scale):
    im = Image.fromarray(arr.astype(np.uint8), "RGBA")
    im.resize((im.width * scale, im.height * scale), Image.NEAREST).save(OUT + name, optimize=True)


def rgba(c, a=255):
    return np.array(list(c) + [a], float)


# ------------------------------------------------------------------ livro aberto (360x230 -> 720x460)
def book():
    W, H = 360, 230
    a = np.zeros((H, W, 4))
    YY, XX = np.mgrid[0:H, 0:W]
    # capa de couro verde com borda escura
    cover = (XX >= 2) & (XX < W - 2) & (YY >= 2) & (YY < H - 2)
    a[cover] = rgba((40, 98, 58))
    a[cover & ((XX < 4) | (XX >= W - 4) | (YY < 4) | (YY >= H - 4))] = rgba((18, 46, 28))
    a[cover & ((XX == 5) | (XX == W - 6) | (YY == 5) | (YY == H - 6)) & (XX >= 5) & (XX <= W - 6) & (YY >= 5) & (YY <= H - 6)] = rgba((70, 140, 88))
    # bloco de folhas embaixo (as bordas das páginas)
    for y in range(212, 222):
        tone = (222, 206, 170) if y % 2 else (196, 176, 136)
        a[y, 12:178 - (y - 212) // 3 * 0] = rgba(tone)
        a[y, 182:348] = rgba(tone)
    a[222, 12:348] = rgba((120, 92, 60))
    # páginas: creme com nuvens bem suaves
    rnd = np.random.default_rng(3)
    small = rnd.random((12, 18))
    noise = np.array(Image.fromarray((small * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)) / 255.0
    pages = ((XX >= 10) & (XX < 178) | (XX >= 182) & (XX < 350)) & (YY >= 8) & (YY < 212)
    base = np.where(noise[..., None] > 0.62, [244, 236, 212], [240, 231, 204])
    base = np.where(noise[..., None] < 0.3, [237, 228, 200], base)
    a[pages, :3] = base[pages]
    a[pages, 3] = 255
    # sombra perto da lombada
    for i in range(10):
        f = (10 - i) / 10 * 0.18
        for x in (177 - i, 182 + i):
            col = a[8:212, x, :3]
            a[8:212, x, :3] = col * (1 - f) + np.array([150, 120, 80]) * f
    # contorno das páginas e lombada
    for x0, x1 in ((10, 178), (182, 350)):
        a[8, x0:x1] = rgba((190, 168, 126))
        a[8:212, x0] = rgba((190, 168, 126))
        a[8:212, x1 - 1] = rgba((190, 168, 126))
    a[6:224, 178:182] = rgba((92, 66, 42))
    a[6:224, 179:181] = rgba((60, 42, 26))
    # cantoneiras douradas
    gold = [(255, 222, 96), (232, 176, 40), (170, 112, 20)]
    for cx, cy in ((0, 0), (W - 10, 0), (0, H - 10), (W - 10, H - 10)):
        a[cy:cy + 10, cx:cx + 10] = rgba(gold[1])
        a[cy:cy + 10, cx:cx + 1] = a[cy:cy + 1, cx:cx + 10] = rgba(gold[0])
        a[cy:cy + 10, cx + 9:cx + 10] = a[cy + 9:cy + 10, cx:cx + 10] = rgba(gold[2])
        a[cy + 3:cy + 7, cx + 3:cx + 7] = rgba(gold[0])
    save(a, "Book.png", 2)


# ------------------------------------------------------------------ caixinhas (8x8 -> 16x16)
def checkboxes():
    for name, done in (("Check.png", True), ("Unchecked.png", False)):
        a = np.zeros((8, 8, 4))
        a[:, :] = rgba((20, 20, 24))
        a[1:7, 1:7] = rgba((235, 228, 210))
        a[2:6, 2:6] = rgba((20, 20, 24)) if not done else rgba((235, 228, 210))
        if done:
            for x, y in ((1, 4), (2, 5), (3, 6), (4, 5), (5, 4), (6, 3), (6, 2), (5, 3)):
                a[y - 1, x] = rgba((232, 176, 40))
                a[y, x] = rgba((232, 176, 40))
        save(a, name, 2)


# ------------------------------------------------------------------ ilustrações (100x70 -> 300x210)
IW, IH = 100, 70
YY, XX = np.mgrid[0:IH, 0:IW]


def canvas(top, bottom):
    t = YY / (IH - 1)
    a = np.zeros((IH, IW, 4))
    for i in range(3):
        a[..., i] = top[i] * (1 - t) + bottom[i] * t
    a[..., 3] = 255
    # bandas de céu (pixel art: sem degradê suave)
    a[..., :3] = np.round(a[..., :3] / 10) * 10
    return a


def put(a, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < IW and 0 <= y < IH:
        a[y, x, :3] = c


def stars(a, n, seed, maxy):
    r = random.Random(seed)
    for _ in range(n):
        put(a, r.randrange(IW), r.randrange(maxy), (240, 240, 255) if r.random() < 0.6 else (180, 180, 220))


def aurora(a, y0, amp, phase, cols, height=14):
    """Faixas de aurora: ondas que descem em cortinas e somem para cima."""
    for x in range(IW):
        yb = y0 + amp * math.sin(x * 0.07 + phase) + 2 * math.sin(x * 0.23 + phase * 2)
        for k in range(height):
            y = yb - k
            if y < 0 or y >= IH:
                continue
            if (x + k) % 3 == 0 and k > height * 0.5:
                continue  # cortina falhando no alto
            c = cols[0] if k < 2 else cols[1] if k < height * 0.55 else cols[2]
            mix = 0.75 if k >= 2 else 1.0
            a[int(y), x, :3] = a[int(y), x, :3] * (1 - mix) + np.array(c) * mix


def crack(a, x0, y0, length, seed, glow=(170, 110, 230)):
    """Rachadura no céu: zigue-zague branco com brilho em volta."""
    r = random.Random(seed)
    pts = [(x0, y0)]
    x, y = x0, y0
    for _ in range(length):
        x += r.choice((-1, 0, 1, 1))
        y += 1
        pts.append((x, y))
        if r.random() < 0.15:  # galho
            bx, by = x, y
            for _ in range(r.randint(2, 5)):
                bx += r.choice((-1, 1))
                by += 1
                put(a, bx - 1, by, glow)
                put(a, bx, by, (230, 220, 255))
    for x, y in pts:
        for dx in (-2, -1, 1, 2):
            put(a, x + dx, y, glow if abs(dx) == 1 else tuple(int(v * 0.6) for v in glow))
    for x, y in pts:
        put(a, x, y, (255, 255, 255))


def hill(a, y0, col, amp=4, f=0.05, ph=0.0):
    top = y0 + amp * np.sin(XX * f + ph)
    a[(YY >= top), :3] = col


def torn(a, seed):
    """Bordas rasgadas e contorno marrom: parece um pedaço de papel colado na página."""
    r = random.Random(seed)
    m = np.ones((IH, IW), bool)
    for x in range(IW):
        for y in range(r.choice((0, 0, 1, 1, 2))):
            m[y, x] = False
        for y in range(r.choice((0, 0, 1, 1, 2))):
            m[IH - 1 - y, x] = False
    for y in range(IH):
        for x in range(r.choice((0, 0, 1, 1, 2))):
            m[y, x] = False
        for x in range(r.choice((0, 0, 1, 1, 2))):
            m[y, IW - 1 - x] = False
    edge = m & ~(np.roll(m, 1, 0) & np.roll(m, -1, 0) & np.roll(m, 1, 1) & np.roll(m, -1, 1))
    a[~m, 3] = 0
    a[edge, :3] = (80, 56, 36)
    return a


def figure(a, x, y, col=(10, 8, 18)):
    """Silhueta de alguém de capuz, de costas, olhando o céu (sem dar pistas de quem é)."""
    shape = [
        "..###..",
        ".#####.",
        ".#####.",
        "..###..",
        ".#####.",
        "#######",
        "#######",
        "#######",
        ".#####.",
        ".#####.",
        ".##.##.",
        ".##.##.",
        ".##.##.",
    ]
    for j, row in enumerate(shape):
        for i, ch in enumerate(row):
            if ch == "#":
                put(a, x + i, y + j, col)


def prologue():
    a = canvas((12, 10, 30), (40, 34, 80))
    stars(a, 60, 1, 50)
    aurora(a, 30, 5, 0.4, [(200, 255, 220), (90, 230, 160), (60, 150, 130)], 16)
    aurora(a, 22, 4, 2.1, [(230, 200, 255), (170, 110, 230), (100, 70, 160)], 10)
    crack(a, 66, 2, 22, 7)
    hill(a, 56, (24, 20, 40), 3, 0.06, 1.0)
    hill(a, 62, (14, 12, 24), 4, 0.04, 2.5)
    figure(a, 30, 46)
    return torn(a, 11)


def brock():
    a = canvas((70, 40, 90), (240, 140, 70))
    stars(a, 12, 2, 20)
    # sol se pondo atrás das montanhas
    sun = (XX - 70) ** 2 + (YY - 40) ** 2 <= 100
    a[sun, :3] = (255, 210, 120)
    hill(a, 40, (90, 60, 80), 6, 0.05, 0.5)
    hill(a, 50, (60, 40, 56), 3, 0.08, 2.0)
    # pedregulho grande
    boulder = ((XX - 50) / 22) ** 2 + ((YY - 46) / 15) ** 2 <= 1
    a[boulder, :3] = (120, 110, 104)
    a[boulder & ((XX - 50) + (YY - 46) * 1.4 < -10), :3] = (160, 150, 140)
    a[boulder & ((XX - 50) + (YY - 46) * 1.4 > 14), :3] = (80, 72, 70)
    for x0, x1, y in ((38, 46, 42), (55, 62, 50), (44, 50, 53)):  # rachaduras
        for x in range(x0, x1):
            put(a, x, y + (x % 3 == 0), (60, 52, 50))
    # chão
    a[58:, :, :3] = (52, 40, 44)
    a[58, :, :3] = (90, 70, 66)
    # três alavancas: só uma brilha
    for i, lx in enumerate((26, 50, 74)):
        a[59:63, lx - 3:lx + 4, :3] = (110, 100, 96)
        a[59, lx - 3:lx + 4, :3] = (150, 140, 130)
        tilt = 1 if i != 1 else -1
        for k in range(8):
            put(a, lx + tilt * k * 0.5, 58 - k, (150, 110, 70))
        tip = (lx + tilt * 4, 50)
        col = (255, 230, 120) if i == 1 else (170, 60, 50)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                put(a, tip[0] + dx, tip[1] + dy, col)
        if i == 1:
            for dx, dy in ((-3, 0), (3, 0), (0, -3), (-2, -2), (2, -2)):
                put(a, tip[0] + dx, tip[1] + dy, (255, 250, 200))
    return torn(a, 12)


def misty():
    a = canvas((10, 12, 34), (36, 50, 96))
    stars(a, 40, 3, 36)
    aurora(a, 24, 4, 1.2, [(200, 255, 220), (90, 230, 160), (60, 150, 130)], 12)
    crack(a, 40, 1, 26, 21, (90, 230, 160))
    # lua
    moon = (XX - 82) ** 2 + (YY - 12) ** 2 <= 30
    a[moon, :3] = (240, 236, 210)
    a[((XX - 84) ** 2 + (YY - 11) ** 2 <= 22) & moon, :3] = (200, 196, 176)
    # mar com o reflexo da rachadura e da aurora
    sea = YY >= 44
    a[sea, :3] = (20, 40, 80)
    a[sea & (YY >= 54), :3] = (14, 28, 60)
    r = random.Random(5)
    for y in range(45, IH, 2):
        for x in range(0, IW, 1):
            if r.random() < 0.12:
                put(a, x, y, (60, 100, 150))
    for y in range(45, 62):
        w = 1 + (y - 45) // 4
        for dx in range(-w, w + 1):
            if (y + dx) % 2 == 0:
                put(a, 40 + dx + (y % 3) - 1, y, (150, 240, 200) if abs(dx) < w else (90, 230, 160))
    a[44, :, :3] = (90, 120, 170)
    # rochedo com o Ginásio ao longe
    hill(a, 40, (16, 18, 34), 2, 0.3, 0.0) if False else None
    a[38:44, 6:20, :3] = (16, 18, 34)
    a[34:38, 9:16, :3] = (16, 18, 34)
    put(a, 12, 33, (255, 220, 120))
    return torn(a, 13)


if __name__ == "__main__":
    book()
    checkboxes()
    for name, f in (("Prologue", prologue), ("Brock", brock), ("Misty", misty)):
        save(f(), f"Page{name}.png", 3)
    print("ok")
