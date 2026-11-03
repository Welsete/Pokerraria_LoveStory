"""Turns Isa's generated move sheets (magenta background) into the frame strips MoveVisuals reads."""
import sys
import numpy as np
from PIL import Image

SRC, OUT = (sys.argv[1], sys.argv[2]) if len(sys.argv) > 2 else (None, None)

# file, move, layout (cols, rows) or list of boxes, downscale, settings for the .txt
SHEETS = [
    ("aurora_beam_vfx_spritesheet_v3_1792871463131.jpg", "AuroraBeam", (4, 2), 3, dict(Travel=2, Ticks=5, Face=1)),
    ("blizzard_vfx_spritesheet_v2_1792871597489.jpg", "Blizzard", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("Bullet Punch.png", "BulletPunch",
     [(0, 0, 85, 85), (85, 0, 170, 85), (170, 0, 256, 85), (0, 85, 128, 170), (128, 85, 256, 170),
      (0, 185, 66, 256), (66, 185, 128, 256), (128, 185, 192, 256)], 1, dict(Travel=0, Ticks=3, Face=1)),
    ("ember_vfx_spritesheet_1792854135105.jpg", "Ember", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("flamethrower_vfx_spritesheet_final_1792855053226.jpg", "Flamethrower",
     [(40, 70, 280, 280), (350, 70, 640, 280), (690, 70, 1010, 280), (1030, 70, 1345, 280),
      (40, 300, 630, 560), (690, 340, 1030, 500), (1040, 360, 1185, 490), (1190, 410, 1340, 540),
      (40, 600, 280, 720), (280, 580, 440, 720), (450, 570, 620, 750), (690, 570, 860, 750),
      (910, 570, 1110, 750), (1190, 600, 1345, 720)], 3, dict(Travel=0, Ticks=4, Face=1, At="middle")),
    ("freeze_dry_vfx_spritesheet_1792871714552.jpg", "FreezeDry", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("haze_vfx_spritesheet_1792876842170.jpg", "Haze", (4, 2), 3, dict(Travel=0, Ticks=6)),
    ("ice_fang_vfx_spritesheet_1792856044803.jpg", "IceFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_punch_vfx_spritesheet_1792856062413.jpg", "IcePunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_shard_vfx_spritesheet_1792859337124.jpg", "IceShard", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("icicle_crash_vfx_spritesheet_1792871266443.jpg", "IcicleCrash", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("icicle_spear_vfx_spritesheet_1792871321463.jpg", "IcicleSpear", (4, 2), 3, dict(Travel=1, Ticks=4, Face=1)),
    ("icy_wind_vfx_spritesheet_1792876366324.jpg", "IcyWind", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1)),
    ("powder_snow_vfx_spritesheet_v2_1792876584939.jpg", "PowderSnow", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("sheer_cold_vfx_spritesheet_1792876713818.jpg", "SheerCold", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("blaze_kick_vfx_spritesheet_1792889557537.jpg", "BlazeKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("eruption_vfx_spritesheet_1792890820208.jpg", "Eruption", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("fire_fang_vfx_spritesheet_1792889724669.jpg", "FireFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("fire_punch_vfx_spritesheet_1792889883740.jpg", "FirePunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("fire_spin_vfx_spritesheet_1792891043474.jpg", "FireSpin", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("flame_charge_vfx_spritesheet_1792890050797.jpg", "FlameCharge", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    ("flame_wheel_vfx_spritesheet_1792890223837.jpg", "FlameWheel", (4, 2), 3, dict(Travel=7, Ticks=3)),
    ("flare_blitz_vfx_spritesheet_1792890417382.jpg", "FlareBlitz", (4, 4), 2, dict(Travel=2, Ticks=4, Face=1)),
    ("heat_crash_vfx_spritesheet_1792890615052.jpg", "HeatCrash", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("heat_wave_vfx_spritesheet_1792891266492.jpg", "HeatWave", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1)),
    ("incinerate_vfx_spritesheet_1792891503107.jpg", "Incinerate", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("inferno_vfx_spritesheet_1792907291854.jpg", "Inferno", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("lava_plume_vfx_spritesheet_1792907545465.jpg", "LavaPlume", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("mist_vfx_spritesheet_1792889252934.jpg", "Mist", (4, 2), 3, dict(Travel=0, Ticks=6)),
    ("overheat_vfx_spritesheet_1792907801494.jpg", "Overheat", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("snowscape_vfx_spritesheet_1792889390727.jpg", "Snowscape", (4, 2), 3, dict(Travel=0, Ticks=6, At="middle")),
    ("sunny_day_vfx_spritesheet_1792908068245.jpg", "SunnyDay", (4, 2), 3, dict(Travel=0, Ticks=6, At="middle")),
    # Water, Electric and Grass, first batch (2026-11-03)
    ("water_gun_vfx_spritesheet_1793721582639.jpg", "WaterGun", (4, 2), 3, dict(Travel=3, Ticks=4, Face=1)),
    ("hydro_pump_vfx_spritesheet_v3_1793721764064.jpg", "HydroPump", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("bubble_beam_spritesheet_clean_1793722444414.jpg", "BubbleBeam", (4, 2), 3, dict(Travel=3, Ticks=4, Face=1)),
    ("aqua_jet_spritesheet_clean_1793722489127.jpg", "AquaJet", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    # Darker cells with magenta grid lines and a bar at the bottom: cut inside the lines
    ("thunder_shock_vfx_spritesheet_1793722431954.jpg", "ThunderShock",
     [(2, 2, 338, 378), (350, 2, 683, 378), (695, 2, 1027, 378), (1039, 2, 1374, 378), (2, 390, 338, 724), (350, 390, 683, 724), (695, 390, 1027, 724), (1039, 390, 1374, 724)], 3, dict(Travel=2, Ticks=4, Face=1)),
    # Leaves out frame 6 (a brown scorch shaped like the bolt) and the near-empty last frame
    ("thunderbolt_vfx_spritesheet_1793721667921.jpg", "Thunderbolt",
     [(0, 0, 344, 384), (344, 0, 688, 384), (688, 0, 1032, 384), (1032, 0, 1376, 384), (0, 384, 344, 768), (688, 384, 1032, 768)], 3, dict(Travel=1, Ticks=4, Face=1)),
    ("thunder_punch_vfx_spritesheet_1793722488169.jpg", "ThunderPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("razor_leaf_vfx_spritesheet_1793721701699.jpg", "RazorLeaf", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
]


# Sheets drawn on a green background instead of magenta. Purple and pink types (Ghost, Psychic, Poison, Dark, Fairy)
# must use green: the magenta key would erase their own colors (72% of a Ghost-palette sheet was lost in a test).
GREEN_KEY = set()


def load(path, key="magenta"):
    """Keys the background out. Uses the soft-matte keyer of agent-sprite-forge (.claude/skills/generate2dsprite) when
    it is there: in a test it kept 100% of a Ghost-palette sheet where the old keyer below lost 72%."""
    im = Image.open(path)
    forge = None if path.endswith(".png") else _forge_matte()  # PNGs come with their own alpha: old path
    if forge:
        keyed, _ = forge.key_still(im.convert("RGB"), quality="soft", key=key)
        return np.array(keyed.convert("RGBA")).astype(float)
    return _legacy_load(im, path, key)


def _forge_matte():
    import os
    here = os.path.dirname(os.path.abspath(__file__))
    scripts = os.path.join(here, "..", "..", ".claude", "skills", "generate2dsprite", "scripts")
    if os.environ.get("VFX_LEGACY_KEY") or not os.path.isdir(scripts):
        return None
    sys.path.insert(0, scripts)
    import forge_matte
    return forge_matte


def _legacy_load(im, path, key):
    if key == "green":
        a = np.array(im.convert("RGB")).astype(float)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        bg = g - np.maximum(r, b) > 60
        from scipy import ndimage
        bg = ndimage.binary_dilation(bg, iterations=1)
        return np.dstack([a, np.where(bg, 0, 255.0)])
    if path.endswith(".png"):
        a = np.array(im.convert("RGBA")).astype(float)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        a[..., 3][(r - g > 50) & (b - g > 50)] = 0  # magenta fringe left by the generator
        return a
    a = np.array(im.convert("RGB")).astype(float)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = (r - g > 60) & (b - g > 60)
    # one more pixel around the background goes too: it's the pink halo of the jpeg
    from scipy import ndimage
    bg = ndimage.binary_dilation(bg, iterations=2)
    alpha = np.where(bg, 0, 255.0)
    return np.dstack([a, alpha])


def shrink(cell, k):
    """Averages k x k blocks (only the opaque pixels count for the color), then makes the edge hard again."""
    if k == 1:
        out = cell.copy()
        out[..., 3] = np.where(out[..., 3] > 127, 255, 0)
        return out
    h, w = cell.shape[0] // k * k, cell.shape[1] // k * k
    c = cell[:h, :w].reshape(h // k, k, w // k, k, 4)
    al = c[..., 3:4] / 255
    rgb = (c[..., :3] * al).sum((1, 3)) / np.maximum(al.sum((1, 3)), 1e-6)
    cov = al.mean((1, 3))[..., 0]
    return np.dstack([rgb, np.where(cov > 0.45, 255, 0)])


def frames_of(a, layout):
    if isinstance(layout, tuple):
        cols, rows = layout
        fw, fh = a.shape[1] // cols, a.shape[0] // rows
        return [a[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw] for r in range(rows) for c in range(cols)]
    return [a[y0:y1, x0:x1] for x0, y0, x1, y1 in layout]


def main():
    only = set(sys.argv[3].split(",")) if len(sys.argv) > 3 else None
    for file, move, layout, k, settings in SHEETS:
        if only and move not in only:
            continue
        a = load(f"{SRC}/{file}", "green" if file in GREEN_KEY else "magenta")
        fr = [shrink(f, k) for f in frames_of(a, layout)]
        w = max(f.shape[1] for f in fr)
        h = max(f.shape[0] for f in fr)
        w += w % 2
        h += h % 2
        strip = np.zeros((h, w * len(fr), 4), np.uint8)
        for i, f in enumerate(fr):
            y, x = (h - f.shape[0]) // 2, (w - f.shape[1]) // 2
            strip[y:y + f.shape[0], i * w + x:i * w + x + f.shape[1]] = f.clip(0, 255).astype(np.uint8)
        Image.fromarray(strip, "RGBA").save(f"{OUT}/{move}.png", optimize=True)
        lines = [f"Frames {len(fr)}"] + [f"{key} {val}" for key, val in settings.items()] + ["Scale 1"]
        open(f"{OUT}/{move}.txt", "w").write("\n".join(lines) + "\n")
        print(move, len(fr), (w, h))


if __name__ == "__main__":
    main()
