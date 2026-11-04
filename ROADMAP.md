# Roadmap · Pokerraria: LoveStory

Atualizado em 2026-11-03. Cada versão é pequena o bastante para ser testada de uma vez pelo Welsete. A ordem dentro
de uma versão é a ordem de ataque. Quem cuida de cada item está entre colchetes (agentes em `.claude/agents/`).

Legenda: `[x]` pronto · `[ ]` a fazer · `(?)` depende de decisão do Welsete (ver `docs/DECISOES.md` ou rodar o
grill-me do mod).

## 0.4.0 · Centro e Celeste (fechar o que já está pronto)

O que está em `## Next` do CHANGELOG, testado e publicado.

- [x] Centro Pokémon de dois andares com máquina de cura e PCs
- [x] Ginásio da Misty na praia, com sprite próprio, arremesso e vitória
- [x] Sprite próprio da Enfermeira Joy
- [x] 35 golpes com animação desenhada
- [x] Misty parada no Ginásio
- [x] Fontes de arte (prompts e spritesheets de VFX) no repositório
- [x] Rodada de teste completa do Welsete num mundo novo [qa-release]
- [x] Corrigir o que aparecer no teste (nada apareceu; o que surgir depois vai para a 0.4.1)
- [x] Fechar a versão: build.txt 0.4.0, CHANGELOG, README, tag `v0.4.0` [qa-release]

## 0.5.0 · Golpes de Água, Elétrico e Planta

- [ ] Gerar no Gemini as sheets dos golpes mais usados de Água, Elétrico e Planta (prompts prontos em
      `art/vfx/prompts/`) (?) quais golpes primeiro [Welsete + animador-vfx]
- [x] Primeiro lote animado: Water Gun, Hydro Pump, Bubble Beam, Aqua Jet, Thunder Shock, Thunderbolt,
      Thunder Punch e Razor Leaf [animador-vfx]
- [ ] Recortar, animar e testar cada um no painel F8 [animador-vfx]
- [x] Golpes de dois turnos com animação de carga (Solar Beam já serve de modelo: Fly, Dig, Skull Bash...)
      [programador-batalha + animador-vfx]
- [x] Golpes básicos desenhados por código, nas cores do tipo (232 golpes; regra em VFX.md) [animador-vfx]
- [ ] Gerar os golpes avançados que faltam (lista em VFX.md) [Welsete + animador-vfx]

## 0.6.0 · LoveStory capítulo 1 (até o Ginásio da Misty)

Decisões em `docs/DECISOES.md` (grill-me de 2026-11-03).

- [x] Definir o enredo do capítulo 1: o jogador do futuro, a Aurora presa entre os mundos [grill-me]
- [x] Menu de diário (J): páginas encontradas, o que já aconteceu e para onde ir [programador-batalha]
- [x] Páginas do diário como itens, com o efeito "Hora da história" (Prólogo, Brock, Misty)
- [ ] Colocar as páginas do Brock e da Misty nos baús das masmorras (quando as masmorras existirem) [construtor-mundo]
- [ ] Encontros com a Aurora entram como passos do capítulo em `Journal.Chapter1` [programador-batalha]
- [ ] Aurora: NPC rival com sprite, dois encontros com batalha (?) onde, quando e com que time [npcs-e-sprites + roteirista-lovestory]
- [x] Masmorra curta do Brock (4 salas, alavancas: só uma abre, as outras soltam pedras; baú com a página) [construtor-mundo]
- [ ] Masmorra curta da Misty (comportas que mudam o nível da água) [construtor-mundo]
- [ ] Gancho no fim do capítulo (?) qual [roteirista-lovestory]
- [ ] Textos pt-BR e en-US: páginas, falas da Aurora, entradas do diário [roteirista-lovestory]

## Eventos no mundo (ideias do Welsete, 2026-11-03; ainda sem versão)

