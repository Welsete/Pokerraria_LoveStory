"""Golpes com referência ao que o golpe é: socos mostram um punho, chutes um pé, Swords Dance espadas girando e os
golpes de dragão uma cabeça de dragão. Os desenhos (punho, bota, espada, cabeça de dragão) são pixel art feita à mão
nos mapas abaixo; as cores vêm do tipo de cada golpe.

Rodar da raiz do repo: python3 tools/vfx/draw_themed_moves.py [Golpe,Golpe]
Esses golpes saem de BASICOS em basicos.py para não serem sobrescritos.
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

KEY = {".": 0, "W": WHITE, "L": LIGHT, "M": MID, "B": BASE, "D": DARK, "O": OUTLINE}


def bitmap(rows):
    return np.array([[KEY[c] for c in r] for r in rows], np.uint8)


# Punho de lado, nós dos dedos para a direita
FIST = bitmap([
    "....OOOOOOOOO...",
    "...OWLLLLLLLLO..",
    "..OLLMMMOMMOMMO.",
    "..OLMMMMOMMOMMMO",
    "OOOLMMMMOMMOMMMO",
    "OLLLMMMMOMMOMMMO",
    "OLMMMBBBOBBOBBBO",
    "OLMMBBBBBBBBBBO.",
    "OMBBBBBBBBBBBBO.",
    "OBBDDDDDDDDDDO..",
    "OODDDDDDDDDDO...",
    "..OOOOOOOOOO....",
])
# Bota de lado, bico para a direita
BOOT = bitmap([
    "..OOOOOO........",
    "..OLMMMO........",
    "..OLMMMO........",
    "..OLMMMO........",
    "..OLMMMO........",
    "..OLMMMOOOOOO...",
    "..OLMMMMMMMMMOO.",
    ".OLLMMMMMMMMMMMO",
    ".OLMMMMMMMMMMMMO",
    ".OBBBBBBBBBBBBBO",
    ".ODDDDDDDDDDDDDO",
    "..OOOOOOOOOOOOO.",
])
# Espada de ponta para cima
SWORD = bitmap([
    "...O...",
    "..OWO..",
    "..OWO..",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    ".OLWMO.",
    "OOOOOOO",
    "OLMMMBO",
    "OOOOOOO",
    "..OBO..",
    "..OBO..",
    "..OBO..",
    ".OMMMO.",
    "..OOO..",
])
# Cabeça de dragão de lado, olhando para a direita
DRAGON = bitmap([
    "..OO..................",
    "..OLO....OO...........",
    "...OLO..OLO...........",
    "....OLOOLO............",
    "...OMMMMMOOO..........",
    "..OMMLLMMMMMOOOO......",
    ".OMMLWDMMMMMMMMMOOO...",
    ".OMMMMMMMMMMMMMMMMMO..",
    ".OBMMMMMMMMMMMMMMMMMO.",
    ".OBBMMMMMMMMMMMMMWOWO.",
    "..OBBBBBBBOOOOOOOO....",
    "..OBBBBBOWOWOWO.......",
    "...OBBBBBBBBBBO.......",
    "....ODDDDDDDDO........",
    ".....OOOOOOOO.........",
])


def stamp(cv, bmp, x, y, flip=False, angle=0):
    """Cola o desenho com o centro em (x, y); angle em graus gira sem suavizar."""
    b = bmp[:, ::-1] if flip else bmp
    if angle:
        b = np.array(Image.fromarray(b, "L").rotate(angle, resample=Image.NEAREST, expand=True))
    h, w = b.shape
    x0, y0 = int(round(x - w / 2)), int(round(y - h / 2))
    for j in range(h):
        for i in range(w):
            if b[j, i] and 0 <= x0 + i < cv.w and 0 <= y0 + j < cv.h:
                cv.img[y0 + j, x0 + i] = b[j, i]


def burst(cv, x, y, k, big, rnd):
    """Impacto: clarão, anel e faíscas, k = frames desde a batida."""
    if k == 0:
        cv.rays(x, y, 8 if big else 6, 3, 20 if big else 15, 0.3, LIGHT, 0.16)
        cv.disc(x, y, 8 if big else 6, MID)
        cv.disc(x, y, 6 if big else 4, LIGHT)
        cv.disc(x, y, 4 if big else 3, WHITE)
    elif k < 4:
        R = (8 if big else 6) + k * (6 if big else 4)
        cv.ellipse_ring(x, y, R, R * 0.9, LIGHT if k == 1 else MID, 1.4)
        for i in range(9 if big else 6):
            a = i * 2 * math.pi / (9 if big else 6) + 0.3
            d = R * 0.8 + rnd.uniform(0, 3)
            cv.line(x + math.cos(a) * d, y + math.sin(a) * d, x + math.cos(a) * (d + 3), y + math.sin(a) * (d + 3),
                    WHITE if k == 1 else LIGHT)


def punch(big):
    frames = []
    cx, cy = 34, 32
    rnd = random.Random(3)
    for k in range(8):
        cv = Canvas(64, 64)
        if k < 3:  # o punho chega com linhas de velocidade
            x = 8 + k * 9
            for j in range(4):
                cv.line(x - 12 - j * 2, cy - 4 + j * 3, x - 8, cy - 4 + j * 3, MID if j % 2 else LIGHT)
            stamp(cv, FIST, x, cy)
        else:
            burst(cv, cx + 4, cy, k - 3, big, rnd)
            if k < 5:
                stamp(cv, FIST, cx - 6, cy)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "Face": 1}


def kick(big):
    frames = []
    cx, cy = 34, 32
    rnd = random.Random(5)
    path = [(-150, 18), (-120, 18), (-80, 18)]  # o pé desce num arco
    for k in range(8):
        cv = Canvas(64, 64)
        if k < 3:
            a0 = math.radians(path[0][0])
            for t in np.linspace(a0, math.radians(path[k][0]), 10):  # rastro do arco
                cv.disc(cx - 6 + math.cos(t) * 20, cy + 6 + math.sin(t) * 20, 1.2, LIGHT)
            a = math.radians(path[k][0])
            stamp(cv, BOOT, cx - 6 + math.cos(a) * 20, cy + 6 + math.sin(a) * 20)
        else:
            burst(cv, cx + 2, cy - 2, k - 3, big, rnd)
            if k < 5:
                stamp(cv, BOOT, cx - 6, cy - 4)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "Face": 1}


def swords_dance():
    frames = []
    cx, cy = 32, 40
    for k in range(10):
        cv = Canvas(64, 72)
        if k < 7:  # seis espadas girando em volta do usuário
            order = []
            for i in range(6):
                a = k * 0.45 + i * math.pi / 3
                order.append((math.sin(a), cx + math.cos(a) * 22, cy + math.sin(a) * 6))
            for _, x, y in sorted(order):
                stamp(cv, SWORD, x, y - 6)
        else:  # sobem e se cruzam no alto, com um brilho
            t = k - 7
            for i in range(6):
                a = i * math.pi / 3
                x = cx + math.cos(a) * (14 - 5 * t)
                stamp(cv, SWORD, x, cy - 14 - 6 * t, angle=math.degrees(math.cos(a)) * 0.4 * (1 + t))
            cv.rays(cx, 10, 8, 2, 10 + 3 * t, 0.2, WHITE, 0.15)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "At": "user"}


def dragon_dance():
    """Um dragão de energia sobe em espiral em volta do usuário."""
    frames = []
    cx, base = 32, 66
    for k in range(10):
        cv = Canvas(64, 80)
        head_t = min(1.0, (k + 1) / 8)
        body = []
        for s in range(16):
            t = head_t - s * 0.045
            if t < 0:
                break
            a = t * 2.2 * math.pi
            body.append((t, cx + math.cos(a) * 18, base - t * 54, math.sin(a)))
        for t, x, y, depth in sorted(body[1:], key=lambda b: b[3]):
            r = 3.4 - (head_t - t) * 3
            cv.disc(x, y, max(1.2, r), MID if depth > 0 else BASE)
            cv.disc(x - 0.5, y - 0.8, max(0.6, r * 0.5), LIGHT)
        if body:
            _, x, y, _ = body[0]
            nxt = body[1] if len(body) > 1 else body[0]
            stamp(cv, DRAGON, x, y, flip=x < nxt[1])
        if k >= 8:
            cv.rays(cx, base - 30, 8, 4, 24, 0.2, LIGHT, 0.12)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "At": "user"}


def dragon_projectile(pulse):
    """Cabeça de dragão de energia voando até o alvo (Dragon Rush, Dragon Pulse)."""
    frames = []
    rnd = random.Random(9)
    for k in range(3):  # voo
        cv = Canvas(64, 64)
        for s in range(6):  # corpo ondulando atrás
            x = 26 - s * 5
            y = 32 + math.sin(s * 0.9 + k * 1.3) * 3
            cv.disc(x, y, 4 - s * 0.5, MID if s % 2 else BASE)
        stamp(cv, DRAGON, 38, 30)
        frames.append(cv.outline())
    for k in range(6):  # impacto
        cv = Canvas(64, 64)
        if pulse and k < 4:
            for j in range(3):
                R = 6 + k * 5 + j * 4
                cv.ellipse_ring(32, 32, R, R * 0.85, [LIGHT, MID, BASE][j], 1.3)
        burst(cv, 32, 32, k, True, rnd)
        if k < 2:
            stamp(cv, DRAGON, 30, 30)
        frames.append(cv.outline())
    return frames, {"Travel": 3, "Ticks": 3, "Face": 1}


def dragon_claw():
    frames = []
    for k in range(8):
        cv = Canvas(64, 64)
        if 1 <= k <= 4:  # a cabeça do dragão aparece atrás das garras
            stamp(cv, DRAGON, 30, 30)
        n = min(k, 3)
        for j in range(3):  # três riscos de garra descendo
            if k < 6:
                x0 = 18 + j * 9
                L = min(1.0, (k + 1) / 3)
                cv.line(x0, 12, x0 + 12 * L, 12 + 36 * L, WHITE if k < 4 else LIGHT, 3)
                cv.line(x0 + 1, 12, x0 + 1 + 12 * L, 12 + 36 * L, MID, 1)
        if k >= 4:
            for i in range(6):
                a = i * 1.05
                cv.put(32 + math.cos(a) * (6 + 3 * (k - 4)), 32 + math.sin(a) * (6 + 3 * (k - 4)), LIGHT)
        frames.append(cv.outline())
    return frames, {"Travel": 0, "Ticks": 3, "Face": 1}


PUNCH = {"Mega Punch": ("Normal", True), "Drain Punch": ("Lutador", False), "Dynamic Punch": ("Lutador", True),
         "Focus Punch": ("Lutador", True), "Mach Punch": ("Lutador", False), "Shadow Punch": ("Fantasma", False),
         "Sucker Punch": ("Sombrio", False), "Meteor Mash": ("Aço", True), "Rage Fist": ("Fantasma", True),
         "Hammer Arm": ("Lutador", True), "Force Palm": ("Lutador", False), "Rock Smash": ("Lutador", False),
         "Brick Break": ("Lutador", False), "Close Combat": ("Lutador", True), "Superpower": ("Lutador", True)}
KICK = {"Axe Kick": ("Lutador", True), "Double Kick": ("Lutador", False), "High Jump Kick": ("Lutador", True),
        "Low Kick": ("Lutador", False), "Low Sweep": ("Lutador", False), "Mega Kick": ("Normal", True),
        "Stomp": ("Normal", False), "Stomping Tantrum": ("Terrestre", True)}
OTHER = {"Swords Dance": ("Normal", swords_dance), "Dragon Dance": ("Dragão", dragon_dance),
         "Dragon Rush": ("Dragão", lambda: dragon_projectile(False)),
         "Dragon Pulse": ("Dragão", lambda: dragon_projectile(True)), "Dragon Claw": ("Dragão", dragon_claw)}


# Socos, chutes, Dragon Rush e Dragon Pulse ficaram feios desenhados assim (decisão do Welsete, 2026-11-03): eles
# voltaram para o gerador de imagem (provisórios em código, em basicos.py). Os moldes punch/kick ficam aqui guardados.
EM_USO = ["Swords Dance", "Dragon Dance", "Dragon Claw"]


def all_moves():
    return {n: OTHER[n] for n in EM_USO}


def move_id(nome):
    import re
    return "".join(p[:1].upper() + p[1:] for p in re.split(r"[^A-Za-z0-9]+", nome) if p)


def save(name, cols, frames, txt):
    h, w = frames[0].shape
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
    for nome, (tipo, make) in all_moves().items():
        if only and nome not in only:
            continue
        frames, txt = make()
        save(move_id(nome), PALETAS[tipo], frames, txt)
