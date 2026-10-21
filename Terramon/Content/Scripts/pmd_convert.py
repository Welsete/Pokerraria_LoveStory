#!/usr/bin/env python3
"""
Turns PMD SpriteCollab sprites (https://github.com/PMDCollab/SpriteCollab) into Terramon's Mystery Dungeon-style
animation folders (Assets/Pokemon/Anim/<Name>/), plus the main walking sheet, the 80x60 mini icons and the 40x40
portraits. Not part of the build (Content/Scripts is in buildIgnore).

Usage, for one Pokémon:
    python3 pmd_convert.py one <sprite folder> <out anim folder> [--shiny <shiny sprite folder>]
        [--old <old Name.png> <old Name_S.png>]

For everything (what was used for the Gen 1 Pokémon):
    python3 pmd_convert.py all <SpriteCollab download> <Terramon folder> <names file: "Name 0001" per line>
        [--starters Name,Name,...]

The download has the same layout as the SpriteCollab repository (sprite/0001/AnimData.xml, sprite/0001/Idle-Anim.png,
sprite/0001/0000/0001/... for shiny, portrait/0001/Normal.png, tracker.json, credit_names.txt).

What it does:
- Takes the left-facing row (row 6 of 8-row sheets) of every animation the mod uses, following CopyOf (a few
  Pokémon that are just a line from the side use the down-left row, see ROW_OVERRIDE).
- Facing fixes (everything has to face left like Walk):
  - Single-direction Sit sheets are always drawn from behind (the Pokémon sitting with its back to the camera), so
    they are left out; the game then stays on Idle and lies down later (see BACK_VIEW).
  - Some Laying sheets are one drawing copied to 7 rows, with only the left row (6) drawn the other way round: lying
    with its head to the right. When the copied drawing looks more like the Pokémon lying on its left side (its last
    Faint/Trip frames and Sleep), that one is used instead (see lying_row).
- Ground comes from the shadow PMD draws under the Pokémon (Idle-Shadow.png). Floating Pokémon (Zubat, Gastly,
  Horsea...) only reach down to their shadow with a wing tip or a trail, so they stand on the shadow and their body
  floats above the ground; "Float" is how far their lowest pixel stays above it. Standing Pokémon stand on their feet.
- The first Idle frame is held for at least 300 ticks: PMD idles bounce every second, which looks restless here.
- Hop frames are lined up on their feet, since the entity itself already moves up and down while jumping.
- Shiny: from the official shiny sprites when there are any, or else from the mod's old normal/shiny sprites (each PMD
  color takes the hue/saturation change of the closest old color). When it is a plain palette swap (every color always
  becomes the same shiny color), Anim.txt only gets a "Shiny" line with the color pairs (normal>shiny) and the game
  recolors the strips itself, which keeps the mod smaller. Otherwise <State>_S.png strips are written.
"""
import argparse
import colorsys
import json
import math
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict

import numpy as np
from PIL import Image

# Terramon state -> SpriteCollab animations to try, in order
STATES = [
    ('Idle', ['Idle']), ('Walk', ['Walk']), ('Attack', ['Attack']), ('SpAttack', ['SpAttack', 'Shoot']),
    ('Status', ['Charge']), ('Hurt', ['Hurt']), ('Sleep', ['Sleep']), ('Faint', ['Faint']),
    ('Strike', ['Strike']), ('QuickStrike', ['QuickStrike']), ('Swing', ['Swing']), ('Double', ['Double']),
    ('Hop', ['Hop']), ('Sit', ['Sit']), ('Laying', ['Laying']), ('Wake', ['Wake']), ('Trip', ['Trip']),
    ('Eat', ['Eat']), ('Pose', ['Pose']),
]
FEET_ALIGNED = {'Hop'}
SCALE = 2
IDLE_HOLD = 300
# Lowest pixel usually this many pixels (or more) above the middle of the shadow: floating
FLOAT_MIN = 1
# Official shiny colors this consistent with the normal ones (1 = every color always becomes the same shiny color)
# are stored as a palette swap instead of separate strips
PALETTE_OK = 0.995
LEFT_ROW = 6
DOWN_LEFT_ROW = 7
# Seen exactly from the side, these are a thin unreadable line: they use the down-left row instead
ROW_OVERRIDE = {'Magnemite': DOWN_LEFT_ROW, 'Staryu': DOWN_LEFT_ROW, 'Starmie': DOWN_LEFT_ROW}
# Single-direction (1-row) sheets of these states show the Pokémon from behind: they are left out
BACK_VIEW = {'Sit'}
# States whose left row can be the odd one out of a copied drawing (see lying_row)
LYING = {'Laying'}
# How much better the copied drawing has to match the lying references before it replaces row 6
LYING_MARGIN = 0.025


