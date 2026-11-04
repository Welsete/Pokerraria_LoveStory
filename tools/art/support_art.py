"""Arte da seção "Apoie o desenvolvedor" do README: faixa de título e cartões do Pix e do Ko-fi, em pixel art no
estilo do projeto (céu com aurora, moldura do diário, letras pixeladas). Gera PT e EN.
Uso: python3 support_art.py <qr_do_banco.png> <pasta_de_saida>"""
import math, random, sys, os
import numpy as np
from PIL import Image

S = 3
F = {
 'A':["01110","10001","10001","11111","10001","10001","10001"],'B':["11110","10001","10001","11110","10001","10001","11110"],
 'C':["01111","10000","10000","10000","10000","10000","01111"],'D':["11110","10001","10001","10001","10001","10001","11110"],
 'E':["11111","10000","10000","11110","10000","10000","11111"],'F':["11111","10000","10000","11110","10000","10000","10000"],
 'G':["01111","10000","10000","10111","10001","10001","01111"],'H':["10001","10001","10001","11111","10001","10001","10001"],
 'I':["11111","00100","00100","00100","00100","00100","11111"],'K':["10001","10010","10100","11000","10100","10010","10001"],
 'L':["10000","10000","10000","10000","10000","10000","11111"],'M':["10001","11011","10101","10101","10001","10001","10001"],
 'N':["10001","11001","10101","10011","10001","10001","10001"],'O':["01110","10001","10001","10001","10001","10001","01110"],
 'P':["11110","10001","10001","11110","10000","10000","10000"],'R':["11110","10001","10001","11110","10100","10010","10001"],
 'S':["01111","10000","10000","01110","00001","00001","11110"],'T':["11111","00100","00100","00100","00100","00100","00100"],
 'U':["10001","10001","10001","10001","10001","10001","01110"],'V':["10001","10001","10001","10001","10001","01010","00100"],
 'W':["10001","10001","10001","10101","10101","11011","10001"],'X':["10001","10001","01010","00100","01010","10001","10001"],
 'Y':["10001","10001","01010","00100","00100","00100","00100"],'Z':["11111","00001","00010","00100","01000","10000","11111"],
 '!':["00100","00100","00100","00100","00100","00000","00100"],'-':["00000","00000","00000","11111","00000","00000","00000"],
 'Q':["01110","10001","10001","10001","10101","10010","01101"],'/':["00001","00010","00010","00100","01000","01000","10000"],' ':["00000"]*7,
}
GOLD = [(255, 222, 96), (232, 176, 40), (170, 112, 20)]


