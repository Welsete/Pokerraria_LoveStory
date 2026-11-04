# Fila do gerador de imagem

Prompts dos golpes que ainda esperam o gerador (regra em [VFX.md](../../VFX.md)). Gere, salve a imagem em
`Desktop/Pokerraria/vfx_spritesheets/` e avise o Claude. Este arquivo é refeito por `tools/vfx/vfx_md.py`.
No Antigravity: abra a pasta Desktop/Pokerraria e rode o workflow /gerar-lote-vfx (regras em .agent/rules). Em outro chat do Gemini, cole antes a instrução fixa de [INSTRUCAO_GEMINI.md](INSTRUCAO_GEMINI.md).
Comece pelas seções **Prioridade** (golpes que hoje têm uma versão provisória fraca).

## Terrestre

### Dig

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Dig | TYPE: Ground
SUBJECT: burrowing completely into subterranean soil then bursting upward in a violent geyser.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria burrow breach look: hollow ground hole, explosive vertical earth fountain.

FRAME BY FRAME:
1: Ground baseline cracks open into a dark circular burrow hole at center.
2: Subterranean pressure builds: ground around hole domes upward with radial fissure lines.
3: Violent upward breach: colossal geyser of solid earth and rock blocks erupts straight up (60px).
4: Peak eruption: earth geyser crowns with a massive soil mushroom cap and 6 flying dirt chunks.
5: Earth column collapses outward in cascading mud falls.
6: Heavy dirt chunks rain down onto ground baseline with thudding impacts.
7: Burrow hole caves in; heavy dust cloud billows outward across the floor.
8: Last frame: only 4 small fading dust motes (1px each, #2D1002).

Do not include characters, text, UI, borders or shadows.
```

### Drill Run

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Drill Run | TYPE: Ground
SUBJECT: spinning the entire body like a corkscrew drill to bore through targets with a high critical rate.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria drill mount / corkscrew strike look: rotating cone of earth, critical friction flash.

FRAME BY FRAME:
1: Conical earth drill head entering from left at high rotational speed, tip glinting white.
2: Drill head accelerates, trailing a spiral cylinder of churned clay and amber wind ribbons.
3: Boring impact: drill tip strikes center target, penetrating with a blinding 4-point critical star (#FFFBEB).
4: Peak boring: drill penetrates deep; concentric conical shock rings blast outward along the axis.
5: 4 flying earth shrapnel chunks erupt radially from contact point.
6: Drill head pulls through; bored hole in target crackles with friction heat.
7: Earth shrapnel chunks tumble toward floor baseline.
8: Last frame: only 3 small fading friction dust pixels (1px each, #2D1002).

Do not include characters, text, UI, borders or shadows.
```

### Earthquake

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Earthquake | TYPE: Ground
SUBJECT: a catastrophic seismic upheaval tearing colossal chasms across the entire battlefield.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 choque tectônico, 6 fenda sísmica massiva, 6 acomodação)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Golem seismic stomp / catastrophic tectonic fault aesthetic.

