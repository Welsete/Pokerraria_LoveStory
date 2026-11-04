"""Tema do Pokerraria nas texturas da interface: o azul-anil herdado do Terramon vira o verde-couro do diário
(mesmo brilho e sombra de cada pixel). Só mexe nos tons azuis; amarelos, cores de tipo e ícones ficam como estão.
A mesma conta está em Content/GUI/Theme.cs para as cores pintadas por código.

Uso (da raiz do repo): python3 tools/art/theme_gui.py [--preview saida.png]
Os originais ficam guardados em art/gui_original/ (rodar de novo parte deles, não acumula).
"""
import colorsys, os, shutil, sys
import numpy as np
from PIL import Image

GUI = "Pokerraria/Assets/GUI/"
ORIG = "art/gui_original/"
FILES = [
    "Hub/PageButtonLeft.png", "Hub/PageButtonRight.png", "Hub/PlayerDexFilter.png", "Hub/PlayerShinyDexFilter.png",
    "Hub/PokedexOverviewHeader.png",
    "Inventory/PartySlotBg.png", "Inventory/PartySlotBgClicked.png", "Inventory/PartySlotBgEmpty.png",
    "PC/ListViewButton.png", "PC/PCSlotBg.png", "PC/PCSlotBgEmpty.png", "PC/SmallPageButtonLeft.png",
    "PC/SmallPageButtonRight.png", "PC/BoxDragBar.png",
    "Party/SidebarClosed.png", "Party/SidebarOpen.png", "Party/HeldItemBox.png", "Party/SpriteBox.png",
    "TurnBased/BallSlots_Simple.png", "TurnBased/PlayerPanel_Simple.png", "TurnBased/Simple_NineSlice_Stretch.png",
]
LEATHER_HUE = 145 / 360  # The journal's green leather


def retheme(rgb):
    """One color: blue-violet tones (hue 200-275) become leather green, a bit less saturated; the rest stays."""
    r, g, b = (c / 255 for c in rgb)
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    if s < 0.12 or not (200 / 360 <= h <= 275 / 360):
        return rgb
    r, g, b = colorsys.hsv_to_rgb(LEATHER_HUE, s * 0.7, v * 0.78)
    return tuple(int(round(c * 255)) for c in (r, g, b))


def retheme_image(im):
    a = np.array(im.convert("RGBA"))
    flat = a.reshape(-1, 4)
    cache = {}
    for i, px in enumerate(flat):
        if px[3] == 0:
            continue
        key = tuple(px[:3])
        if key not in cache:
            cache[key] = retheme(key)
        flat[i, :3] = cache[key]
    return Image.fromarray(a, "RGBA")


if __name__ == "__main__":
    preview = sys.argv[2] if len(sys.argv) > 2 and sys.argv[1] == "--preview" else None
    pairs = []
    for f in FILES:
        src = ORIG + f
        if not os.path.exists(src):
            os.makedirs(os.path.dirname(src), exist_ok=True)
            shutil.copy(GUI + f, src)
        before = Image.open(src)
        after = retheme_image(before)
        pairs.append((before.convert("RGBA"), after))
        if not preview:
            after.save(GUI + f, optimize=True)
    if preview:
        cw = 160
        sheet = Image.new("RGBA", (cw * 2, cw * len(pairs)), (30, 30, 34, 255))
        for i, (b, a) in enumerate(pairs):
            for j, im in enumerate((b, a)):
                k = min(1, (cw - 10) / max(im.size))
                im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.NEAREST)
                sheet.alpha_composite(im, (j * cw + 5, i * cw + 5))
        sheet.save(preview)
    print(len(pairs), "texturas")