class Card:
    def __init__(self, w, h, seed):
        self.W, self.H = w, h
        self.img = np.zeros((h, w, 3), np.uint8)
        self.YY, self.XX = np.mgrid[0:h, 0:w]
        t = self.YY / h
        for i, (a, b) in enumerate(zip((10, 10, 34), (40, 34, 80))):
            self.img[..., i] = np.round((a * (1 - t) + b * t) / 8) * 8
        rnd = random.Random(seed)
        for _ in range(w * h // 300):
            x, y = rnd.randrange(w), rnd.randrange(h)
            self.img[y, x] = (240, 240, 255) if rnd.random() < .6 else (170, 170, 220)

    def aurora(self, y0, amp, ph, green=True, h=16):
        cols = [(200, 255, 220), (90, 230, 160), (60, 150, 130)] if green else [(230, 200, 255), (170, 110, 230), (100, 70, 160)]
        for x in range(self.W):
            yb = y0 + amp * math.sin(x * 0.06 + ph) + 2 * math.sin(x * 0.21 + ph * 2)
            for k in range(h):
                y = int(yb - k)
                if not 0 <= y < self.H or ((x + k) % 3 == 0 and k > h * .5):
                    continue
                c = np.array(cols[0] if k < 2 else cols[1] if k < h * .55 else cols[2])
                self.img[y, x] = c if k < 2 else (self.img[y, x] * 0.25 + c * 0.75).astype(np.uint8)

    def rect(self, x0, y0, x1, y1, c):
        self.img[y0:y1, x0:x1] = c

    def frame(self):
        W, H = self.W, self.H
        for d, c in ((0, (18, 46, 28)), (1, (40, 98, 58)), (2, (40, 98, 58)), (3, (40, 98, 58)), (4, (70, 140, 88)), (5, (18, 46, 28))):
            self.img[d, d:W - d] = c; self.img[H - 1 - d, d:W - d] = c
            self.img[d:H - d, d] = c; self.img[d:H - d, W - 1 - d] = c
        for cx, cy in ((0, 0), (W - 12, 0), (0, H - 12), (W - 12, H - 12)):
            self.rect(cx, cy, cx + 12, cy + 12, GOLD[1]); self.rect(cx, cy, cx + 12, cy + 1, GOLD[0]); self.rect(cx, cy, cx + 1, cy + 12, GOLD[0])
            self.rect(cx, cy + 11, cx + 12, cy + 12, GOLD[2]); self.rect(cx + 11, cy, cx + 12, cy + 12, GOLD[2]); self.rect(cx + 4, cy + 4, cx + 8, cy + 8, GOLD[0])
            self.img[cy + 5:cy + 7, cx + 5:cx + 7] = (255, 250, 210)

    def text(self, s, cy, scale, fill, shade, outline=(20, 14, 30)):
        w = len(s) * 6 * scale - scale; x0 = (self.W - w) // 2
        mask = np.zeros((self.H, self.W), bool)
        for i, ch in enumerate(s):
            for r, row in enumerate(F[ch]):
                for c, v in enumerate(row):
                    if v == '1':
                        y, x = cy + r * scale, x0 + i * 6 * scale + c * scale
                        mask[y:y + scale, x:x + scale] = True
        o = np.zeros_like(mask)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                o |= np.roll(np.roll(mask, dy, 0), dx, 1)
        self.img[o & ~mask] = outline; self.img[mask] = fill
        self.img[mask & (self.YY >= cy + 4 * scale)] = shade

    def stamp(self, rows, x, y, k=2):
        pal = {'O': (60, 30, 20), 'w': (250, 245, 235), 'o': (220, 220, 230), 'h': (232, 90, 140), 'H': (255, 200, 220),
               'c': (150, 95, 60), 'C': (190, 130, 80), 'r': (255, 94, 91), 'R': (255, 170, 160)}
        for r, row in enumerate(rows):
            for c, v in enumerate(row):
                if v in pal:
                    self.img[y + r * k:y + r * k + k, x + c * k:x + c * k + k] = pal[v]

    def image(self):
        return Image.fromarray(self.img).resize((self.W * S, self.H * S), Image.NEAREST)


CUP = ["..o.o....", "...o.o...", "OOOOOOO..", "OwwwwwOOO", "OwwwwwO.O", "OwwwwwOOO", "OwwwwwO..", ".OOOOO...", "OOOOOOOO."]
HEART = [".hh.hh.", "hHhhhhh", "hhhhhhh", ".hhhhh.", "..hhh..", "...h..."]
BIG_CUP = [
    "....o..o..o......",
    ".....o..o..o.....",
    "....o..o..o......",
    ".................",
    "OOOOOOOOOOOOO....",
    "OwwwwwwwwwwwOOOO.",
    "OwwwwrrwrrwwO..O.",
    "OwwwrRrrrrrwO..O.",
    "OwwwrrrrrrrwO..O.",
    "OwwwwrrrrrwwOOOO.",
    "OwwwwwrrrwwwO....",
    "OwwwwwwrwwwwO....",
    ".OwwwwwwwwwO.....",
    "..OOOOOOOOO......",
    "OOOOOOOOOOOOOOO..",
]


def banner(title, sub):
    c = Card(300, 52, 5)
    c.aurora(30, 4, 0.5, True, 18); c.aurora(20, 3, 2.2, False, 12)
    c.frame()
    c.text(title, 12, 2, (255, 215, 90), (232, 160, 40))
    c.text(sub, 34, 1, (255, 240, 250), (240, 180, 210))
    c.stamp(HEART, 14, 34, 1); c.stamp(HEART, 300 - 21, 34, 1)
    return c.image()


def pix_card(qr_path, title, thanks):
    W, H = 190, 250
    c = Card(W, H, 3)
    c.aurora(52, 5, 0.3, True, 22); c.aurora(40, 4, 2.0, False, 14); c.aurora(232, 4, 1.1, True, 14)
    c.frame()
    c.text(title, 16, 2, (255, 215, 90), (232, 160, 40))
    px0, py0, ps = 21, 40, 148
    c.rect(px0 - 3, py0 - 3, px0 + ps + 3, py0 + ps + 3, (120, 92, 60)); c.rect(px0 - 2, py0 - 2, px0 + ps + 2, py0 + ps + 2, (241, 232, 206))
    c.rect(px0, py0, px0 + ps, py0 + ps, (255, 255, 255))
    c.text(thanks, py0 + ps + 14, 2, (255, 240, 250), (240, 160, 200))
    fy = py0 + ps + 30
    c.stamp(CUP, W // 2 - 9, fy); c.stamp(HEART, W // 2 - 34, fy + 3); c.stamp(HEART, W // 2 + 20, fy + 3)
    out = c.image()
    qr = Image.open(qr_path).convert('RGB')
    q = (ps - 8) * S
    out.paste(qr.resize((q, q), Image.LANCZOS), ((px0 + 4) * S, (py0 + 4) * S))
    return out


def kofi_card(title, line1, line2, thanks):
    W, H = 190, 250
    c = Card(W, H, 9)
    c.aurora(52, 5, 1.3, False, 22); c.aurora(40, 4, 0.2, True, 14); c.aurora(232, 4, 2.1, False, 14)
    c.frame()
    c.text(title, 16, 2, (255, 215, 90), (232, 160, 40))
    px0, py0, ps = 21, 40, 148
    c.rect(px0 - 3, py0 - 3, px0 + ps + 3, py0 + ps + 3, (120, 92, 60)); c.rect(px0 - 2, py0 - 2, px0 + ps + 2, py0 + ps + 2, (241, 232, 206))
    c.rect(px0, py0, px0 + ps, py0 + ps, (250, 244, 228))
    # a big coffee cup with a heart on it, on a saucer
    c.stamp(BIG_CUP, px0 + 22, py0 + 18, 6)
    c.text(line1, py0 + 112, 2, (255, 94, 91), (200, 60, 70), outline=(250, 244, 228))
    c.text(line2, py0 + 132, 1, (90, 70, 60), (90, 70, 60), outline=(250, 244, 228))
    c.text(thanks, py0 + ps + 14, 2, (255, 240, 250), (240, 160, 200))
    fy = py0 + ps + 30
    c.stamp(HEART, W // 2 - 7, fy + 3)
    return c.image()


if __name__ == '__main__':
    qr, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    banner("APOIE O DESENVOLVEDOR", "GRATUITO PARA SEMPRE - DOE SE QUISER").save(f"{out}/support_banner_pt.png", optimize=True)
    banner("SUPPORT THE DEVELOPER", "FREE FOREVER - DONATE IF YOU WANT").save(f"{out}/support_banner_en.png", optimize=True)
    pix_card(qr, "APOIE COM PIX", "OBRIGADO!").save(f"{out}/support_pix_pt.png", optimize=True)
    pix_card(qr, "PIX - BRAZIL", "THANK YOU!").save(f"{out}/support_pix_en.png", optimize=True)
    kofi_card("DO EXTERIOR", "KO-FI", "PAYPAL OU CARTAO", "CLIQUE AQUI").save(f"{out}/support_kofi_pt.png", optimize=True)
    kofi_card("KO-FI", "CLICK", "PAYPAL OR CARD", "THANK YOU!").save(f"{out}/support_kofi_en.png", optimize=True)
