# Changelog

Versions of **Pokerraria: LoveStory**, by Welsete. Based on [Terramon](https://github.com/JamzOJamz/Terramon) 0.1.3.1 (GPL-3.0).

## Next

- New entrance to Brock's Gym (new worlds): a rocky hill with its layers of stone, clay, sandstone and granite showing, and a fossil dig in its face where the skeleton of a huge ancient creature lies in the rock. A mine tunnel at ground level, shored with timber, with a minecart track and lanterns, leads into the hill to the rope down. Brock's Gym is also no longer built hundreds of tiles deep.

- Fixed: entering a world with Brock's dungeon could crash the game (a stack overflow when the dungeon's traps were reset).

- Fixed: the rope down to Brock's Gym could start on a floating island and go through the sky down to the caves. It now always starts on the ground.

- Brock's Gym dungeon (new worlds): between the rope and the hall, ten rooms of old stone, about 270 tiles long. A tall dark hall with three identical levers (only one opens the stone gate, the other two drop a boulder from high up in the dark); a spike pit crossed on pillars; a dart gallery; a tall corridor with hidden boulders; a false floor that opens over spikes; a lava pit crossed on pillars one block wide; a deep well with darts in both walls down to a dark gallery with lava; a huge sunken dart hall where you run from shelter to shelter between volleys; and a generous chest (the diary page "Stone by stone", a random evolution stone, Ultra Balls, Potions, Repels, a Hard Stone, a Focus Sash, Revives, Rare Candies, Exp. Candies, berries, Nuggets, a Copper Relic, Healing and Obsidian Skin potions, a Compass and rope), which brings boulders down from the ceiling when opened. The dungeon's dart traps fire on their own on a timer. Cave bats (and hellbats near the lava) swarm inside. The dungeon starts over whenever nobody is left inside (gate and false floor closed, levers off, boulders back), so dying means doing it all again; a switch behind the gate opens it on the way back. In older worlds the page turns up in Brock's hall, and Misty's page in her Gym.
- New item: Repel, a spray that keeps wild Pokémon away for 3 minutes and halves other creature spawns (crafted with a Bottle, Deathweed and Gel at a Placed Bottle).

- Two-turn moves now show their charging turn: Fly and Bounce rise in a gust, Dig throws up dirt and opens a hole, Dive sends up a water spout, Phantom Force and Shadow Force sink into ghostly smoke, and Skull Bash, Sky Attack, Razor Wind, Solar Beam, Solar Blade, Meteor Beam, Geomancy, Electro Shot, Freeze Shock and Ice Burn gather energy on the user. In the test panel these moves play their charging turn first.

- LoveStory journal (J), drawn as an open book: the contents list the diary pages with a progress bar, where to go next in chapter 1 and what already happened. Each page opens with its own picture (a black silhouette until it is found) and its text, with arrows to turn the pages. A new character starts with the first page (the prologue) in the bag.
- Diary pages: using one reads it, keeps it in the journal and starts Story time (no enemies or wild Pokémon spawn for a minute, and while the journal is open). The Brock and Misty pages will go in the Gym dungeons; for now they come from the test panel (League tab: "Give diary pages", "Reset journal").

## 0.4.0

