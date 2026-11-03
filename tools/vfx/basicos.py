"""Golpes básicos desenhados por código (codeart2d), sem gerador de imagem.

Cada golpe básico usa um MOLDE (impacto, corte, projétil, pulso, aura...) nas 6 cores do seu tipo. Os golpes que não
estão em BASICOS ficam para o gerador de imagem (Gemini): veja VFX.md na raiz do repositório.

Uso (da raiz do repo):  python3 tools/vfx/basicos.py [Nome,Nome,...]
Escreve art/vfx/codeart/basicos/<tipo>.fx.json, roda o fx_build.py do codeart2d e grava Terramon/Assets/Moves/<Golpe>.png/.txt.
Também grava art/vfx/golpes-status.json (todos os golpes do catálogo: animado, básico ou gerador), lido pelo VFX.md.
Requer: pip install --user "resvg-py>=0.5,<0.6"
"""
import glob
import hashlib
import json
import os
import re
import subprocess
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FX_BUILD = os.path.join(ROOT, ".claude", "skills", "codeart2d", "scripts", "fx_build.py")
SPECS = os.path.join(ROOT, "art", "vfx", "codeart", "basicos")
MOVES = os.path.join(ROOT, "Terramon", "Assets", "Moves")
WORK = os.path.join(os.path.expanduser("~"), "tmp", "basicos")
N = 56       # pixels de arte por lado do frame
SCALE = 2    # cada pixel de arte vira 2x2 na tela, como os sprites do Terraria

# Paletas de 6 tons por tipo (art/vfx/prompts/README.md): núcleo, alto, médio, base, escuro, contorno
PALETAS = {
    "Fogo": ["#FFF6C8", "#FFD84A", "#FF9A1F", "#E8480F", "#A3200E", "#4A1010"],
    "Água": ["#FFFFFF", "#C6F5FF", "#63D2FF", "#1E88E5", "#0D47A1", "#071F4F"],
    "Planta": ["#FFFFFF", "#FFF494", "#A7F432", "#46C328", "#1B7A22", "#0C3F15"],
    "Elétrico": ["#FFFFFF", "#FFFF7A", "#FFD700", "#FFA500", "#D97706", "#592700"],
    "Gelo": ["#FFFFFF", "#E0F7FA", "#80DEEA", "#26C6DA", "#00838F", "#004D40"],
    "Psíquico": ["#FFFFFF", "#FFE4F2", "#F472B6", "#DB2777", "#831843", "#4A044E"],
    "Fantasma": ["#FFFFFF", "#E9D5FF", "#A855F7", "#7E22CE", "#4C1D95", "#240046"],
    "Sombrio": ["#F3E8FF", "#9333EA", "#4B1058", "#2D0036", "#18001E", "#0A000D"],
    "Dragão": ["#FFFFFF", "#A5B4FC", "#6366F1", "#4338CA", "#251F72", "#130D3E"],
    "Aço": ["#FFFFFF", "#F1F5F9", "#CBD5E1", "#64748B", "#334155", "#0F172A"],
    "Terrestre": ["#FFFBEB", "#FDE68A", "#D97706", "#92400E", "#582306", "#2D1002"],
    "Pedra": ["#F8FAFC", "#E2E8F0", "#94A3B8", "#64748B", "#475569", "#1E293B"],
    "Venenoso": ["#FFFFFF", "#F5D0FE", "#D946EF", "#9333EA", "#581C87", "#2E0854"],
    "Inseto": ["#FEF08A", "#BEF264", "#84CC16", "#4D7C0F", "#365314", "#1A2E05"],
    "Voador": ["#FFFFFF", "#F0FDF4", "#BBF7D0", "#6EE7B7", "#14B8A6", "#0F766E"],
    "Lutador": ["#FFF1F2", "#FECDD3", "#FB7185", "#E11D48", "#9F1239", "#4C0519"],
    "Fada": ["#FFFFFF", "#FDF2F8", "#F472B6", "#EC4899", "#9D174D", "#500724"],
    "Normal": ["#FFFFFF", "#F5F5F5", "#D4D4D4", "#A3A3A3", "#525252", "#171717"],
}
ARQUIVOS = {"fogo": "Fogo", "agua": "Água", "planta": "Planta", "eletrico": "Elétrico", "gelo": "Gelo",
            "dragao": "Dragão", "fantasma": "Fantasma", "sombrio": "Sombrio", "aco": "Aço", "terrestre": "Terrestre",
            "pedra": "Pedra", "venenoso": "Venenoso", "inseto": "Inseto", "voador": "Voador", "lutador": "Lutador",
            "fada": "Fada", "psiquico": "Psíquico", "normal_fisico": "Normal", "normal_especial": "Normal",
            "normal_status": "Normal"}

