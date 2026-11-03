"""Golpes de luz e raios desenhados à mão em código, no estilo do Solar Beam (tools/vfx/draw_solar_beam.py):
Hyper Beam, Psybeam, Charge Beam, Flash Cannon (raios: carga no usuário, raio esticado até o alvo, impacto),
Thunder (raio caindo do céu no alvo) e Dazzling Gleam (explosão de luz saindo do usuário).

Só as 6 cores do tipo de cada golpe, pixel art 2x sem suavizar.
Rodar da raiz do repo: python3 tools/vfx/draw_light_moves.py [Golpe,Golpe]
"""
import math
import random
import sys

import numpy as np
from PIL import Image

PAL = {
    "Normal": ["#FFFFFF", "#F5F5F5", "#D4D4D4", "#A3A3A3", "#525252", "#171717"],
    "Psíquico": ["#FFFFFF", "#FFE4F2", "#F472B6", "#DB2777", "#831843", "#4A044E"],
    "Elétrico": ["#FFFFFF", "#FFFF7A", "#FFD700", "#FFA500", "#D97706", "#592700"],
    "Aço": ["#FFFFFF", "#F1F5F9", "#CBD5E1", "#64748B", "#334155", "#0F172A"],
    "Fada": ["#FFFFFF", "#FDF2F8", "#F472B6", "#EC4899", "#9D174D", "#500724"],
    "Gelo": ["#FFFFFF", "#E0F7FA", "#80DEEA", "#26C6DA", "#00838F", "#004D40"],
}
WHITE, LIGHT, MID, BASE, DARK, OUTLINE = range(1, 7)
SCALE = 2


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = np.zeros((h, w), np.uint8)
        self.YY, self.XX = np.mgrid[0:h, 0:w]

    def put(self, x, y, c, only_empty=False):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h and (not only_empty or self.img[y, x] == 0):
            self.img[y, x] = c

    def disc(self, cx, cy, r, c, sx=1.0):
        self.img[((self.XX - cx) / sx) ** 2 + (self.YY - cy) ** 2 <= r * r + 0.5] = c

    def ellipse_ring(self, cx, cy, rx, ry, c, t=1.2):
        d = np.sqrt(((self.XX - cx) / max(rx, 0.1)) ** 2 + ((self.YY - cy) / max(ry, 0.1)) ** 2)
        self.img[np.abs(d - 1) * min(rx, ry) <= t / 2 + 0.3] = c

    def rays(self, cx, cy, n, r0, r1, rot, c, width=0.14, only_empty=True):
        a = np.arctan2(self.YY - cy, self.XX - cx)
        r = np.hypot(self.XX - cx, self.YY - cy)
        step = 2 * math.pi / n
        d = np.abs(((a - rot) + step / 2) % step - step / 2)
        m = (d < width * (1 - (r - r0) / max(1, r1 - r0)) + 0.02) & (r >= r0) & (r <= r1)
        if only_empty:
            m &= self.img == 0
        self.img[m] = c

    def line(self, x0, y0, x1, y1, c, w=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for t in np.linspace(0, 1, n):
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            if w <= 1:
                self.put(x, y, c)
            else:  # traço grosso: um disco em cada ponto
                r = w / 2
                ys, xs = slice(max(0, int(y - r)), int(y + r) + 1), slice(max(0, int(x - r)), int(x + r) + 1)
                sub = (self.XX[ys, xs] - x) ** 2 + (self.YY[ys, xs] - y) ** 2 <= r * r
                self.img[ys, xs][sub] = c

    def bolt(self, x0, y0, x1, y1, segs, jitter, rnd, c, w=1, branch=0):
        pts = [(x0, y0)]
        for i in range(1, segs):
            t = i / segs
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            nx, ny = -(y1 - y0), (x1 - x0)
            ln = math.hypot(nx, ny) or 1
            o = rnd.uniform(-jitter, jitter)
            pts.append((px + nx / ln * o, py + ny / ln * o))
        pts.append((x1, y1))
        for (a, b), (c2, d) in zip(pts, pts[1:]):
            self.line(a, b, c2, d, c, w)
        for _ in range(branch):
            i = rnd.randrange(1, len(pts) - 1)
            bx, by = pts[i]
            ang = math.atan2(y1 - y0, x1 - x0) + rnd.choice([-1, 1]) * rnd.uniform(0.5, 1.1)
            L = rnd.uniform(4, 9)
            self.bolt(bx, by, bx + math.cos(ang) * L, by + math.sin(ang) * L, 3, 1.5, rnd, c, 1)
        return pts

    def diamond(self, cx, cy, ang, size, cols=(BASE, MID, LIGHT)):
        ca, sa = math.cos(ang), math.sin(ang)
        u = (self.XX - cx) * ca + (self.YY - cy) * sa
        v = -(self.XX - cx) * sa + (self.YY - cy) * ca
        body = np.abs(u) / size + np.abs(v) / (size * 0.5) <= 1
        self.img[body] = cols[0]
        self.img[body & (v < 0)] = cols[1]
        self.img[body & (np.abs(v) < 0.7) & (np.abs(u) < size - 1)] = cols[2]

    def outline(self):
        f = self.img > 0
        e = np.zeros_like(f)
        e[1:, :] |= f[:-1, :]
        e[:-1, :] |= f[1:, :]
        e[:, 1:] |= f[:, :-1]
        e[:, :-1] |= f[:, 1:]
        self.img[e & ~f] = OUTLINE
        return self.img


# ------------------------------------------------------------------ raios (carga, raio, impacto)
W, H = 150, 48
CX, CY = W // 2, H // 2


def orb(cv, r):
    cv.disc(CX, CY, r, BASE)
    cv.disc(CX, CY, r - 1.5, MID)
    cv.disc(CX, CY, r - 3, LIGHT)
    cv.disc(CX, CY, max(1, r - 4.5), WHITE)


def charge(style, k, n, seed):
    cv = Canvas(W, H)
    t = (k + 1) / n
    rnd = random.Random(seed)
    cv.rays(CX, CY, 8, 4, 12 + 11 * t, k * 0.35, LIGHT if k % 2 else MID, 0.16)
    if style == "rings":  # ondas psíquicas se fechando sobre o orbe
        for i in range(3):
            r = (24 - 6 * i) * (1 - t) + 6 + 3 * i * (1 - t)
            cv.ellipse_ring(CX, CY, r * 1.3, r * 0.85, [LIGHT, MID, BASE][i], 1.4)
    elif style == "sparks":  # arcos elétricos pulando para o orbe
        for i in range(4):
            a = k * 0.8 + i * math.pi / 2 + rnd.uniform(-0.3, 0.3)
            R = 22 - 8 * t
            cv.bolt(CX + math.cos(a) * R * 1.4, CY + math.sin(a) * R * 0.9, CX, CY, 4, 2.5, rnd, LIGHT if i % 2 else WHITE)
    elif style == "shards":  # lascas de metal girando
        for i in range(4):
            a = k * 0.7 + i * math.pi / 2
            cv.diamond(CX + math.cos(a) * (20 - 7 * t) * 1.3, CY + math.sin(a) * (14 - 5 * t), a, 5)
    elif style == "shock":  # energia sendo sugada: linhas convergindo
        for i in range(12):
            a = i * math.pi / 6 + k * 0.2
            R0, R1 = 23 - 10 * t, 14 - 8 * t
            cv.line(CX + math.cos(a) * R0 * 1.4, CY + math.sin(a) * R0 * 0.9, CX + math.cos(a) * R1 * 1.4,
                    CY + math.sin(a) * R1 * 0.9, MID if i % 2 else LIGHT)
    for i in range(12):  # motas de luz vindo para o centro
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(18, 23) * (1 - t) + 3
        for j in range(3):
            cv.put(CX + math.cos(a) * (r + j * 1.5) * 1.3, CY + math.sin(a) * (r + j * 1.5) * 0.85, [WHITE, LIGHT, MID][j])
    orb(cv, 4 + 8 * t)
    if k == n - 1:
        cv.rays(CX, CY, 4, 9, 22, math.pi / 4, WHITE, 0.08)
    return cv.outline()


def beam(style, k, thick, seed):
    cv = Canvas(W, H)
    XX, YY = cv.XX, cv.YY
    x = XX.astype(float)
    on = x <= CX
    taper = np.clip((CX - x) / 6, 0, 1)
    wave = 0.8 * np.sin(x * 0.35 - k * 1.7)
    half = thick * (0.55 + 0.45 * taper) + wave * (thick > 3)
    dy = np.abs(YY - CY)
    body = on & (dy <= half)
    cv.img[body] = BASE
    cv.img[body & (dy <= half * 0.75)] = MID
    cv.img[body & (dy <= half * 0.5)] = LIGHT
    cv.img[body & (dy <= half * 0.25)] = WHITE
    rnd = random.Random(seed * 31 + k)
    if style == "rings":  # Psybeam: anéis coloridos correndo pelo raio
        for x0 in range((k * 4) % 12, CX - 3, 12):
            cv.ellipse_ring(x0, CY, 2.2, thick + 4, MID if (x0 // 12) % 2 else LIGHT, 1.3)
        band = on & (dy <= half * 0.5) & (((XX + k * 3) // 4) % 3 == 0)
        cv.img[band] = MID
    elif style == "zigzag":  # Charge Beam: dois raios elétricos enrolando no feixe
        for j in range(2):
            pts_y = CY + (thick + 3) * (1 if j else -1)
            cv.bolt(1, pts_y, CX - 2, CY, 14, 3.5, rnd, WHITE if j else LIGHT, 1, branch=2)
    elif style == "segments":  # Flash Cannon: faixas de luz em chevron correndo, borda metálica
        for x0 in range((k * 5) % 10, CX - 4, 10):
            for d in range(-int(half.max()), int(half.max()) + 1):
                cv.put(x0 + abs(d) * 0.6, CY + d, WHITE if abs(d) < thick * 0.4 else LIGHT, only_empty=False)
        edge = on & (np.abs(dy - half) < 0.8) & (half > 2)
        cv.img[edge] = DARK
    elif style == "ice":  # Ice Beam: cristais de gelo brotando das bordas do raio, brilhos de geada
        hm = float(half.max())
        for x0 in range((k * 3) % 9 + 4, CX - 6, 9):
            L = 4 + (x0 * 7 + k * 3) % 5
            for side in (-1, 1):
                for d in range(L):
                    wdt = (L - d) * 0.45
                    y = CY + side * (hm - 1 + d)
                    for dx in np.arange(-wdt, wdt + 0.01, 0.5):
                        cv.put(x0 + dx + side * d * 0.35, y, WHITE if abs(dx) < 0.6 else LIGHT if d < L - 1 else MID,
                               only_empty=True)
        for x0 in range((k * 5) % 13, CX - 4, 13):
            cv.put(x0, CY - 1, WHITE)
            cv.put(x0 + 1, CY, WHITE)
    elif style == "pulse":  # Hyper Beam: ondas de choque em anel ao longo do raio
        for x0 in range((k * 6) % 16, CX - 4, 16):
            cv.ellipse_ring(x0, CY, 2.5, thick + 6, LIGHT, 1.5)
            cv.ellipse_ring(x0 + 1, CY, 2.5, thick + 6, WHITE, 0.8)
    cv.rays(CX, CY, 8, 2, thick + 10, k * 0.4, LIGHT, 0.12)
    cv.disc(CX, CY, thick + 2, MID)
    cv.disc(CX, CY, thick, LIGHT)
    cv.disc(CX, CY, max(1, thick - 2), WHITE)
    cv.disc(2, CY, thick + 1, LIGHT)
    cv.disc(2, CY, max(1, thick - 1), WHITE)
    return cv.outline()


def impact(style, k, seed):
    cv = Canvas(W, H)
    rnd = random.Random(seed + 5)
    if k == 0:
        cv.rays(CX, CY, 12, 3, 22, 0.1, LIGHT, 0.14)
        cv.disc(CX, CY, 10, MID, 1.2)
        cv.disc(CX, CY, 8, LIGHT, 1.2)
        cv.disc(CX, CY, 6, WHITE, 1.2)
    elif k == 1:
        cv.ellipse_ring(CX, CY, 20, 15, MID, 2.5)
        cv.ellipse_ring(CX, CY, 20, 15, LIGHT, 1.0)
        cv.rays(CX, CY, 8, 6, 20, 0.4, LIGHT, 0.1)
        cv.disc(CX, CY, 4, WHITE)
    for i in range(8):
        a = i * 2 * math.pi / 8 + rnd.uniform(-0.2, 0.2)
        d = 8 + k * 5 + rnd.uniform(0, 2)
        x, y = CX + math.cos(a) * d * 1.6, CY + math.sin(a) * d * 0.8
        if k < 1:
            continue
        if style == "shards":
            cv.diamond(x, y + k * k * 0.5, a + k, 5 - 0.6 * k)
        elif style == "sparks" and k < 4:
            cv.bolt(x - math.cos(a) * 4, y - math.sin(a) * 3, x + math.cos(a) * 3, y + math.sin(a) * 2, 3, 1.5, rnd,
                    LIGHT if k < 3 else MID)
        elif style == "rings" and k < 4 and i % 2 == 0:
            cv.ellipse_ring(x, y, 2.5, 2.5, [LIGHT, MID, BASE][k - 1], 1)
        elif style == "shock" and i == 0 and k < 4:
            cv.ellipse_ring(CX, CY, 14 + 8 * k, 9 + 4 * k, [LIGHT, MID, BASE][k - 1], 1.2)
    if k >= 2:
        for i in range(12):
            a = rnd.uniform(0, 2 * math.pi)
            d = rnd.uniform(4, 10 + 4 * k)
            cv.put(CX + math.cos(a) * d * 1.4, CY + math.sin(a) * d * 0.8, [WHITE, LIGHT, MID][k - 2])
    return cv.outline()


def beam_move(charge_style, beam_style, impact_style, thick, seed):
    ch = [charge(charge_style, k, 5, seed) for k in range(5)]
    tk = [max(2, thick - 5), thick, thick + 1, thick + 1, max(3, thick - 4)]
    bm = [beam(beam_style, k, tk[k], seed) for k in range(5)]
    im = [impact(impact_style, k, seed) for k in range(5)]
    txt = {"Charge": 5, "Travel": 0, "Beam": "5 9", "Ticks": 4, "Face": 1}
    return ch + bm + im, txt


# ------------------------------------------------------------------ Thunder: raio caindo do céu no alvo
def thunder():
    w, h = 64, 96
    cx, cy = w // 2, 60  # o alvo fica no centro do frame; o raio vem do topo
    frames = []
    seq = ["nuvem", "fio", "raio", "raio2", "clarao", "chao", "brasas"]
    for k, kind in enumerate(seq):
        cv = Canvas(w, h)
        rnd = random.Random(40 + k)
        ty = h // 2  # centro do frame = alvo
        if kind == "nuvem":  # faíscas juntando no alto
            for i in range(10):
                cv.put(cx + rnd.uniform(-14, 14), rnd.uniform(2, 10), LIGHT if i % 2 else WHITE)
            cv.disc(cx, 4, 5, MID, 2.2)
            cv.disc(cx, 4, 3, LIGHT, 2.2)
        elif kind == "fio":  # o primeiro fio do raio desce
            cv.bolt(cx, 0, cx + 2, ty - 10, 7, 4, rnd, LIGHT, 2)
        elif kind in ("raio", "raio2"):  # raio grosso com galhos, núcleo branco
            pts = cv.bolt(cx, 0, cx, ty, 9, 6, random.Random(77 + (kind == "raio2")), MID, 5, branch=4)
            for (a, b), (c, d) in zip(pts, pts[1:]):
                cv.line(a, b, c, d, LIGHT, 3)
                cv.line(a, b, c, d, WHITE, 1)
            cv.rays(cx, ty, 10, 3, 16, k * 0.3, LIGHT, 0.14)
            cv.disc(cx, ty, 6, LIGHT, 1.4)
            cv.disc(cx, ty, 4, WHITE, 1.4)
        elif kind == "clarao":
            cv.rays(cx, ty, 12, 3, 26, 0.2, LIGHT, 0.14)
            cv.disc(cx, ty, 10, MID, 1.4)
            cv.disc(cx, ty, 8, LIGHT, 1.4)
            cv.disc(cx, ty, 6, WHITE, 1.4)
        elif kind == "chao":  # anel de choque e faíscas saltando
            cv.ellipse_ring(cx, ty + 6, 24, 7, MID, 2)
            cv.ellipse_ring(cx, ty + 6, 24, 7, LIGHT, 0.8)
            for i in range(6):
                a = -math.pi * (0.15 + 0.7 * i / 5)
                cv.bolt(cx, ty, cx + math.cos(a) * 18, ty + math.sin(a) * 14, 3, 2, rnd, LIGHT)
        else:
            for i in range(14):
                cv.put(cx + rnd.uniform(-22, 22), ty + rnd.uniform(-14, 8), [LIGHT, MID][i % 2])
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 4}, (w, h)


# ------------------------------------------------------------------ Dazzling Gleam: explosão de luz em volta do usuário
def dazzling_gleam():
    w, h = 112, 80
    cx, cy = w // 2, h // 2
    frames = []
    for k in range(9):
        cv = Canvas(w, h)
        rnd = random.Random(90)
        t = k / 8
        if k < 3:
            cv.rays(cx, cy, 4, 2, 6 + 8 * k, math.pi / 4, LIGHT, 0.12)
            cv.disc(cx, cy, 2 + 2 * k, WHITE)
        else:
            R = 10 + (k - 3) * 6
            cv.ellipse_ring(cx, cy, R * 1.4, R, MID if k < 6 else BASE, 3.2 if k < 6 else 2.2)
            cv.ellipse_ring(cx, cy, R * 1.4, R, LIGHT if k < 6 else MID, 0.9)
            for i in range(10):  # estrelas de 4 pontas espalhadas pela onda
                a = i * 2 * math.pi / 10 + k * 0.15
                x, y = cx + math.cos(a) * R * 1.4, cy + math.sin(a) * R
                s = 5 if k < 6 else 3
                cv.line(x - s, y, x + s, y, WHITE if k < 6 else LIGHT)
                cv.line(x, y - s, x, y + s, WHITE if k < 6 else LIGHT)
            if k < 5:
                cv.rays(cx, cy, 8, 4, 18, 0.39, LIGHT, 0.1)
                cv.disc(cx, cy, 6 - k, WHITE)
        for i in range(int(6 * t) + 2):  # brilhos soltos
            a = rnd.uniform(0, 2 * math.pi)
            d = rnd.uniform(6, 14 + 26 * t)
            cv.put(cx + math.cos(a) * d * 1.4, cy + math.sin(a) * d, LIGHT)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "At": "user"}, (w, h)


MOVES = {
    "HyperBeam": ("Normal", lambda: beam_move("shock", "pulse", "shock", 10, 3)),
    "Psybeam": ("Psíquico", lambda: beam_move("rings", "rings", "rings", 6, 5)),
    "ChargeBeam": ("Elétrico", lambda: beam_move("sparks", "zigzag", "sparks", 5, 7)),
    "FlashCannon": ("Aço", lambda: beam_move("shards", "segments", "shards", 8, 9)),
    "IceBeam": ("Gelo", lambda: beam_move("shards", "ice", "shards", 6, 11)),
    "Thunder": ("Elétrico", thunder),
    "DazzlingGleam": ("Fada", dazzling_gleam),
}


def save(name, tipo, frames, txt, size):
    w, h = size
    cols = PAL[tipo]
    def rgba(img):
        out = np.zeros((h, w, 4), np.uint8)
        for i, hx in enumerate(cols, start=1):
            out[img == i] = [int(hx[j:j + 2], 16) for j in (1, 3, 5)] + [255]
        return out.repeat(SCALE, 0).repeat(SCALE, 1)
    strip = np.concatenate([rgba(f) for f in frames], axis=1)
    Image.fromarray(strip, "RGBA").save(f"Terramon/Assets/Moves/{name}.png", optimize=True)
    lines = [f"Frames {len(frames)}"] + [f"{k} {v}" for k, v in txt.items()] + ["Scale 1"]
    open(f"Terramon/Assets/Moves/{name}.txt", "w").write("\n".join(lines) + "\n")
    print(name, len(frames), w * SCALE, h * SCALE)


if __name__ == "__main__":
    only = set(sys.argv[1].split(",")) if len(sys.argv) > 1 else None
    for name, (tipo, make) in MOVES.items():
        if only and name not in only:
            continue
        r = make()
        if len(r) == 2:
            frames, txt = r
            size = (W, H)
        else:
            frames, txt, size = r
        save(name, tipo, frames, txt, size)
