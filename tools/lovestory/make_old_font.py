"""Gera as fontes de "livro antigo" do diário (IM Fell English, licença OFL, em tools/lovestory/fonts) como folhas de
letras que o jogo desenha (Pokerraria/Assets/GUI/Journal/OldFont*.png + .txt).

Cada letra fica branca com transparência na folha; o jogo pinta na cor da tinta. O .txt tem, por letra:
código x y largura altura deslocamento_x deslocamento_y avanço, e na primeira linha a altura da linha.
Rodar da raiz do repo: python3 tools/lovestory/make_old_font.py
"""
from PIL import Image, ImageDraw, ImageFont

FONTS = "tools/lovestory/fonts/"
OUT = "Pokerraria/Assets/GUI/Journal/"
CHARS = [chr(c) for c in range(32, 127)] + [chr(c) for c in range(160, 256)] + list("“”‘’–…")


def build(ttf, size, name):
    font = ImageFont.truetype(FONTS + ttf, size)
    ascent, descent = font.getmetrics()
    glyphs = []
    for ch in CHARS:
        box = font.getbbox(ch)
        w, h = max(1, box[2] - box[0]), max(1, box[3] - box[1])
        glyphs.append((ch, box, w, h, round(font.getlength(ch))))
    # Pack in rows
    W = 512
    x = y = row = 0
    places = []
    for ch, box, w, h, adv in glyphs:
        if x + w + 2 > W:
            x, y, row = 0, y + row + 2, 0
        places.append((x, y))
        x += w + 2
        row = max(row, h)
    H = y + row + 2
    sheet = Image.new("RGBA", (W, H), (255, 255, 255, 0))
    draw = ImageDraw.Draw(sheet)
    lines = [str(ascent + descent + 2)]
    for (ch, box, w, h, adv), (px, py) in zip(glyphs, places):
        draw.text((px - box[0], py - box[1]), ch, font=font, fill=(255, 255, 255, 255))
        lines.append(f"{ord(ch)} {px} {py} {w} {h} {box[0]} {box[1]} {adv}")
    sheet.save(OUT + name + ".png", optimize=True)
    open(OUT + name + ".txt", "w").write("\n".join(lines) + "\n")
    print(name, sheet.size, len(glyphs))


build("IMFeENrm28P.ttf", 21, "OldFont")
build("IMFeENit28P.ttf", 30, "OldFontTitle")
