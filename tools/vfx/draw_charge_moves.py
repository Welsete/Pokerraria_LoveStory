"""Turno de carga dos golpes de dois turnos (Fly, Dig, Skull Bash...), desenhado em código.

No turno de carga o Showdown manda o golpe "parado" e o jogo não mostrava nada. Estas animações tocam no Pokémon que
está carregando (Assets/Moves/Charge/<MoveID>.png, mesmo formato dos outros golpes; At user).
Pixel art 2x, só as 6 cores do tipo, contorno de 1px (Canvas de draw_light_moves.py, paletas de basicos.py).

Rodar da raiz do repo: python3 tools/vfx/draw_charge_moves.py [MoveID,MoveID]
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from draw_light_moves import Canvas, WHITE, LIGHT, MID, BASE, DARK, OUTLINE, SCALE  # noqa: E402
from basicos import PALETAS  # noqa: E402

W = H = 64
CX, CY = W // 2, H // 2
N = 9  # frames


def orb(cv, x, y, r):
    if r <= 0:
        return
    cv.disc(x, y, r, BASE)
    cv.disc(x, y, r - 1.5, MID)
    cv.disc(x, y, r - 3, LIGHT)
    cv.disc(x, y, max(0.8, r - 4.5), WHITE)


# ------------------------------------------------------------------ juntar energia
def gather(extra=None, seed=1):
    """Partículas girando para o centro, raios e um orbe que cresce; o último quadro é um lampejo."""
    frames = []
    rnd = random.Random(seed)
    motes = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(20, 28)) for _ in range(14)]
    for k in range(N):
        cv = Canvas(W, H)
        t = (k + 1) / N
        if k < N - 1:
            cv.rays(CX, CY, 8, 4, 8 + 14 * t, k * 0.3, LIGHT if k % 2 else MID, 0.14)
            for a0, r0 in motes:
                a = a0 + t * 2.2
                r = r0 * (1 - t) + 3
                for j in range(3):  # rastro curto girando
                    aj = a - j * 0.12
                    cv.put(CX + math.cos(aj) * (r + j), CY + math.sin(aj) * (r + j), [WHITE, LIGHT, MID][j])
            if extra:
                extra(cv, k, t)
            orb(cv, CX, CY, 2 + 6 * t)
        else:
            cv.rays(CX, CY, 4, 6, 28, math.pi / 4, WHITE, 0.08, only_empty=False)
            cv.rays(CX, CY, 8, 4, 18, 0, LIGHT, 0.1)
            orb(cv, CX, CY, 9)
        frames.append(cv.outline())
    return frames


def leaves(cv, k, t):
    for i in range(3):
        a = k * 0.8 + i * 2 * math.pi / 3
        cv.diamond(CX + math.cos(a) * (18 - 8 * t), CY + math.sin(a) * (14 - 6 * t), a + math.pi / 2, 3.5)


def rocks(cv, k, t):
    for i in range(4):
        a = k * 0.5 + i * math.pi / 2
        x, y = CX + math.cos(a) * 20, CY + math.sin(a) * 9
        cv.disc(x, y, 3, BASE)
        cv.disc(x - 0.8, y - 0.8, 1.6, MID)


def sparks(cv, k, t):
    rnd = random.Random(k)
    for _ in range(2):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(14, 24)
        cv.bolt(CX + math.cos(a) * r, CY + math.sin(a) * r, CX + math.cos(a) * 8, CY + math.sin(a) * 8, 4, 2.5, rnd, LIGHT)


def crystals(cv, k, t):
    for i in range(5):
        a = i * 2 * math.pi / 5 + 0.3
        size = 2 + 4 * min(1, t * 1.4)
        cv.diamond(CX + math.cos(a) * 16, CY + math.sin(a) * 14, a, size, (BASE, MID, WHITE))


def stars4(cv, k, t):
    rnd = random.Random(k * 7)
    for _ in range(5):
        x, y = CX + rnd.uniform(-24, 24), CY + rnd.uniform(-24, 24)
        s = rnd.choice((2, 3))
        cv.line(x - s, y, x + s, y, LIGHT)
        cv.line(x, y - s, x, y + s, LIGHT)
        cv.put(x, y, WHITE)


def crescents(cv, k, t):
    for i in range(3):
        a = k * 0.9 + i * 2 * math.pi / 3
        r = 20 - 6 * t
        x, y = CX + math.cos(a) * r, CY + math.sin(a) * r * 0.7
        for j in range(7):
            b = a + math.pi / 2 + (j - 3) * 0.25
            cv.put(x + math.cos(b) * 5, y + math.sin(b) * 5, LIGHT if abs(j - 3) < 2 else MID)


def brace(cv, k, t):
    """Skull Bash: a cabeça abaixa e uma couraça de energia se fecha na frente."""
    for i in range(3):
        rx = 10 + i * 5 + (1 - t) * 6
        a0 = -0.9
        for j in range(18):
            b = a0 + j * 0.1
            cv.put(CX + 4 + math.cos(b) * rx, CY + math.sin(b) * rx, [WHITE, LIGHT, MID][i])
    # setas de defesa subindo
    for i, x in enumerate((CX - 18, CX - 10)):
        y = CY + 14 - ((k * 4 + i * 7) % 22)
        cv.line(x, y, x, y + 5, MID)
        cv.put(x - 1, y + 1, MID)
        cv.put(x + 1, y + 1, MID)


def flames_up(cv, k, t):
    """Sky Attack: labaredas claras em volta do brilho, apontando para fora e tremendo."""
    for i in range(8):
        a = i * 2 * math.pi / 8 + k * 0.15
        flick = 3 * math.sin(k * 1.7 + i * 2.1)
        r0, r1 = 9 + 4 * t, 14 + 9 * t + flick
        for j in range(int(r1 - r0) + 1):
            r = r0 + j
            f = j / max(1, r1 - r0)
            w = 2.4 * (1 - f) + 0.4
            x, y = CX + math.cos(a) * r, CY + math.sin(a) * r
            c = WHITE if f < 0.25 else LIGHT if f < 0.6 else MID
            cv.disc(x, y, w, c)


# ------------------------------------------------------------------ subir (Fly, Bounce)
def rise(feathers):
    frames = []
    for k in range(N):
        cv = Canvas(W, H)
        rnd = random.Random(k)
        if k < 4:  # vento juntando embaixo
            cv.ellipse_ring(CX, CY + 18, 10 + k * 3, 3 + k * 0.6, MID, 1.3)
            cv.ellipse_ring(CX, CY + 18, 4 + k * 2, 1.5 + k * 0.4, LIGHT, 1.1)
        for i in range(6):  # linhas de vento subindo
            x = CX + (i - 2.5) * 7
            y = CY + 22 - ((k * 9 + i * 13) % 44)
            L = 6 + (i % 3) * 3
            cv.line(x, y, x, y + L, LIGHT if i % 2 else WHITE)
        if feathers:
            for i in range(3):
                x = CX + rnd.uniform(-18, 18)
                y = CY + 20 - ((k * 6 + i * 15) % 40)
                cv.diamond(x, y, rnd.uniform(-0.6, 0.6) + math.pi / 2, 3, (BASE, MID, WHITE))
        else:  # molas: anéis comprimindo e soltando
            r = 6 + abs(4 - k) * 2
            cv.ellipse_ring(CX, CY + 12 - k, r, r * 0.35, MID, 1.3)
        if k >= N - 3:  # rastro de partida para o alto
            for j in range(3):
                cv.line(CX - 3 + j * 3, CY - 30, CX - 3 + j * 3, CY - 30 + (N - k) * 10, WHITE if j == 1 else LIGHT)
        frames.append(cv.outline())
    return frames


# ------------------------------------------------------------------ cavar (Dig)
def dig():
    frames = []
    clods = [(random.Random(i).uniform(-1.2, 1.2), random.Random(i * 5).uniform(2.5, 4.0)) for i in range(10)]
    ground = CY + 16
    for k in range(N):
        cv = Canvas(W, H)
        # buraco crescendo
        cv.disc(CX, ground, 4 + k * 1.6, DARK, 3.0) if False else None
        rx = 6 + k * 2
        cv.img[((cv.XX - CX) / rx) ** 2 + ((cv.YY - ground) / max(1.5, rx * 0.3)) ** 2 <= 1] = DARK
        cv.img[((cv.XX - CX) / (rx * 0.7)) ** 2 + ((cv.YY - ground - 1) / max(1, rx * 0.2)) ** 2 <= 1] = OUTLINE
        # torrões voando em arco
        for i, (vx, vy) in enumerate(clods):
            tt = (k - i % 3) * 0.9
            if tt < 0:
                continue
            x = CX + vx * 7 * tt
            y = ground - (vy * 4 * tt - 0.9 * tt * tt)
            if y > ground + 2:
                continue
            cv.disc(x, y, 2.2 if i % 2 else 1.6, BASE)
            cv.put(x - 0.6, y - 0.6, MID)
        # poeira nas bordas
        if k >= 2:
            for s in (-1, 1):
                cv.disc(CX + s * (rx + 2), ground - 2, 2.5 - 0.2 * k, LIGHT)
        frames.append(cv.outline())
    return frames


# ------------------------------------------------------------------ mergulhar (Dive)
def dive():
    frames = []
    surface = CY + 14
    for k in range(N):
        cv = Canvas(W, H)
        h = [4, 12, 22, 30, 32, 26, 16, 8, 3][k]
        w = 5 + k * 0.6
        for y in range(surface - h, surface):
            t = (surface - y) / max(h, 1)
            half = w * (1 - 0.5 * t)
            c = WHITE if t > 0.8 else LIGHT if t > 0.4 else MID
            cv.line(CX - half, y, CX + half, y, c)
        cv.ellipse_ring(CX, surface, 12 + k * 2, 3 + k * 0.4, MID, 1.3)
        cv.ellipse_ring(CX, surface, 6 + k, 2, BASE, 1.2)
        rnd = random.Random(k)
        for _ in range(5):  # gotas
            x = CX + rnd.uniform(-14, 14)
            y = surface - h + rnd.uniform(-4, 10)
            cv.disc(x, y, 1.3, LIGHT)
        frames.append(cv.outline())
    return frames


# ------------------------------------------------------------------ sumir nas sombras (Phantom/Shadow Force)
def vanish():
    frames = []
    for k in range(N):
        cv = Canvas(W, H)
        t = k / (N - 1)
        for i in range(6):  # fiapos de fumaça girando e fechando
            a0 = i * 2 * math.pi / 6 + k * 0.5
            r0 = 26 - 16 * t
            for j in range(9):
                a = a0 + j * 0.13
                r = r0 - j * 0.6
                cv.disc(CX + math.cos(a) * r, CY + math.sin(a) * r * 0.8, max(0.6, 2.4 - j * 0.22),
                        BASE if j > 4 else MID)
        if k >= 3:  # olhos de sombra e o portal no centro
            r = 3 + (k - 3) * 1.5 if k < N - 1 else 1.5
            cv.disc(CX, CY, r + 2, DARK)
            cv.disc(CX, CY, r, OUTLINE)
            if k < N - 1:
                cv.put(CX - 2, CY - 1, WHITE)
                cv.put(CX + 2, CY - 1, WHITE)
        frames.append(cv.outline())
    return frames


MOVES = {
    # MoveID: (tipo, desenho)
    "Fly": ("Voador", lambda: rise(True)),
    "Bounce": ("Voador", lambda: rise(False)),
    "Dig": ("Terrestre", dig),
    "Dive": ("Água", dive),
    "PhantomForce": ("Fantasma", vanish),
    "ShadowForce": ("Fantasma", vanish),
    "SkullBash": ("Normal", lambda: gather(brace, 2)),
    "SkyAttack": ("Voador", lambda: gather(flames_up, 3)),
    "RazorWind": ("Normal", lambda: gather(crescents, 4)),
    "SolarBeam": ("Planta", lambda: gather(leaves, 5)),
    "SolarBlade": ("Planta", lambda: gather(leaves, 6)),
    "MeteorBeam": ("Pedra", lambda: gather(rocks, 7)),
    "Geomancy": ("Fada", lambda: gather(stars4, 8)),
    "ElectroShot": ("Elétrico", lambda: gather(sparks, 9)),
    "FreezeShock": ("Gelo", lambda: gather(lambda cv, k, t: (crystals(cv, k, t), sparks(cv, k, t)), 10)),
    "IceBurn": ("Gelo", lambda: gather(crystals, 11)),
}


def save(name, cols, frames):
    def rgba(img):
        out = np.zeros((H, W, 4), np.uint8)
        for i, hx in enumerate(cols, start=1):
            out[img == i] = [int(hx[j:j + 2], 16) for j in (1, 3, 5)] + [255]
        return out.repeat(SCALE, 0).repeat(SCALE, 1)
    strip = np.concatenate([rgba(f) for f in frames], axis=1)
    Image.fromarray(strip, "RGBA").save(f"Pokerraria/Assets/Moves/Charge/{name}.png", optimize=True)
    open(f"Pokerraria/Assets/Moves/Charge/{name}.txt", "w").write(f"Frames {len(frames)}\nTravel 0\nTicks 4\nAt user\nScale 1\n")
    return strip


if __name__ == "__main__":
    only = set(sys.argv[1].split(",")) if len(sys.argv) > 1 and sys.argv[1] else None
    rows = []
    for name, (tipo, make) in MOVES.items():
        if not only or name in only:
            rows.append(save(name, PALETAS[tipo], make()))
            print(name, tipo)
    if len(sys.argv) > 2:  # prévia: todos um embaixo do outro em fundo escuro
        prev = Image.new("RGBA", (max(r.shape[1] for r in rows), sum(r.shape[0] for r in rows)), (36, 36, 54, 255))
        y = 0
        for r in rows:
            prev.alpha_composite(Image.fromarray(r, "RGBA"), (0, y))
            y += r.shape[0]
        prev.save(sys.argv[2])
