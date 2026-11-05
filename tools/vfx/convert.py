"""Turns the generated move sheets (magenta background) into the frame strips MoveVisuals reads."""
import sys
import numpy as np
from PIL import Image

SRC, OUT = (sys.argv[1], sys.argv[2]) if len(sys.argv) > 2 else (None, None)

# file, move, layout (cols, rows) or list of boxes, downscale, settings for the .txt
SHEETS = [
    ("aurora_beam_vfx_spritesheet_v3_1792871463131.jpg", "AuroraBeam", (4, 2), 3, dict(Travel=2, Ticks=5, Face=1)),
    ("blizzard_vfx_spritesheet_v2_1792871597489.jpg", "Blizzard", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("Bullet Punch.png", "BulletPunch",
     [(0, 0, 85, 85), (85, 0, 170, 85), (170, 0, 256, 85), (0, 85, 128, 170), (128, 85, 256, 170),
      (0, 185, 66, 256), (66, 185, 128, 256), (128, 185, 192, 256)], 1, dict(Travel=0, Ticks=3, Face=1)),
    ("ember_vfx_spritesheet_1792854135105.jpg", "Ember", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("flamethrower_vfx_spritesheet_final_1792855053226.jpg", "Flamethrower",
     [(40, 70, 280, 280), (350, 70, 640, 280), (690, 70, 1010, 280), (1030, 70, 1345, 280),
      (40, 300, 630, 560), (690, 340, 1030, 500), (1040, 360, 1185, 490), (1190, 410, 1340, 540),
      (40, 600, 280, 720), (280, 580, 440, 720), (450, 570, 620, 750), (690, 570, 860, 750),
      (910, 570, 1110, 750), (1190, 600, 1345, 720)], 3, dict(Travel=0, Ticks=4, Face=1, At="middle")),
    ("freeze_dry_vfx_spritesheet_1792871714552.jpg", "FreezeDry", (4, 2), 3, dict(Travel=1, Ticks=5)),
    ("haze_vfx_spritesheet_1792876842170.jpg", "Haze", (4, 2), 3, dict(Travel=0, Ticks=6)),
    ("ice_fang_vfx_spritesheet_1792856044803.jpg", "IceFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_punch_vfx_spritesheet_1792856062413.jpg", "IcePunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("ice_shard_vfx_spritesheet_1792859337124.jpg", "IceShard", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("icicle_crash_vfx_spritesheet_1792871266443.jpg", "IcicleCrash", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("icicle_spear_vfx_spritesheet_1792871321463.jpg", "IcicleSpear", (4, 2), 3, dict(Travel=1, Ticks=4, Face=1)),
    ("icy_wind_vfx_spritesheet_1792876366324.jpg", "IcyWind", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1)),
    ("powder_snow_vfx_spritesheet_v2_1792876584939.jpg", "PowderSnow", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("sheer_cold_vfx_spritesheet_1792876713818.jpg", "SheerCold", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("blaze_kick_vfx_spritesheet_1792889557537.jpg", "BlazeKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("eruption_vfx_spritesheet_1792890820208.jpg", "Eruption", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("fire_fang_vfx_spritesheet_1792889724669.jpg", "FireFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("fire_punch_vfx_spritesheet_1792889883740.jpg", "FirePunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("fire_spin_vfx_spritesheet_1792891043474.jpg", "FireSpin", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("flame_charge_vfx_spritesheet_1792890050797.jpg", "FlameCharge", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    ("flame_wheel_vfx_spritesheet_1792890223837.jpg", "FlameWheel", (4, 2), 3, dict(Travel=7, Ticks=3)),
    ("flare_blitz_vfx_spritesheet_1792890417382.jpg", "FlareBlitz", (4, 4), 2, dict(Travel=2, Ticks=4, Face=1)),
    ("heat_crash_vfx_spritesheet_1792890615052.jpg", "HeatCrash", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("heat_wave_vfx_spritesheet_1792891266492.jpg", "HeatWave", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1)),
    ("incinerate_vfx_spritesheet_1792891503107.jpg", "Incinerate", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("inferno_vfx_spritesheet_1792907291854.jpg", "Inferno", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("lava_plume_vfx_spritesheet_1792907545465.jpg", "LavaPlume", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("mist_vfx_spritesheet_1792889252934.jpg", "Mist", (4, 2), 3, dict(Travel=0, Ticks=6)),
    ("overheat_vfx_spritesheet_1792907801494.jpg", "Overheat", (4, 4), 2, dict(Travel=0, Ticks=4)),
    ("snowscape_vfx_spritesheet_1792889390727.jpg", "Snowscape", (4, 2), 3, dict(Travel=0, Ticks=6, At="middle")),
    ("sunny_day_vfx_spritesheet_1792908068245.jpg", "SunnyDay", (4, 2), 3, dict(Travel=0, Ticks=6, At="middle")),
    # Water, Electric and Grass, first batch (2026-11-03)
    ("water_gun_vfx_spritesheet_1793721582639.jpg", "WaterGun", (4, 2), 3, dict(Travel=3, Ticks=4, Face=1)),
    ("hydro_pump_vfx_spritesheet_v3_1793721764064.jpg", "HydroPump", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("bubble_beam_spritesheet_clean_1793722444414.jpg", "BubbleBeam", (4, 2), 3, dict(Travel=3, Ticks=4, Face=1)),
    ("aqua_jet_spritesheet_clean_1793722489127.jpg", "AquaJet", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    # Darker cells with magenta grid lines and a bar at the bottom: cut inside the lines
    ("thunder_shock_vfx_spritesheet_1793722431954.jpg", "ThunderShock",
     [(2, 2, 338, 378), (350, 2, 683, 378), (695, 2, 1027, 378), (1039, 2, 1374, 378), (2, 390, 338, 724), (350, 390, 683, 724), (695, 390, 1027, 724), (1039, 390, 1374, 724)], 3, dict(Travel=2, Ticks=4, Face=1)),
    # Leaves out frame 6 (a brown scorch shaped like the bolt) and the near-empty last frame
    ("thunderbolt_vfx_spritesheet_1793721667921.jpg", "Thunderbolt",
     [(0, 0, 344, 384), (344, 0, 688, 384), (688, 0, 1032, 384), (1032, 0, 1376, 384), (0, 384, 344, 768), (688, 384, 1032, 768)], 3, dict(Travel=1, Ticks=4, Face=1)),
    ("thunder_punch_vfx_spritesheet_1793722488169.jpg", "ThunderPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("razor_leaf_vfx_spritesheet_1793721701699.jpg", "RazorLeaf", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    # Gemini, 2752x1536: dragon heads flying (6 frames loop on the way), then the front-facing skull on the target;
    # draw_themed_moves.py append_burst adds a dragon-colored burst after it
    ("dragon_rush_gemini.jpg", "DragonRush", [(688, 0, 1376, 768), (1376, 0, 2064, 768), (2064, 0, 2752, 768), (688, 768, 1376, 1536), (1376, 768, 2064, 1536), (2064, 768, 2752, 1536), (0, 768, 688, 1536)], 6, dict(Travel=6, Ticks=4, Face=1)),
    # Lote 1 do Gemini (2026-11-03): socos, chutes e Dragon Pulse
    ("lote1_dragon_pulse.png", "DragonPulse", (4, 2), 3, dict(Travel=3, Ticks=4, Face=1)),
    ("lote1_axe_kick.png", "AxeKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote1_brick_break.png", "BrickBreak", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote1_close_combat.png", "CloseCombat", (4, 4), 2, dict(Travel=0, Ticks=3, Face=1)),
    ("lote1_double_kick.png", "DoubleKick", (4, 2), 3, dict(Travel=0, Ticks=3, Face=1)),
    ("lote1_meteor_mash.png", "MeteorMash", (4, 4), 2, dict(Travel=2, Ticks=3, Face=1)),
    ("lote1_rage_fist.png", "RageFist", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote1_shadow_punch.png", "ShadowPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote1_stomping_tantrum.png", "StompingTantrum", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote1_sucker_punch.png", "SuckerPunch", (4, 2), 3, dict(Travel=0, Ticks=3, Face=1)),
    # Lote 2 do Gemini (2026-11-03)
    ("lote2_drain_punch.png", "DrainPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_dynamic_punch.png", "DynamicPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_focus_punch.png", "FocusPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_force_palm.png", "ForcePalm", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_hammer_arm.png", "HammerArm", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_low_kick.png", "LowKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_low_sweep.png", "LowSweep", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_rock_smash.png", "RockSmash", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote3_mega_kick.png", "MegaKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote3_mega_punch.png", "MegaPunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote3_stomp.png", "Stomp", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote3_superpower.png", "Superpower", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote4_aqua_tail.png", "AquaTail", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote4_dive.png", "Dive", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote4_waterfall.png", "Waterfall", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote4_muddy_water.png", "MuddyWater", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote4_petal_blizzard.png", "PetalBlizzard", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote4_power_whip.png", "PowerWhip", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote4_rain_dance.png", "RainDance", (4, 2), 3, dict(Travel=0, Ticks=6, At="middle")),
    ("lote4_surf.png", "Surf", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote4_whirlpool.png", "Whirlpool", (4, 2), 3, dict(Travel=0, Ticks=4)),
    # Wave Crash came as a 4x4 sheet of loose pieces: only the wave rising, crashing and the spray are used
    ("lote4_wave_crash.png", "WaveCrash", [(0, 0, 256, 256), (256, 0, 512, 256), (512, 0, 768, 256), (768, 0, 1024, 256), (0, 256, 256, 512), (512, 256, 768, 512), (512, 512, 768, 768), (768, 512, 1024, 768), (256, 768, 512, 1024), (512, 768, 768, 1024)], 2, dict(Travel=0, Ticks=4, Face=1)),
    ("lote5_absorb.png", "Absorb", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote5_giga_drain.png", "GigaDrain", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote5_grassy_terrain.png", "GrassyTerrain", (4, 2), 3, dict(Travel=0, Ticks=5, At="middle")),
    ("lote5_ingrain.png", "Ingrain", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote5_leaf_storm.png", "LeafStorm", (4, 4), 2, dict(Travel=0, Ticks=3, Face=1)),
    ("lote5_leech_seed.png", "LeechSeed", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote5_mega_drain.png", "MegaDrain", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote5_petal_dance.png", "PetalDance", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote5_thunder_fang.png", "ThunderFang", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote5_vine_whip.png", "VineWhip", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote6_curse.png", "Curse", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote6_destiny_bond.png", "DestinyBond", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote6_dragon_breath.png", "DragonBreath", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1, At="middle")),
    ("lote6_dragon_tail.png", "DragonTail", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote6_electric_terrain.png", "ElectricTerrain", (4, 2), 3, dict(Travel=0, Ticks=5, At="middle")),
    ("lote6_lick.png", "Lick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote6_magnetic_flux.png", "MagneticFlux", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote6_outrage.png", "Outrage", (4, 4), 2, dict(Travel=0, Ticks=3)),
    ("lote6_shadow_sneak.png", "ShadowSneak", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote6_zap_cannon.png", "ZapCannon", (4, 2), 3, dict(Travel=2, Ticks=4)),
    ("lote7_bite.png", "Bite", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote7_bone_rush.png", "BoneRush", (4, 2), 3, dict(Travel=0, Ticks=3, Face=1)),
    ("lote7_bonemerang.png", "Bonemerang", (4, 2), 3, dict(Travel=4, Ticks=3)),
    ("lote7_bulldoze.png", "Bulldoze", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote7_crunch.png", "Crunch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote7_fling.png", "Fling", (4, 2), 3, dict(Travel=1, Ticks=4, Face=1)),
    ("lote7_gyro_ball.png", "GyroBall", (4, 2), 3, dict(Travel=0, Ticks=3)),
    ("lote7_iron_tail.png", "IronTail", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote7_memento.png", "Memento", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("lote7_switcheroo.png", "Switcheroo", (4, 2), 3, dict(Travel=0, Ticks=4, At="middle")),
    # Lote 8 (Ground and Rock, 2026-11-04)
    ("lote8_ancient_power.jpg", "AncientPower", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("lote8_dig.jpg", "Dig", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote8_drill_run.jpg", "DrillRun", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    # 4x4 sheet with 14 frames: the last two cells are empty
    ("lote8_earthquake.jpg", "Earthquake", [(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)][:14], 2, dict(Travel=0, Ticks=3)),
    ("lote8_fissure.jpg", "Fissure", (4, 2), 3, dict(Travel=0, Ticks=4)),
    # Leaves out frame 7: its dust sits on a lighter, tinted block of background
    ("lote8_head_smash.jpg", "HeadSmash", [(c * 344, r * 384, c * 344 + 344, r * 384 + 384) for r in range(2) for c in range(4) if r * 4 + c != 6], 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote8_rollout.jpg", "Rollout", (4, 2), 3, dict(Travel=7, Ticks=3, Face=1)),
    ("lote8_sand_tomb.jpg", "SandTomb", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote8_spikes.jpg", "Spikes", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote8_stone_edge.jpg", "StoneEdge", (4, 2), 3, dict(Travel=0, Ticks=4)),
    # Lotes 9 a 11 (Poison, Bug, Flying, Fighting, Psychic, Fairy and a few more, 2026-11-05)
    # Bounce leaves out frame 6 and Hurricane frame 4 (drawn on a filled square); the 4x4 sheets leave out their empty last cells
    ("lote9_acid_spray.png", "AcidSpray", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("lote9_belch.png", "Belch", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1, At="middle")),
    ("lote9_bug_bite.png", "BugBite", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote9_gunk_shot.png", "GunkShot", (4, 2), 3, dict(Travel=1, Ticks=4)),
    ("lote9_poison_fang.png", "PoisonFang", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote9_sandstorm.png", "Sandstorm", (4, 2), 3, dict(Travel=0, Ticks=5, At="middle")),
    ("lote9_sludge_wave.png", "SludgeWave", [(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)], 2, dict(Travel=0, Ticks=3, Face=1)),
    ("lote9_stealth_rock.png", "StealthRock", (4, 2), 3, dict(Travel=0, Ticks=5)),
    ("lote9_toxic_spikes.png", "ToxicSpikes", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote9_wide_guard.png", "WideGuard", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote10_bounce.png", "Bounce", [(c * 344, r * 384, c * 344 + 344, r * 384 + 384) for r in range(2) for c in range(4) if r * 4 + c != 5], 3, dict(Travel=0, Ticks=4)),
    ("lote10_brave_bird.png", "BraveBird", [(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)][:14], 2, dict(Travel=0, Ticks=3, Face=1)),
    ("lote10_gust.png", "Gust", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote10_hurricane.png", "Hurricane", [b for i, b in enumerate([(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)][:14]) if i != 3], 2, dict(Travel=0, Ticks=3)),
    ("lote10_leech_life.png", "LeechLife", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote10_mirror_move.png", "MirrorMove", (4, 2), 3, dict(Travel=0, Ticks=5, At="user")),
    ("lote10_sky_attack.png", "SkyAttack", [(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)][:13], 2, dict(Travel=0, Ticks=3, Face=1)),
    ("lote10_string_shot.png", "StringShot", (4, 2), 3, dict(Travel=2, Ticks=4, Face=1)),
    ("lote10_tailwind.png", "Tailwind", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1, At="user")),
    ("lote10_u_turn.png", "UTurn", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote11_counter.png", "Counter", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote11_dream_eater.png", "DreamEater", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote11_final_gambit.png", "FinalGambit", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote11_future_sight.png", "FutureSight", [(c * 256, r * 256, c * 256 + 256, r * 256 + 256) for r in range(4) for c in range(4)][:14], 2, dict(Travel=0, Ticks=4)),
    ("lote11_misty_terrain.png", "MistyTerrain", (4, 2), 3, dict(Travel=0, Ticks=5, Face=1, At="middle")),
    ("lote11_play_rough.png", "PlayRough", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote11_quick_guard.png", "QuickGuard", (4, 2), 3, dict(Travel=0, Ticks=4, At="user")),
    ("lote11_seismic_toss.png", "SeismicToss", (4, 2), 3, dict(Travel=0, Ticks=4)),
    ("lote11_storm_throw.png", "StormThrow", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote11_vital_throw.png", "VitalThrow", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    # High Jump Kick keeps the kicking silhouette (Welsete liked it); Mach Punch came with a boxer on the left of each
    # frame (2 columns x 4 rows): only the part right of him is cut
    ("lote2_high_jump_kick.png", "HighJumpKick", (4, 2), 3, dict(Travel=0, Ticks=4, Face=1)),
    ("lote2_mach_punch.png", "MachPunch", [(295, 0, 688, 192), (860, 0, 1253, 192), (295, 192, 688, 384), (860, 192, 1253, 384), (295, 384, 688, 576), (860, 384, 1253, 576), (295, 576, 688, 768), (860, 576, 1253, 768)], 3, dict(Travel=0, Ticks=3, Face=1)),
]


# Sheets drawn on a green background instead of magenta. Purple and pink types (Ghost, Psychic, Poison, Dark, Fairy)
# must use green: the magenta key would erase their own colors (72% of a Ghost-palette sheet was lost in a test).
GREEN_KEY = {"lote1_rage_fist.png", "lote1_shadow_punch.png", "lote1_sucker_punch.png",
             "lote6_curse.png", "lote6_destiny_bond.png", "lote6_lick.png", "lote6_shadow_sneak.png", "lote7_bite.png", "lote7_crunch.png", "lote7_fling.png", "lote7_memento.png", "lote7_switcheroo.png",
             "lote9_acid_spray.png", "lote9_belch.png", "lote9_gunk_shot.png", "lote9_poison_fang.png", "lote9_sludge_wave.png", "lote9_toxic_spikes.png", "lote11_dream_eater.png", "lote11_future_sight.png", "lote11_misty_terrain.png", "lote11_play_rough.png"}


def load(path, key="magenta"):
    """Keys the background out. Uses the soft-matte keyer of agent-sprite-forge (.claude/skills/generate2dsprite) when
    it is there: in a test it kept 100% of a Ghost-palette sheet where the old keyer below lost 72%."""
    im = Image.open(path)
    # A PNG that already has transparency keeps it (old path); a flat PNG or a JPEG gets keyed
    own_alpha = im.mode in ("RGBA", "LA", "P") and np.array(im.convert("RGBA"))[..., 3].min() < 255
    forge = None if own_alpha else _forge_matte()
    if forge:
        keyed, _ = forge.key_still(im.convert("RGB"), quality="soft", key=key)
        return np.array(keyed.convert("RGBA")).astype(float)
    return _legacy_load(im, path, key)


def _forge_matte():
    import os
    here = os.path.dirname(os.path.abspath(__file__))
    scripts = os.path.join(here, "..", "..", ".claude", "skills", "generate2dsprite", "scripts")
    if os.environ.get("VFX_LEGACY_KEY") or not os.path.isdir(scripts):
        return None
    sys.path.insert(0, scripts)
    import forge_matte
    return forge_matte


def _legacy_load(im, path, key):
    if key == "green":
        a = np.array(im.convert("RGB")).astype(float)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        bg = g - np.maximum(r, b) > 60
        from scipy import ndimage
        bg = ndimage.binary_dilation(bg, iterations=1)
        return np.dstack([a, np.where(bg, 0, 255.0)])
    if path.endswith(".png"):
        a = np.array(im.convert("RGBA")).astype(float)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        a[..., 3][(r - g > 50) & (b - g > 50)] = 0  # magenta fringe left by the generator
        return a
    a = np.array(im.convert("RGB")).astype(float)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = (r - g > 60) & (b - g > 60)
    # one more pixel around the background goes too: it's the pink halo of the jpeg
    from scipy import ndimage
    bg = ndimage.binary_dilation(bg, iterations=2)
    alpha = np.where(bg, 0, 255.0)
    return np.dstack([a, alpha])


def shrink(cell, k):
    """Averages k x k blocks (only the opaque pixels count for the color), then makes the edge hard again."""
    if k == 1:
        out = cell.copy()
        out[..., 3] = np.where(out[..., 3] > 127, 255, 0)
        return out
    h, w = cell.shape[0] // k * k, cell.shape[1] // k * k
    c = cell[:h, :w].reshape(h // k, k, w // k, k, 4)
    al = c[..., 3:4] / 255
    rgb = (c[..., :3] * al).sum((1, 3)) / np.maximum(al.sum((1, 3)), 1e-6)
    cov = al.mean((1, 3))[..., 0]
    return np.dstack([rgb, np.where(cov > 0.45, 255, 0)])


def frames_of(a, layout):
    if isinstance(layout, tuple):
        cols, rows = layout
        fw, fh = a.shape[1] // cols, a.shape[0] // rows
        return [a[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw] for r in range(rows) for c in range(cols)]
    return [a[y0:y1, x0:x1] for x0, y0, x1, y1 in layout]


# Sheets that came in the wrong colors (the queue gave them the palette of only one of their types): each pixel is
# moved to the color of the same brightness in this ramp, darkest (outline) to lightest
RECOLOR = {
    "MuddyWater": ["#1C1108", "#36220F", "#52371A", "#6E4E28", "#8E6A3E", "#B48E5E"],  # silt brown
    "PetalBlizzard": ["#3E0F2A", "#7A1F4E", "#C23D7E", "#F07AAE", "#FFB8D6", "#FFF0F6"],  # flower pink
}


def recolor(strip, ramp):
    rgb = strip[..., :3].astype(float)
    lum = rgb @ np.array([0.299, 0.587, 0.114])
    on = strip[..., 3] > 0
    if not on.any():
        return strip
    # spread the brightness of this sheet over the whole ramp
    lo, hi = np.percentile(lum[on], 2), np.percentile(lum[on], 98)
    t = np.clip((lum - lo) / max(hi - lo, 1), 0, 0.999)
    cols = np.array([[int(c[i:i + 2], 16) for i in (1, 3, 5)] for c in ramp])
    out = strip.copy()
    out[..., :3][on] = cols[(t[on] * len(ramp)).astype(int)]
    return out


# Sheets that came with a flat block of ground drawn under the effect (a cream slab under Dig): big patches of that
# color are cut out, small bits of it inside the art (highlights) stay
SLAB = {"Dig": (254, 249, 225)}


def cut_slab(a, color, tol=14, min_size=1500):
    from scipy import ndimage
    near = (np.abs(a[..., :3] - np.array(color, float)).max(-1) < tol) & (a[..., 3] > 0)
    lab, n = ndimage.label(near)
    if n:
        sizes = ndimage.sum(near, lab, range(1, n + 1))
        big = np.isin(lab, np.nonzero(sizes >= min_size)[0] + 1)
        big = ndimage.binary_dilation(big, iterations=2) & (np.abs(a[..., :3] - np.array(color, float)).max(-1) < tol * 3)
        a = a.copy()
        a[..., 3][big] = 0
    return a


# Pink left by a tinted magenta background (a lighter patch behind Head Smash's dust, the line under Sand Tomb):
# "cut" drops it, a color paints it (Earthquake's last dust cloud came out pink)
DEPINK = {"HeadSmash": "cut", "SandTomb": "cut", "Fissure": "cut", "Earthquake": (222, 196, 150),
          # Glows that soaked up the magenta background (teal, rock and sand moves have no pink of their own)
          "SkyAttack": "cut", "Tailwind": "cut", "BraveBird": "cut", "StealthRock": "cut", "WideGuard": "cut",
          "Sandstorm": "cut", "BugBite": "cut"}

# Sheets with a slab of ground drawn under every frame: everything a few pixels below the ground's top goes, so the
# effect sits on the real Terraria ground instead of a block of fake one
GROUND_CUT = {"Earthquake"}


def depink(strip, how):
    out = strip.copy()
    rgb = out[..., :3].astype(int)
    pink = (rgb[..., 0] - rgb[..., 1] > 25) & (rgb[..., 2] - rgb[..., 1] > 12) & (out[..., 3] > 0)
    if how == "cut":
        out[..., 3][pink] = 0
    else:
        out[..., :3][pink] = how
    return out


def cut_ground(strip, frames):
    w = strip.shape[1] // frames
    last = strip[:, (frames - 1) * w:, 3] > 0
    rows = np.nonzero(last.mean(1) > 0.8)[0]
    if len(rows):
        strip = strip.copy()
        strip[rows[0] + 3:, :, 3] = 0
    return strip


def despill(a):
    """Green left in the art by a green background (smoke or chains blended with it): no pixel may be greener than its
    red or blue, so those parts turn back to gray or purple. Only for GREEN_KEY sheets (purple and dark types)."""
    a = a.copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    a[..., 1] = np.minimum(g, np.maximum(r, b))
    return a


def main():
    only = set(sys.argv[3].split(",")) if len(sys.argv) > 3 else None
    for file, move, layout, k, settings in SHEETS:
        if only and move not in only:
            continue
        a = load(f"{SRC}/{file}", "green" if file in GREEN_KEY else "magenta")
        if file in GREEN_KEY:
            a = despill(a)
        if move in SLAB:
            a = cut_slab(a, SLAB[move])
        fr = [shrink(f, k) for f in frames_of(a, layout)]
        w = max(f.shape[1] for f in fr)
        h = max(f.shape[0] for f in fr)
        w += w % 2
        h += h % 2
        strip = np.zeros((h, w * len(fr), 4), np.uint8)
        for i, f in enumerate(fr):
            y, x = (h - f.shape[0]) // 2, (w - f.shape[1]) // 2
            strip[y:y + f.shape[0], i * w + x:i * w + x + f.shape[1]] = f.clip(0, 255).astype(np.uint8)
        if move in RECOLOR:
            strip = recolor(strip, RECOLOR[move])
        if move in DEPINK:
            strip = depink(strip, DEPINK[move])
        if move in GROUND_CUT:
            strip = cut_ground(strip, len(fr))
        Image.fromarray(strip, "RGBA").save(f"{OUT}/{move}.png", optimize=True)
        lines = [f"Frames {len(fr)}"] + [f"{key} {val}" for key, val in settings.items()] + ["Scale 1"]
        open(f"{OUT}/{move}.txt", "w").write("\n".join(lines) + "\n")
        print(move, len(fr), (w, h))


if __name__ == "__main__":
    main()
