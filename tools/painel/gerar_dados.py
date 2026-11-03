"""Gera os dados do Painel Pokerraria (dashboard no claude.ai): roadmap, golpes animados por tipo, commits e builds.
Uso: python3 tools/painel/gerar_dados.py <pasta de saída>   (rodar na raiz do repo, com internet para os builds)
Depois, suba cada JSON como nova versão do arquivo no painel (o Claude faz isso com upload_asset + update)."""
import json, os, re, subprocess, sys, urllib.request

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
dump = lambda name, rows: json.dump(rows, open(os.path.join(OUT, name), 'w'), ensure_ascii=False)

# Golpes: catálogo de prompts por tipo x animações em Terramon/Assets/Moves
TIPOS = {'fogo': 'Fogo', 'agua': 'Água', 'planta': 'Planta', 'eletrico': 'Elétrico', 'gelo': 'Gelo', 'dragao': 'Dragão',
         'fantasma': 'Fantasma', 'sombrio': 'Sombrio', 'aco': 'Aço', 'terrestre': 'Terrestre', 'pedra': 'Pedra',
         'venenoso': 'Venenoso', 'inseto': 'Inseto', 'voador': 'Voador', 'lutador': 'Lutador', 'fada': 'Fada',
         'psiquico': 'Psíquico', 'normal_fisico': 'Normal', 'normal_especial': 'Normal', 'normal_status': 'Normal'}
# Golpes animados que não estão no catálogo de prompts: tipo de cada um
FORA_DO_CATALOGO = {'Ember': 'Fogo', 'FireBlast': 'Fogo', 'SolarBeam': 'Planta'}
norm = lambda s: re.sub(r'[^a-z]', '', s.lower())
anim = {os.path.splitext(f)[0] for f in os.listdir('Terramon/Assets/Moves') if f.endswith('.png')}
animn = {norm(a): a for a in anim}
rows, achados = {}, set()
for arq, tipo in TIPOS.items():
    # "## 2. Aqua Jet *(Já Gerado)*": a marca entre parênteses não faz parte do nome
    nomes = [re.sub(r'\s*\*?\(.*$', '', n) for n in
             re.findall(r'^## \d+\.\s*(.+)$', open(f'art/vfx/prompts/{arq}.md').read(), re.M)]
    r = rows.setdefault(tipo, {'tipo': tipo, 'catalogados': 0, 'animados': 0})
    r['catalogados'] += len(nomes)
    for n in nomes:
        if norm(n) in animn:
            r['animados'] += 1
            achados.add(norm(n))
for a in anim:
    if norm(a) not in achados:
        tipo = FORA_DO_CATALOGO.get(a)
        if not tipo:
            print('Golpe animado sem tipo conhecido, adicione em FORA_DO_CATALOGO:', a)
            continue
        rows[tipo]['animados'] += 1
        rows[tipo]['catalogados'] += 1
dump('golpes.json', sorted(rows.values(), key=lambda r: (-r['animados'], -r['catalogados'])))

# Roadmap: itens de ROADMAP.md por versão
itens, ver, nome = [], None, ''
linhas = []
for line in open('ROADMAP.md'):
    # Itens que continuam na linha de baixo (indentados) viram uma linha só
    if line.startswith('      ') and linhas:
        linhas[-1] = linhas[-1].rstrip('\n') + ' ' + line.strip() + '\n'
    else:
        linhas.append(line)
for line in linhas:
    m = re.match(r'^## (\d+\.\d+\.\d+) · (.+)', line)
    if m:
        ver, nome = m.group(1), m.group(2).strip()
        continue
    if line.startswith('## '):
        ver = None
        continue
    m = re.match(r'^- \[( |x)\] (.+)', line)
    if m and ver:
        txt = m.group(2).strip()
        eq = re.search(r'\[([^\]]+)\]\s*$', txt)
        txt = re.sub(r'\s*\[[^\]]+\]\s*$', '', txt)
        # "(?) assunto" marca uma decisão pendente do Welsete sobre aquele assunto
        item, _, decisao = txt.replace('**', '').partition('(?)')
        itens.append({'id': len(itens) + 1, 'versao': ver, 'versao_nome': nome,
                      'item': re.sub(r'\s+', ' ', item).strip().rstrip(','),
                      'status': 'feito' if m.group(1) == 'x' else 'a fazer',
                      'decisao_pendente': decisao.strip() or ('a definir' if '(?)' in txt else ''), 'equipe': eq.group(1) if eq else ''})
dump('roadmap.json', itens)

# Commits recentes do main
log = subprocess.run(['git', 'log', '-15', '--date=short', '--format=%h\t%ad\t%s'], capture_output=True, text=True).stdout
dump('commits.json', [dict(zip(['commit', 'data', 'mensagem'], l.split('\t'))) for l in log.strip().split('\n')])

# Builds do GitHub Actions no main
runs = json.load(urllib.request.urlopen(
    'https://api.github.com/repos/Welsete/Pokerraria_LoveStory/actions/runs?per_page=15&branch=main'))['workflow_runs']
nomes = {'success': 'ok', 'failure': 'falhou', 'cancelled': 'cancelado'}
dump('builds.json', [{'data': x['created_at'][:16].replace('T', ' '), 'commit': x['head_sha'][:8],
                      'resultado': nomes.get(x['conclusion'] or '', 'rodando')} for x in runs])
print('Dados em', OUT)