- Test panel: "Teleport on map" (right click anywhere on the full screen map to go there, landing above ground), and in Debug Mode the map marks the Gym and League pedestals.
- 8 more drawn move animations, the first of Water, Electric and Grass: Water Gun, Hydro Pump, Bubble Beam, Aqua Jet, Thunder Shock, Thunderbolt, Thunder Punch and Razor Leaf (43 in total).
- Shadow Ball gets an animation drawn pixel by pixel in code, in the Ghost palette (44 in total).
- Night Slash: a test animation built with the codeart2d tool from an effect description (45 in total).
- 232 basic moves get code-drawn animations in their type's colors: punches, kicks, slashes, claws, simple projectiles, sound and psychic waves, powders and gases, buffs, heals and status effects (277 in total).
- New Solar Beam, drawn in code: sunlight gathers on the user with orbiting leaves, the beam has a white core with vines of light spiralling around it, and it bursts into light and scattered leaves on the target.
- Hyper Beam, Psybeam, Charge Beam, Flash Cannon, Thunder and Dazzling Gleam drawn in code in the same style: a charge on the user, a beam with its own texture (shock rings, psychic rings, crackling bolts, light chevrons), a bolt falling from the sky, a ring of starlight (283 in total).
- Will-O-Wisp (three ghost flames that fly over, circle the target and sink in), Twister (a dragon-colored tornado with spinning debris) and Rock Slide (boulders crashing down in dust) drawn in code (286 in total).
- Swords Dance shows six swords spinning around the user, Dragon Dance an energy dragon spiralling up, Dragon Claw a dragon head behind the claws. Punches, kicks, Dragon Rush and Dragon Pulse keep simple impacts until their generated art arrives.
- Dragon Rush: a dragon skull of blue fire flies at the target and bursts (generated art).
- Generated art for Dragon Pulse, Axe Kick, Brick Break, Close Combat, Double Kick, Meteor Mash, Rage Fist, Shadow Punch, Stomping Tantrum and Sucker Punch.
- Generated art for Drain Punch, Dynamic Punch, Focus Punch, Force Palm, Hammer Arm, Low Kick, Low Sweep and Rock Smash.
- Generated art for Mach Punch (the boxer in the sheet cut out) and High Jump Kick (with a kicking silhouette).
- Generated art for Mega Kick, Mega Punch, Stomp and Superpower (first batch made in Antigravity). No move uses a provisional code drawing anymore.
- Generated art for Aqua Tail, Dive, Waterfall, Muddy Water (recolored to silt brown), Surf, Wave Crash, Whirlpool, Rain Dance, Petal Blizzard (recolored to pink petals) and Power Whip.
- Ice Beam redrawn in code: ice crystals growing out of the beam's edges.
- Twister and Rock Slide now sit on the target instead of below it.
- Test panel, Moves tab: search moves by name (click the box and type; right click clears). The rest of the moves are listed in VFX.md for the image generator.
- Every drawn move animation is cut out again with a cleaner background keyer: outlines and small sparks that were lost now show.
- 17 more drawn move animations: Blaze Kick, Eruption, Fire Fang, Fire Punch, Fire Spin, Flame Charge, Flame Wheel, Flare Blitz, Heat Crash, Heat Wave, Incinerate, Inferno, Lava Plume, Overheat, Sunny Day, Mist and Snowscape (35 in total).
- Nurse Joy's healing machine on the counter beside her: your Poké Balls go into its slots one by one and glow while the healing jingle plays.
- New Pokémon Center: two floors under a red roof with a giant Poké Ball, Nurse Joy at the counter downstairs and a room with four PCs upstairs. New worlds get it near spawn; older ones can build it from the League tab of the test panel.
- Buildings designed with the Blueprint Builder mod can use this mod's items, and PCs placed that way work.

## 0.3.0

- New logo and icon: a Poké Ball pocket watch with moving hands, in the menu, the mod list and on GitHub. Author shown as Welsete everywhere.
- 18 moves now have drawn animations (Ember, Flamethrower, Solar Beam, Ice Beam, Blizzard, Sheer Cold, Aurora Beam, Icy Wind, Ice Punch, Ice Fang, Ice Shard, Icicle Crash, Icicle Spear, Freeze-Dry, Powder Snow, Haze, Bullet Punch, Fire Blast).
- Beam moves travel from the attacker to the target, and charge moves (Solar Beam) show the charge before firing.
- Multi-hit moves animate every hit, and HP only drops when each hit lands.
- Rain in battle now really rains.
- Pokédex type filter fixed.
- README rewritten as a full guide with screenshots, story teaser and roadmap.

## 0.2.0

First version with its own number. Everything added on top of Terramon so far:

- Turn-based battles (Pokémon Showdown) against wild Pokémon, trainers and Gym Leaders, with paced battle text, weather, terrain, side conditions, stat stages and Disable shown on screen.
- Kanto League tied to Terraria's bosses: Brock's Gym (temple entrance, traps, smarter AI), the other leaders, Elite Four and Champion.
- Level cap from the character's badges (15 up to 62, no limit for Champions); Pokémon above it may disobey.
- Pokémon Center next to spawn with Nurse Joy, Chansey and Blissey; safe zone where wild Pokémon stay weak; PCs only in valid houses or the Center.
- Mystery Dungeon style animated Pokémon, Summary screen, Pokédex with filters, held items, animated evolution with music.
- Move sprites (Fire Blast first), F8 test panel.
- New name, animated menu logo, icon and in-game watermark; links to the original Terramon removed.