class PmdFolder:
    """One SpriteCollab sprite folder (AnimData.xml + <Anim>-Anim.png)."""

    def __init__(self, path):
        self.path = path
        root = ET.parse(os.path.join(path, 'AnimData.xml')).getroot()
        self.anims = {a.find('Name').text: a for a in root.iter('Anim')}

    def resolve(self, name):
        copy = self.anims[name].find('CopyOf')
        return self.resolve(copy.text) if copy is not None else name

    def pick(self, candidates):
        for c in candidates:
            if c in self.anims and os.path.exists(os.path.join(self.path, self.resolve(c) + '-Anim.png')):
                return self.resolve(c)
        return None

    def info(self, name):
        a = self.anims[name]
        return (int(a.find('FrameWidth').text), int(a.find('FrameHeight').text),
                [int(d.text) for d in a.iter('Duration')])

    def rows(self, name):
        """How many directions a sheet has (8, or 1 for single-direction animations)."""
        fh = self.info(name)[1]
        return Image.open(os.path.join(self.path, name + '-Anim.png')).height // fh

    def strip(self, name, row=LEFT_ROW, kind='Anim'):
        """The frames of one direction, side by side."""
        fw, fh, durations = self.info(name)
        im = Image.open(os.path.join(self.path, '%s-%s.png' % (name, kind))).convert('RGBA')
        rows = im.height // fh
        r = row if rows == 8 else 0
        return im.crop((0, r * fh, len(durations) * fw, (r + 1) * fh)), fw, fh, durations


