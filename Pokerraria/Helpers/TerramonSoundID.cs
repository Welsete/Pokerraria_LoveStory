using Terraria.Audio;

namespace Pokerraria.Helpers;

public static class TerramonSoundID
{
    private const string CryPath = "Pokerraria/Sounds/Cries/";
    public static readonly SoundStyle PkballConsume = new("Pokerraria/Sounds/pkball_consume") { Volume = 0.35f };
    public static readonly SoundStyle PkballThrow = new("Pokerraria/Sounds/pkball_throw") { Volume = 0.8f };
    public static readonly SoundStyle PkballBounce = new("Pokerraria/Sounds/pkball_bounce") { Volume = 0.75f };
    /// <summary>
    ///     The evolution theme (about 22 s). The evolution animation is timed to it.
    /// </summary>
    public static readonly SoundStyle EvolutionTheme = new("Pokerraria/Sounds/evolution_theme") { Volume = 0.8f };

    // Twinkle played when a shiny Pokémon is near or enters a battle
    public static readonly SoundStyle ShinySparkle = new("Pokerraria/Sounds/shiny_sparkle") { Volume = 0.6f };

    public static readonly SoundStyle PkballCatchPla = new("Pokerraria/Sounds/pkball_catch_pla") { Volume = 0.75f };
    public static readonly SoundStyle PkmnRecall = new("Pokerraria/Sounds/pkmn_recall") { Volume = 0.35f };
    public static readonly SoundStyle ButtonSmm = new("Pokerraria/Sounds/button_smm") { Pitch = 0.6f, Volume = 0.2925f };
    public static readonly SoundStyle ButtonLocked = new("Pokerraria/Sounds/button_locked") { Volume = 0.25f };
    public static readonly SoundStyle CatchClick = new("Pokerraria/Sounds/ls_catch_click");
    public static readonly SoundStyle CatchFail = new("Pokerraria/Sounds/ls_catch_fail");
    public static readonly SnVariants CatchWobble = new("Pokerraria/Sounds/ls_catch_wobble", numVariants: 3);
    // Nurse Joy's healing jingle (about 2.5 s); the healing machine's glow is timed to it
    public static readonly SoundStyle PokeCenterHeal = new("Pokerraria/Sounds/pokecenter_heal") { Volume = 0.6f };
    public static readonly SoundStyle PCOn = new("Pokerraria/Sounds/ls_pc_on") { Volume = 0.54f };
    public static readonly SoundStyle PCOff = new("Pokerraria/Sounds/ls_pc_off") { Volume = 0.54f };
    public static readonly SoundStyle DexOpen = new("Pokerraria/Sounds/dex_open") { Volume = 0.48f };
    public static readonly SoundStyle DexClose = new("Pokerraria/Sounds/dex_close") { Volume = 0.48f };
    public static readonly SoundStyle DexPageUp = new("Pokerraria/Sounds/dex_pageup") { Volume = 0.325f };

    public static readonly SoundStyle BattlePing = new("Pokerraria/Sounds/battle_tb_ping")
        { Volume = 0.3f, MaxInstances = 0 };

    public static readonly SoundStyle BattlePingEmpty = new("Pokerraria/Sounds/battle_tb_empty")
        { Volume = 0.3f, MaxInstances = 0 };

    public static readonly SoundStyle BattleStart = new("Pokerraria/Sounds/battle_tb_start") { Volume = 0.3f };
    public static readonly SoundStyle BattleDecide = new("Pokerraria/Sounds/battle_decide") { Volume = 0.3f };
    public static readonly SoundStyle BattleCancel = new("Pokerraria/Sounds/battle_cancel") { Volume = 0.3f };
    public static readonly SoundStyle BattleRun = new("Pokerraria/Sounds/battle_run") { Volume = 0.3f };

    public static readonly SoundStyle HitNormalDamage = new("Pokerraria/Sounds/hit_normal_damage")
        { Volume = 0.165f, PitchVariance = 0.12f };

    public static readonly SoundStyle RealtimeEXPGain = new("Pokerraria/Sounds/realtime_exp_gain")
        { Volume = 0.5f, PitchRange = (-0.1f, 0.1f) };

    public static SoundStyle GetCry(ushort id, float volume = 0.15f) =>
        Pokerraria.DatabaseV2.GetPokemon(id).GetCry(volume);

    public static SoundStyle GetCry(this DatabaseV2.PokemonSchema schema, float volume = 0.15f) =>
        Cry(schema.Identifier, volume);

    public static SoundStyle GetCry(this PokemonData data, float volume = 0.15f) =>
        Cry(data.InternalName, volume);

    /// <summary>
    ///     A Pokémon's cry. Pokémon that don't have one yet (like the ones past Gen 1 being tried out) borrow
    ///     Machoke's, pitched up, instead of crashing the game.
    /// </summary>
    private static SoundStyle Cry(string name, float volume) =>
        ModContent.HasAsset(CryPath + name)
            ? new SoundStyle(CryPath + name) { Volume = volume }
            : new SoundStyle(CryPath + "Machoke") { Volume = volume, Pitch = 0.35f };
}

public readonly record struct SnVariants(SoundStyle Child)
{
    public SnVariants(string path, int numVariants) : this(Child: new SoundStyle(path, numVariants))
    {
    }

    public SoundStyle this[int i] => Child with { SelectedVariant = i };
}