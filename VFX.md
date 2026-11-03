# VFX dos golpes: quem faz o quê

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

Meio-termo: **raios, feixes e golpes de pura luz** são desenhados à mão em código, com carga, raio e impacto próprios
(Solar Beam, Hyper Beam, Psybeam, Charge Beam, Flash Cannon, Thunder, Dazzling Gleam), e alguns golpes com forma
simples de desenhar (Will-O-Wisp, Twister, Rock Slide). Estão em `tools/vfx/draw_solar_beam.py`,
`tools/vfx/draw_light_moves.py` e `tools/vfx/draw_shape_moves.py`. Golpes que pedem um símbolo (socos com punho, chutes com
bota, Swords Dance com espadas, golpes de dragão com cabeça de dragão) ficam em `tools/vfx/draw_themed_moves.py`; um raio novo é uma linha em `MOVES` com o estilo
de carga, de raio e de impacto.

Na dúvida, começa no código: é de graça e na hora. Se ficar fraco no jogo, o golpe sobe para o gerador.

## Como fazer

**Código:** o molde de cada golpe básico está em `tools/vfx/basicos.py` (`BASICOS`). Para criar ou mudar um:
coloque o nome em `BASICOS` com o molde, rode `python3 tools/vfx/basicos.py "Nome do Golpe"` e depois
`python3 tools/vfx/vfx_md.py`. Moldes: `impacto`, `impacto_forte`, `corte`, `garra`, `cruz`, `projetil`, `pulso`,
`nuvem`, `aura`, `cura`, `debuff`. As cores vêm da paleta de 6 tons do tipo.

**Gerador:** o prompt pronto de cada golpe está em `art/vfx/prompts/<tipo>.md`. O Welsete gera no
Gemini, a imagem vai para `vfx_spritesheets/` e o Claude recorta (`tools/vfx/convert.py`), anima e testa no painel
F8, aba Moves. Fantasma, Psíquico, Venenoso, Sombrio e Fada são gerados com **fundo verde `#00FF00`**.

Quando um golpe do gerador fica pronto, a animação dele substitui a versão de código (tire o nome de `BASICOS`).

## Situação

416 golpes no catálogo: **78** com arte própria, **204** desenhados por código, **134** esperando o gerador.

## Faltam: para o gerador de imagem

Em ordem de tipo. Entre parênteses, o arquivo de prompts. Os marcados com * já têm uma versão provisória em código (o Welsete achou fraca) e são a **prioridade**.

- **Água** (8, `agua.md`): Aqua Tail, Dive, Waterfall, Wave Crash, Muddy Water, Surf, Whirlpool, Rain Dance
- **Planta** (11, `planta.md`): Petal Blizzard, Power Whip, Vine Whip, Absorb, Giga Drain, Leaf Storm, Mega Drain, Petal Dance, Grassy Terrain, Ingrain, Leech Seed
- **Elétrico** (4, `eletrico.md`): Thunder Fang, Zap Cannon, Electric Terrain, Magnetic Flux
- **Dragão** (3, `dragao.md`): Dragon Tail, Outrage, Dragon Breath
- **Fantasma** (4, `fantasma.md` · fundo verde): Lick, Shadow Sneak, Curse, Destiny Bond
- **Sombrio** (5, `sombrio.md` · fundo verde): Bite, Crunch, Fling, Memento, Switcheroo
- **Aço** (2, `aco.md`): Gyro Ball, Iron Tail
- **Terrestre** (9, `terrestre.md`): Bone Rush, Bonemerang, Bulldoze, Dig, Drill Run, Earthquake, Fissure, Sand Tomb, Spikes
- **Pedra** (7, `pedra.md`): Head Smash, Rollout, Stone Edge, Ancient Power, Sandstorm, Stealth Rock, Wide Guard
- **Venenoso** (6, `venenoso.md` · fundo verde): Gunk Shot, Poison Fang, Acid Spray, Belch, Sludge Wave, Toxic Spikes
- **Inseto** (4, `inseto.md`): Bug Bite, Leech Life, U-turn, String Shot
- **Voador** (7, `voador.md`): Bounce, Brave Bird, Sky Attack, Gust, Hurricane, Mirror Move, Tailwind
- **Lutador** (7, `lutador.md`): Counter, Seismic Toss, Storm Throw, Superpower*, Vital Throw, Final Gambit, Quick Guard
- **Fada** (2, `fada.md` · fundo verde): Play Rough, Misty Terrain
- **Psíquico** (15, `psiquico.md` · fundo verde): Dream Eater, Future Sight, Mirror Coat, Psystrike, Ally Switch, Gravity, Guard Swap, Healing Wish, Imprison, Light Screen, Power Swap, Reflect, Role Play, Teleport, Wonder Room
- **Normal** (40, `normal_*.md`): Bind, Explosion, Giga Impact, Guillotine, Horn Drill, Hyper Fang, Mega Kick*, Mega Punch*, Pay Day, Raging Bull, Rapid Spin, Self-Destruct, Stomp*, Super Fang, Thrash, Vice Grip, Wrap, Tri Attack, After You, Baton Pass, Block, Conversion, Conversion 2, Copycat, Follow Me, Helping Hand, Metronome, Mimic, Pain Split, Perish Song, Recycle, Reflect Type, Roar, Safeguard, Sleep Talk, Splash, Substitute, Teeter Dance, Transform, Whirlwind

