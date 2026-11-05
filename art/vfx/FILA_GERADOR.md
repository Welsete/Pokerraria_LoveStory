# Fila do gerador de imagem

Prompts dos golpes que ainda esperam o gerador (regra em [VFX.md](../../VFX.md)). Gere, salve a imagem em
`Desktop/Pokerraria/vfx_spritesheets/` e avise o Claude. Este arquivo é refeito por `tools/vfx/vfx_md.py`.
No Antigravity: abra a pasta Desktop/Pokerraria e rode o workflow /gerar-lote-vfx (regras em .agent/rules). Em outro chat do Gemini, cole antes a instrução fixa de [INSTRUCAO_GEMINI.md](INSTRUCAO_GEMINI.md).
Comece pelas seções **Prioridade** (golpes que hoje têm uma versão provisória fraca).

## Psíquico (fundo verde #00FF00)

### Mirror Coat

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Mirror Coat | TYPE: Psychic
SUBJECT: a prism mirror barrier absorbing special attacks and reflecting double the damage back.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria prismatic mirror barrier look: faceted diamond shield, doubled beam rebound.

FRAME BY FRAME:
1: Faceted crystalline mirror shield forming vertically in front of center, glinting white (#FFFFFF).
2: Special attack strikes mirror surface, absorbing into the crystal facets with deep pink charge.
3: Charge reaches critical threshold: mirror core flares with an intense white-magenta flash.
4: Retaliation rebound: colossal horizontal beam of doubled energy bursts straight back to left.
5: Reflected beam pierces through; mirror shield shatters into 4 glowing crystal fragments.
6: Crystal fragments tumble in air, shedding sparkling glass dust.
7: Glass dust drifts downward.
8: Last frame: only 3 small fading mirror sparkle pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Psystrike

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Psystrike | TYPE: Psychic
SUBJECT: Mewtwo's signature move: materializing colossal solid psychic waves crushing physical defenses.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 materialização de ondas psíquicas, 6 impacto titânico, 6 dissipação)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria Last Prism / Nebula Blaze ultimate look: solid psychic wave crests, apocalyptic nova.

FRAME BY FRAME:
1: Massive concentric wave of solid magenta energy materializing at left, cresting like a tidal breaker.
2: Wave accelerates forward, front edge hardening into razor-sharp crystalline energy teeth.
3: Impact collision: wave slams into center target with a blinding full-frame white flash (#FFFFFF).
4: Cataclysmic detonation: colossal 100px radial explosion of solid psychic chunks and shock hoops.
5: Peak psystrike: maximum power, blinding white core flanked by 8 radiating telekinetic blades.
6: Shockwave ring: outer supersonic compression ring tears across the screen boundaries.
7: Blast hollows out into an expanding crater ring of churning psychic plasma.
8: Solid psychic chunks reach apex and begin heavy ballistic descent.
9: Dense carpet of glittering magenta stardust blankets the central area.
10: Ground crater crackles with residual physical force arcs.
11: Stardust particles begin ascending with gentle buoyant drift.
12: Solid psychic chunks crumble into fine crystalline dust on the floor.
13: Dust thins out into transparent pink clusters.
14: Residual force arcs fade from ground crater.
15: 4 tiny drifting psystrike pixels remaining.
16: Last frame: only 3 small fading psychic motes (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Ally Switch

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Ally Switch | TYPE: Psychic / Status
SUBJECT: warping space instantaneously to switch battlefield positions with a partner ally.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria teleportation portal look: twin swirling vortex portals, position crossing arcs.

FRAME BY FRAME:
1: Pair of swirling magenta teleport portals open on left and right sides of center.
2: Portals spin rapidly, emitting curved spatial warp ribbons bridging between them.
3: Instant swap: two silhouettes vanish into portals, crossing in mid-warp with a white flash (#FFFFFF).
4: Peak switch: silhouettes emerge at opposite portals; 4 tricky psychic sparks pop off.
5: Portals shrink rapidly toward center drain points.
6: Warp ribbons dissolve into drifting pink stardust.
7: Portals snap shut with tiny sparks.
8: Last frame: only 3 small fading swap pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Gravity

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Gravity | TYPE: Psychic / Status
SUBJECT: intensifying gravitational pull to ground all airborne foes and make moves 100% groundable.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria gravity globe / heavy field look: downward gravity grid lines, crushing planar force.

FRAME BY FRAME:
1: Heavy grid of magenta gravitational field lines materializes across top edge of frame.
2: Field activates: dense downward-pointing force vectors plunge down like an anvil sheet.
3: Gravity crush: all aerial silhouettes slammed down to ground baseline with a white flash (#FFFFFF).
4: Peak gravity: ground baseline compresses; 3 heavy horizontal force rings clamp over floor.
5: Airborne flying status stripped; heavy downward chevrons pin all combatants to earth.
6: Gravitational field stabilizes into a persistent ambient grid.
7: Ambient air crackles with dense gravitational tension.
8: Last frame: only 3 small fading gravity line pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Guard Swap

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Guard Swap | TYPE: Psychic / Status
SUBJECT: swapping defensive stat modifications with the target through a telekinetic link.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria stat swap link look: twin opposing defense shields crossing in mid-air.

FRAME BY FRAME:
1: Two glowing shield icons (one on left, one on right) circled by curved swap arrows.
2: Shields accelerate along the arrow arcs, heading toward each other across center.
3: Crossing impact: shields pass through each other at center with a bright white-pink flash (#FFFFFF).
4: Peak swap: shields arrive at exchanged targets, locking into place with a sharp chime.
5: Swap arrows dissolve into trailing pink mist.
6: Exchanged defense values integrate into targets.
7: Shield icons dissolve into body outlines.
8: Last frame: only 3 small fading shield sparkle pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Healing Wish

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Healing Wish | TYPE: Psychic / Status
SUBJECT: sacrificing oneself in a shower of celestial shooting stars to fully heal the replacement ally.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria self-sacrifice / shooting star burst look: fading silhouette, torrential stardust shower.

FRAME BY FRAME:
1: Center silhouette turns translucent white (#FFFFFF), hands folded in selfless prayer.
2: Silhouette dissolves into a brilliant supernova starburst of pure celestial light.
3: Starburst detonates: 8 shooting stars arc upward and outward like a firework fountain.
4: Peak wish: shooting stars crest overhead and cascade downward in a dazzling rain of stardust.
5: Stardust shower blankets the field, carrying 100% HP restore and status cure for incoming ally.
6: Stardust particles float gently with buoyant fluttering drift.
7: Ambient air glows with warm, protective starlight.
8: Last frame: only 4 small fading wishing star motes (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Imprison

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Imprison | TYPE: Psychic / Status
SUBJECT: sealing the opponent's moves by locking shared attacks behind telekinetic seal chains.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria seal rune / lock chains look: glowing padlock seal, crossed chain barrier.

FRAME BY FRAME:
1: Ornate telekinetic padlock glyph materializes at center, flanked by 2 crossed seal chains.
2: Chains snap tight across target attack silhouette with a sharp metallic clink.
3: Lock engagement: central padlock snaps shut with an intense white-pink flash (#FFFFFF).
4: Peak imprisonment: seal pulses with an absolute prohibition barrier; shared moves forbidden.
5: Chains become semi-translucent, sinking into target attack options.
6: Padlock glyph settles into an ominous stationary seal.
7: Residual seal energy crackles along chain links.
8: Last frame: only 3 small fading lock motes (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Light Screen

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Light Screen | TYPE: Psychic / Status
SUBJECT: erecting a shimmering hexagonal light pane that halves all incoming Special damage.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria crystal screen / forcefield look: interlocking hexagonal light panes, defensive sheen.

FRAME BY FRAME:
1: Hexagonal light grid begins assembling in front of center, edges glowing pale pink (#FFE4F2).
2: Hexagonal panes lock together, forming a tall translucent barrier screen (60px high).
3: Barrier locks solid: seam lines flash with brilliant pure white luminescence (#FFFFFF).
4: Peak screen: light screen pulses with an impenetrable defensive sheen, halving Special damage for 5 turns.
5: Screen absorbs ambient light, softening into a steady translucent protective pane.
6: Hexagonal grid lines pulse gently in rhythm.
7: Screen stabilizes into passive protective stance.
8: Last frame: only 3 small fading screen glint pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Power Swap

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Power Swap | TYPE: Psychic / Status
SUBJECT: swapping Attack and Special Attack modifications with the target via mental link.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria sword swap link look: twin crossed sword icons exchanging positions.

FRAME BY FRAME:
1: Two glowing sword icons (one on left, one on right) circled by curved swap arrows.
2: Swords accelerate along the arrow arcs, heading toward each other across center.
3: Crossing impact: sword icons pass through each other at center with a bright white-pink flash (#FFFFFF).
4: Peak swap: swords arrive at exchanged targets, locking into place with a sharp chime.
5: Swap arrows dissolve into trailing pink mist.
6: Exchanged offensive stat values integrate into targets.
7: Sword icons dissolve into body silhouettes.
8: Last frame: only 3 small fading sword sparkle pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Reflect

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Reflect | TYPE: Psychic / Status
SUBJECT: raising a sturdy diamond-faceted barrier pane that halves all incoming Physical damage.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria physical barrier look: diamond prism shield, physical deflect glints.

FRAME BY FRAME:
1: Diamond-faceted glass pane materializing vertically in front of center, sharp faceted edges.
2: Glass pane thickens into a heavy armored prism shield, glinting with pure white facets (#FFFFFF).
3: Barrier locks solid: prism shield pulses with an impenetrable defensive flash, halving Physical damage.
4: Peak reflect: shield deflects an incoming kinetic impact, releasing 4 sharp diamond sparks.
5: Shield absorbs kinetic force, softening into a steady translucent barrier.
6: Diamond facets reflect ambient light cleanly.
7: Shield settles into passive protective posture.
8: Last frame: only 3 small fading reflect glint pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Role Play

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Role Play | TYPE: Psychic / Status
SUBJECT: mimicking the opponent's behavior to copy and adopt their special ability.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria theater mask / mimic look: comedy/tragedy mask glyph, copying mirror aura.

FRAME BY FRAME:
1: Pair of theatrical comedy and tragedy masks materializing at upper center.
2: Masks spin in an orbital loop, emitting glowing pink telepathic scanning beams down to target.
3: Ability copy: scanning beams decode target ability, flashing with bright white insight (#FFFFFF).
4: Peak role play: comedy mask absorbs into user silhouette, granting copied ability with a chime.
5: User silhouette pulses with target's unique ability aura.
6: Theatrical scanning lines dissolve into air.
7: Copied ability integrates permanently into user.
8: Last frame: only 3 small fading role play pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Teleport

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Teleport | TYPE: Psychic / Status
SUBJECT: instantly vanishing from space with a collapsing warp ring and escaping battle.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria Rod of Discord / Teleportation Potion look: collapsing silhouette, vertical warp flash.

FRAME BY FRAME:
1: Silhouette at center turns into a glowing vertical cyan-magenta light streak.
2: Light streak contracts inward vertically, compressing into a brilliant white point (#FFFFFF).
3: Instant vanish: center point snaps shut with a crisp sonic pop; silhouette completely gone.
4: Peak departure: expanding circular warp ring bursts outward from departure node (40px wide).
5: Warp ring expands toward screen edges, thinning out.
6: Vertical speed lines fade from top to bottom.
7: Departure point clears completely.
8: Last frame: only 2 small fading warp sparkle pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Wonder Room

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Wonder Room | TYPE: Psychic / Status
SUBJECT: distorting dimensions to create an unearthly cube room that swaps Defense and Special Defense.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria dimensional cube room look: 3D wireframe isometric cube, stat inversion runes.

FRAME BY FRAME:
1: Isometric wireframe cube draws in at center in pale pink lines (#FFE4F2).
2: Cube expands outward, enclosing the entire combat area in a bizarre geometric room.
3: Dimensional flip: cube vertices pulse brightly (#FFFFFF); floor and ceiling trade perspective.
4: Peak wonder room: all combatants' Defense and Special Defense swap values with a chiming pulse.
5: Wireframe cube lines settle into translucent ambient grid boundaries.
6: Stat inversion runes settle above combatants' heads.
7: Wonder room persists as an active dimensional distortion field.
8: Last frame: only 4 small fading wireframe vertex pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

## Normal

### Bind

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Bind | TYPE: Normal
SUBJECT: heavy constricting coiled ropes/tentacles wrapping around target and squeezing with kinetic pressure.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Coiled thick binding rope loops entering from left and right, wrapping around center.
2: Ropes cross, constricting tightly around target torso with white friction highlights (#FFFFFF).
3: Squeeze impact: ropes tighten to maximum tension, releasing 4 inward pressure ticks.
4: Peak bind: target compressed; concentric kinetic shock rings burst outward from ribs.
5: Ropes lock in place as a persistent binding knot.
6: Knot pulses with residual crushing damage ticks.
7: Rope tension settles into stationary constriction.
8: Last frame: only 3 small fading constriction dust pixels (1px each, #171717).
```

### Explosion

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Explosion | TYPE: Normal
SUBJECT: the ultimate 250-power self-sacrificing nuclear detonation obliterating the screen.
DURATION & FRAME RULES: Golpes especiais: 15–24 frames. Explosões: 8–14 frames.
SHEET TYPE: SPECIAL MOVE, 16 frames (4 ignição nuclear, 6 mega detonação radial, 6 dispersão atômica)
CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Center silhouette glows blinding white-hot (#FFFFFF), body swelling with extreme pressure.
2: Critical breach: body cracks open into 4 diagonal quadrants releasing blinding light needles.
3: Singularity collapse: light needles snap inward into a pinprick nuclear core.
4: Apocalypse ignition: core explodes outward into a colossal 60px blinding white sphere.
5: Primary supernova: explosion reaches maximum 110px diameter, jagged fiery perimeter teeth.
6: Shockwave ring: outer supersonic compression hoop tears across all screen boundaries.
7: Blast hollows out into a massive circular fireball doughnut; center clears into vacuum.
8: Fireball doughnut shatters into 12 flying explosive shrapnel blocks.
9: Dense mushroom cloud begins billowing upward from ground baseline.
10: Ground crater glows molten gray; shrapnel blocks reach apex.
11: Heavy fallout shower: soot flakes and ash chunks rain down across frame.
12: Mushroom cloud cap expands across top ceiling.
13: Fallout settles onto floor baseline.
14: Mushroom cloud thins into transparent smoke clusters.
15: 4 tiny drifting ash pixels remaining.
16: Last frame: only 3 small fading explosion motes (1px each, #171717).
```

### Giga Impact

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Giga Impact | TYPE: Normal
SUBJECT: the ultimate physical giga tackle charging forward in a spiral drill of cosmic kinetic power.
DURATION & FRAME RULES: Golpes especiais: 15–24 frames. Explosões: 8–14 frames.
SHEET TYPE: SPECIAL MOVE, 16 frames (4 carga giga sônica, 6 impacto titânico, 6 exaustão total)
CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Massive conical kinetic aura forms around charging user silhouette, breaking sound barrier.
2: Aura begins high-speed spiral drill rotation, trailing broad silver-white wind ribbons.
3: Supersonic compression: drill nose superheats to pure white (#FFFFFF), sonic cone rings stacking.
4: Cataclysmic collision: drill slams into center target with a blinding full-frame white flash.
5: Continental detonation: colossal 100px radial explosion of shattered earth and kinetic plasma.
6: Shockwave ring: outer supersonic compression ring tears across screen boundaries.
7: Blast hollows out into an expanding impact crater ring; 8 heavy debris blocks blasted airborne.
8: Debris blocks reach apex; target bowled over across entire battlefield.
9: Heavy shockwave settles; dense curtain of impact dust billows upward.
10: User silhouette collapses into exhausted resting stance (recharge turn required).
11: Debris blocks crash down onto ground baseline.
12: Exhaustion steam wisps rise from user body.
13: Dust curtain thins into transparent smoke clusters.
14: Ground crater cools to dark gray.
15: 4 tiny drifting kinetic sparks remaining.
16: Last frame: only 3 small fading impact motes (1px each, #171717).
```

### Guillotine

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Guillotine | TYPE: Normal
SUBJECT: colossal scissor pincers snapping shut to cleanly decapitate target in a 1-hit KO.
DURATION & FRAME RULES: Explosões: 8–14 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Massive vertical guillotine pincers wide open overhead (50px tall), razor blade edges glinting.
2: Pincers drive downward with terrifying speed, blade edges aligned at target neck level.
3: Snap decapitation: pincers slam shut with a blinding white horizontal cutting flash (#FFFFFF).
4: Peak execution: 1-hit KO shockwave tears horizontally; 4 flying severed metal sparks pop off.
5: Pincers hold locked clamp pose; clean horizontal severance line cuts through frame.
6: Pincers open and retract; cutting line dissolves into fading silver motes.
7: Severance dust settles on floor baseline.
8: Last frame: only 3 small fading execution sparks (1px each, #171717).
```

### Horn Drill

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Horn Drill | TYPE: Normal
SUBJECT: a rapidly spinning spiral horn boring through defenses in a lethal 1-hit KO.
DURATION & FRAME RULES: Explosões: 8–14 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Spiral grooved horn rotating at extreme RPM, tip glinting white (#FFFFFF).
2: Horn drives forward, trailing a swirling cylindrical aerodynamic wind tunnel.
3: 1-hit KO puncture: drill tip strikes center target, penetrating clean through with blinding flash.
4: Peak drill: target defenses shattered; concentric conical shock rings blast outward along axis.
5: 4 flying bone/metal shrapnel shards burst radially from contact point.
6: Horn pulls through follow-through; bored hole in target crackles with friction heat.
7: Shrapnel shards tumble toward floor baseline.
8: Last frame: only 3 small fading drill sparks (1px each, #171717).
```

### Hyper Fang

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Hyper Fang | TYPE: Normal
SUBJECT: oversized sharp rodent incisors clamping down with crushing bite force that can flinch.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pair of massive flat front incisors wide open at center, enamel glinting white (#FFFFFF).
2: Incisors accelerate inward, trailing white kinetic speed lines.
3: Snap bite: incisors clamp shut, colliding with an intense white contact flash.
4: Peak hyper fang: jagged horizontal shockwave bursts outward from locked teeth; 3 tooth chips fly.
5: Teeth sink into target; target flinches backward.
6: Incisors retract; tooth chips crumble into white powder.
7: Powder settles on baseline.
8: Last frame: only 3 small fading enamel pixels (1px each, #171717).
```

### Pay Day

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Pay Day | TYPE: Normal
SUBJECT: hurling a shower of gleaming golden coins that damage foes and scatter loot for pickup.
DURATION & FRAME RULES: Projéteis elementais: 8–12 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Cluster of 4 shiny golden/silver coins launching forward in a spinning fan.
2: Coins fly toward center, spinning with bright white edge glints (#FFFFFF).
3: Impact barrage: coins strike target in rapid succession with sharp metallic clinks.
4: Peak pay day: coins bounce off target, scattering in an outward firework fountain.
5: Coins tumble through air, glinting as they flip end-over-end.
6: Coins fall toward floor baseline with gravity, bouncing on the ground.
7: Coins settle into a collectible glittering loot pile on floor.
8: Last frame: only 3 small fading coin glint pixels (1px each, #171717).
```

### Raging Bull

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Raging Bull | TYPE: Normal
SUBJECT: Tauros's signature charge: a furious rampaging bull charge shattering barriers and screens.
DURATION & FRAME RULES: Explosões: 8–14 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Massive horned bull skull ramming horizontally from left at full gallop.
2: Hooves churn ground baseline into a heavy rooster-tail of dirt; horns glint white (#FFFFFF).
3: Barrier shatter: horns smash through enemy Reflect/Light Screen, shattering it into glass shards.
4: Peak ram: horns plunge into center target; colossal horizontal shockwave blasts right (80px wide).
5: Target bowled over; charger plows through to right edge, trailing churned earth.
6: Broken screen shards rain down to floor baseline.
7: Heavy combat dust billows across floor.
8: Last frame: only 4 small fading ramming dust motes (1px each, #171717).
```

### Rapid Spin

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Rapid Spin | TYPE: Normal
SUBJECT: spinning at high speed like a top to blow away entry hazards and boost Speed (+1).
DURATION & FRAME RULES: Projéteis elementais: 8–12 frames em loop.
SHEET TYPE: PROJECTILE LOOP, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Spherical shell spinning at center baseline, outer rim trailing horizontal speed lines.
2: Shell spin accelerates, forming a continuous circular cutting disc silhouette.
3: Centrifugal wind expands; 3 trapped hazard spikes/seeds blown away from center.
4: Peak spin: shell pulses with pure white speed rim (#FFFFFF); upward chevron surges (Speed +1).
5: Hazards ejected completely off-screen; shell maintains high-RPM spin.
6: Shell begins slowing spin axis, speed lines softening.
7: Shell stabilizes back toward upright posture.
8: Shell finishes 360-degree rotation cycle, aligning seamlessly back to Frame 1.
```

### Self-Destruct

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Self-Destruct | TYPE: Normal
SUBJECT: a 200-power explosive self-sacrifice detonating in a massive mushroom cloud.
DURATION & FRAME RULES: Explosões: 8–14 frames.
SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Center silhouette superheats to pure white (#FFFFFF), body swelling with explosive pressure.
2: Detonation: body explodes outward in a colossal spherical flash (60px diameter).
3: Peak self-destruct: massive 80px radial shockwave of smoke and fiery blast chunks.
4: Shockwave ring tears outward; central mushroom stem begins rising.
5: Mushroom cap expands across top ceiling; 6 flying blast fragments.
6: Blast fragments crash down to baseline; mushroom stem thickens with dense smoke.
7: Heavy soot clouds settle across floor baseline.
8: Last frame: only 4 small fading soot motes (1px each, #171717).
```

### Super Fang

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Super Fang | TYPE: Normal
SUBJECT: colossal razor incisors biting down to unerringly cut target's current HP clean in half (50%).
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pair of massive razor incisors wide open at center, enamel glinting pure white (#FFFFFF).
2: Incisors snap shut with terrifying speed, cutting through target silhouette.
3: 50% HP slice: incisors clamp together dead center with a blinding white horizontal cutting flash.
4: Peak super fang: target HP cut clean in half; 4 sharp tooth sparks burst outward in cross pattern.
5: Incisors retract; target reels from massive percentage HP loss.
6: Cutting line fades from white to gray.
7: Tooth sparks dissolve into air.
8: Last frame: only 2 small fading fang pixels (1px each, #171717).
```

### Thrash

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Thrash | TYPE: Normal
SUBJECT: a rampaging multi-turn violent tantrum thrashing target before succumbing to confusion.
DURATION & FRAME RULES: Explosões: 8–14 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Rampaging silhouette flailing violently at center, wild punch arcs crossing in chaos.
2: Heavy hook punch connects with center target, white impact flash (#FFFFFF).
3: Immediate follow-up: brutal roundhouse kick slams target from opposite side.
4: Peak thrash: violent radial shockwave explodes 360 degrees (70px wide); 4 flying sparks.
5: Rampage subsides; dizzy spiral star glyph emerges above user head (Confusion).
6: Shockwave expands to borders; user wobbles unsteadily on feet.
7: Dizzy stars orbit head level.
8: Last frame: only 3 small fading dizzy sparks (1px each, #171717).
```

### Vice Grip

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Vice Grip | TYPE: Normal
SUBJECT: massive industrial crab/beetle pincers clamping down with bone-crushing mechanical grip.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pair of massive steel-hard pincers wide open on left and right, pincer teeth glinting white (#FFFFFF).
2: Pincers clamp inward with immense hydraulic torque, trailing gray speed arcs.
3: Clamp impact: pincers lock shut on center target with a sharp metallic clang and white contact star.
4: Peak grip: crushing kinetic pressure releases 4 horizontal shock needles from pincer teeth.
5: Pincers hold target locked in crushing vice; target struggles.
6: Pincers open and release; target dropped onto baseline.
7: Kinetic needles dissolve into air.
8: Last frame: only 2 small fading clamp pixels (1px each, #171717).
```

### Wrap

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Wrap | TYPE: Normal
SUBJECT: a long serpentine body or vine coiling around target to trap and constrict over turns.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Slender serpentine tail entering from left, curling into an S-curve around center.
2: Tail wraps completely around target waist, forming 3 tight overlapping coils.
3: Constriction begins: coils tighten with white pressure points (#FFFFFF) at contact nodes.
4: Peak wrap: target trapped; small circular constrict shock ring pulses outward.
5: Coils maintain tight grip, locking target in stationary hold.
6: Residual pressure ticks pulse along coils.
7: Coils settle into steady constriction.
8: Last frame: only 2 small fading wrap pixels (1px each, #171717).
```

### Tri Attack

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Tri Attack | TYPE: Normal
SUBJECT: simultaneous triangular convergence of fire, ice, and electricity firing as one unified beam.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Trinity / elemental prism look: triangular three-beam convergence, tri-elemental sparks.

FRAME BY FRAME:
1: Equilateral triangle of 3 elemental energy nodes (Flame top, Frost left, Spark right) hovering at left.
2: Nodes connect with triangular laser beams, forming a spinning elemental triangle prism.
3: Tri-beam launch: triangle prism fires a thick unified tri-beam horizontally across center.
4: Impact convergence: beam slams into target center with an explosive white trinity flash (#FFFFFF).
5: Peak tri attack: 3 secondary status sparks (burning ember, freezing flake, static jolt) burst out.
6: Unified beam cuts off; trinity impact crater crackles with alternating elemental traces.
7: Elemental traces dissipate into ambient air.
8: Last frame: only 3 small fading tri-elemental motes (1px each, #171717).

Do not include characters, text, UI, borders or shadows.
```

### After You

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: After You | TYPE: Normal / Status
SUBJECT: a polite courteous bow offering immediate initiative to the target partner.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Polite bowing silhouette at left, right hand gesturing forward in open courtesy.
2: Glowing silver initiative baton glyph materializes in hand, glinting white (#FFFFFF).
3: Baton glides forward smoothly through air toward target ally.
4: Peak after you: baton reaches ally, transferring immediate priority with a bright chime.
5: Ally silhouette pulses with active initiative aura; bowing silhouette steps back.
6: Transfer trail dissolves into soft silver dust.
7: Bowing silhouette returns to resting stance.
8: Last frame: only 2 small fading courtesy sparkles (1px each, #171717).
```

### Baton Pass

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Baton Pass | TYPE: Normal / Status
SUBJECT: passing a glowing silver relay baton to switch out while transferring all active stat boosts.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Luminous relay baton glyph glowing at center, surrounded by active stat boost rings.
2: Silhouette dashes off-screen right, passing baton forward into incoming ally slot.
3: Hand-off: replacement ally grips baton, absorbing all stat rings with a white flash (#FFFFFF).
4: Peak pass: baton pulses with transferred power; all stacked chevrons integrate into new ally.
5: Baton dissolves into a radiant silver protective aura cloaking incoming ally.
6: Protective aura settles into ally silhouette.
7: Residual transfer speed lines fade into air.
8: Last frame: only 3 small fading relay motes (1px each, #171717).
```

### Block

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Block | TYPE: Normal / Status
SUBJECT: spreading wide arms and establishing an inescapable barricade to prevent target escape.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Wide barricade arms spreading outward from center, forming a heavy horizontal gate.
2: Two stone gate pillars rise from ground baseline on left and right flanks.
3: Gate snaps shut: interlocking iron bars slam together across target with a metallic clang (#FFFFFF).
4: Peak block: escape blocked; red-and-white prohibition X-symbol clamps over exit paths.
5: Barricade settles into a permanent stationary prison wall.
6: Prohibition runes pulse once with locking authority.
7: Ambient dust settles at gate base.
8: Last frame: only 3 small fading barricade pixels (1px each, #171717).
```

### Conversion

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Conversion | TYPE: Normal / Status
SUBJECT: Porygon's digital code rewriting its cellular type to match the type of its first move.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Silhouette at center breaks into a wireframe digital polygon mesh.
2: Digital matrix code (0s and 1s) streams down vertically through the polygon vertices.
3: Type rewrite: polygon mesh flashes with pure white cellular re-coding (#FFFFFF).
4: Peak conversion: new elemental type aura wraps silhouette; digital wireframe solidifies.
5: Binary code streams dissolve into sparkling digital dust.
6: Reformed body pulses with newly adopted elemental texture.
7: Wireframe edges fade into smooth physical skin.
8: Last frame: only 3 small fading digital matrix pixels (1px each, #171717).
```

### Conversion 2

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Conversion 2 | TYPE: Normal / Status
SUBJECT: digitally analyzing the opponent's last attack and re-coding user type to resist it.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Holographic digital scanning reticle sweeps over incoming attack data at center.
2: Threat analyzed: resistant defensive shield glyph computes in green-white wireframe (#FFFFFF).
3: Re-coding flash: digital shield glyph absorbs into user body with a computational chime.
4: Peak conversion 2: user type alters to perfect defensive resistance; shield aura pulses.
5: Defensive polygon plates lock over silhouette, deflecting simulated damage.
6: Scanning reticle dissolves into digital pixels.
7: Polygon plates integrate smoothly into body.
8: Last frame: only 3 small fading calculation pixels (1px each, #171717).
```

### Copycat

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Copycat | TYPE: Normal / Status
SUBJECT: playfully mimicking the opponent's very last move with perfect theatrical precision.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Translucent mirror phantom appears at center, mimicking opponent's previous stance.
2: Phantom performs copied attack motion; motion lines glow white (#FFFFFF).
3: Replication flash: real user steps into phantom silhouette, aligning with the motion.
4: Peak copycat: copied move unleashes from user with identical VFX properties.
5: Mirror phantom dissolves into glittering silver stardust.
6: User completes copied move follow-through.
7: Stardust motes settle.
8: Last frame: only 2 small fading copycat pixels (1px each, #171717).
```

### Follow Me

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Follow Me | TYPE: Normal / Status
SUBJECT: a spotlight gesture drawing all opposing single-target attacks toward the user.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Silhouette beckons with wagging index finger, glowing spotlight cone shining from above.
2: Spotlight expands, illuminating center silhouette in brilliant white attention (#FFFFFF).
3: Redirection pulse: magnetic attraction rings radiate outward across entire battlefield.
4: Peak follow me: all enemy targeting lines snap onto center silhouette; taunt stars pop.
5: Spotlight cone maintains steady beam; user takes centered guard stance.
6: Attraction rings fade into air.
7: Spotlight beam softens into ambient glow.
8: Last frame: only 3 small fading spotlight motes (1px each, #171717).
```

### Helping Hand

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Helping Hand | TYPE: Normal / Status
SUBJECT: an inspiring cheer and supportive clasp boosting an ally's move power by 50%.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pair of supportive open hands reaching forward from left, glowing with golden-silver cheer.
2: Hands clap together in an enthusiastic high-five with a crisp white chime (#FFFFFF).
3: Encouragement shock: massive 50% power boost aura bursts outward toward ally slot.
4: Peak helping hand: partner ally wrapped in a brilliant empowered aura; attack damage +50%.
5: Supportive hands dissolve into flying celebration sparkles.
6: Ally aura blazes with enhanced combat readiness.
7: Sparkles drift downward gently.
8: Last frame: only 3 small fading cheer sparkles (1px each, #171717).
```

### Metronome

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Metronome | TYPE: Normal / Status
SUBJECT: a wagging finger ticking rhythmically back and forth before triggering a completely random move.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pointed index finger tilted 45 degrees left, ticking with a crisp metronome tick sound.
2: Finger swings 90 degrees to the right in smooth rhythmic pendulum arc.
3: Pendulum crosses center: tip glints with a curious white spark (#FFFFFF).
4: Peak metronome: finger stops upright; dazzling roulette burst of 8 random elemental sparks explodes.
5: Roulette sparks spin outward in a rainbow firework ring.
6: Chosen random attack begins to manifest from the central spark.
7: Roulette ring dissolves into stardust.
8: Last frame: only 3 small fading metronome motes (1px each, #171717).
```

### Mimic

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Mimic | TYPE: Normal / Status
SUBJECT: closely observing an opponent's chosen move to copy and memorize it for the battle.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Pair of observant eyes framed by a camera aperture glyph hovering at upper center.
2: Aperture clicks shut, capturing target move animation in a flash of white light (#FFFFFF).
3: Copy memorization: captured move icon translates into user move slot with a chime.
4: Peak mimic: user silhouette pulses with newly learned move properties; aperture dissolves.
5: User strike pose mirrors target technique perfectly.
6: Aperture lines fade into air.
7: Ambient copy dust settles.
8: Last frame: only 2 small fading mimic pixels (1px each, #171717).
```

### Pain Split

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Pain Split | TYPE: Normal / Status
SUBJECT: sharing suffering through a karmic link that adds both combatants' HP and splits it equally.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Karmic energy thread bridges horizontally between user and target across center.
2: Thread vibrates: two heart glyphs (one full, one empty) appear over combatants.
3: Karmic calculation: energy flows through thread, equalizing heart levels with a white flash (#FFFFFF).
4: Peak pain split: both HP bars adjust simultaneously to exact median average.
5: Karmic thread dissolves into a gentle shower of shared vitality droplets.
6: Target reels from HP loss while user revitalizes with HP gain.
7: Droplets absorb into combatants.
8: Last frame: only 3 small fading karmic motes (1px each, #171717).
```

### Perish Song

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Perish Song | TYPE: Normal / Status
SUBJECT: an eerie requiem funeral song counting down 3 turns until all who heard it faint.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Dark gothic treble clef glyph materializes at center, surrounded by mourning musical notes.
2: Clef plays an ominous requiem note; heavy circular soundwave washes over all combatants.
3: Perish doom count: gothic skull clock glyph appears overhead with the number "3" ticking down (#FFFFFF).
4: Peak doom: doom count locks onto all active combatants; 3 turns until inevitable fainting.
5: Mourning notes begin descending like falling black rain.
6: Soundwave expands off-screen.
7: Skull clock ticks into stationary ominous countdown.
8: Last frame: only 3 small fading requiem motes (1px each, #171717).
```

### Recycle

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Recycle | TYPE: Normal / Status
SUBJECT: recycling used resources to magically regenerate a consumed single-use held item.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Green-silver triangular recycling arrow glyph materializes over user hands.
2: Arrows spin rapidly clockwise, gathering ambient discarded item particles toward center.
3: Regeneration flash: particles coalesce at center with a bright white pop (#FFFFFF).
4: Peak recycle: consumed berry/seed item fully regenerates in user grasp with a pristine glint.
5: Recycling arrows dissolve into floating eco-sparks.
6: Regenerated item settles firmly into held slot.
7: Eco-sparks drift upward.
8: Last frame: only 2 small fading recycle pixels (1px each, #171717).
```

### Reflect Type

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Reflect Type | TYPE: Normal / Status
SUBJECT: reflecting the target's elemental nature to transform user type into a match for the opponent.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Prismatic mirror pane appears between user and target, reflecting target elemental aura.
2: Mirror absorbs target's type essence, glowing with intense elemental colors.
3: Type reflection: mirror projects absorbed essence onto user body with a white flash (#FFFFFF).
4: Peak reflect type: user body transforms to match target's exact elemental type combinations.
5: Mirror pane dissolves into sparkling prismatic glass dust.
6: User silhouette stabilizes in newly adopted elemental color scheme.
7: Prismatic dust drifts downward.
8: Last frame: only 3 small fading reflection pixels (1px each, #171717).
```

### Roar

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Roar | TYPE: Normal / Status
SUBJECT: a terrifying alpha predator roar that frightens wild foes away or forces target switch.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Massive roaring beast mouth silhouette opening at left, sound waves building at throat.
2: Roar erupts: colossal jagged acoustic shockwave arcs tear forward across center.
3: Screen shakes: deafening sound waves blast over target with white sonic teeth (#FFFFFF).
4: Peak roar: target terrified, bowled over by concussive acoustic force (forced switch).
5: Target retreats off-screen right; acoustic shockwaves expand to boundaries.
6: Beast mouth closes; sound waves dissipate.
7: Ambient dust settles on baseline.
8: Last frame: only 3 small fading roar pixels (1px each, #171717).
```

### Safeguard

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Safeguard | TYPE: Normal / Status
SUBJECT: cloaking the party in a radiant celestial veil preventing all status conditions for 5 turns.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Celestial protective sigil draws in on ground baseline around center silhouette.
2: Pillars of pure white protective light (#FFFFFF) rise from sigil vertices.
3: Light pillars weave overhead into a protective dome veil cloaking the entire party.
4: Peak safeguard: status immunity veil pulses with a brilliant silver chime; burns/paralysis barred.
5: Dome veil softens into a steady translucent protective aura.
6: Protective runes pulse gently along the veil perimeter.
7: Veil settles into passive stance.
8: Last frame: only 3 small fading safeguard glints (1px each, #171717).
```

### Sleep Talk

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Sleep Talk | TYPE: Normal / Status
SUBJECT: mumbling in one's sleep to randomly execute one of the user's other moves while asleep.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Sleeping silhouette on baseline with floating Zzz glyphs over head.
2: Dream speech bubble emerges with an active random move icon inside.
3: Sleep talk activation: speech bubble pops with a bright white insight flash (#FFFFFF).
4: Peak sleep talk: chosen move executes from the sleeping body without waking up.
5: Executed attack wave travels toward opponent.
6: Speech bubble dissolves into dream dust.
7: Silhouette rolls over, continuing peaceful sleep.
8: Last frame: only 3 small fading sleep talk motes (1px each, #171717).
```

### Splash

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Splash | TYPE: Normal / Status
SUBJECT: the legendary futile flop on the ground: absolutely nothing happens, but with great enthusiasm!
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Fish silhouette flopping into air at center with enthusiastic upward leap.
2: Flop apex: fish reaches 30px height, tail waving with pure white water droplets (#FFFFFF).
3: Gravity takes over: fish plummets back down toward baseline.
4: Peak splash: fish slaps flat onto floor baseline with a tiny comic water splash.
5: "...But nothing happened!" comic text bubble pops briefly.
6: 3 tiny water droplets bounce on ground.
7: Fish lies on side, panting optimistically.
8: Last frame: only 2 small fading splash droplet pixels (1px each, #171717).
```

### Substitute

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Substitute | TYPE: Normal / Status
SUBJECT: paying 25% max HP to create an iconic green plush decoy doll taking hits for the user.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: 25% HP cost flashes from user chest as a crisp white vitality orb (#FFFFFF).
2: Smoke poof at center: classic substitution smoke cloud erupts with comic exclamation.
3: Smoke clears: iconic green plush reptilian substitute doll sitting firmly on baseline.
4: Peak substitute: user silhouette safely retreats into rear shadow; doll takes active combat slot.
5: Substitute doll blinks with button eyes, ready to absorb incoming attacks.
6: Smoke poof completely clears.
7: Doll stands firm on baseline.
8: Last frame: only 2 small fading substitute smoke pixels (1px each, #171717).
```

### Teeter Dance

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Teeter Dance | TYPE: Normal / Status
SUBJECT: a wobbly, infectious dizzy dance that plunges all surrounding Pokémon into confusion.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Silhouette wobbles left and right on uneven legs in a bizarre dizzy dance step.
2: Wobble creates an undulating spiral wave radiating outward across floor baseline.
3: Infectious rhythm: all combatants' feet wobble in sync; white dizzy stars pop (#FFFFFF).
4: Peak teeter dance: confusion stars orbit all Pokémon heads; universal Confusion applied.
5: Dance rhythm dissolves into chaotic stumbling steps.
6: Confusion stars spin in eccentric ellipses.
7: Ambient air filled with dizzy spiral dust.
8: Last frame: only 3 small fading teeter pixels (1px each, #171717).
```

### Transform

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Transform | TYPE: Normal / Status
SUBJECT: Ditto's cellular transmutation morphing 100% into an exact copy of the target.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Gelatinous Ditto blob at center softens, body undulating like liquid clay.
2: Morph begins: blob stretches upward, reshaping cellular structure into target outline.
3: Transmutation flash: entire body flashes with a blinding pure white cellular metamorphosis (#FFFFFF).
4: Peak transform: exact identical duplicate of target emerges, complete with moves and stats.
5: Small Ditto face glint winks briefly before setting into perfect disguise.
6: Transformation energy wave expands outward and clears.
7: New body solidifies firmly on floor baseline.
8: Last frame: only 2 small fading transformation pixels (1px each, #171717).
```

### Whirlwind

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.
ATTACK: Whirlwind | TYPE: Normal / Status
SUBJECT: whipping up a ferocious whirlwind funnel that forcefully blows the opponent off the field.
DURATION & FRAME RULES: Impactos pequenos: 5–8 frames.
SHEET TYPE: IMPACT ONE-SHOT, 8 frames
CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px, effect centered.
PALETTE: #FFFFFF #F5F5F5 #D4D4D4 #A3A3A3 #525252 #171717 | Background: #FF00FF
FRAME BY FRAME:
1: Spiraling wind lines condense rapidly at center baseline, whipping loose dust upward.
2: Whirlwind erupts: tall twisting wind cyclone shoots up from floor to ceiling (50px wide).
3: High velocity: cyclone reaches peak rotation, center core glowing pure white (#FFFFFF).
4: Peak whirlwind: target swept up by gale-force vortex and blown forcefully off-screen right.
5: Cyclone breaks apart into expanding horizontal breeze ribbons.
6: Breeze ribbons drift across screen and thin out.
7: Ambient air settles.
8: Last frame: only 3 small fading whirlwind dust pixels (1px each, #171717).
```