# Moldes:
#   impacto        golpe físico de contato: clarão, anel e faíscas no alvo
#   impacto_forte  o mesmo, maior, com poeira no chão
#   corte          lâmina em arco no alvo
#   garra          três cortes paralelos
#   cruz           dois cortes em X
#   projetil       um orbe voa até o alvo e estoura
#   pulso          anéis concêntricos no alvo (ondas sonoras, psíquicas, de energia)
#   nuvem          pó, esporos, gás ou areia no alvo
#   aura           buff no próprio usuário: anel e faíscas subindo
#   cura           cura no próprio usuário: brilhos subindo
#   debuff         efeito de status no alvo: anel que fecha e faíscas caindo
# Swords Dance, Dragon Dance e Dragon Claw têm desenho próprio em draw_themed_moves.py
BASICOS = {
    # Provisórios: esperando arte do gerador de imagem (ver PROVISORIOS)
    "Mega Punch": "impacto_forte", "Drain Punch": "impacto", "Dynamic Punch": "impacto_forte", "Focus Punch": "impacto_forte", "Mach Punch": "impacto", "Shadow Punch": "impacto", "Sucker Punch": "impacto", "Meteor Mash": "impacto_forte", "Rage Fist": "impacto_forte", "Hammer Arm": "impacto_forte", "Force Palm": "impacto", "Rock Smash": "impacto", "Brick Break": "impacto", "Close Combat": "impacto_forte", "Superpower": "impacto_forte", "Axe Kick": "impacto_forte", "Double Kick": "impacto", "High Jump Kick": "impacto_forte", "Low Kick": "impacto", "Low Sweep": "impacto", "Mega Kick": "impacto_forte", "Stomp": "impacto", "Stomping Tantrum": "impacto_forte", "Dragon Rush": "impacto_forte", "Dragon Pulse": "pulso",
    # Normal
    "Body Slam": "impacto_forte", "Covet": "impacto", "Crush Claw": "garra", "Cut": "corte", "Double-Edge": "impacto_forte",
    "Double Hit": "impacto", "Endeavor": "impacto", "Extreme Speed": "impacto", "Facade": "impacto", "Fake Out": "impacto",
    "False Swipe": "corte", "Feint": "impacto", "Flail": "impacto", "Fury Attack": "impacto", "Fury Swipes": "garra",
    "Headbutt": "impacto", "Horn Attack": "impacto", "Last Resort": "impacto_forte",     "Pound": "impacto", "Quick Attack": "impacto", "Rage": "impacto",
    "Retaliate": "impacto", "Scratch": "garra", "Slam": "impacto", "Slash": "corte",     "Strength": "impacto_forte", "Tackle": "impacto", "Take Down": "impacto_forte",
    "Echoed Voice": "pulso", "Hyper Voice": "pulso", "Round": "pulso", "Snore": "pulso", "Uproar": "pulso",
    "Spit Up": "projetil", "Swift": "projetil",
    "Acupressure": "aura", "Belly Drum": "aura", "Defense Curl": "aura", "Double Team": "aura", "Endure": "aura",
    "Focus Energy": "aura", "Growth": "aura", "Harden": "aura", "Howl": "aura", "Minimize": "aura",
    "Protect": "aura", "Psych Up": "aura", "Shell Smash": "aura", "Stockpile": "aura",     "Work Up": "aura", "Recover": "cura", "Slack Off": "cura", "Soft-Boiled": "cura", "Swallow": "cura",
    "Disable": "debuff", "Encore": "debuff", "Glare": "debuff", "Growl": "debuff", "Leer": "debuff",
    "Lock-On": "debuff", "Mean Look": "debuff", "Play Nice": "debuff", "Scary Face": "debuff", "Screech": "pulso",
    "Sing": "debuff", "Supersonic": "pulso", "Swagger": "debuff", "Tail Whip": "debuff", "Tearful Look": "debuff",
    "Tickle": "debuff", "Yawn": "debuff", "Lovely Kiss": "debuff", "Smokescreen": "nuvem", "Sweet Scent": "nuvem",
    # Água
    "Aqua Cutter": "corte", "Crabhammer": "impacto_forte", "Liquidation": "impacto_forte", "Razor Shell": "corte",
    "Water Pulse": "pulso", "Brine": "pulso", "Aqua Ring": "cura", "Life Dew": "cura", "Withdraw": "aura",
    "Soak": "debuff",
    # Planta
    "Branch Poke": "impacto", "Bullet Seed": "projetil", "Leaf Blade": "corte", "Leafage": "projetil",
    "Seed Bomb": "projetil", "Wood Hammer": "impacto_forte", "Energy Ball": "projetil", "Magical Leaf": "projetil",
    "Synthesis": "cura", "Aromatherapy": "cura", "Worry Seed": "debuff", "Sleep Powder": "nuvem", "Spore": "nuvem",
    "Stun Spore": "nuvem",
    # Elétrico
    "Nuzzle": "impacto", "Spark": "impacto", "Discharge": "pulso", "Electro Ball": "projetil", "Shock Wave": "pulso",
    "Charge": "aura", "Magnet Rise": "aura", "Eerie Impulse": "debuff", "Thunder Wave": "debuff",
    # Dragão
    "Dual Chop": "cruz",         # Fantasma
    "Astonish": "impacto", "Hex": "pulso",
    "Night Shade": "pulso", "Confuse Ray": "debuff", "Spite": "debuff",
    # Sombrio
    "Assurance": "impacto", "Knock Off": "impacto", "Payback": "impacto_forte", "Pursuit": "impacto",
    "Dark Pulse": "pulso", "Snarl": "pulso", "Hone Claws": "aura", "Nasty Plot": "aura",
    "Fake Tears": "debuff", "Flatter": "debuff", "Taunt": "debuff", "Torment": "debuff",
    # Aço
    "Heavy Slam": "impacto_forte", "Iron Head": "impacto", "Metal Claw": "garra",     "Smart Strike": "impacto", "Iron Defense": "aura", "Metal Sound": "pulso",
    # Terrestre
    "High Horsepower": "impacto_forte", "Earth Power": "pulso",
    "Mud Shot": "projetil", "Mud Slap": "nuvem", "Sand Attack": "nuvem",
    # Pedra
    "Rock Blast": "projetil", "Rock Throw": "projetil", "Smack Down": "projetil", "Power Gem": "projetil",
    "Rock Polish": "aura",
    # Venenoso
    "Cross Poison": "cruz", "Poison Jab": "impacto", "Poison Sting": "projetil", "Acid": "projetil",
    "Sludge": "projetil", "Sludge Bomb": "projetil", "Venoshock": "projetil", "Acid Armor": "aura", "Coil": "aura",
    "Poison Gas": "nuvem", "Poison Powder": "nuvem", "Smog": "nuvem", "Clear Smog": "nuvem", "Toxic": "debuff",
    "Gastro Acid": "debuff",
    # Inseto
    "Fell Stinger": "projetil", "Fury Cutter": "corte", "Megahorn": "impacto_forte", "Pin Missile": "projetil",
    "Twineedle": "projetil", "X-Scissor": "cruz", "Bug Buzz": "pulso", "Quiver Dance": "aura", "Rage Powder": "nuvem",
    # Voador
    "Aerial Ace": "corte", "Drill Peck": "impacto", "Peck": "impacto", "Pluck": "impacto", "Wing Attack": "corte",
    "Acrobatics": "impacto", "Air Cutter": "corte", "Air Slash": "corte", "Roost": "cura", "Feather Dance": "debuff",
    # Lutador
    "Cross Chop": "cruz",
            "Revenge": "impacto", "Reversal": "impacto",     "Submission": "impacto_forte", "Aura Sphere": "projetil", "Vacuum Wave": "pulso",
    "Bulk Up": "aura", "Detect": "aura",
    # Fada
    "Disarming Voice": "pulso", "Moonblast": "projetil", "Moonlight": "cura", "Baby-Doll Eyes": "debuff",
    "Charm": "debuff", "Sweet Kiss": "debuff",
    # Psíquico
    "Psycho Cut": "corte", "Zen Headbutt": "impacto", "Confusion": "pulso", "Extrasensory": "pulso", "Psychic": "pulso",
    "Psyshock": "pulso", "Stored Power": "pulso", "Agility": "aura", "Amnesia": "aura", "Calm Mind": "aura",
    "Cosmic Power": "aura", "Rest": "cura", "Heal Pulse": "cura", "Hypnosis": "debuff", "Kinesis": "debuff",
}
# Golpes com versão de código só até a arte do gerador chegar: aparecem na fila do gerador (VFX.md)
PROVISORIOS = {"Mega Punch", "Drain Punch", "Dynamic Punch", "Focus Punch", "Mach Punch", "Shadow Punch", "Sucker Punch", "Meteor Mash", "Rage Fist", "Hammer Arm", "Force Palm", "Rock Smash", "Brick Break", "Close Combat", "Superpower", "Axe Kick", "Double Kick", "High Jump Kick", "Low Kick", "Low Sweep", "Mega Kick", "Stomp", "Stomping Tantrum", "Dragon Rush", "Dragon Pulse"}
# Status no próprio usuário (os outros moldes tocam no alvo)
NO_USUARIO = {"aura", "cura"}


