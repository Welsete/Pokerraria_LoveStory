"""Escreve VFX.md na raiz: a regra de quem faz cada golpe e as listas, a partir de art/vfx/golpes-status.json
(gerado por tools/vfx/basicos.py). Rode depois de animar golpes novos: python3 tools/vfx/basicos.py && python3 tools/vfx/vfx_md.py"""
import collections
import json
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
status = json.load(open(os.path.join(ROOT, "art", "vfx", "golpes-status.json"), encoding="utf-8"))
ARQ = {"Fogo": "fogo", "Água": "agua", "Planta": "planta", "Elétrico": "eletrico", "Gelo": "gelo", "Dragão": "dragao",
       "Fantasma": "fantasma", "Sombrio": "sombrio", "Aço": "aco", "Terrestre": "terrestre", "Pedra": "pedra",
       "Venenoso": "venenoso", "Inseto": "inseto", "Voador": "voador", "Lutador": "lutador", "Fada": "fada",
       "Psíquico": "psiquico", "Normal": "normal_*"}
VERDE = {"Fantasma", "Psíquico", "Venenoso", "Sombrio", "Fada"}
moves = os.path.join(ROOT, "Terramon", "Assets", "Moves")
mid = lambda n: "".join(p[:1].upper() + p[1:] for p in re.split(r"[^A-Za-z0-9]+", n) if p)
for s in status:  # o que já tem animação conta como feito, venha de onde vier
    if s["situacao"] == "gerador" and os.path.exists(os.path.join(moves, mid(s["golpe"]) + ".png")):
        s["situacao"] = "animado"
c = collections.Counter(s["situacao"] for s in status)
por = lambda sit: collections.OrderedDict((t, [s for s in status if s["tipo"] == t and s["situacao"] == sit]) for t in ARQ)

L = []
L.append("""# VFX dos golpes: quem faz o quê

Regra do projeto (decisão do Welsete, 2026-11-03): **golpe básico é desenhado por código, golpe avançado vai para o
gerador de imagem.** Assim ninguém gasta gerador de imagem com um soco ou um anel, e o código não tenta imitar uma
onda gigante ou um meteoro.

## Como decidir

| Vai para o **código** (codeart2d) | Vai para o **gerador** (Gemini) |
| --- | --- |
| Socos, chutes, cabeçadas, investidas: impacto com clarão, anel e faíscas | Golpes com forma própria: mordidas, chicotes, ossos, bolhas, ondas, tornados, meteoros, raios do céu |
| Cortes, garras e cortes em X | Clima e campo: chuva, sol, tempestade de areia, terrenos, Trick Room, telas, espinhos no chão |
| Projéteis simples: orbe, ferrão, semente, pedra pequena | Raios e feixes grandes, explosões únicas, golpes-assinatura (Hyper Beam, Earthquake, Hurricane...) |
| Ondas de som, psíquicas e de energia: anéis no alvo | Golpes que mostram uma criatura, um objeto ou o próprio Pokémon se transformando |
| Pós, esporos, gás e areia: nuvem no alvo | Drenar vida (partículas voltando ao usuário), golpes de duas etapas |
| Buffs, curas e debuffs simples | Qualquer básico que, no jogo, ficou genérico demais e merece arte própria |

Na dúvida, começa no código: é de graça e na hora. Se ficar fraco no jogo, o golpe sobe para o gerador.

## Como fazer

**Código:** o molde de cada golpe básico está em `tools/vfx/basicos.py` (`BASICOS`). Para criar ou mudar um:
coloque o nome em `BASICOS` com o molde, rode `python3 tools/vfx/basicos.py "Nome do Golpe"` e depois
`python3 tools/vfx/vfx_md.py`. Moldes: `impacto`, `impacto_forte`, `corte`, `garra`, `cruz`, `projetil`, `pulso`,
`nuvem`, `aura`, `cura`, `debuff`. As cores vêm da paleta de 6 tons do tipo.

**Gerador:** o prompt pronto de cada golpe está em `art/vfx/prompts/<tipo>.md`. O Welsete ou a Isa geram no
Gemini, a imagem vai para `vfx_spritesheets/` e o Claude recorta (`tools/vfx/convert.py`), anima e testa no painel
F8, aba Moves. Fantasma, Psíquico, Venenoso, Sombrio e Fada são gerados com **fundo verde `#00FF00`**.

Quando um golpe do gerador fica pronto, a animação dele substitui a versão de código (tire o nome de `BASICOS`).
""")
L.append(f"## Situação\n\n{len(status)} golpes no catálogo: **{c['animado']}** com arte própria, **{c['codigo']}** "
         f"desenhados por código, **{c['gerador']}** esperando o gerador.\n")
L.append("## Faltam: para o gerador de imagem\n\nEm ordem de tipo. Entre parênteses, o arquivo de prompts.\n")
for t, l in por("gerador").items():
    if l:
        verde = " · fundo verde" if t in VERDE else ""
        L.append(f"- **{t}** ({len(l)}, `{ARQ[t]}.md`{verde}): " + ", ".join(s["golpe"] for s in l))
L.append("\n## Feitos por código\n")
for t, l in por("codigo").items():
    if l:
        L.append(f"- **{t}** ({len(l)}): " + ", ".join(f"{s['golpe']} ({s['molde']})" for s in l))
L.append("\n## Com arte própria (gerador ou desenho à mão)\n")
for t, l in por("animado").items():
    if l:
        L.append(f"- **{t}** ({len(l)}): " + ", ".join(s["golpe"] for s in l))
open(os.path.join(ROOT, "VFX.md"), "w", encoding="utf-8").write("\n".join(L) + "\n")
print(dict(c))
