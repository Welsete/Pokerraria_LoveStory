"""Solar Beam desenhado por código (sem gerador de imagem): carga de luz solar no usuário, um raio com núcleo branco,
vinhas de luz girando em volta e faíscas correndo, e uma explosão de luz e folhas no alvo.

Arte em 150x48 pixels por frame, escala 2x sem suavizar (cada pixel vira 2x2 na tela), só as 6 cores do tipo Planta.
Frames de raio: a ponta fica no centro do frame e o raio ocupa a metade esquerda; o jogo estica essa metade do usuário
até o alvo (Beam A B em MoveVisuals.cs).

Rodar da raiz do repo: python3 tools/vfx/draw_solar_beam.py
Grava Terramon/Assets/Moves/SolarBeam.png/.txt e uma prévia em fundo magenta em art/vfx/sheets/.
"""
import math
import random

import numpy as np
from PIL import Image

WHITE, LIGHT, MID, BASE, DARK, OUTLINE = range(1, 7)
COLORS = {WHITE: "#FFFFFF", LIGHT: "#FFF494", MID: "#A7F432", BASE: "#46C328", DARK: "#1B7A22", OUTLINE: "#0C3F15"}
W, H = 150, 48       # frame em pixels de arte
SCALE = 2
CX, CY = W // 2, H // 2  # centro: onde fica o Pokémon (carga), o alvo (impacto) e a ponta do raio
YY, XX = np.mgrid[0:H, 0:W]


def canvas():
    return np.zeros((H, W), np.uint8)