def move_id(nome):
    """'Mud-Slap' -> 'MudSlap', 'U-turn' -> 'UTurn', 'Conversion 2' -> 'Conversion2'."""
    return "".join(p[:1].upper() + p[1:] for p in re.split(r"[^A-Za-z0-9]+", nome) if p)


def semente(nome):
    return int(hashlib.sha1(nome.encode()).hexdigest()[:6], 16)


def efeitos(nome, molde):
    """Uma ou duas descrições fx.v1 (a primeira de voo, quando é projétil) e os parâmetros do .txt."""
    s = semente(nome)
    v = (s % 7) / 6.0                      # variação 0..1 por golpe, para dois golpes do mesmo molde não ficarem iguais
    eid = move_id(nome).lower()
    brilho = "brilho"
    O = [28, 28]
    if molde in ("impacto", "impacto_forte"):
        forte = molde == "impacto_forte"
        prims = [
            {"type": "flash", "ramp": brilho, "origin": O, "radius": 6 if forte else 5, "rays": 8 if forte else 4 + 4 * (s % 2),
             "rayLength": 15 if forte else 11 + int(3 * v), "rotation": 45 * (s % 2), "lifeMs": 220},
            {"type": "ring", "ramp": brilho, "origin": O, "radius": [3, 20 if forte else 14 + int(3 * v)],
             "width": [4 if forte else 3, 1], "lifeMs": 260},
            {"type": "sparks", "ramp": brilho, "origin": O, "count": 12 if forte else 8, "spread": 360,
             "speed": 60 if forte else 50, "lifeMs": 260, "length": 6, "width": 2},
        ]
        if forte:
            prims.append({"type": "dust", "ramp": "fumaca", "origin": [28, 44], "count": 6, "spread": 22, "rise": 8,
                          "drift": 6, "radius": [1.5, 3.5], "lifeMs": 380})
        return [{"id": eid, "durationMs": 450 if forte else 350, "impactMs": 0, "seed": s, "outline": "contorno",
                 "ramps": rampas(), "primitives": prims}], {"Travel": 0, "Ticks": 3, "Face": 1}
    if molde in ("corte", "garra", "cruz"):
        if molde == "corte":
            cortes = [([24, 24], 18, -150, 30)]
        elif molde == "garra":
            cortes = [([17 + 8 * k, 24], 15, -140, 20) for k in range(3)]
        else:
            cortes = [([4, 4], 34, 15, 75), ([52, 4], 34, 165, 105)]
        prims = [{"type": "slash", "ramp": "lamina", "center": c, "radius": r, "from": a, "to": b, "width": 7 if molde == "corte" else 5,
                  "trail": 0.85} for c, r, a, b in cortes]
        prims.append({"type": "sparks", "ramp": brilho, "origin": [36, 33], "count": 8, "angle": 20, "spread": 160,
                      "speed": 42, "lifeMs": 240, "length": 5, "width": 2})
        return [{"id": eid, "durationMs": 400, "impactMs": 150, "seed": s, "outline": "contorno", "ramps": rampas(),
                 "primitives": prims}], {"Travel": 0, "Ticks": 3, "Face": 1}
    if molde == "projetil":
        voo = {"id": eid + "voo", "durationMs": 200, "impactMs": 0, "seed": s, "loop": True, "outline": "contorno",
               "ramps": rampas(), "primitives": [{"type": "projectile", "ramp": brilho, "origin": [34, 28], "angle": 0,
                                                  "radius": 4 + int(2 * v), "trail": 16, "orbiters": 2 + s % 2}]}
        hit = {"id": eid, "durationMs": 300, "impactMs": 0, "seed": s, "outline": "contorno", "ramps": rampas(),
               "primitives": [
                   {"type": "flash", "ramp": brilho, "origin": O, "radius": 3, "rays": 6, "rayLength": 9, "rotation": 30,
                    "lifeMs": 120},
                   {"type": "ring", "ramp": brilho, "origin": O, "radius": [3, 13], "width": [3, 1], "lifeMs": 220},
                   {"type": "sparks", "ramp": brilho, "origin": O, "count": 9, "spread": 360, "speed": 40,
                    "lifeMs": 240, "length": 5, "width": 2}]}
        return [voo, hit], {"Ticks": 3, "Face": 1}
    if molde == "pulso":
        prims = [{"type": "ring", "ramp": brilho, "origin": O, "atMs": 90 * k, "radius": [4, 22], "width": [3, 1],
                  "lifeMs": 300} for k in range(3)]
        prims.append({"type": "flash", "ramp": brilho, "origin": O, "radius": 4, "rays": 4 if s % 2 else 6,
                      "rayLength": 8, "lifeMs": 140})
        return [{"id": eid, "durationMs": 500, "impactMs": 0, "seed": s, "outline": "contorno", "ramps": rampas(),
                 "primitives": prims}], {"Travel": 0, "Ticks": 3}
    if molde == "nuvem":
        prims = [{"type": "dust", "ramp": "nevoa", "origin": [28, 38], "count": 10, "spread": 18, "rise": 20,
                  "drift": 10, "radius": [2.5, 5], "lifeMs": 480},
                 {"type": "sparks", "ramp": brilho, "origin": [28, 30], "count": 6, "angle": -90, "spread": 160,
                  "speed": 26, "lifeMs": 380, "length": 3, "atMs": 80}]
        return [{"id": eid, "durationMs": 520, "impactMs": 0, "seed": s, "outline": "contorno", "ramps": rampas(),
                 "primitives": prims}], {"Travel": 0, "Ticks": 3}
    if molde in ("aura", "cura"):
        cura = molde == "cura"
        prims = [{"type": "ring", "ramp": brilho, "origin": [28, 40], "radius": [6, 20], "width": [3, 1],
                  "squash": 0.5, "lifeMs": 380},
                 {"type": "sparks", "ramp": brilho, "origin": [28, 40], "count": 14 if cura else 12, "angle": -90,
                  "spread": 80 if cura else 50, "speed": 60, "lifeMs": 440, "length": 3 if cura else 7, "width": 2}]
        if not cura:
            prims.append({"type": "flash", "ramp": brilho, "origin": [28, 26], "radius": 2, "rays": 4,
                          "rayLength": 9, "atMs": 250, "lifeMs": 200})
        return [{"id": eid, "durationMs": 520, "impactMs": 0, "seed": s, "outline": "contorno", "ramps": rampas(),
                 "primitives": prims}], {"Travel": 0, "Ticks": 3, "At": "user"}
    if molde == "debuff":
        prims = [{"type": "ring", "ramp": brilho, "origin": [28, 24], "radius": [18, 6], "width": [2, 3], "squash": 0.5,
                  "lifeMs": 360},
                 {"type": "sparks", "ramp": brilho, "origin": [28, 12], "count": 10, "angle": 90, "spread": 60,
                  "speed": 50, "lifeMs": 380, "length": 6, "width": 2, "atMs": 100}]
        return [{"id": eid, "durationMs": 520, "impactMs": 0, "seed": s, "outline": "contorno", "ramps": rampas(),
                 "primitives": prims}], {"Travel": 0, "Ticks": 3, "At": "target"}
    raise ValueError(molde)