FRAME BY FRAME:
1: Epic ground stomp hits baseline center with a blinding white seismic contact node (#FFFBEB).
2: Tectonic fault line rips open: horizontal jagged chasm tears across the entire bottom frame.
3: Subterranean roar: chasm edges offset vertically; 4 massive earth pillars erupt upward.
4: Earth pillars reach maximum height (80px), tilting inward like colliding tectonic plates.
5: Cataclysmic collision: pillars smash together at center, detonating into a colossal radial explosion.
6: Peak earthquake: 100px explosion of shattered clay boulders, amber shock rings, and soil plumes.
7: Shockwave hoop tears outward to screen boundaries; central impact crater hollows out.
8: Shattered earth boulders reach apex and begin heavy ballistic descent.
9: Boulders crash into ground baseline, creating secondary impact craters and dust clouds.
10: Chasm gap fills with falling rubble and jagged stone teeth.
11: Heavy dust billows from the fault lines, rising into a thick ground-level fog.
12: Fault lines settle into locked jagged trenches.
13: Dust fog thins out into floating particulate.
14: Residual seismic vibrations cease along floor.
15: 4 tiny drifting earth dust pixels remaining.
16: Last frame: only 3 small fading soil motes (1px each, #2D1002).

Do not include characters, text, UI, borders or shadows.
```

### Fissure

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Fissure | TYPE: Ground
SUBJECT: a bottomless abyss opening beneath the target to swallow them in a 1-hit KO drop.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria hellstone chasm look: bottomless pitch-black crevasse, collapsing ground edges.

FRAME BY FRAME:
1: Hairline fracture appears at center baseline, glowing with deep amber subterranean heat.
2: Fracture splits wide open with immense torque, earth tearing apart into a dark yawning chasm.
3: Bottomless crevasse established: pitch-black abyss (40px wide) with sheer jagged cliff walls.
4: Peak fissure: ground edges collapse inward; upward suction vortex pulls debris down into abyss.
5: Target silhouette plunges down into the abyss; 4 airborne earth chunks tumble into the void.
6: Crevasse begins grinding shut under immense subterranean tectonic pressure.
7: Chasm edges slam back together with a violent dust puff.
8: Last frame: only 3 small fading dust motes (1px each, #2D1002) above sealed seam.

Do not include characters, text, UI, borders or shadows.
```

### Sand Tomb

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Sand Tomb | TYPE: Ground
SUBJECT: a swirling quicksand vortex trapping target in a grinding funnel of coarse abrasive sand.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames em loop.

SHEET TYPE: PROJECTILE LOOP, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Antlion sand vortex look: funneling sand bands, grinding abrasive particles.

FRAME BY FRAME:
1: Concentric sand funnel on ground baseline, 3 spiral sand bands coiling into a dark center drain.
2: Sand bands rotate 45 degrees clockwise, outer rim flaring with airborne sand spray (#FDE68A).
3: Funnel deepens; center drain sinks downward, generating strong inward suction lines.
4: Abrasive sand teeth churn along the inner wall, shedding 3 loose sand grain motes.
5: Bands rotate 180 degrees, spiral geometry inverted symmetrically.
6: Inner vortex wall pulses with grinding friction heat.
7: Sand ribbons travel smoothly inward down the funnel slope.
8: Vortex completes 360-degree rotation cycle, aligning seamlessly back to Frame 1.

Do not include characters, text, UI, borders or shadows.
```

### Spikes

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Spikes | TYPE: Ground / Status
SUBJECT: scattering sharp caltrops and pointed earthen spikes across the battlefield floor.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFBEB #FDE68A #D97706 #92400E #582306 #2D1002
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria caltrop / floor hazard look: triangular caltrops, treacherous floor field.

FRAME BY FRAME:
1: Cluster of 4 sharp four-pointed earthen caltrops tossed into frame from left.
2: Caltrops tumble through air, sharp tips glinting with pale calcium white (#FFFBEB).
3: Ground impact: caltrops bounce onto floor baseline, settling with upward-pointing spikes.
4: Peak hazard: ground baseline bristling with a lethal carpet of triangular spikes.
5: Caltrops lock firmly into the soil, creating a permanent hazard zone.
6: Dust puffs from impact drift away into air.
7: Caltrops remain sharp and ominous on the floor.
8: Last frame: only 3 small fading dust pixels (1px each, #2D1002).

Do not include characters, text, UI, borders or shadows.
```

## Pedra

### Head Smash

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Head Smash | TYPE: Rock
SUBJECT: a reckless bone-crushing headbutt with a diamond-hard rocky skull releasing recoil shrapnel.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.
Every art pixel is exactly 1 pixel (no mixed pixel sizes).

PALETTE (use only these colors, flat fills, no gradients, no glow):
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria heavy ram / boulder smash look: massive rocky skull forehead, explosive stone shrapnel.

FRAME BY FRAME:
1: Hardened rocky skull driving horizontally from left at ramming speed, stone facets glinting white (#F8FAFC).
2: Skull connects with center target, forehead plate flashing with a blinding impact contact spark.
3: Cataclysmic collision: 150-power kinetic shockwave explodes radially, shattering target defenses.
4: Peak smash: colossal 70px radial explosion of flying sharp rock boulders and heavy recoil sparks.
5: Recoil shock: skull recoils violently backward; stone fractures spiderweb through user forehead.
6: Flying rock boulders tumble outward, shedding clouds of gray stone dust.
7: Boulders crash to floor baseline; stone dust billows upward.
8: Last frame: only 3 small fading stone dust motes (1px each, #1E293B).

Do not include characters, text, UI, borders or shadows.
```

### Rollout

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Rollout | TYPE: Rock
SUBJECT: curling into an unstoppable spinning stone boulder rolling with exponentially increasing speed.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames em loop.

SHEET TYPE: PROJECTILE LOOP, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria boulder / rolling cactus look: spherical stone wheel, ground-churning dust rooster-tail.

FRAME BY FRAME:
1: Spherical rocky boulder curling on floor baseline at 0 degrees, jagged mineral crust.
2: Boulder rolls forward 45 degrees, rear baseline churning up a rooster-tail of gray dust.
3: Boulder rolls to 90 degrees, high-speed spin flattening the bottom contact rim.
4: Boulder rolls to 135 degrees; centrifugal force sheds 2 flying gravel motes from the rim.
5: Boulder reaches 180 degrees, stone facet patterns inverted symmetrically.
6: Boulder rolls to 225 degrees; forward momentum builds with speed lines.
7: Boulder rolls to 270 degrees, crushing the ground surface beneath.
8: Boulder completes 360-degree rotation cycle, aligning seamlessly back to Frame 1.

Do not include characters, text, UI, borders or shadows.
```

### Stone Edge

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Stone Edge | TYPE: Rock
SUBJECT: razor-sharp pointed megalithic stone pillars erupting from the earth to impale targets.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria stone monolith / crystal impalement look: razor-sharp pointed stalagmites, high crit flash.

FRAME BY FRAME:
1: Ground baseline cracks with 3 upward fissure lines under center target.
2: Upward thrust: 3 razor-sharp pointed megalithic stone pillars burst upward through the cracks.
3: Impalement: center stone pillar shoots to maximum height (60px), tip tipped in pure white (#F8FAFC).
4: Peak stone edge: flanking stone pillars erupt at 30-degree angles, creating a lethal stone trident.
5: 4-point critical star flash detonates at apex; stone edges glint with deadly sharpness.
6: Stone pillars begin fracturing under seismic pressure, shedding sharp gravel flakes.
7: Pillars crumble downward into a pile of jagged rocky rubble.
8: Last frame: only 4 small fading rock chips (1px each, #1E293B).

Do not include characters, text, UI, borders or shadows.
```

### Ancient Power

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Ancient Power | TYPE: Rock
SUBJECT: prehistoric primordial megaliths levitating in mystical orbit and hurtling into target.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria ancient relic / floating rune stone look: levitating runic stones, prehistoric aura.

FRAME BY FRAME:
1: 3 ancient carved rune stones levitating in a triangle around center, pulsing with white runes.
2: Stones accelerate, spiraling inward rapidly with glowing gray kinetic slipstreams.
3: Convergence impact: all 3 rune stones collide at center point with a bright white flash (#F8FAFC).
4: Peak ancient burst: collision detonates into an 8-point prehistoric starburst shockwave (70px wide).
5: Shockwave ring expands outward; ancient stone runes detach as floating glowing hieroglyphs.
6: Hieroglyphs dissolve into sparkling prehistoric stardust; stones shatter.
7: Stardust motes drift upward with mystic buoyancy.
8: Last frame: only 3 small fading ancient power motes (1px each, #1E293B).

Do not include characters, text, UI, borders or shadows.
```

### Sandstorm

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Sandstorm | TYPE: Rock / Ground / Status
SUBJECT: summoning a howling desert sandstorm of churning abrasive grit that batters non-rock types.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Projéteis elementais: 8–12 frames.

SHEET TYPE: SPECIAL MOVE, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Sandstorm in a bottle / desert gale look: dense horizontal sand slipstreams, tumbling grit.

FRAME BY FRAME:
1: Sharp horizontal wind streaks surge in from left, carrying fine gray sand dust.
2: Gale intensifies: dense horizontal curtain of abrasive grit and sand grains sweeps across frame.
3: Full howling sandstorm: 3 undulating sand hurricane waves rushing across center.
4: Peak sandstorm: maximum grit density, abrasive rock particles scouring every pixel with white flecks.
5: Sand waves roll continuously, creating high-friction atmospheric turbulence.
6: Sandstorm stabilizes into a persistent howling weather effect.
7: Wind ribbons stream steadily across the upper and lower borders.
8: Last frame: only 4 small flying sand grains (1px each, #1E293B), weather loop maintained.

Do not include characters, text, UI, borders or shadows.
```

### Stealth Rock

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Stealth Rock | TYPE: Rock / Status
SUBJECT: floating levitating pointed rock shards hovering imperceptibly in opposing airspace.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria floating crystal trap look: levitating pointed rocks, ominous hazard minefield.

FRAME BY FRAME:
1: Cluster of 4 sharp pointed stone shards materializing around center, tips glinting white (#F8FAFC).
2: Shards expand outward to perimeter corners, locking into a floating minefield trap.
3: Trap locks: shards pulse with a sharp mineral chime, becoming semi-translucent.
4: Peak stealth: floating shards hover in fixed coordinates, ready to impale switching foes.
5: Shard facets glint once softly with slate-gray camouflage (#94A3B8).
6: Shards blend almost seamlessly into background airspace.
7: Ambient dust settles beneath the floating trap.
8: Last frame: only 3 small fading stealth glint pixels (1px each, #1E293B).

Do not include characters, text, UI, borders or shadows.
```

### Wide Guard

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Wide Guard | TYPE: Rock / Status
SUBJECT: an immense wide stone fortress wall locking into place to shield entire party from AoE attacks.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#F8FAFC #E2E8F0 #94A3B8 #64748B #475569 #1E293B
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria fortress bulwark / rampart look: interlocking granite masonry blocks, wide party shield.

FRAME BY FRAME:
1: Heavy granite foundation blocks rising rapidly from ground baseline across center.
2: Heavy stone masonry blocks interlock with heavy thuds, forming a wide rampart wall (60px wide).
3: Wall locks solid: masonry seams seal shut with a brilliant white fortress glint (#F8FAFC).
4: Peak wide guard: fortress wall emits a wide protective forcefield protecting all allies from AoE.
5: Wall absorbs incoming multi-target shockwave, deflecting 4 flying stone sparks.
6: Forcefield dissipates; granite blocks begin lowering back into the ground baseline.
7: Masonry blocks sink beneath soil line, dust settling.
8: Last frame: only 3 small fading masonry dust pixels (1px each, #1E293B).

Do not include characters, text, UI, borders or shadows.
```

## Venenoso (fundo verde #00FF00)

### Gunk Shot

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Gunk Shot | TYPE: Poison
SUBJECT: a massive filthy ball of toxic garbage and sludge hurled forcefully and detonating in slime.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria toxic sludge bomb look: irregular lumpy sludge ball, explosive splatter star.

FRAME BY FRAME:
1: Massive lumpy ball of toxic sludge and rusted debris flying in horizontally from left.
2: Sludge ball contacts center target, deforming under kinetic impact with white froth seams.
3: Catastrophic splatter: sludge ball bursts outward into a massive 6-point radial slime star.
4: Peak gunk: violent explosion flings 6 heavy toxic sludge globules and 4 flying trash shards.
5: Sludge splatters against floor and air boundaries, dripping downward in thick purple runs.
6: Toxic globules hit floor baseline, bubbling with corrosive caustic gas.
7: Sludge pools flatten into a greasy toxic stain; purple fumes rise.
8: Last frame: only 4 small fading sludge drip pixels (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

### Poison Fang

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Poison Fang | TYPE: Poison
SUBJECT: dripping hollow viper fangs snapping shut and injecting venom deep into target veins.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria venom fang / spider bite look: translucent purple fangs, dripping venom beads.

FRAME BY FRAME:
1: Pair of curved venomous viper fangs wide open at 60 degrees, glowing purple tips (#D946EF).
2: Fangs accelerate inward, trailing fine lavender venom mist lines behind the sharp tips.
3: Snap bite: upper and lower fangs clamp together at center with a bright white contact spark (#FFFFFF).
4: Peak envenomation: fangs sink into target; horizontal shockwave of purple venom rings bursts out.
5: Fangs retract; bite puncture points glow with intense corrosive magenta venom.
6: Venom diffuses through target veins in spiderweb poison cracks.
7: Fangs pull fully off-screen; poison cracks pulse with lingering toxic damage.
8: Last frame: only 3 small fading venom droplet pixels (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

### Acid Spray

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Acid Spray | TYPE: Poison
SUBJECT: a high-pressure shotgun blast of ultra-corrosive acid heavily melting Special Defense (-2).

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria shotgun spray / caustic melt look: wide conical fluid blast, harsh debuff arrows.

FRAME BY FRAME:
1: Concentrated nozzle of acidic fluid launching a wide conical spray from left edge.
2: Spray fan widens rapidly (60px spread), hundreds of microscopic acid droplets tearing forward.
3: Target engulfed: acid spray coats center, melting target surface with white sizzle points (#FFFFFF).
4: Peak melt: two heavy downward debuff chevrons (Sp.Def -2) drop onto target head.
5: Acid mist washes over target and continues drifting rightward.
6: Sizzling surface releases heavy plumes of acrid purple vapor.
7: Vapor plumes rise and thin out into air.
8: Last frame: only 3 small fading caustic droplets (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

### Belch

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Belch | TYPE: Poison
SUBJECT: an offensive gaseous burp of digested toxic berry sludge blasting forward after berry consumption.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria toxic breath / gas belch look: expanding foul gas cloud, acoustic shock rings.

FRAME BY FRAME:
1: Compressed ball of nauseating green-purple stomach gas bursting from left mouth point.
2: Gas expands violently with jagged acoustic roar rings shaking the screen.
3: Cataclysmic belch: massive billowing toxic cloud bursts across the entire center (70px wide).
4: Peak stench: toxic cloud erupts with 4 floating digested berry chunks and noxious bubbles (#FFFFFF).
5: Noxious gas rolls forward in heavy billowing mushroom lobes.
6: Berry chunks dissolve into foul dripping slime droplets.
7: Gas cloud begins lifting and thinning into ambient air.
8: Last frame: only 4 small fading stench motes (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

### Sludge Wave

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Sludge Wave | TYPE: Poison
SUBJECT: an apocalyptic tidal wave of foul churning toxic sludge drowning the entire battlefield.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 onda de esgoto montando, 6 tsunami tóxico brutal, 6 ressaca ácida)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria Duke Fishron tsunami / Corrupt wave visual: colossal rolling purple tidal wave, acidic froth.

FRAME BY FRAME:
1: Massive wall of bubbling purple sludge rising from left, deep violet base swelling high.
2: Sludge wave curls forward, foaming crest bulging with toxic bubbles and rusted debris.
3: Breaker crest: towering wave curls over completely, forming an epic barrel of vile slime.
4: Impact crash: curling sludge crest slams down onto center baseline with colossal toxic froth.
5: Hydraulic explosion: colossal splash geyser erupts outward in all directions (110px spread).
6: Shockwave ring: concentric wave of pressurized purple foam tears along the ground plane.
7: Peak dispersion: sludge mass shatters into countless airborne toxic chunks and acidic spray.
8: Sludge chunks begin gravitational descent like heavy toxic rain.
9: Heavy sheets of caustic sludge wash across the ground level.
10: Ground wash rolls left and right as churning noxious undertow.
11: Undertow thins out into bubbling purple foam carpets.
12: Foam carpets break into scattered bubbling sludge puddles.
13: Corrosive puddles eat into the floor; thick acrid purple vapor rises into air.
14: Vapor cloud thins out into transparent toxic clusters.
15: 4 tiny sludge droplet pixels remaining.
16: Last frame: only 3 small fading toxic droplets (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

### Toxic Spikes

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Toxic Spikes | TYPE: Poison / Status
SUBJECT: scattering venom-coated caltrops across the battlefield floor to poison switching foes.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F5D0FE #D946EF #9333EA #581C87 #2E0854
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria toxic caltrop / floor hazard look: venom-dripping barbed spikes, corrosive floor puddles.

FRAME BY FRAME:
1: Cluster of 4 sharp four-pointed purple caltrops tossed into frame from left.
2: Caltrops tumble through air, sharp tips glinting with toxic white venom beads (#FFFFFF).
3: Ground impact: caltrops bounce onto floor baseline, settling with upward-pointing spikes.
4: Peak hazard: ground baseline bristling with venom-dripping caltrops; small purple puddles form.
5: Caltrops lock firmly into the soil, creating a permanent toxic trap field.
6: Caltrop tips sizzle, releasing tiny wisps of purple vapor into air.
7: Caltrops remain sharp, dripping and menacing on the floor.
8: Last frame: only 3 small fading venom vapor pixels (1px each, #2E0854).

Do not include characters, text, UI, borders or shadows.
```

## Inseto

### Bug Bite

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Bug Bite | TYPE: Bug
SUBJECT: serrated insectoid pincers snapping shut to chew the target and steal its berry.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.
Every art pixel is exactly 1 pixel (no mixed pixel sizes).

PALETTE (use only these colors, flat fills, no gradients, no glow):
#FEF08A #BEF264 #84CC16 #4D7C0F #365314 #1A2E05
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Beetle Armor / mandibles look: sharp serrated insect pincers, green chitin chips.

FRAME BY FRAME:
1: Pair of serrated insectoid mandibles wide open at 70 degrees, sharp curved tips glinting yellow (#FEF08A).
2: Mandibles accelerate inward, trailing fine green speed streaks behind each tooth tip.
3: Snap bite: upper and lower mandibles clamp together at center with a sharp white-yellow spark.
4: Peak chew: horizontal shockwave bursts from locked mandibles; 3 green chitin sparks pop off.
5: Mandibles tear through target, chewing motion releases tiny organic sap droplets.
6: Mandibles open slightly and retract; sap droplets fall downward with gravity.
7: Chitin sparks dissolve into ambient air.
8: Last frame: only 3 small fading insectoid sparks (1px each, #1A2E05).

Do not include characters, text, UI, borders or shadows.
```

### Leech Life

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Leech Life | TYPE: Bug
SUBJECT: piercing proboscis draining substantial vitality and siphoning glowing life orbs back into user.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FEF08A #BEF264 #84CC16 #4D7C0F #365314 #1A2E05
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Vampire Knives / mosquito siphon look: hollow siphon needle, flowing vitality orbs.

FRAME BY FRAME:
1: Hollow needle proboscis thrusting forward horizontally from left, tip glinting white.
2: Proboscis pierces target center; green siphon suction lines spiral around the needle shaft.
3: Life drain: target vitality pulses; 4 luminous green nutrient orbs erupt from target.
4: Peak siphon: nutrient orbs flow backward in rapid sequence through the hollow proboscis.
5: Proboscis retracts; nutrient orbs absorb into user silhouette with bright healing crosses.
6: Siphon suction lines dissolve into ambient air.
7: Healing crosses ascend toward ceiling height.
8: Last frame: only 3 small fading vitality sparkles (1px each, #1A2E05).

Do not include characters, text, UI, borders or shadows.
```

### U-turn

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: U-turn | TYPE: Bug
SUBJECT: a high-speed strike followed by an immediate acrobatic hairpin turn switching out of battle.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FEF08A #BEF264 #84CC16 #4D7C0F #365314 #1A2E05
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria ninja dash / hairpin turn look: looped U-shaped speed trail, switch-out flash.

FRAME BY FRAME:
1: High-speed strike silhouette thrusting in from left, impacting center target with a white spark.
2: Immediate pivot: silhouette hooks upward and curls into a tight 180-degree hairpin U-turn.
3: Peak turn: U-turn apex flashes with a bright yellow chevron loop (#FEF08A); target knocked back.
4: Silhouette accelerates back toward left edge at double speed, trailing green wind ribbons.
5: Silhouette exits left screen boundary; U-turn flight trail lingers like a glowing hairpin ribbon.
6: Hairpin flight trail breaks into expanding breeze segments.
7: Breeze segments dissolve into ambient air.
8: Last frame: only 2 small fading speed pixels (1px each, #1A2E05).

Do not include characters, text, UI, borders or shadows.
```

### String Shot

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: String Shot | TYPE: Bug / Status
SUBJECT: spraying sticky coils of white silk webbing to entangle and drastically reduce target Speed.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FEF08A #BEF264 #84CC16 #4D7C0F #365314 #1A2E05
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria cobweb / silk shooter look: sticky silk cords, entangling web cocoon.

FRAME BY FRAME:
1: Jet of white silk thread shooting horizontally from left mouth nozzle, tip glinting yellow.
2: Thread expands into 3 undulating strands of sticky silk webbing spraying over center.
3: Webbing impacts target, wrapping tightly around limbs in a coiled cocoon.
4: Peak entangle: target bound firmly; two downward speed debuff chevrons drop (Speed -2).
5: Silk threads stick firmly to floor baseline, anchoring target in place.
6: Debuff chevrons dissolve; sticky silk cocoon remains taut.
7: Ambient silk strands settle.
8: Last frame: only 3 small fading silk thread pixels (1px each, #1A2E05).

Do not include characters, text, UI, borders or shadows.
```

## Voador

### Bounce

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Bounce | TYPE: Flying
SUBJECT: springing high into clouds and crashing back down like a spring-loaded aerial meteor.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria pogo spring / aerial drop look: vertical sonic descent, parabolic shockwave rings.

FRAME BY FRAME:
1: Target silhouette plunging straight down from top edge with vertical white wind slipstreams.
2: Ground slam: foot crashes onto baseline center, creating a compressed circular air cushion.
3: Elastic rebound: air cushion detonates into a colossal radial explosion of wind and feathers.
4: Peak bounce: dual 45-degree wind geysers erupt outward (80px wide); 3 paralysis sparks pop.
5: Silhouette springs high back into air; ground shockwave spreads horizontally along floor.
6: Wind geysers dissolve into tumbling air vortices.
7: Feathers drift downward toward floor.
8: Last frame: only 3 small fading bounce motes (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Brave Bird

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Brave Bird | TYPE: Flying
SUBJECT: a suicidal full-throttle divebomb engulfed in a blazing cyan avian energy phantom.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 mergulho da ave fênix, 6 impacto titânico, 6 dispersão e recuo)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Solar / Vortex endgame avian dive aesthetic: giant spectral falcon, apocalyptic shockwave.

FRAME BY FRAME:
1: Majestic cyan falcon phantom aura diving from top-right (45 degrees), wings swept back.
2: Falcon accelerates: beak tip superheats to pure white (#FFFFFF), trailing broad feathered wings.
3: Sonic barrier breach: conical shockwave ring forms around falcon head as it contacts center.
4: Impact crash: falcon strikes target, initiating a blinding full-frame white explosion.
5: Cataclysmic detonation: massive 100px radial explosion of aerodynamic blades and wind wings.
6: Shockwave ring: outer supersonic compression ring tears across screen boundaries.
7: Falcon phantom shatters into 8 independent flying energy feathers and air chunks.
8: Blast hollows out into a celestial impact crater of churning wind.
9: Heavy feather shower: dozens of glowing cyan feathers rain downward across frame.
10: Ground fissure glows with residual recoil heat; recoil shock jolts the center.
11: Heavy wind plumes begin ascending toward top edge.
12: Flying feathers lose velocity and flutter gently with gravity.
13: Wind plumes thin out into transparent breeze clusters.
14: Ground fissure cools to dark turquoise.
15: 4 tiny drifting feather pixels remaining.
16: Last frame: only 3 small fading wind sparks (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Sky Attack

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Sky Attack | TYPE: Flying
SUBJECT: cloaking in brilliant celestial sunlight and plunging down in an all-out blinding solar dive.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 carga celestial, 6 mergulho solar devastador, 6 dissipação)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Daybreak / Solar Cultist dive look: blinding glowing bird silhouette, celestial shockwave.

FRAME BY FRAME:
1: Silhouette gathers celestial light high above, glowing with pure white solar aura (#FFFFFF).
2: Solar wings unfurl to maximum width (60px), pulsing with brilliant white-and-cyan radiance.
3: Target locked: bird dives straight down at terminal velocity, trailing a massive solar comet tail.
4: Sonic impact: bird strikes center target with a blinding full-frame flash.
5: Apocalyptic blast: colossal radial shockwave of solar feathers explodes outward (100px wide).
6: Shockwave ring: outer solar hoop expands across screen boundaries.
7: Solar core shatters into 8 flying celestial feather blades and blinding rays.
8: Center clears into a glowing impact crater; outer blades continue radial flight.
9: Dense carpet of glittering solar feathers showers down over the entire area.
10: Ground crater crackles with residual celestial energy.
11: Solar feathers begin ascending with gentle buoyant drift.
12: Feathers fade from bright white to pale cyan (#BBF7D0).
13: Ambient air filled with glittering sky dust.
14: Ground crater settles into dark turquoise.
15: 4 tiny drifting solar sparks remaining.
16: Last frame: only 3 small fading sky motes (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Gust

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Gust | TYPE: Flying
SUBJECT: a sudden localized whirlwind gust of wind that lifts and tumbles small targets.

DURATION & FRAME RULES:
- Projéteis elementais: 8–12 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria gentle wind swirl look: concentric spiral wind loops, floating leaves and dust.

FRAME BY FRAME:
1: Three horizontal wind ribbons sweep in from left, curling into an upward swirl at center.
2: Swirl tightens into a miniature vertical funnel, lift vortex beginning.
3: Peak gust: wind funnel reaches maximum rotation (40px high), lifting 3 floating feathers.
4: Funnel top curls outward into a mushrooming crown of breeze.
5: Wind ribbons begin detaching from central vortex; feathers tumble at apex.
6: Funnel collapses into horizontal breeze wisps drifting right.
7: Feathers flutter downward with gravity.
8: Last frame: only 3 small fading gust pixels (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Hurricane

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Hurricane | TYPE: Flying
SUBJECT: a monstrous towering tropical cyclone of torrential winds and rain engulfing everything.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 gênese do ciclone, 6 fúria do olho do furacão, 6 dissipação)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Duke Fishron cyclone / tornado look: multi-tiered storm funnel, roaring rain bands.

FRAME BY FRAME:
1: Ground spiral of wind lines condensing rapidly at baseline, drawing in loose air motes.
2: Funnel erupts upward, twisting into an expanding cone of gale-force winds.
3: Colossal hurricane funnel reaches full height (110px), towering from floor to ceiling.
4: Spiral rain bands wrap around the storm funnel; eye of the hurricane opens at center.
5: Peak hurricane: maximum rotational velocity, outer wind teeth tearing across 90px diameter.
6: Violent lightning wind shears pulse through the cyclone; debris sucked into the vortex eye.
7: Hurricane waist constricts; top and bottom plumes flare outward in massive bell shapes.
8: Funnel begins de-stabilizing: central wind column tears open in radial burst.
9: Dense cloud of tumbling water droplets and wind shards explodes outward across frame.
10: Wind shards decelerate and begin falling with gravitational drift.
11: Heavy carpet of foamy rain wash forms on ground baseline.
12: Storm winds die down from a roar to steady blustery gusts.
13: Rain wash drains away; remaining airborne mist settles downward.
14: Ambient air clears, leaving faint soothing moisture.
15: 4 tiny drifting wind motes remaining.
16: Last frame: only 3 small fading storm pixels (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Mirror Move

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Mirror Move | TYPE: Flying / Status
SUBJECT: a crystalline mirror of silver air reflecting the opponent's last move right back at them.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Magic Mirror / reflection shield look: vertical glass mirror frame, reflective flash.

FRAME BY FRAME:
1: Ornate oval mirror frame woven from wind ribbons materializes in front of center.
2: Mirror glass surface crystallizes into a reflective pane, glinting pure white (#FFFFFF).
3: Reflection capture: incoming attack hits mirror surface, reversing direction with a bright chime spark.
4: Peak mirror: mirror surface pulses with a blinding duplication flash, launching the copied move.
5: Copied move shoots back toward opponent; mirror glass begins to dissolve.
6: Mirror frame unweaves into loose floating wind ribbons.
7: Wind ribbons drift apart.
8: Last frame: only 3 small fading mirror sparkle pixels (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

### Tailwind

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Tailwind | TYPE: Flying / Status
SUBJECT: summoning a tempestuous gale blowing at the team's back to double Speed for several turns.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #F0FDF4 #BBF7D0 #6EE7B7 #14B8A6 #0F766E
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria Hermes Boots / wind speed buff visual: rushing horizontal wind streams, speed chevrons.

FRAME BY FRAME:
1: Sharp horizontal wind streaks surge in from left edge, blowing forward.
2: Wind streams accelerate, forming 3 parallel aerodynamic slipstream tubes across center.
3: Peak tailwind: slipstream tubes pulse brightly (#FFFFFF); 2 forward-pointing chevron arrows shoot right.
4: Chevrons glide through with sonic crack; team speed doubled aura envelops party.
5: Wind tubes broaden into a gentle steady tailwind blowing across the entire field.
6: Slipstream tubes settle into translucent ambient breeze lines.
7: Floating feathers ride the tailwind smoothly off-screen.
8: Last frame: only 3 small fading tailwind motes (1px each, #0F766E).

Do not include characters, text, UI, borders or shadows.
```

## Lutador

### Counter

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Counter | TYPE: Fighting
SUBJECT: bracing against physical impact to unleash a devastating doubled retaliatory strike.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria parry shield flash into colossal counter-strike.

FRAME BY FRAME:
1: Cross-arm guard absorbing impact at center, dark red intake shock ring.
2: Guard supercharges: contact point flashes blinding white (#FFF1F2) with kinetic lines.
3: Counter eruption: colossal red fist or boot strikes forward with doubled mass.
4: Peak retaliation: massive horizontal conical shockwave blasts outward to right (70px wide).
5: Fist punches clean through; 4 heavy red energy shards burst radially.
6: Shockwave expands to perimeter; fist snaps back to ready stance.
7: Red energy shards dissolve into drifting combat dust.
8: Last frame: only 3 small fading counter sparks (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

### Seismic Toss

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Seismic Toss | TYPE: Fighting
SUBJECT: lifting target into the stratosphere, orbiting the planet, and slamming down seismically.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria planetary seismic slam look: terminal velocity descent, colossal impact geysers.

FRAME BY FRAME:
1: Target silhouette plunging down from top edge at terminal velocity, engulfed in friction fire.
2: Slam contact: target crashes onto ground baseline center, causing instant ground depression.
3: Cataclysmic seismic shock: colossal V-shaped shockwave geysers erupt left and right (80px wide).
4: Peak seismic: central impact crater flashes pure white (#FFF1F2), 6 heavy earth blocks airborne.
5: Seismic geysers crest at maximum height; ground shockwave spreads horizontally.
6: Earth blocks crash back to floor; seismic crater billows with heavy red dust.
7: Dust cloud thins out into floating particulate.
8: Last frame: only 4 small fading seismic debris pixels (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

### Storm Throw

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Storm Throw | TYPE: Fighting
SUBJECT: an infallible overhead judo slam guaranteeing a critical strike with brutal torque.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria judo throw look: high-velocity circular arc, guaranteed critical flash.

FRAME BY FRAME:
1: Target silhouette gripped and swung overhead in a high circular throwing arc.
2: Target accelerated downward along arc at terminal speed, trailing red kinetic blur.
3: Floor slam: target crashes into floor baseline with a 4-point critical star flash (#FFF1F2).
4: Peak throw: guaranteed critical shockwave bursts outward in an elliptical ground ring.
5: Ground shockwave shatters into flying kinetic sparks; thrower returns to ready stance.
6: Kinetic sparks lose velocity, floating upward.
7: Sparks fade to dark burgundy.
8: Last frame: only 3 small fading throw sparks (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

### Vital Throw

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Vital Throw | TYPE: Fighting
SUBJECT: a patient sluggish throw that strikes second but guarantees 100% infallible hit.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria heavy counter-throw look: deliberate deliberate torque, guaranteed slam star.

FRAME BY FRAME:
1: Heavy hands gripping target silhouette deliberately, red torque lines coiling.
2: Slow deliberate rotation: thrower pivots on heel, swinging target in a massive vertical arc.
3: Infallible slam: target crashes into ground baseline with a guaranteed white impact star (#FFF1F2).
4: Peak vital throw: heavy circular shockwave bursts outward along floor baseline.
5: Shockwave dissipates; thrower finishes clean follow-through posture.
6: Ground shockwave breaks into fading kinetic dust.
7: Dust settles.
8: Last frame: only 3 small fading throw motes (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

### Final Gambit

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Final Gambit | TYPE: Fighting
SUBJECT: an all-or-nothing suicidal combustion converting user's entire HP pool into raw damage.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: EXPLOSION ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria self-destruct / suicide bomb look: total body combustion, apocalyptic radial nova.

FRAME BY FRAME:
1: Center silhouette superheats to pure white, surrounded by 4 inward-collapsing red rings.
2: Absolute collapse: silhouette implodes into a pinprick white kinetic singularity.
3: Nuclear detonation: colossal spherical explosion bursts outward to 90px diameter.
4: Peak gambit: full-frame crimson nova with blinding white core and 8 flying energy blocks.
5: Blast hollows out into an expanding circular shockwave ring; center clears completely.
6: Shockwave ring shatters into chaotic flying red embers and heavy dark smoke.
7: Smoke cloud settles onto floor baseline.
8: Last frame: only 4 small fading gambit sparks (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

### Quick Guard

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Quick Guard | TYPE: Fighting / Status
SUBJECT: an instantaneous priority barrier intercepting and blocking all incoming priority moves.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFF1F2 #FECDD3 #FB7185 #E11D48 #9F1239 #4C0519
Background: flat solid #FF00FF, nothing else in it.

STYLE: Terraria shield parry / buckler look: instantaneous hexagonal force buckler.

FRAME BY FRAME:
1: Hexagonal crimson buckler barrier snaps into existence instantaneously in front of center.
2: Barrier border glints with pure white energy seams (#FFF1F2).
3: Impact block: incoming priority strike hits buckler, deflecting with a sharp white spark.
4: Peak guard: buckler pulses with an impenetrable defensive flash, protecting allies.
5: Buckler plates loosen as the priority threat is neutralized.
6: Buckler begins dissolving into drifting red energy flakes.
7: Energy flakes dissolve into air.
8: Last frame: only 3 small fading guard pixels (1px each, #4C0519).

Do not include characters, text, UI, borders or shadows.
```

## Fada (fundo verde #00FF00)

### Play Rough

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Play Rough | TYPE: Fairy
SUBJECT: an aggressive cartoon brawl dust cloud filled with tumbling limbs, comic stars, and cute hearts.

DURATION & FRAME RULES:
- Explosões: 8–14 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.
Every art pixel is exactly 1 pixel (no mixed pixel sizes).

PALETTE (use only these colors, flat fills, no gradients, no glow):
#FFFFFF #FDF2F8 #F472B6 #EC4899 #9D174D #500724
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria cartoon brawl dust cloud aesthetic: tumbling comic fight cloud, hearts and stars.

FRAME BY FRAME:
1: Target tackled into a round cartoon dust cloud at center, pink heart icon pops at top.
2: Dust cloud expands and churns violently; a fist and kicking boot poke out of cloud.
3: Violent brawl: cloud erupts with comic impact stars (#FFFFFF) and angry exclamation marks.
4: Peak roughhousing: dust cloud reaches maximum volume (60px wide); 3 pink hearts burst out.
5: Cloud begins to settle; target knocked flat on floor baseline with spinning dizzy stars.
6: Dust cloud thins into transparent pink smoke puffs.
7: Floating hearts pop into sparkling stardust motes.
8: Last frame: only 3 small fading fairy dust pixels (1px each, #500724).

Do not include characters, text, UI, borders or shadows.
```

### Misty Terrain

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Misty Terrain | TYPE: Fairy / Status
SUBJECT: blanketing the battlefield in a protective enchanted pink mist protecting from all status conditions.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FDF2F8 #F472B6 #EC4899 #9D174D #500724
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria fairy forest / enchanted fog look: swirling ground mist, protective star runes.

FRAME BY FRAME:
1: Horizontal wave of sparkling pink mist sweeps along ground baseline from left.
2: Mist billows upward into soft fluffy clouds, blanket rising to knee height across screen.
3: Peak terrain: ground covered in an opaque enchanted fog; 4 sparkling fairy stars glint (#FFFFFF).
4: Protective ward activates: status immunity aura pulses across floor with circular ripples.
5: Mist curls gracefully in slow rolling currents along the baseline.
6: Fairy stars rise slowly from the fog, hovering like fireflies.
7: Terrain stabilizes into a steady enchanted field.
8: Last frame: only 3 small floating mist sparkle pixels (1px each, #500724).

Do not include characters, text, UI, borders or shadows.
```

## Psíquico (fundo verde #00FF00)

### Dream Eater

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Dream Eater | TYPE: Psychic
SUBJECT: a shadowy psychic specter devouring sleeping target's dreams and restoring massive HP.

DURATION & FRAME RULES:
- Impactos pequenos: 5–8 frames.

SHEET TYPE: IMPACT ONE-SHOT, 8 frames

CANVAS: 512x256 px total, grid 4 columns x 2 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria dream siphon / Baku phantom look: gaping ethereal dream jaw, flowing vitality orbs.

FRAME BY FRAME:
1: Ethereal tapir phantom snout materializing over sleeping target, inhaling deep purple dream vapor.
2: Target's dream orb (luminous pink sphere) is pulled out of target head by mental suction.
3: Snout opens jaw, biting into the dream orb with a bright white-pink flash (#FFFFFF).
4: Peak feast: dream orb shatters in mouth; 4 glowing vitality droplets siphon back toward user.
5: Vitality droplets absorb into user with revitalizing cross sparkles (HP +50%).
6: Phantom snout dissolves into transparent pink dream fog.
7: Dream fog ascends toward ceiling.
8: Last frame: only 3 small fading dream motes (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

### Future Sight

```text
Pixel-art VFX sprite sheet, original design, for a Terraria-style 2D game.

ATTACK: Future Sight | TYPE: Psychic
SUBJECT: a delayed cosmic omen opening a celestial rift 2 turns later to rain devastating light.

DURATION & FRAME RULES:
- Golpes especiais: 15–24 frames.
- Explosões: 8–14 frames.

SHEET TYPE: SPECIAL MOVE, 16 frames (4 presságio cósmico, 6 ruptura dimensional, 6 impacto temporal)

CANVAS: 512x512 px total, grid 4 columns x 4 rows, each frame 128x128 px,
effect centered on the frame center, same scale in every frame.

PALETTE:
#FFFFFF #FFE4F2 #F472B6 #DB2777 #831843 #4A044E
Background: flat solid #00FF00, nothing else in it.

STYLE: Terraria Lunar Flare / Nebula Pillar endgame strike aesthetic: cosmic rift, colossal starlight pillar.

FRAME BY FRAME:
1: Floating cosmic eye glyph materializes high above target, ticking like a clock hand.
2: Clock hand hits zero: eye glyph flares blinding white (#FFFFFF); sky cracks open.
3: Dimensional fissure tears open: massive cosmic rift (40px wide) glowing with deep magenta nebula fire.
4: Cataclysmic strike: colossal vertical laser pillar of concentrated future starlight spears straight down.
5: Impact collision: pillar slams into target center with an apocalyptic full-frame white explosion.
6: Radial nova: massive 100px circular shockwave of cosmic stardust explodes outward in 360 degrees.
7: Shockwave hoop tears outward; central impact crater hollows out into pure light.
8: Laser pillar cuts off, leaving an ionized vertical after-image streak.
9: Heavy shower of cosmic stardust cascades down over the entire battlefield.
10: Ground crater crackles with residual temporal distortion rings.
11: Cosmic stardust begins ascending with gentle buoyant drift.
12: Dimensional rift overhead snaps shut with a bright star flash.
13: Temporal distortion rings settle into baseline.
14: Ambient stardust thins out into transparent pink clusters.
15: 4 tiny drifting future motes remaining.
16: Last frame: only 3 small fading stardust pixels (1px each, #4A044E).

Do not include characters, text, UI, borders or shadows.
```

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