Os prompts de todos esses, prontos para copiar, estão em [art/vfx/FILA_GERADOR.md](art/vfx/FILA_GERADOR.md).

## Feitos por código

- **Água** (10): Aqua Cutter (corte), Crabhammer (impacto_forte), Liquidation (impacto_forte), Razor Shell (corte), Brine (pulso), Water Pulse (pulso), Aqua Ring (cura), Life Dew (cura), Soak (debuff), Withdraw (aura)
- **Planta** (14): Branch Poke (impacto), Bullet Seed (projetil), Leaf Blade (corte), Leafage (projetil), Seed Bomb (projetil), Wood Hammer (impacto_forte), Energy Ball (projetil), Magical Leaf (projetil), Aromatherapy (cura), Sleep Powder (nuvem), Spore (nuvem), Stun Spore (nuvem), Synthesis (cura), Worry Seed (debuff)
- **Elétrico** (9): Nuzzle (impacto), Spark (impacto), Discharge (pulso), Electro Ball (projetil), Shock Wave (pulso), Charge (aura), Eerie Impulse (debuff), Magnet Rise (aura), Thunder Wave (debuff)
- **Dragão** (1): Dual Chop (cruz)
- **Fantasma** (5): Astonish (impacto), Hex (pulso), Night Shade (pulso), Confuse Ray (debuff), Spite (debuff)
- **Sombrio** (12): Assurance (impacto), Knock Off (impacto), Payback (impacto_forte), Pursuit (impacto), Dark Pulse (pulso), Snarl (pulso), Fake Tears (debuff), Flatter (debuff), Hone Claws (aura), Nasty Plot (aura), Taunt (debuff), Torment (debuff)
- **Aço** (6): Heavy Slam (impacto_forte), Iron Head (impacto), Metal Claw (garra), Smart Strike (impacto), Iron Defense (aura), Metal Sound (pulso)
- **Terrestre** (5): High Horsepower (impacto_forte), Earth Power (pulso), Mud Shot (projetil), Mud Slap (nuvem), Sand Attack (nuvem)
- **Pedra** (5): Rock Blast (projetil), Rock Throw (projetil), Smack Down (projetil), Power Gem (projetil), Rock Polish (aura)
- **Venenoso** (15): Cross Poison (cruz), Poison Jab (impacto), Poison Sting (projetil), Acid (projetil), Clear Smog (nuvem), Sludge (projetil), Sludge Bomb (projetil), Smog (nuvem), Venoshock (projetil), Acid Armor (aura), Coil (aura), Gastro Acid (debuff), Poison Gas (nuvem), Poison Powder (nuvem), Toxic (debuff)
- **Inseto** (9): Fell Stinger (projetil), Fury Cutter (corte), Megahorn (impacto_forte), Pin Missile (projetil), Twineedle (projetil), X-Scissor (cruz), Bug Buzz (pulso), Quiver Dance (aura), Rage Powder (nuvem)
- **Voador** (10): Acrobatics (impacto), Aerial Ace (corte), Drill Peck (impacto), Peck (impacto), Pluck (impacto), Wing Attack (corte), Air Cutter (corte), Air Slash (corte), Feather Dance (debuff), Roost (cura)
- **Lutador** (8): Cross Chop (cruz), Revenge (impacto), Reversal (impacto), Submission (impacto_forte), Aura Sphere (projetil), Vacuum Wave (pulso), Bulk Up (aura), Detect (aura)
- **Fada** (6): Disarming Voice (pulso), Moonblast (projetil), Baby-Doll Eyes (debuff), Charm (debuff), Moonlight (cura), Sweet Kiss (debuff)
- **Psíquico** (15): Psycho Cut (corte), Zen Headbutt (impacto), Confusion (pulso), Extrasensory (pulso), Psychic (pulso), Psyshock (pulso), Stored Power (pulso), Agility (aura), Amnesia (aura), Calm Mind (aura), Cosmic Power (aura), Heal Pulse (cura), Hypnosis (debuff), Kinesis (debuff), Rest (cura)
- **Normal** (74): Body Slam (impacto_forte), Covet (impacto), Crush Claw (garra), Cut (corte), Double-Edge (impacto_forte), Double Hit (impacto), Endeavor (impacto), Extreme Speed (impacto), Facade (impacto), Fake Out (impacto), False Swipe (corte), Feint (impacto), Flail (impacto), Fury Attack (impacto), Fury Swipes (garra), Headbutt (impacto), Horn Attack (impacto), Last Resort (impacto_forte), Pound (impacto), Quick Attack (impacto), Rage (impacto), Retaliate (impacto), Scratch (garra), Slam (impacto), Slash (corte), Strength (impacto_forte), Tackle (impacto), Take Down (impacto_forte), Echoed Voice (pulso), Hyper Voice (pulso), Round (pulso), Snore (pulso), Spit Up (projetil), Swift (projetil), Uproar (pulso), Acupressure (aura), Belly Drum (aura), Defense Curl (aura), Disable (debuff), Double Team (aura), Encore (debuff), Endure (aura), Focus Energy (aura), Glare (debuff), Growl (debuff), Growth (aura), Harden (aura), Howl (aura), Leer (debuff), Lock-On (debuff), Lovely Kiss (debuff), Mean Look (debuff), Minimize (aura), Play Nice (debuff), Protect (aura), Psych Up (aura), Recover (cura), Scary Face (debuff), Screech (pulso), Shell Smash (aura), Sing (debuff), Slack Off (cura), Smokescreen (nuvem), Soft-Boiled (cura), Stockpile (aura), Supersonic (pulso), Swagger (debuff), Swallow (cura), Sweet Scent (nuvem), Tail Whip (debuff), Tearful Look (debuff), Tickle (debuff), Work Up (aura), Yawn (debuff)