Eventos no estilo das invasões do Terraria (como o Exército Goblin): aparecem de vez em quando ou quando uma condição
é cumprida, têm barra de progresso e uma recompensa no fim.

- [ ] **Equipe Rocket pelo mapa:** grupos de recrutas da Rocket aparecem em pontos do mapa, roubam Pokémon ou itens
      e fogem; vencer as batalhas devolve o que levaram. Pode crescer até uma invasão com Jessie, James e Meowth no
      dirigível (?) quando começa e o que roubam [programador-batalha + npcs-e-sprites + construtor-mundo]
- [ ] **Invasão dos Spearow:** um bando de Spearow ataca um Pikachu ferido; o jogador protege o Pikachu até a barra
      do evento encher; no fim o Pikachu pode se juntar à equipe (?) gatilho e recompensa [programador-batalha]
- [ ] **Torre Pokémon de Lavender:** estrutura gerada no mundo com andares de Pokémon Fantasma, névoa e um chefe no
      topo (o espírito da Marowak); de noite fica mais perigosa (?) bioma e se precisa do Silph Scope
      [construtor-mundo + roteirista-lovestory]

## Chefes Pokémon no estilo Terraria (ideia do Welsete, 2026-11-03; ainda sem versão)

Pokémon gigantes lutando como os chefes do Terraria (tipo o Olho de Cthulhu): luta em tempo real, barra de vida de
chefe, padrões de ataque próprios, item para invocar e recompensas. Diferente da batalha por turno do Showdown.

- [ ] **Base de chefes:** um molde de chefe Pokémon (fases por vida, ataques com animação dos golpes, música, barra
      de chefe, item de invocação, recompensas) para os próximos reaproveitarem [programador-batalha + animador-vfx]
- [ ] **Gengar gigante:** chefe final da Torre de Lavender. Some e reaparece pela sombra, solta Shadow Ball, chama
      fantasmas menores, usa Hypnosis para deixar o jogador lento (?) ataques e fases [programador-batalha]
- [ ] **Mewtwo, chefe final:** duas batalhas seguidas. Primeiro a batalha Pokémon por turno (Showdown); depois a luta
      de ação estilo Terraria, com ataques próprios (Psystrike, barreiras, clones) (?) ordem das fases e onde
      acontece [programador-batalha + roteirista-lovestory]
- [ ] Chefes para outros eventos (Equipe Rocket, invasões) (?) quais [programador-batalha]

## 0.7.0 · Ginásios e treinadores vivos

- [ ] Sprites próprios para os 6 líderes restantes no padrão da Misty (Surge, Erika, Koga, Sabrina, Blaine,
      Giovanni) [npcs-e-sprites]
- [ ] Ginásios temáticos por blueprint para cada líder, no bioma que combina (?) bioma de cada um
      [construtor-mundo]
- [ ] NPCs com dicas no caminho [roteirista-lovestory + npcs-e-sprites]
- [ ] Mais Centros Pokémon para construir depois de 4 insígnias [construtor-mundo]

## 0.8.0 · Batalhas com cara de lugar

- [ ] Arenas de batalha por bioma (fundo/chão conforme onde a batalha começa) [programador-batalha + construtor-mundo]
- [ ] Mais 3 tipos de golpes animados (?) quais [animador-vfx]

## 0.9.0 · Criação de Pokémon

- [ ] Ovos e Creche [programador-batalha + construtor-mundo]
- [ ] Itens para treinar EVs e melhorar IVs [programador-batalha]

## 1.0.0 · Jornada completa

- [ ] Montar, surfar, voar e cavar com Pokémon
- [ ] Mega Evolução com o Mega Bracelete
- [ ] Barra de emotes para o Pokémon fazer poses
- [ ] Todos os tipos de golpe com animações desenhadas para os golpes principais

## Sempre que der

- [ ] Limpar branches antigas no GitHub (`tools/dump-ids` e as `feature/*` já incorporadas)
- [x] Resolver os avisos RCS1242 do build (o build compila sem nenhum aviso)