def put(img, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        img[y, x] = c


def disc(img, cx, cy, r, c, only_empty=False):
    m = (XX - cx) ** 2 + (YY - cy) ** 2 <= r * r + 0.5
    if only_empty:
        m &= img == 0
    img[m] = c


def outline(img):
    f = img > 0
    e = np.zeros_like(f)
    e[1:, :] |= f[:-1, :]
    e[:-1, :] |= f[1:, :]
    e[:, 1:] |= f[:, :-1]
    e[:, :-1] |= f[:, 1:]
    img[e & ~f] = OUTLINE
    return img


def leaf(img, cx, cy, ang, size=4):
    """Folha em losango com nervura clara, girada em ang (radianos)."""
    ca, sa = math.cos(ang), math.sin(ang)
    u = (XX - cx) * ca + (YY - cy) * sa
    v = -(XX - cx) * sa + (YY - cy) * ca
    body = np.abs(u) / size + np.abs(v) / (size * 0.45) <= 1
    img[body] = BASE
    img[body & (v < 0)] = MID
    img[body & (np.abs(v) < 0.6) & (np.abs(u) < size - 1)] = LIGHT


def rays(img, cx, cy, n, r0, r1, rot, c, width=0.12):
    """Raios de sol: cunhas finas saindo do centro."""
    a = np.arctan2(YY - cy, XX - cx)
    r = np.hypot(XX - cx, YY - cy)
    step = 2 * math.pi / n
    d = np.abs(((a - rot) + step / 2) % step - step / 2)
    m = (d < width * (1 - (r - r0) / max(1, r1 - r0)) + 0.02) & (r >= r0) & (r <= r1)
    img[m & (img == 0)] = c


# ---------------------------------------------------------------- carga (no usuário)
def charge(k, n=5):
    """Luz do sol se juntando: partículas vindo de fora, raios girando, folhas orbitando, orbe crescendo."""
    img = canvas()
    t = (k + 1) / n
    rnd = random.Random(7)
    rays(img, CX, CY, 8, 4, 12 + 11 * t, k * 0.35, LIGHT if k % 2 else MID, 0.18)
    for i in range(14):  # motas de luz vindo para o centro
        a = rnd.uniform(0, 2 * math.pi)
        r0 = rnd.uniform(18, 23)
        r = r0 * (1 - t) + 3
        for j in range(3):  # rastro curto
            put(img, CX + math.cos(a) * (r + j * 1.5), CY + math.sin(a) * (r + j * 1.5) * 0.9, [WHITE, LIGHT, MID][j])
    for i in range(3):  # folhas girando em volta
        a = k * 0.9 + i * 2 * math.pi / 3
        leaf(img, CX + math.cos(a) * (19 - 6 * t), CY + math.sin(a) * (14 - 5 * t), a + math.pi / 2, 6)
    r = 4 + 8 * t
    disc(img, CX, CY, r, BASE)
    disc(img, CX, CY, r - 1.5, MID)
    disc(img, CX, CY, r - 3, LIGHT)
    disc(img, CX, CY, max(1, r - 4.5), WHITE)
    if k == n - 1:  # último frame: lampejo antes de disparar
        rays(img, CX, CY, 4, 9, 22, math.pi / 4, WHITE, 0.08)
    return outline(img)


# ---------------------------------------------------------------- raio (esticado do usuário ao alvo)
def beam(k, thick):
    """Raio na metade esquerda, ponta no centro. thick: meia-altura do raio. k desloca a textura (o raio corre)."""
    img = canvas()
    x = XX.astype(float)
    on = x <= CX
    taper = np.clip((CX - x) / 6, 0, 1)  # a ponta arredonda nos últimos pixels
    wave = 0.8 * np.sin(x * 0.35 - k * 1.7)  # a borda ondula de leve
    half = thick * (0.55 + 0.45 * taper) + wave * (thick > 3)
    dy = np.abs(YY - CY)
    body = on & (dy <= half)
    img[body] = BASE
    img[body & (dy <= half * 0.75)] = MID
    img[body & (dy <= half * 0.5)] = LIGHT
    img[body & (dy <= half * 0.25)] = WHITE
    # vinhas de luz em espiral em volta do raio
    for phase in (0, math.pi):
        for xi in range(0, CX + 1):
            yv = CY + (thick + 3) * math.sin(xi * 0.18 - k * 1.3 + phase)
            front = math.cos(xi * 0.18 - k * 1.3 + phase) > 0
            if front or img[int(round(yv)) % H, xi] == 0:
                c = BASE if not front else (LIGHT if xi % 7 < 5 else WHITE)
                put(img, xi, yv, c)
                put(img, xi, yv + 1, c)
    # faíscas correndo dentro do raio
    for xi in range((k * 7) % 11, CX - 4, 11):
        put(img, xi, CY - 1, WHITE)
        put(img, xi + 1, CY - 1, WHITE)
        put(img, xi + 2, CY, LIGHT)
    # ponta: clarão no alvo
    rays(img, CX, CY, 8, 2, thick + 9, k * 0.4, LIGHT, 0.12)
    disc(img, CX, CY, thick + 2, MID)
    disc(img, CX, CY, thick, LIGHT)
    disc(img, CX, CY, max(1, thick - 2), WHITE)
    # boca do raio no usuário
    disc(img, 2, CY, thick + 1, LIGHT, only_empty=False)
    disc(img, 2, CY, max(1, thick - 1), WHITE)
    return outline(img)


# ---------------------------------------------------------------- impacto (no alvo)
def impact(k):
    img = canvas()
    rnd = random.Random(11)
    if k == 0:
        rays(img, CX, CY, 12, 3, 22, 0.1, LIGHT, 0.14)
        disc(img, CX, CY, 10, MID)
        disc(img, CX, CY, 8, LIGHT)
        disc(img, CX, CY, 6, WHITE)
    elif k == 1:
        r = 15
        m = (np.hypot(XX - CX, YY - CY) - r)
        img[(m >= -2.5) & (m <= 0)] = MID
        img[(m >= -1.2) & (m <= 0)] = LIGHT
        rays(img, CX, CY, 8, 6, 20, 0.4, LIGHT, 0.1)
        disc(img, CX, CY, 4, WHITE)
    for i in range(9):  # folhas espalhando
        a = i * 2 * math.pi / 9 + rnd.uniform(-0.2, 0.2)
        d = 8 + k * 5 + rnd.uniform(0, 2)
        if k >= 1:
            leaf(img, CX + math.cos(a) * d * 1.6, CY + math.sin(a) * d * 0.8 + k * k * 0.7, a + k * 0.8, 6 - 0.6 * k)
    if k >= 2:
        for i in range(10):  # brilhos sumindo
            a = rnd.uniform(0, 2 * math.pi)
            d = rnd.uniform(4, 10 + 4 * k)
            c = WHITE if k == 2 else LIGHT if k == 3 else MID
            put(img, CX + math.cos(a) * d * 1.4, CY + math.sin(a) * d * 0.8, c)
    return outline(img)


CHARGE = [charge(k) for k in range(5)]
BEAM = [beam(0, 3), beam(1, 8), beam(2, 9), beam(3, 9), beam(4, 4)]
IMPACT = [impact(k) for k in range(5)]
FRAMES = CHARGE + BEAM + IMPACT


def to_rgba(img):
    out = np.zeros((H, W, 4), np.uint8)
    for idx, hexc in COLORS.items():
        out[img == idx] = [int(hexc[i:i + 2], 16) for i in (1, 3, 5)] + [255]
    return out.repeat(SCALE, 0).repeat(SCALE, 1)


strip = np.concatenate([to_rgba(f) for f in FRAMES], axis=1)
Image.fromarray(strip, "RGBA").save("Terramon/Assets/Moves/SolarBeam.png", optimize=True)
b0 = len(CHARGE)
open("Terramon/Assets/Moves/SolarBeam.txt", "w").write(
    f"Frames {len(FRAMES)}\nCharge {len(CHARGE)}\nTravel 0\nBeam {b0} {b0 + len(BEAM) - 1}\nTicks 4\nFace 1\nScale 1\n")
prev = Image.new("RGBA", (strip.shape[1], strip.shape[0]), (255, 0, 255, 255))
prev.alpha_composite(Image.fromarray(strip, "RGBA"))
prev.convert("RGB").save("art/vfx/sheets/solar_beam_desenhado_por_codigo.png")
print("SolarBeam", len(FRAMES), W * SCALE, H * SCALE)
