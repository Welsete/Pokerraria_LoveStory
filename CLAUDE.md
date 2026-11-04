# Pokerraria: LoveStory · Guia para o Claude

Mod de Pokémon para Terraria (tModLoader, C#), fork do [Terramon](https://github.com/JamzOJamz/Terramon) 0.1.3.1.
Dono: **Welsete**. Ele é iniciante em programação: fale em português do Brasil, direto, sem enrolação, e
**nunca use o caractere travessão longo** em nada que ele vá ler (textos, docs, commits, comentários).

Leia também: [VFX.md](VFX.md) (quais golpes são feitos por código e quais vão para o gerador de imagem), [ROADMAP.md](ROADMAP.md) (o que fazer e em que ordem), [docs/DECISOES.md](docs/DECISOES.md) (decisões
de design já tomadas, alimentadas pelo grill-me) e [CHANGELOG.md](CHANGELOG.md).

## Mapa do código (`Terramon/`)

| Pasta | O que tem |
| --- | --- |
| `Core/Battling/` | Batalha por turno via Pokémon Showdown: `BattleManager`, `BattleField`, `BattlePokemon`, `MoveVisuals` (animações de golpe) |
| `Content/GUI/TurnBased/` | Interface da batalha (`BattleUI`, `BattleLogUI`, `ParticipantPanel`) e o painel de testes F8 (`TestUI`) |
| `Content/NPCs/Trainers/` | Líderes, Elite dos Quatro e Campeão (`GymLeader`, `LeagueMember`, `TrainerNPC`, um arquivo por treinador) |
| `Content/NPCs/Town/` | Enfermeira Joy, máquina de cura, Pokémon do Centro, vendedor |
| `Content/NPCs/ProfessorOak.cs` | Professor Carvalho e a jornada inicial |
| `Content/LoveStory/` | História: `Journal` (páginas e passos do capítulo), `JournalPlayer` (o que o jogador leu, salvo no personagem); a tela é `Content/GUI/JournalUI.cs` (tecla J), as páginas são `Content/Items/KeyItems/DiaryPage.cs` e a Hora da história é `Content/Buffs/StoryTime.cs`. Textos em `Localization`, chave `LoveStory` |
| `Content/WorldGeneration/` | Estruturas geradas no mundo: `Blueprint.cs` (lê designs do Blueprint Builder), `PokemonCenter`, `MistyGym`, `BrockGym`, `GymBuildings` |
| `Content/Pokemon/*.hjson` | Dados de cada Pokémon |
| `Assets/Moves/` | Animações desenhadas de golpe: `<Golpe>.png` (tira de frames) + `<Golpe>.txt` (parâmetros, ver `MoveVisuals.cs`) |
| `Assets/NPCs/` | Sprites de NPC (Joy, Carvalho, Misty, Brock...) |
| `Assets/Structures/*.json` | Blueprints das estruturas |
| `Localization/<idioma>/` | Textos. **Sempre** atualize `pt-BR` e `en-US` juntos |

Outros projetos: `TmonRuntimes/` (mod dependência com o runtime do Showdown), `EasyPacketsLib/`, `Terramon.DataGen/`.

## Ferramentas (`tools/`)

- `tools/vfx/convert.py` e `tools/vfx/beams.py`: transformam as spritesheets geradas no Gemini (fundo magenta
  `#FF00FF`) nas tiras de frames de `Assets/Moves/`. Cada golpe novo vira uma linha na lista `SHEETS`.
- `tools/sprites/cut_frames.py`: corta frames de NPC. `tools/structures/*.py`: estruturas. `tools/logo/logo.py`.
- `tools/build/build_local.sh`: compila o mod no shell Linux do PC e copia os `.tmod` para a pasta Mods.

## Estruturas (Ginásios, Centro Pokémon)

São desenhadas com o mod **Blueprint Builder** e salvas em JSON (formatVersion 1). O `Blueprint.cs` lê esse JSON e
constrói a estrutura em mundos novos, sem precisar do Blueprint Builder instalado.
- Designs do Welsete no jogo: `C:\Users\Welsete\Documents\My Games\Terraria\tModLoader\BlueprintBuilder\Designs`
  (conectar à sessão). Tem conceitos do Centro e do Ginásio da Misty, e uma biblioteca de prédios (castelo, arena
  mágica, vila do deserto, tavernas...) para Ginásios e arenas futuras. Desde 2026-11-03 o nome de cada arquivo é igual ao
  campo `name` do design; mantenha assim ao salvar designs novos.
- Versão final que vai no mod: `Terramon/Assets/Structures/<Nome>.json`, gerada/ajustada pelos scripts de
  `tools/structures/` (que usam `terraria-ids.txt` para validar nomes de itens).
- Para o Welsete ver um design no jogo: copie o JSON para a pasta Designs e ele abre no Blueprint Builder.

## Arte (`art/`, não vai para dentro do mod)

- `art/vfx/prompts/`: catálogo mestre de prompts de VFX por tipo (paletas de 6 tons, contagem de frames, regras de
  pixel art). Use ao pedir/gerar spritesheets novas.
- `art/vfx/sheets/`: spritesheets originais vindas do Gemini (fonte do `convert.py`).
- `art/npcs/`, `art/references/`: frames e referências de NPC e estruturas.

## Fluxo de trabalho (sessão do Cowork ligada ao PC do Welsete)

1. Trabalhe num clone do repo no shell do PC (`device_bash`), em `$HOME/repo`.
2. Compile com `bash tools/build/build_local.sh "$HOME/mnt/tModLoader/Mods"` (a pasta
   `Documentos/My Games/Terraria/tModLoader` precisa estar conectada à sessão). O build leva uns 30 s.
3. O Welsete testa no jogo: menu Mods, **Recarregar mods** (ou reabrir o tModLoader).
4. Funcionou: commit em `main` e push. O CI publica os `.tmod` no release `build-main`; tags `v*` geram release
   de versão com a seção do CHANGELOG.

Painel do projeto (dashboard no claude.ai): https://claude.ai/artifact/9NgURoGYbuDrwcqVPDp1mG. Depois de cada push,
rode `python3 tools/painel/gerar_dados.py "$HOME/mnt/Pokerraria/painel"`, suba cada JSON alterado ao painel
(Artifact `asset: true`) e atualize o `source.url` e o `updated` do dataset correspondente (`roadmap`, `golpes`,
`commits`, `builds`).

ffmpeg 7 (com libvpx-vp9 e libx264, exigido pelas partes de vídeo do agent-sprite-forge) fica em `$HOME/bin`, fora
do PATH padrão do shell: use `export PATH="$HOME/bin:$PATH"` antes. Se o ambiente for recriado, baixe de novo o build
estático em johnvansickle.com/ffmpeg (o do sistema é 4.4).

Commits: mensagem curta em inglês, no estilo dos existentes ("Misty stands at her Gym spot again"), descrevendo o
que muda para quem joga. Toda mudança visível vai para a seção `## Next` do CHANGELOG.

## Regras do projeto

- Não quebre mundos salvos: estruturas novas também precisam de um jeito de serem construídas em mundos antigos
  (aba League do painel F8).
- Textos de jogo em pt-BR e en-US sempre juntos.
- Decisões de estilo, nomes e humor são do Welsete. Na dúvida, pergunte (ou rode o grill-me do mod).
- A Misty fica **parada** no Ginásio (a caminhada foi deixada de lado). Os frames de andar continuam no sprite.
- VFX: o Welsete gera as spritesheets no Gemini a partir de `art/vfx/prompts/`; o Claude recorta, anima e
  ajusta o `.txt`.

## Equipe (`.claude/agents/`)

| Agente | Cuida de |
| --- | --- |
| `animador-vfx` | Spritesheets de golpe: prompts, recorte, `.txt`, teste no painel F8 |
| `programador-batalha` | Showdown, regras de batalha, IA dos treinadores, interface da batalha |
| `construtor-mundo` | Ginásios, Centros Pokémon, estruturas por blueprint, geração de mundo |
| `npcs-e-sprites` | Sprites e comportamento de NPCs (líderes, Joy, Carvalho), retratos |
| `roteirista-lovestory` | História, diálogos, textos e traduções pt-BR/en-US |
| `qa-release` | Build, revisão de código, checklist de teste para o Welsete, CHANGELOG e versões |
| `cacador-de-skills` | Avalia repositórios de skills (nota 1 a 10) e instala as com nota 9 ou 10 |

## Skills (`.claude/skills/`)

`handoff` (fim de sessão, salva em `docs/handoffs/`), `diagnosing-bugs` (bug relatado no teste), `teach` (ensinar o
Welsete com o código do mod, em `docs/aprendizado/`), `writing-for-agents` (ao editar este arquivo, agentes ou
skills), `generate2dsprite` e `generate2dmedia` (sprites; o keyer dela é o que o `convert.py` usa). Leia o `SKILL.md` direto pelo caminho quando o caso aparecer. Lista e avaliações: `.claude/skills/README.md`.

O Claude principal é o líder: quebra cada item do roadmap em tarefas, chama os agentes certos (em paralelo quando
não mexem nos mesmos arquivos) e passa tudo pelo `qa-release` antes de entregar.