## Com arte própria (gerador ou desenho à mão)

- **Fogo** (19): Blaze Kick, Fire Fang, Fire Punch, Flame Charge, Flame Wheel, Flare Blitz, Heat Crash, Ember, Eruption, Fire Blast, Fire Spin, Flamethrower, Heat Wave, Incinerate, Inferno, Lava Plume, Overheat, Sunny Day, Will-O-Wisp
- **Água** (4): Aqua Jet, Bubble Beam, Hydro Pump, Water Gun
- **Planta** (2): Razor Leaf, Solar Beam
- **Elétrico** (5): Thunder Punch, Charge Beam, Thunder, Thunder Shock, Thunderbolt
- **Gelo** (15): Ice Fang, Ice Punch, Ice Shard, Icicle Crash, Icicle Spear, Aurora Beam, Blizzard, Freeze Dry, Ice Beam, Icy Wind, Powder Snow, Sheer Cold, Haze, Mist, Snowscape
- **Dragão** (5): Dragon Claw, Dragon Rush, Dragon Pulse, Twister, Dragon Dance
- **Fantasma** (3): Rage Fist, Shadow Punch, Shadow Ball
- **Sombrio** (2): Night Slash, Sucker Punch
- **Aço** (3): Bullet Punch, Meteor Mash, Flash Cannon
- **Terrestre** (1): Stomping Tantrum
- **Pedra** (1): Rock Slide
- **Lutador** (14): Axe Kick, Brick Break, Close Combat, Double Kick, Drain Punch, Dynamic Punch, Focus Punch, Force Palm, Hammer Arm, High Jump Kick, Low Kick, Low Sweep, Mach Punch, Rock Smash
- **Fada** (1): Dazzling Gleam
- **Psíquico** (1): Psybeam
- **Normal** (2): Hyper Beam, Swords Dance