def rampas():
    return {"brilho": ["nucleo", "alto", "medio", "base"], "lamina": ["nucleo", "alto", "medio", "base"],
            "fumaca": ["medio", "base", "escuro"], "nevoa": ["alto", "medio", "base"]}


def catalogo():
    """Todos os golpes de art/vfx/prompts, com tipo."""
    out = []
    for arq, tipo in ARQUIVOS.items():
        txt = open(os.path.join(ROOT, "art", "vfx", "prompts", arq + ".md"), encoding="utf-8").read()
        for blk in txt.split("\n## ")[1:]:
            nome = re.sub(r"^\d+\.\s*", "", blk.split("\n", 1)[0])
            nome = re.sub(r"\s*\*?\(.*$", "", nome).strip()
            out.append((tipo, nome))
    return out


def main():
    so = set(sys.argv[1].split(",")) if len(sys.argv) > 1 else None
    move_ids = set(re.findall(r"^\s*(\w+),", open(os.path.join(ROOT, "Terramon", "ID", "MoveID.cs")).read(), re.M))
    feitos_antes = {os.path.splitext(f)[0] for f in os.listdir(MOVES) if f.endswith(".png")}
    cat = catalogo()
    norm = lambda s: re.sub(r"[^a-z0-9]", "", s.lower())
    status = []
    por_tipo = {}
    for tipo, nome in cat:
        mid = move_id(nome)
        molde = BASICOS.get(nome)
        if mid in feitos_antes and not (molde and os.path.exists(os.path.join(SPECS, "feitos", mid))):
            status.append({"tipo": tipo, "golpe": nome, "situacao": "animado"})
            continue
        if not molde:
            status.append({"tipo": tipo, "golpe": nome, "situacao": "gerador"})
            continue
        if mid not in move_ids:
            print("Sem MoveID, pulado:", nome, mid)
            status.append({"tipo": tipo, "golpe": nome, "situacao": "sem MoveID"})
            continue
        status.append({"tipo": tipo, "golpe": nome, "situacao": "provisorio" if nome in PROVISORIOS else "codigo",
                       "molde": molde})
        if so and nome not in so:
            continue
        por_tipo.setdefault(tipo, []).append((nome, mid, molde))
    json.dump(status, open(os.path.join(ROOT, "art", "vfx", "golpes-status.json"), "w", encoding="utf-8"),
              ensure_ascii=False, indent=1)

    os.makedirs(SPECS, exist_ok=True)
    os.makedirs(os.path.join(SPECS, "feitos"), exist_ok=True)
    for tipo, golpes in por_tipo.items():
        pal = PALETAS[tipo]
        efs, txts = [], {}
        for nome, mid, molde in golpes:
            e, t = efeitos(nome, molde)
            efs += e
            txts[mid] = (e, t)
        spec = {"schema": "codeart2d.fx.v1", "canvas": [N, N], "origin": [N // 2, N // 2],
                "palette": dict(zip(["nucleo", "alto", "medio", "base", "escuro", "contorno"], pal)),
                "routes": ["pixel"], "effects": efs}
        slug = norm(tipo.encode("ascii", "ignore").decode()) or "tipo"
        path = os.path.join(SPECS, f"{slug}.fx.json")
        json.dump(spec, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        out = os.path.join(WORK, slug)
        subprocess.run(["rm", "-rf", out])
        r = subprocess.run([sys.executable, FX_BUILD, "--spec", path, "--output-dir", out, "--route", "pixel"],
                           capture_output=True, text=True)
        last = (r.stdout.strip().splitlines() or ["{}"])[-1]
        info = json.loads(last) if last.startswith("{") else {}
        print(tipo, len(golpes), "golpes, QA:", info.get("qa"), info.get("failed_checks"), r.stderr.strip()[-300:])
        for mid, (e, t) in txts.items():
            # Frames iguais entre golpes são gravados uma vez só: o relatório diz qual arquivo cada frame usa
            rel = json.load(open(os.path.join(out, "fx-report.json")))["effects"]
            frames = [[os.path.join(out, f["file"]) for f in rel[ef["id"]]["frames"]] if ef["id"] in rel else []
                      for ef in e]
            if not all(frames):
                print("  sem frames:", mid)
                continue
            seq = [f for fs in frames for f in fs]
            ims = [Image.open(f).convert("RGBA") for f in seq]
            strip = Image.new("RGBA", (N * len(ims), N))
            for i, im in enumerate(ims):
                strip.paste(im, (i * N, 0))
            strip = strip.resize((strip.width * SCALE, strip.height * SCALE), Image.NEAREST)
            strip.save(os.path.join(MOVES, mid + ".png"), optimize=True)
            t = dict(t)
            if len(e) == 2:
                t = {"Travel": len(frames[0]), **t}
            linhas = [f"Frames {len(ims)}"] + [f"{k} {v}" for k, v in t.items()] + ["Scale 1"]
            open(os.path.join(MOVES, mid + ".txt"), "w").write("\n".join(linhas) + "\n")
            open(os.path.join(SPECS, "feitos", mid), "w").write(BASICOS_NOME(mid, golpes) + "\n")


def BASICOS_NOME(mid, golpes):
    return next(n for n, m, _ in golpes if m == mid)


if __name__ == "__main__":
    main()
