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
- [ ] Rodada de teste completa do Welsete num mundo novo [qa-release]
- [ ] Corrigir o que aparecer no teste [programador-batalha, construtor-mundo]
- [ ] Fechar a versão: build.txt 0.4.0, CHANGELOG, README, tag `v0.4.0` [qa-release]

## 0.5.0 · Golpes de Água, Elétrico e Planta

- [ ] Gerar no Gemini as sheets dos golpes mais usados de Água, Elétrico e Planta (prompts prontos em
      `art/vfx/prompts/`) (?) quais golpes primeiro [Welsete/Isa + animador-vfx]
- [x] Primeiro lote animado: Water Gun, Hydro Pump, Bubble Beam, Aqua Jet, Thunder Shock, Thunderbolt,
      Thunder Punch e Razor Leaf [animador-vfx]
- [ ] Recortar, animar e testar cada um no painel F8 [animador-vfx]
- [ ] Golpes de dois turnos com animação de carga (Solar Beam já serve de modelo: Fly, Dig, Skull Bash...)
      [programador-batalha + animador-vfx]
- [x] Golpes básicos desenhados por código, nas cores do tipo (232 golpes; regra em VFX.md) [animador-vfx]
- [ ] Gerar os golpes avançados que faltam (lista em VFX.md) [Welsete/Isa + animador-vfx]

## 0.6.0 · LoveStory capítulo 1

- [ ] Definir o enredo do capítulo 1: quem "sabia que você viria", onde a história aparece, como termina o
      capítulo (?) [grill-me + roteirista-lovestory]
- [ ] Itens/notas/encontros que contam a história no mundo [roteirista-lovestory + construtor-mundo]
- [ ] Diálogos pt-BR e en-US [roteirista-lovestory]

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
- [ ] Resolver os avisos RCS1242 do build
