using Pokerraria.Content.NPCs.Trainers;
using Terraria.Localization;

namespace Pokerraria.Content.LoveStory;

/// <summary>
///     The LoveStory journal: the diary pages of the chapter, in the order they're meant to be found, and the steps of
///     the chapter that the journal shows as "what happened" and "where to go next".
/// </summary>
public static class Journal
{
    /// <summary>
    ///     Every diary page, in story order. The text of each one is in Mods.Pokerraria.LoveStory.Pages.(id).
    /// </summary>
    public static readonly string[] Pages = ["Prologue", "Brock", "Misty"];

    /// <summary>
    ///     A step of the chapter: done when <see cref="Done" /> says so. Texts in Mods.Pokerraria.LoveStory.Steps.(key).
    /// </summary>
    public sealed record Step(string Key, Func<Player, bool> Done);

    /// <summary>
    ///     Chapter 1, from the start to Misty's Gym. The two meetings with Aurora come in here once she exists.
    /// </summary>
    public static readonly Step[] Chapter1 =
    [
        new("Starter", p => p.Pokerraria().HasChosenStarter),
        new("Brock", p => p.GetModPlayer<GymPlayer>().Badges.Contains("Brock")),
        new("Misty", p => p.GetModPlayer<GymPlayer>().Badges.Contains("Misty"))
    ];

    private const string Key = "Mods.Pokerraria.LoveStory.";

    public static string Text(string key, params object[] args) => Language.GetTextValue(Key + key, args);

    public static LocalizedText Label(string key) => Language.GetText(Key + key);

    public static LocalizedText PageTitle(string id) => Language.GetText($"{Key}Pages.{id}.Title");

    public static string PageText(string id) => Language.GetTextValue($"{Key}Pages.{id}.Text");

    /// <summary>
    ///     The first step of the chapter not done yet, or null when the whole chapter is done.
    /// </summary>
    public static Step NextStep(Player player) => Chapter1.FirstOrDefault(s => !s.Done(player));
}