def frames(strip, fw):
    return [strip.crop((i * fw, 0, (i + 1) * fw, strip.height)) for i in range(strip.width // fw)]


def union_bbox(imgs):
    boxes = [b for b in (i.getbbox() for i in imgs) if b]
    if not boxes:
        return None
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def shadow_y(folder, name, row=LEFT_ROW):
    """Middle of the shadow under the first frame, from the top of the frame."""
    if not os.path.exists(os.path.join(folder.path, name + '-Shadow.png')):
        return None
    s, fw, fh, _ = folder.strip(name, row, kind='Shadow')
    a = np.array(s.crop((0, 0, fw, fh)))[:, :, 3]
    ys = np.nonzero(a)[0]
    return None if len(ys) == 0 else float(ys.min() + ys.max()) / 2


def _shape(im, size=24):
    """A small color+alpha thumbnail of a frame's visible part, for comparing poses."""
    a = np.asarray(im.crop(im.getbbox()).resize((size, size), Image.BILINEAR)).astype(float) / 255
    m = a[:, :, 3:4]
    return np.concatenate([a[:, :, :3] * m, m], axis=2)


def lying_row(folder, name, row=LEFT_ROW):
    """
    The row to take for a lying animation. Some sheets are one drawing copied to every row except the left one, which
    is the same drawing the other way round. If the copied drawing is clearly more like the Pokémon's own lying frames
    facing left (last frames of Faint and Trip, Sleep), it is used instead of the left row.
    """
    fw, fh, d = folder.info(name)
    im = np.asarray(Image.open(os.path.join(folder.path, name + '-Anim.png')).convert('RGBA'))
    if im.shape[0] // fh != 8:
        return row
    rows = [im[r * fh:(r + 1) * fh, :len(d) * fw] for r in range(8)]
    others = [r for r in range(8) if r != row]
    if not all(np.array_equal(rows[others[0]], rows[r]) for r in others) or np.array_equal(rows[row], rows[others[0]]):
        return row
    refs = []
    for ref in ('Faint', 'Trip', 'Sleep'):
        if ref not in folder.anims:
            continue
        ref = folder.resolve(ref)
        strip, rfw, _, rd = folder.strip(ref, row)
        for fr in frames(strip, rfw)[-2:] if ref != 'Sleep' else frames(strip, rfw):
            if fr.getbbox():
                refs.append(_shape(fr))
    first = lambda r: Image.fromarray(rows[r][:, :fw])
    if not refs or not first(row).getbbox():
        return row
    score = lambda r: max(-np.abs(_shape(first(r)) - x).mean() for x in refs)
    return others[0] if score(others[0]) > score(row) + LYING_MARGIN else row


# ---------------------------------------------------------------------------------------------------------------- color
def rgb_hex(c):
    return '%02X%02X%02X' % tuple(int(x) for x in c[:3])


def apply_map(img, cmap):
    """Recolors an RGBA image with {(r,g,b): (r,g,b)}."""
    if not cmap:
        return img.copy()
    a = np.array(img)
    out = a.copy()
    flat = a[:, :, :3].reshape(-1, 3)
    key = (flat[:, 0].astype(np.int32) << 16) | (flat[:, 1].astype(np.int32) << 8) | flat[:, 2]
    o = out[:, :, :3].reshape(-1, 3)
    vis = a[:, :, 3].reshape(-1) > 0
    for src, dst in cmap.items():
        k = (src[0] << 16) | (src[1] << 8) | src[2]
        o[(key == k) & vis] = dst
    out[:, :, :3] = o.reshape(a.shape[0], a.shape[1], 3)
    return Image.fromarray(out)


def colors_of(imgs):
    c = Counter()
    for img in imgs:
        a = np.array(img).reshape(-1, 4)
        a = a[a[:, 3] > 0]
        c.update(map(tuple, a[:, :3].tolist()))
    return c


def map_from_pairs(normal_imgs, shiny_imgs):
    """Color pairs from the same art in normal and shiny colors. Returns (map, how consistent, changed anything)."""
    pairs = defaultdict(Counter)
    for n, s in zip(normal_imgs, shiny_imgs):
        if n.size != s.size:
            continue
        a, b = np.array(n), np.array(s)
        m = (a[:, :, 3] > 0) & (b[:, :, 3] > 0)
        for x, y in zip(map(tuple, a[m][:, :3].tolist()), map(tuple, b[m][:, :3].tolist())):
            pairs[x][y] += 1
    cmap, good, total = {}, 0, 0
    for src, dsts in pairs.items():
        dst, n = dsts.most_common(1)[0]
        good += n
        total += sum(dsts.values())
        cmap[src] = dst
    changed = any(k != v for k, v in cmap.items())
    return cmap, (good / total if total else 0), changed


def map_from_old(old_normal, old_shiny, pmd_colors):
    """
    The mod's old normal and shiny sprites are the same art with only the colors changed. Each PMD color takes the hue
    and saturation change of the closest old normal color, and keeps its own brightness.
    """
    a = np.array(Image.open(old_normal).convert('RGBA'))
    b = np.array(Image.open(old_shiny).convert('RGBA'))
    if a.shape != b.shape:
        return {}
    m = (a[:, :, 3] > 0) & (b[:, :, 3] > 0)
    pairs = defaultdict(Counter)
    for x, y in zip(map(tuple, a[m][:, :3].tolist()), map(tuple, b[m][:, :3].tolist())):
        pairs[x][y] += 1
    old = [(src, dsts.most_common(1)[0][0]) for src, dsts in pairs.items()]
    if not old:
        return {}
    hsv = lambda c: colorsys.rgb_to_hsv(*(v / 255 for v in c))
    olds = [(np.array(s, float), hsv(s), hsv(d)) for s, d in old]
    cmap = {}
    for c in pmd_colors:
        h, s, v = hsv(c)
        if s < 0.12 or v < 0.12:  # outlines, black, white and grays stay as they are
            continue
        best = min(olds, key=lambda o: np.sum((o[0] - np.array(c, float)) ** 2))
        (h0, s0, v0), (h1, s1, v1) = best[1], best[2]
        if s0 < 0.12:
            continue
        nh = (h + (h1 - h0)) % 1
        ns = min(1, s * (s1 / s0)) if s0 > 0 else s
        r = tuple(int(round(x * 255)) for x in colorsys.hsv_to_rgb(nh, ns, v))
        if r != c:
            cmap[c] = r
    return cmap


# --------------------------------------------------------------------------------------------------------------- output
def convert(src, out, shiny_src=None, old=None, states=None, row=LEFT_ROW):
    """Writes Anim.txt and the state strips. Returns what it found (ground, size, missing states, shiny source...)."""
    f = PmdFolder(src)
    os.makedirs(out, exist_ok=True)
    for old_file in os.listdir(out):
        if old_file.endswith('.png') or old_file == 'Anim.txt':
            os.remove(os.path.join(out, old_file))

    idle_name = f.pick(['Idle'])
    idle, ifw, ifh, _ = f.strip(idle_name, row)
    box = union_bbox(frames(idle, ifw))
    body_bottom = box[3]
    sy = shadow_y(f, idle_name, row)
    # PMD stands every Pokémon on the middle of its shadow. Standing ones reach a little below it with their feet, and
    # here (no shadow) the feet are the ground. Floating ones (Zubat, Horsea...) stay above it when resting (first Idle
    # frame; later ones are often a little hop) and while moving: they keep that gap as Float, so they hover above the
    # ground instead of standing on it.
    shadow = int(math.floor(sy + 0.5)) if sy is not None else body_bottom
    gap = shadow - idle.crop((0, 0, ifw, ifh)).getbbox()[3]
    walk_name = f.pick(['Walk'])
    wsy = shadow_y(f, walk_name, row) if walk_name else None
    if walk_name and wsy is not None:
        wstrip, wfw, wfh, _ = f.strip(walk_name, row)
        wb = sorted(b[3] for b in (fr.getbbox() for fr in frames(wstrip, wfw)) if b)
        gap = min(gap, int(math.floor(wsy + 0.5)) - wb[len(wb) // 2])
    float_px = gap if gap >= FLOAT_MIN else 0
    ground = (shadow if float_px else body_bottom) - ifh // 2

    def feet_align(strip, fw, fh, like=None):
        """Lines each frame up on its feet; `like` reuses the offsets of the normal-colored strip."""
        fixed = Image.new('RGBA', strip.size)
        for i, fr in enumerate(frames(like or strip, fw)):
            bb = fr.getbbox()
            if bb:
                src = strip.crop((i * fw, 0, (i + 1) * fw, fh))
                fixed.alpha_composite(src, (i * fw, fh // 2 + ground - float_px - bb[3]))
        return fixed

    lines, strips, raw, missing = [], {}, {}, []
    for st, cands in STATES:
        if states and st not in states:
            continue
        name = f.pick(cands)
        if name is None or (st in BACK_VIEW and f.rows(name) == 1):
            missing.append(st)
            continue
        st_row = lying_row(f, name, row) if st in LYING else row
        strip, fw, fh, d = f.strip(name, st_row)
        raw[st] = (name, strip, st_row)
        if st in FEET_ALIGNED:
            strip = feet_align(strip, fw, fh)
        if st == 'Idle':
            d = [max(d[0], IDLE_HOLD)] + d[1:]
        strips[st] = (strip, fw, fh)
        strip.save(os.path.join(out, st + '.png'), optimize=True)
        lines.append('%s %d %d %s' % (st, fw, fh, ','.join(map(str, d))))

    # Shiny: the official shiny strips that match the normal ones pixel for pixel
    cmap, shiny_from, consistency, shiny_strips = {}, None, None, {}
    if shiny_src and os.path.exists(os.path.join(shiny_src, 'AnimData.xml')):
        sf = PmdFolder(shiny_src)
        for st, (name, strip, st_row) in raw.items():
            if name not in sf.anims or not os.path.exists(os.path.join(sf.path, sf.resolve(name) + '-Anim.png')):
                continue
            sstrip = sf.strip(sf.resolve(name), st_row)[0]
            if sstrip.size != strip.size:
                continue
            na, sa = np.array(strip)[:, :, 3] > 0, np.array(sstrip)[:, :, 3] > 0
            if (na == sa).mean() < 0.999:
                continue
            _, fw, fh = strips[st]
            shiny_strips[st] = feet_align(sstrip, fw, fh, like=strip) if st in FEET_ALIGNED else sstrip
        cmap, consistency, changed = map_from_pairs([strips[st][0] for st in shiny_strips], list(shiny_strips.values()))
        if changed:
            shiny_from = 'official'
        else:
            cmap, shiny_strips = {}, {}
    if not shiny_from and old and all(os.path.exists(p) for p in old):
        cmap = map_from_old(old[0], old[1], colors_of(s for s, _, _ in strips.values()))
        shiny_from = 'old-sprite' if cmap else None
        consistency = 1.0

    header = ['Ground %d' % ground, 'Scale %d' % SCALE]
    if float_px:
        header.append('Float %d' % float_px)
    if shiny_from and consistency >= PALETTE_OK:
        # A plain palette swap: the game recolors the normal strips itself, nothing else to store
        pairs = ' '.join('%s>%s' % (rgb_hex(k), rgb_hex(v)) for k, v in sorted(cmap.items()) if k != v)
        header.append('Shiny ' + pairs)
        shiny_strips = {st: apply_map(strip, cmap) for st, (strip, _, _) in strips.items()}
        shiny_mode = 'palette'
    elif shiny_from:
        # Some colors become different shiny colors in different places: keep real shiny strips
        for st, (strip, _, _) in strips.items():
            if st not in shiny_strips:
                shiny_strips[st] = apply_map(strip, cmap)
            shiny_strips[st].save(os.path.join(out, st + '_S.png'), optimize=True)
        shiny_mode = 'strips'
    else:
        shiny_mode = None
    with open(os.path.join(out, 'Anim.txt'), 'w', newline='\n') as fp:
        fp.write('\n'.join(header + lines) + '\n')

    walk = strips.get('Walk')
    return {
        'ground': ground, 'float': float_px, 'idle_bbox': box, 'idle_frame': (ifw, ifh), 'missing': missing,
        'shiny': shiny_from, 'shiny_consistency': consistency, 'shiny_mode': shiny_mode, 'cmap': cmap,
        'strips': strips, 'shiny_strips': shiny_strips,
        'walk_durations': f.info(f.pick(['Walk']))[2] if walk else None, 'folder': f,
    }


def walk_sheet(info, shiny=False):
    """Main sprite sheet: the walking frames at 2x, one under the other (what the NPC/pet frame size is based on)."""
    strip, fw, fh = info['strips']['Walk']
    if shiny:
        strip = info['shiny_strips'].get('Walk', strip)
    fr = frames(strip, fw)
    sheet = Image.new('RGBA', (fw * SCALE, fh * SCALE * len(fr)))
    for i, x in enumerate(fr):
        sheet.alpha_composite(x.resize((fw * SCALE, fh * SCALE), Image.NEAREST), (0, i * fh * SCALE))
    return sheet


def mini_icon(folder):
    """80x60 icon from the first Idle frame (facing down-left), at 2x when it fits, centered."""
    name = folder.pick(['Idle'])
    strip, fw, fh, _ = folder.strip(name, row=DOWN_LEFT_ROW)
    fr = strip.crop((0, 0, fw, fh))
    fr = fr.crop(fr.getbbox())
    scale = 2 if fr.width * 2 <= 76 and fr.height * 2 <= 58 else 1
    if fr.width * scale > 80 or fr.height * scale > 60:  # very big ones: crop to fit
        fr = fr.crop((max(0, (fr.width - 80) // 2), max(0, fr.height - 60), max(0, (fr.width - 80) // 2) + min(80, fr.width),
                      fr.height))
    fr = fr.resize((fr.width * scale, fr.height * scale), Image.NEAREST)
    icon = Image.new('RGBA', (80, 60))
    icon.alpha_composite(fr, ((80 - fr.width) // 2, (60 - fr.height) // 2))
    return icon


def highlighted(icon, color=(255, 230, 71, 255)):
    """Legacy starter select hover icon: a 2 px yellow outline around the mini icon."""
    a = np.array(icon)
    m = a[:, :, 3] > 0
    grow = m.copy()
    for _ in range(2):
        g = grow.copy()
        g[1:, :] |= grow[:-1, :]
        g[:-1, :] |= grow[1:, :]
        g[:, 1:] |= grow[:, :-1]
        g[:, :-1] |= grow[:, 1:]
        grow = g
    out = a.copy()
    out[grow & ~m] = color
    return Image.fromarray(out)


# Rounded like the party sidebar's sprite box (Assets/GUI/Party/SpriteBox.png)
CORNER = [(0, 0), (1, 0), (2, 0), (3, 0), (0, 1), (1, 1), (0, 2), (0, 3)]


def portrait(path):
    im = Image.open(path).convert('RGBA')
    if im.size != (40, 40):
        im = im.crop((0, 0, 40, 40))
    a = np.array(im)
    for x, y in CORNER:
        for cx, cy in ((x, y), (39 - x, y), (x, 39 - y), (39 - x, 39 - y)):
            a[cy, cx, 3] = 0
    return Image.fromarray(a)


# ---------------------------------------------------------------------------------------------------------------- hjson
def set_block(text, key, fields, remove=()):
    """Sets fields of a flat hjson block like `Key: { A: 1 }` (keeps the block's indentation)."""
    m = re.search(r'(?m)^([ \t]*)' + key + r':\s*\{([^{}]*)\}', text)
    if not m:
        return text, False
    indent = m.group(1)
    body = m.group(2)
    entries = []
    for line in body.split('\n'):
        line = line.strip()
        if not line:
            continue
        k = line.split(':', 1)[0].strip()
        if k in fields or k in remove:
            continue
        entries.append(line)
    for k, v in fields.items():
        entries.append('%s: %s' % (k, v))
    inner = indent + '  '
    block = '%s%s:\n%s{\n%s\n%s}' % (indent, key, indent, '\n'.join(inner + e for e in entries), indent)
    return text[:m.start()] + block + text[m.end():], True


def update_hjson(path, frame_count, frame_time, width, height):
    with open(path, encoding='utf-8-sig') as fp:
        text = fp.read()
    text, _ = set_block(text, 'Transform', {'Width': width, 'Height': height}, remove=('DrawOffsetY',))
    for key in ('WalkingBehaviour', 'GenericPet'):
        text, _ = set_block(text, key, {'FrameCount': frame_count, 'FrameTime': frame_time, 'IsClassic': 'false'},
                            remove=('AnimationType', 'ExtraStopFrame'))
    for key in ('WanderingHoverBehaviour', 'BounceBehaviour', 'FlyingPet'):
        text, _ = set_block(text, key, {'FrameCount': frame_count, 'FrameTime': frame_time})
    text = re.sub(r'^\s*// Drawn with the Mystery Dungeon.*\n', '', text, flags=re.M)
    text = text.replace('{\n', '{\n  // Drawn with the Mystery Dungeon-style animations in Assets/Pokemon/Anim (see PokemonAnimations)\n', 1)
    with open(path, 'w', encoding='utf-8', newline='\n') as fp:
        fp.write(text)


def hitbox(info, hovering):
    """About 0.8 of the drawn Idle size; walking Pokémon that float also cover the gap under them."""
    x0, y0, x1, y1 = info['idle_bbox']
    w = max(16, min(56, int(round((x1 - x0) * SCALE * 0.8))))
    body = (y1 - y0) * SCALE * 0.8
    if not hovering:
        body += info['float'] * SCALE
    h = max(16, min(64, int(round(body))))
    return w, h


# ----------------------------------------------------------------------------------------------------------------- main
def run_one(args):
    info = convert(args.src, args.out, args.shiny, args.old)
    print(open(os.path.join(args.out, 'Anim.txt')).read())
    print('missing:', info['missing'], 'shiny:', info['shiny'], 'float:', info['float'])


def run_all(args):
    dl, repo = args.download, args.terramon
    starters = set(filter(None, (args.starters or '').split(',')))
    skip = set(filter(None, (args.skip or '').split(',')))
    only = set(filter(None, (args.only or '').split(',')))
    pk = os.path.join(repo, 'Assets', 'Pokemon')
    report = {}
    for line in open(args.names):
        name, dex = line.split()
        if only and name not in only:
            continue
        src = os.path.join(dl, 'sprite', dex)
        shiny_src = os.path.join(src, '0000', '0001')
        r = {'dex': dex}
        # Portraits for everyone
        os.makedirs(os.path.join(pk, 'Portraits'), exist_ok=True)
        norm_p = os.path.join(dl, 'portrait', dex, 'Normal.png')
        shiny_p = os.path.join(dl, 'portrait', dex, '0000', '0001', 'Normal.png')
        if os.path.exists(norm_p):
            portrait(norm_p).save(os.path.join(pk, 'Portraits', name + '.png'), optimize=True)
        folder = PmdFolder(src)
        if name in starters or name in skip:
            # Only the icons, from the PMD Idle (normal and official shiny)
            icon = mini_icon(folder)
            sf = PmdFolder(shiny_src)
            icon_s = mini_icon(sf)
            cmap, _, _ = map_from_pairs([icon], [icon_s])
            if name in starters:
                icon.save(os.path.join(pk, name + '_Mini.png'), optimize=True)
                icon_s.save(os.path.join(pk, name + '_Mini_S.png'), optimize=True)
            if os.path.exists(shiny_p):
                portrait(shiny_p).save(os.path.join(pk, 'Portraits', name + '_S.png'), optimize=True)
            elif os.path.exists(norm_p):
                apply_map(portrait(norm_p), cmap).save(os.path.join(pk, 'Portraits', name + '_S.png'), optimize=True)
            r['only_icons'] = True
            report[name] = r
            continue

        old = (os.path.join(pk, name + '.png'), os.path.join(pk, name + '_S.png'))
        old_copy = None
        if all(os.path.exists(p) for p in old):
            # Keep the old sheets around for the shiny fallback and the before/after previews
            keep = os.path.join(args.old_dir, name)
            os.makedirs(keep, exist_ok=True)
            for p in old:
                dst = os.path.join(keep, os.path.basename(p))
                if not os.path.exists(dst):
                    Image.open(p).save(dst)
            old_copy = tuple(os.path.join(keep, os.path.basename(p)) for p in old)
        info = convert(src, os.path.join(pk, 'Anim', name), shiny_src, old_copy, args.states,
                       ROW_OVERRIDE.get(name, LEFT_ROW))
        cmap = info['cmap']
        sheet = walk_sheet(info)
        sheet.save(os.path.join(pk, name + '.png'), optimize=True)
        walk_sheet(info, shiny=True).save(os.path.join(pk, name + '_S.png'), optimize=True)
        icon = mini_icon(info['folder'])
        icon.save(os.path.join(pk, name + '_Mini.png'), optimize=True)
        icon_s = mini_icon(PmdFolder(shiny_src)) if info['shiny'] == 'official' else None
        if icon_s is None or icon_s.size != icon.size or (np.array(icon_s)[:, :, 3] > 0).sum() != (
                np.array(icon)[:, :, 3] > 0).sum():
            icon_s = apply_map(icon, cmap)
        icon_s.save(os.path.join(pk, name + '_Mini_S.png'), optimize=True)
        r['shiny_mode'] = info['shiny_mode']
        if os.path.exists(os.path.join(pk, name + '_Mini_Highlighted.png')):
            highlighted(icon).save(os.path.join(pk, name + '_Mini_Highlighted.png'), optimize=True)
        if os.path.exists(shiny_p):
            portrait(shiny_p).save(os.path.join(pk, 'Portraits', name + '_S.png'), optimize=True)
        elif os.path.exists(norm_p):
            apply_map(portrait(norm_p), cmap).save(os.path.join(pk, 'Portraits', name + '_S.png'), optimize=True)

        # Female forms the mod tells apart (<Name>F.png): their own animation folder when SpriteCollab has one
        fem = os.path.join(src, '0000', '0000', '0002')
        if os.path.exists(os.path.join(pk, name + 'F.png')) or os.path.exists(os.path.join(pk, 'Anim', name + 'F')):
            if os.path.exists(os.path.join(fem, 'AnimData.xml')):
                finfo = convert(fem, os.path.join(pk, 'Anim', name + 'F'), os.path.join(src, '0000', '0001', '0002'),
                                None, args.states)
                fsheet = walk_sheet(finfo)
                fsheet.save(os.path.join(pk, name + 'F.png'), optimize=True)
                (walk_sheet(finfo, shiny=True) if finfo['shiny'] else apply_map(fsheet, cmap)).save(
                    os.path.join(pk, name + 'F_S.png'), optimize=True)
                r['female_shiny'] = finfo['shiny_mode']
                r['female'] = 'pmd'
            else:
                for suffix in ('F.png', 'F_S.png'):
                    if os.path.exists(os.path.join(pk, name + suffix)):
                        os.remove(os.path.join(pk, name + suffix))
                r['female'] = 'dropped'

        hjson = os.path.join(repo, 'Content', 'Pokemon', name + '.hjson')
        text = open(hjson, encoding='utf-8-sig').read()
        hovering = 'WanderingHoverBehaviour' in text
        d = info['walk_durations']
        w, h = hitbox(info, hovering)
        update_hjson(hjson, len(d), max(1, int(round(sum(d) / len(d)))), w, h)
        r.update({k: info[k] for k in ('ground', 'float', 'idle_bbox', 'idle_frame', 'missing', 'shiny',
                                       'shiny_consistency')})
        r.update({'hitbox': (w, h), 'hovering': hovering, 'walk_frames': len(d), 'states': list(info['strips'])})
        report[name] = r
        print(name, dex, 'ground', info['ground'], 'float', info['float'], 'hitbox', (w, h), 'missing',
              ','.join(info['missing']), 'shiny', info['shiny'], flush=True)
    with open(args.report, 'w') as fp:
        json.dump(report, fp, indent=1)


# -------------------------------------------------------------------------------------------------------------- credits
LICENSES = {
    'CC_BY-NC_4': 'CC BY-NC 4.0', 'PMDCollab_1': 'PMDCollab 1', 'PMDCollab_2': 'PMDCollab 2',
    'Unspecified': 'official PMD sprite',
}


def credit_names(dl):
    names = {}
    for line in open(os.path.join(dl, 'credit_names.txt'), encoding='utf-8').read().splitlines()[1:]:
        parts = line.split('\t')
        if len(parts) >= 2 and parts[0].strip():
            names[parts[1].strip()] = parts[0].strip()
    return names


def authors(path, used, names):
    """Who made what we use from one SpriteCollab folder (its credits.txt), with their licenses."""
    if not os.path.exists(path):
        return []
    out = []
    for line in open(path, encoding='utf-8').read().splitlines():
        parts = line.split('\t')
        if len(parts) < 5 or not (set(parts[4].split(',')) & used):
            continue
        who = names.get(parts[1], parts[1].replace('<@!', 'Discord user ').replace('>', ''))
        entry = 'Spike Chunsoft' if who == 'CHUNSOFT' else '%s (%s)' % (who, LICENSES.get(parts[3], parts[3]))
        if entry not in out:
            out.append(entry)
    return out


def run_credits(args):
    names = credit_names(args.download)
    states = {c for _, cands in STATES for c in cands} | {'Charge'}
    starters = set(filter(None, (args.starters or '').split(',')))
    lines = [
        '# Pokémon sprite and portrait credits',
        '',
        'The animated Pokémon sprites (`Assets/Pokemon/Anim`, the walking sheets and mini icons made from them) and the',
        'portraits (`Assets/Pokemon/Portraits`) come from the [PMD Sprite Repository (SpriteCollab)]'
        '(https://github.com/PMDCollab/SpriteCollab), by the PMDCollab community: <https://sprites.pmdcollab.org/>.',
        '',
        'SpriteCollab art is shared under [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/): it may only be',
        'used non-commercially, with credit. Art credited to "Spike Chunsoft" is the official Pokémon Mystery',
        'Dungeon sprites (Pokémon © Nintendo, Creatures, GAME FREAK, The Pokémon Company; PMD by Spike Chunsoft).',
        'They were cut into single-direction strips and drawn at 2x for Terramon (see `Content/Scripts/pmd_convert.py`).',
        '',
        '| # | Pokémon | Sprites | Shiny sprites | Portrait | Shiny portrait |',
        '|---|---|---|---|---|---|',
    ]
    for name, dex in sorted((l.split() for l in open(args.names)), key=lambda e: int(e[1])):
        sp = os.path.join(args.download, 'sprite', dex)
        po = os.path.join(args.download, 'portrait', dex)
        cols = [authors(os.path.join(sp, 'credits.txt'), states, names),
                authors(os.path.join(sp, '0000', '0001', 'credits.txt'), states, names),
                authors(os.path.join(po, 'credits.txt'), {'Normal'}, names),
                authors(os.path.join(po, '0000', '0001', 'credits.txt'), {'Normal'}, names)]
        if name in starters:  # only the icons (first Idle frame)
            cols[0] = authors(os.path.join(sp, 'credits.txt'), {'Idle'}, names)
            cols[1] = authors(os.path.join(sp, '0000', '0001', 'credits.txt'), {'Idle'}, names)
        fem = os.path.join(sp, '0000', '0000', '0002', 'credits.txt')
        if os.path.exists(os.path.join(args.terramon, 'Assets', 'Pokemon', 'Anim', name + 'F', 'Anim.txt')):
            cols[0] += ['female: ' + a for a in authors(fem, states, names)]
            cols[1] += ['female: ' + a for a in
                        authors(os.path.join(sp, '0000', '0001', '0002', 'credits.txt'), states, names)]
        lines.append('| %d | %s | %s |' % (int(dex), name, ' | '.join(', '.join(c) or '-' for c in cols)))
    with open(args.out, 'w', encoding='utf-8', newline='\n') as fp:
        fp.write('\n'.join(lines) + '\n')


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest='cmd', required=True)
    one = sub.add_parser('one')
    one.add_argument('src')
    one.add_argument('out')
    one.add_argument('--shiny')
    one.add_argument('--old', nargs=2)
    one.set_defaults(func=run_one)
    a = sub.add_parser('all')
    a.add_argument('download')
    a.add_argument('terramon')
    a.add_argument('names')
    a.add_argument('--starters', help='Pokémon that only get portraits and mini icons')
    a.add_argument('--skip', help='Pokémon that keep their old sprites (only portraits)')
    a.add_argument('--only', help='Convert just these')
    a.add_argument('--states', nargs='*', help='States to keep (default: all)')
    a.add_argument('--old-dir', default='old-sprites', help='Where to keep a copy of the old sheets')
    a.add_argument('--report', default='pmd_report.json')
    a.set_defaults(func=run_all)
    c = sub.add_parser('credits')
    c.add_argument('download')
    c.add_argument('terramon')
    c.add_argument('names')
    c.add_argument('out')
    c.add_argument('--starters')
    c.set_defaults(func=run_credits)
    args = p.parse_args()
    args.func(args)


if __name__ == '__main__':
    sys.exit(main())
