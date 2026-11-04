"""Arte da seção de instalação do README: faixa de título, cartões de download, os 4 passos e o quadro de controles,
no mesmo estilo da seção de apoio (céu com aurora, moldura dourada, letras pixeladas). Gera PT e EN.
Uso: python3 install_art.py <pasta_de_saida>"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np
from support_art import Card, F, GOLD

F.update({
 'J': ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
 '0': ["01110", "10011", "10101", "10101", "10101", "11001", "01110"], '1': ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
 '2': ["01110", "10001", "00001", "00110", "01000", "10000", "11111"], '3': ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
 '4': ["10010", "10010", "10010", "11111", "00010", "00010", "00010"], '5': ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
 '6': ["01110", "10000", "10000", "11110", "10001", "10001", "01110"], '7': ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
 '8': ["01110", "10001", "10001", "01110", "10001", "10001", "01110"], '9': ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
 '.': ["00000", "00000", "00000", "00000", "00000", "00000", "00100"], "'": ["00100", "00100", "00000", "00000", "00000", "00000", "00000"],
 '[': ["01110", "01000", "01000", "01000", "01000", "01000", "01110"], ']': ["01110", "00010", "00010", "00010", "00010", "00010", "01110"],
})

DARK = (20, 14, 30)
PARCH, PARCH_EDGE = (241, 232, 206), (120, 92, 60)
INK = (70, 48, 30)
TITLE = ((255, 215, 90), (232, 160, 40))
WHITE = ((255, 240, 250), (220, 205, 235))


def text_w(s, scale):
    return len(s) * 6 * scale - scale


def text_at(c, s, x0, cy, scale, fill, shade, outline=DARK):
    mask = np.zeros((c.H, c.W), bool)
    for i, ch in enumerate(s):
        for r, row in enumerate(F[ch]):
            for col, v in enumerate(row):
                if v == '1':
                    y, x = cy + r * scale, x0 + i * 6 * scale + col * scale
                    mask[y:y + scale, x:x + scale] = True
    o = np.zeros_like(mask)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            o |= np.roll(np.roll(mask, dy, 0), dx, 1)
    if outline is not None:
        c.img[o & ~mask] = outline
    c.img[mask] = fill
    c.img[mask & (c.YY >= cy + 4 * scale)] = shade


def text_c(c, s, cx, cy, scale, fill, shade, outline=DARK):
    text_at(c, s, cx - text_w(s, scale) // 2, cy, scale, fill, shade, outline)


def box(c, x0, y0, x1, y1):
    c.rect(x0 - 2, y0 - 2, x1 + 2, y1 + 2, PARCH_EDGE)
    c.rect(x0 - 1, y0 - 1, x1 + 1, y1 + 1, (200, 180, 140))
    c.rect(x0, y0, x1, y1, PARCH)


PAL = {'O': DARK, 'r': (226, 60, 60), 'R': (255, 140, 130), 'w': (250, 248, 240), 'g': (190, 190, 205), 'k': (40, 36, 50),
       's': (150, 156, 176), 'S': (210, 214, 228), 'd': (96, 100, 120), 'y': (240, 190, 70), 'Y': (255, 225, 130),
       'b': (170, 120, 40), 'G': (90, 200, 110), 'L': (160, 240, 160), 'B': (80, 150, 230), 'C': (150, 205, 255),
       'n': (110, 80, 50), 'h': (232, 90, 140), 'H': (255, 200, 220)}


def icon(c, rows, x, y, k):
    for r, row in enumerate(rows):
        for col, v in enumerate(row):
            if v in PAL:
                c.img[y + r * k:y + r * k + k, x + col * k:x + col * k + k] = PAL[v]


BALL = ["...OOOOOO...", ".OOrrrrRROO.", ".OrrrrrrRRO.", "OrrrrrrrrrRO", "OrrrrOOrrrrO", "OkkkOwwOkkkO",
        "OwwwOwwOwwwO", "OwwwwOOwwwgO", ".OwwwwwwwgO.", ".OOwwwwggOO.", "...OOOOOO...", "............"]
GEAR = ["....OOOO....", "..O.OSSO.O..", ".OSOSSSSOSO.", "..OSSSSSSO..", "OOSSsOOsSSOO", "OSSSO..OSssO",
        "OSSSO..OsssO", "OOSssOOssdOO", "..OssssssO..", ".OsOssssOdO.", "..O.OddO.O..", "....OOOO...."]
ARROW = ["....OOOO....", "....OYyO....", "....OYyO....", "....OYyO....", "....OYyO....", ".OOOOYyOOOO.",
         ".OYYYYyyyyO.", "..OYYyyyyO..", "...OYyyyO...", "....OYyO....", ".....OO.....", "OOOOOOOOOOOO"]
FOLDER = ["............", ".OOOOO......", "OyyyyyOOOOO.", "OYYYYYYYYYYO", "OyyyyyyyyyyO", "OyyyyyyyyyyO",
          "OyyyyyyyyyyO", "OyyyyyyyyyyO", "ObbbbbbbbbbO", "OOOOOOOOOOOO", "............", "............"]
CHECK = ["OOOOOOOOOOOO", "OwwwwwwwwwGO", "OwwwwwwwwGLO", "OwwwwwwwGLwO", "OwGwwwwGLwwO", "OwLGwwGLwwwO",
         "OwwLGGLwwwwO", "OwwwLLwwwwwO", "OwwwwwwwwwwO", "OwwwwwwwwwwO", "OOOOOOOOOOOO", "............"]
WORLD = ["...OOOOOO...", ".OOCCBBCCOO.", ".OCCGGBBCCO.", "OCCGGGGBBCCO", "OCGGGGGGBBCO", "OBBGGGLGGBBO",
         "OBBBGGGGBBBO", "OCBBBGGBBBCO", "OCCBBBBBBCCO", ".OCCBBBBCCO.", ".OOCCCCCCOO.", "...OOOOOO..."]


def banner(title, sub):
    c = Card(300, 52, 11)
    c.aurora(30, 4, 1.4, True, 18); c.aurora(20, 3, 0.4, False, 12)
    c.frame()
    text_c(c, title, 150, 12, 2, *TITLE)
    text_c(c, sub, 150, 34, 1, *WHITE)
    icon(c, BALL, 16, 20, 1); icon(c, BALL, 300 - 28, 20, 1)
    return c.image()


def download(head, name, foot, ico, seed, green):
    W, H = 170, 88
    c = Card(W, H, seed)
    c.aurora(26, 3, seed * 0.7, green, 14); c.aurora(84, 3, seed, not green, 10)
    c.frame()
    text_c(c, head, W // 2, 10, 2, *TITLE)
    box(c, 12, 30, W - 12, 62)
    icon(c, ico, 18, 34, 2)
    text_at(c, name, 46, 43, 1, INK, INK, None)
    text_c(c, foot, W // 2, 70, 1, *WHITE)
    return c.image()


def steps(labels):
    W, H = 400, 104
    c = Card(W, H, 21)
    c.aurora(22, 3, 0.9, True, 14); c.aurora(100, 3, 2.4, False, 10)
    c.frame()
    for i, ((l1, l2), ico) in enumerate(zip(labels, (ARROW, FOLDER, CHECK, WORLD))):
        x0 = 12 + i * 96
        box(c, x0, 14, x0 + 88, 62)
        icon(c, ico, x0 + 26, 16, 3)
        # number badge
        c.rect(x0 - 2, 12, x0 + 12, 26, DARK); c.rect(x0 - 1, 13, x0 + 11, 25, GOLD[1])
        c.rect(x0 - 1, 13, x0 + 11, 14, GOLD[0]); c.rect(x0 - 1, 24, x0 + 11, 25, GOLD[2])
        text_at(c, str(i + 1), x0 + 3, 16, 1, DARK, DARK, None)
        text_c(c, l1, x0 + 44, 72, 1, *WHITE)
        text_c(c, l2, x0 + 44, 84, 1, *TITLE)
    return c.image()


def keycap(c, label, x, y):
    w = max(16, text_w(label, 1) + 8)
    c.rect(x, y, x + w, y + 17, DARK)
    c.rect(x + 1, y + 1, x + w - 1, y + 16, (150, 145, 170))
    c.rect(x + 1, y + 1, x + w - 1, y + 13, (226, 222, 238))
    c.rect(x + 2, y + 2, x + w - 2, y + 3, (250, 248, 255))
    text_c(c, label, x + w // 2, y + 4, 1, (40, 30, 60), (40, 30, 60), None)
    return x + w


def controls(title, rows):
    W, H = 330, 146
    c = Card(W, H, 33)
    c.aurora(26, 3, 0.2, False, 14); c.aurora(142, 3, 1.7, True, 10)
    c.frame()
    text_c(c, title, W // 2, 11, 2, *TITLE)
    for i, (keys, label) in enumerate(rows):
        x = 18 + (i // 4) * 158
        y = 36 + (i % 4) * 25
        for k in (keys if isinstance(keys, tuple) else (keys,)):
            x = keycap(c, k, x, y) + 3
        text_at(c, label, x + 4, y + 5, 1, *WHITE)
    return c.image()


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    banner("COMO INSTALAR", "4 PASSOS E A JORNADA COMECA").save(f"{out}/install_banner_pt.png", optimize=True)
    banner("HOW TO INSTALL", "4 STEPS AND YOUR JOURNEY BEGINS").save(f"{out}/install_banner_en.png", optimize=True)
    for lang, head, foot in (("pt", "BAIXAR", "VERSAO ESTAVEL"), ("en", "DOWNLOAD", "LATEST STABLE")):
        download(head, "POKERRARIA.TMOD", foot, BALL, 3, True).save(f"{out}/install_dl_pokerraria_{lang}.png", optimize=True)
        download(head, "TMONRUNTIMES.TMOD", foot, GEAR, 4, False).save(f"{out}/install_dl_runtimes_{lang}.png", optimize=True)
    steps([("BAIXE OS", "DOIS ARQUIVOS"), ("COLOQUE NA", "PASTA MODS"), ("ATIVE OS DOIS", "E RECARREGUE"),
           ("CRIE UM", "MUNDO NOVO")]).save(f"{out}/install_steps_pt.png", optimize=True)
    steps([("DOWNLOAD", "BOTH FILES"), ("PUT THEM IN", "MODS FOLDER"), ("ENABLE BOTH", "AND RELOAD"),
           ("START A", "NEW WORLD")]).save(f"{out}/install_steps_en.png", optimize=True)
    controls("CONTROLES", [("I", "RESUMO DO POKEMON"), ("P", "POKEDEX"), ("F", "MOSTRAR A EQUIPE"),
                           ("'", "CHAMAR O PARCEIRO"), (("[", "]"), "TROCAR POKEMON"), ("L", "HISTORICO"),
                           ("B", "CANCELAR EVOLUCAO"), ("F8", "PAINEL DE TESTES")]).save(f"{out}/install_controls_pt.png", optimize=True)
    controls("CONTROLS", [("I", "POKEMON SUMMARY"), ("P", "POKEDEX"), ("F", "SHOW THE PARTY"),
                          ("'", "CALL YOUR PARTNER"), (("[", "]"), "SWITCH POKEMON"), ("L", "BATTLE HISTORY"),
                          ("B", "STOP EVOLUTION"), ("F8", "TEST PANEL")]).save(f"{out}/install_controls_en.png", optimize=True)
