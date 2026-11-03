"""Golpes com forma própria desenhados à mão em código: Will-O-Wisp (chamas-fantasma que voam e rodeiam o alvo),
Twister (tornado de dragão) e Rock Slide (pedras caindo do alto no alvo).

Usa o Canvas de draw_light_moves.py: pixel art 2x sem suavizar, só as 6 cores do tipo, contorno de 1px.
Rodar da raiz do repo: python3 tools/vfx/draw_shape_moves.py [Golpe,Golpe]
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from draw_light_moves import Canvas, WHITE, LIGHT, MID, BASE, DARK, OUTLINE, SCALE  # noqa: E402

PAL = {
    "Fogo": ["#FFF6C8", "#FFD84A", "#FF9A1F", "#E8480F", "#A3200E", "#4A1010"],
    "Dragão": ["#FFFFFF", "#A5B4FC", "#6366F1", "#4338CA", "#251F72", "#130D3E"],
    "Pedra": ["#F8FAFC", "#E2E8F0", "#94A3B8", "#64748B", "#475569", "#1E293B"],
}


# ------------------------------------------------------------------ Will-O-Wisp
def wisp(cv, x, y, size, flick):
    """Chama-fantasma: bola com cauda que sobe e treme."""
    tail = size * 2.2 + flick
    for i in range(int(tail)):  # cauda afinando para cima
        t = i / tail
        r = size * (1 - t) * 0.9
        cv.disc(x + math.sin(i * 0.9 + flick) * 0.8 * t, y - i, max(0.5, r), BASE if t > 0.5 else MID)
    cv.disc(x, y, size, MID)
    cv.disc(x - 0.5, y - 0.5, size * 0.7, LIGHT)
    cv.disc(x - 0.8, y - 0.8, max(0.8, size * 0.35), WHITE)


def will_o_wisp():
    w, h = 64, 56
    cx, cy = w // 2, h // 2 + 4
    frames = []
    # voo: três chamas girando umas em volta das outras
    for k in range(4):
        cv = Canvas(w, h)
        for i in range(3):
            a = k * 0.9 + i * 2 * math.pi / 3
            wisp(cv, cx + math.cos(a) * 9, cy + math.sin(a) * 5, 4.8, k + i)
        frames.append(cv.outline())
    # no alvo: as chamas rodeiam, encolhem a órbita, mergulham e queimam
    for k in range(7):
        cv = Canvas(w, h)
        if k < 4:
            R = 18 - 3 * k
            for i in range(3):
                a = k * 1.1 + i * 2 * math.pi / 3
                wisp(cv, cx + math.cos(a) * R * 1.3, cy + math.sin(a) * R * 0.5, 5 - 0.4 * k, k + i)
        elif k == 4:  # clarão da queimadura
            cv.rays(cx, cy, 8, 3, 16, 0.3, LIGHT, 0.16)
            cv.disc(cx, cy, 7, MID)
            cv.disc(cx, cy, 5, LIGHT)
            cv.disc(cx, cy, 3, WHITE)
        else:  # brasas subindo
            rnd = random.Random(k)
            for i in range(8):
                x = cx + rnd.uniform(-14, 14)
                y = cy - (k - 4) * 6 - rnd.uniform(0, 8)
                cv.disc(x, y, 1.2, MID if k == 5 else BASE)
                cv.put(x, y - 2, LIGHT)
        frames.append(cv.outline())
    return frames, {"Travel": 4, "Ticks": 4, "At": "target"}, (w, h)


# ------------------------------------------------------------------ Twister
def twister():
    w, h = 72, 88
    cx, base = w // 2, h - 8
    frames = []
    heights = [20, 40, 62, 70, 70, 70, 70, 55, 30]
    for k, H in enumerate(heights):
        cv = Canvas(w, h)
        rnd = random.Random(k * 3)
        top = base - H
        # funil: fatias horizontais, mais largas em cima, com listras girando
        for y in range(top, base):
            t = (base - y) / 70
            half = 3 + 20 * t ** 1.3
            sway = math.sin(y * 0.12 + k * 0.8) * (2 + 4 * t)
            for x in range(int(cx + sway - half), int(cx + sway + half) + 1):
                u = (x - (cx + sway)) / max(half, 1)
                stripe = math.sin(u * 3 + y * 0.35 - k * 1.4)
                c = LIGHT if stripe > 0.6 else MID if stripe > -0.2 else BASE
                if abs(u) > 0.85:
                    c = DARK
                cv.put(x, y, c)
        # poeira e detritos girando em volta
        for i in range(10):
            a = k * 0.7 + i * 2 * math.pi / 10
            yy = base - rnd.uniform(4, H)
            t = (base - yy) / 70
            r = 5 + 22 * t ** 1.3
            x = cx + math.cos(a) * r
            if math.sin(a) < 0 and cv.img[int(min(h - 1, max(0, yy))), int(min(w - 1, max(0, x)))]:
                continue  # atrás do funil
            cv.disc(x, yy, 1.2, WHITE if i % 3 == 0 else LIGHT)
        if k < 7:  # redemoinho de poeira no chão
            cv.ellipse_ring(cx, base + 2, 14 + k, 3, MID, 1.2)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 4}, (w, h)


# ------------------------------------------------------------------ Rock Slide
def rock(cv, x, y, r, seed):
    """Pedra irregular com luz no alto à esquerda."""
    rnd = random.Random(seed)
    bumps = [rnd.uniform(0.75, 1.15) for _ in range(9)]
    a = np.arctan2(cv.YY - y, cv.XX - x)
    d = np.hypot(cv.XX - x, cv.YY - y)
    idx = ((a + math.pi) / (2 * math.pi) * 9).astype(int) % 9
    rr = np.array(bumps)[idx] * r
    body = d <= rr
    cv.img[body] = BASE
    lit = body & ((cv.XX - x) + (cv.YY - y) < -r * 0.2)
    cv.img[lit] = MID
    cv.img[body & ((cv.XX - x) + (cv.YY - y) < -r * 0.9)] = LIGHT
    cv.img[body & ((cv.XX - x) + (cv.YY - y) > r * 0.9)] = DARK
    for _ in range(2):  # rachaduras
        px, py = x + rnd.uniform(-r / 2, r / 2), y + rnd.uniform(-r / 2, r / 2)
        cv.line(px, py, px + rnd.uniform(-3, 3), py + rnd.uniform(1, 3), DARK)


def rock_slide():
    w, h = 80, 88
    ground = h - 14
    frames = []
    rocks = [(-16, 11, 0), (8, 14, 2), (24, 9, 4), (-4, 8, 5)]  # (deslocamento x, raio, frame em que cai)
    for k in range(11):
        cv = Canvas(w, h)
        rnd = random.Random(k)
        for i, (dx, r, start) in enumerate(rocks):
            t = k - start
            if t < 0:
                continue
            x = w / 2 + dx
            if t < 3:  # caindo, com rastro
                y = 6 + r + t * (ground - r - 6) / 3
                for j in range(1, 4):
                    cv.put(x + rnd.uniform(-r / 2, r / 2), y - r - j * 3, LIGHT if j == 1 else MID)
                rock(cv, x, y, r, i)
            elif t < 6:  # no chão, poeira subindo
                rock(cv, x, ground - r * 0.8, r, i)
                for j in range(5):
                    a = math.pi + j * math.pi / 4
                    R = 4 + (t - 2) * 4
                    cv.disc(x + math.cos(a) * (r + R), ground + math.sin(a) * R * 0.4 - 1, 3.4 - 0.5 * (t - 3),
                            LIGHT if t == 3 else MID)
            elif t < 8:  # pedra se desfazendo em cascalho
                for j in range(6):
                    cv.disc(x + rnd.uniform(-r, r), ground - rnd.uniform(0, r * (8 - t) / 2), 2.4, BASE if j % 2 else MID)
        if 2 <= k <= 7:  # tremor: rachaduras no chão
            for i in range(3):
                x0 = w / 2 + (i - 1) * 16
                cv.line(x0, ground + 3, x0 + rnd.uniform(-5, 5), ground + 7, DARK)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 4}, (w, h)


MOVES = {"WillOWisp": ("Fogo", will_o_wisp), "Twister": ("Dragão", twister), "RockSlide": ("Pedra", rock_slide)}


def save(name, cols, frames, txt, size):
    w, h = size
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
        if not only or name in only:
            frames, txt, size = make()
            save(name, PAL[tipo], frames, txt, size)
