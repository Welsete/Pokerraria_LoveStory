"""Moldura do menu de batalha: cantos de latão com gema de aurora e um brasão de coração no topo.
Gera Pokerraria/Assets/GUI/TurnBased/StoryFrame.png (canto 16x16 em (0,0), brasão 23x7 em (16,0)).
Rodar: python3 tools/art/story_frame.py [preview.png]"""
import sys
from PIL import Image

PAL = {
    'o': (40, 26, 16), 'h': (245, 210, 120), 'b': (196, 146, 62), 's': (128, 86, 36),
    'g': (110, 235, 170), 't': (90, 190, 215), 'v': (170, 112, 235), 'w': (255, 255, 240),
}

def put(im, x, y, c):
    if c != '.':
        im.putpixel((x, y), PAL[c] + (255,))

def corner():
    g = [['.'] * 16 for _ in range(16)]
    arm = {2: 'o', 3: 'h', 4: 'b', 5: 's', 6: 'o'}
    for y, c in arm.items():
        for x in range(5, 14):
            g[y][x] = c
    g[3][14] = 'o'; g[4][14] = 'b'; g[4][15] = 'o'; g[5][14] = 'o'
    # rebite no meio do braço
    g[4][10] = 'h'
    for y in range(16):          # braço vertical = transposta
        for x in range(16):
            if g[x][y] != '.' and g[y][x] == '.':
                g[y][x] = {'h': 'h', 's': 's'}.get(g[x][y], g[x][y])
    for y in range(7):           # gema em losango no canto
        for x in range(7):
            d = abs(x - 3) + abs(y - 3)
            if d == 3:
                g[y][x] = 'o'
            elif d < 3:
                dy = y - 3
                g[y][x] = 'g' if dy < 0 else ('t' if dy == 0 else 'v')
    g[2][2] = 'w'
    return g

HEART = [
    ".oo.oo.",
    "owgogto",
    "oggttto",
    "ogttvvo",
    ".otvvo.",
    "..ovo..",
    "...o...",
]

def crest():
    g = [['.'] * 23 for _ in range(7)]
    for side in (range(0, 8), range(15, 23)):
        for x in side:
            g[1][x] = 'o'; g[2][x] = 'h'; g[3][x] = 'b'; g[4][x] = 'o'
    for x0 in (0, 22):
        g[2][x0] = 'o'; g[3][x0] = 'o'
    g[2][4] = 'w'; g[2][18] = 'w'
    for y, row in enumerate(HEART):
        for x, c in enumerate(row):
            if c != '.':
                g[y][8 + x] = c
    return g

def build():
    im = Image.new('RGBA', (39, 16), (0, 0, 0, 0))
    for y, row in enumerate(corner()):
        for x, c in enumerate(row):
            put(im, x, y, c)
    for y, row in enumerate(crest()):
        for x, c in enumerate(row):
            put(im, 16 + x, y, c)
    return im

if __name__ == '__main__':
    im = build()
    im.save('Pokerraria/Assets/GUI/TurnBased/StoryFrame.png')
    if len(sys.argv) > 1:
        # prévia: painel azul 200x70 px com a moldura, escala 4
        ns = Image.open('Pokerraria/Assets/GUI/TurnBased/Simple_NineSlice_Stretch.png').convert('RGBA')
        W, H = 200, 70
        p = ns.resize((W, H), Image.NEAREST)
        # nine-slice simples: copia as bordas de 4px
        p = Image.new('RGBA', (W, H)); fill = ns.getpixel((6, 6))
        p.paste(fill, (0, 0, W, H))
        for i in range(4):
            c = ns.getpixel((i, 6))
            for x in range(i, W - i):
                p.putpixel((x, i), c); p.putpixel((x, H - 1 - i), c)
            for y in range(i, H - i):
                p.putpixel((i, y), c); p.putpixel((W - 1 - i, y), c)
        line = PAL['s'] + (255,)
        for x in range(5, W - 5):
            p.putpixel((x, 5), line); p.putpixel((x, H - 6), line)
        for y in range(5, H - 5):
            p.putpixel((5, y), line); p.putpixel((W - 6, y), line)
        c = im.crop((0, 0, 16, 16))
        p.alpha_composite(c, (0, 0))
        p.alpha_composite(c.transpose(Image.FLIP_LEFT_RIGHT), (W - 16, 0))
        p.alpha_composite(c.transpose(Image.FLIP_TOP_BOTTOM), (0, H - 16))
        p.alpha_composite(c.transpose(Image.ROTATE_180), (W - 16, H - 16))
        p.alpha_composite(im.crop((16, 0, 39, 7)), ((W - 23) // 2, 2))
        p.resize((W * 4, H * 4), Image.NEAREST).save(sys.argv[1])
