using System.Globalization;
using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Keys of the HUD's own texts, and how the HUD writes costs, progress and the notices of
/// Ages reached. The words, and the order and punctuation that join them, live in the game's
/// translations; a key that takes arguments has a <c>{0}</c>, <c>{1}</c> and so on in its text.
/// </summary>
public static class HudTexts
{
    /// <summary>Label of the population against the population limit.</summary>
    public const string Population = "HUD_POPULATION";

    /// <summary>Label of the button that orders the Age Advance; <c>{0}</c> is the name of the next Age.</summary>
    public const string AdvanceTo = "HUD_ADVANCE_TO";

    /// <summary>Label of an Age Advance underway; <c>{0}</c> is the name of the Age it leads to.</summary>
    public const string AdvancingTo = "HUD_ADVANCING_TO";

    /// <summary>Why a choice is unavailable; <c>{0}</c> is the name of the Age that unlocks it.</summary>
    public const string LockedUntil = "HUD_LOCKED_UNTIL";

    /// <summary>Heading of the buildings the selected Villagers can place.</summary>
    public const string Build = "HUD_BUILD";

    /// <summary>Heading of the units the selected building trains.</summary>
    public const string Train = "HUD_TRAIN";

    /// <summary>Heading of the training queue.</summary>
    public const string TrainingQueue = "HUD_TRAINING_QUEUE";

    /// <summary>How to cancel a unit of the training queue.</summary>
    public const string CancelTrainingHint = "HUD_CANCEL_TRAINING_HINT";

    /// <summary>Label of the construction progress of a construction site.</summary>
    public const string Construction = "HUD_CONSTRUCTION";

    /// <summary>Label of hit points.</summary>
    public const string HitPoints = "HUD_HIT_POINTS";

    /// <summary>How to set the rally point of the selected building.</summary>
    public const string RallyPointHint = "HUD_RALLY_POINT_HINT";

    /// <summary>How to place the chosen building; <c>{0}</c> is its name.</summary>
    public const string PlacementHint = "HUD_PLACEMENT_HINT";

    /// <summary>How many units are selected; <c>{0}</c> is the count.</summary>
    public const string SelectedUnits = "HUD_SELECTED_UNITS";

    /// <summary>The cost of something that costs nothing.</summary>
    public const string Free = "HUD_FREE";

    /// <summary>The notice of an Age reached; <c>{0}</c> is its name.</summary>
    public const string AgeReached = "HUD_AGE_REACHED";

    /// <summary>A value under its label, such as an amount of a Resource; <c>{0}</c> is the label and <c>{1}</c> the value.</summary>
    public const string Labelled = "HUD_LABELLED";

    /// <summary>The population against the population limit; <c>{0}</c> is the population and <c>{1}</c> the limit.</summary>
    public const string PopulationOfLimit = "HUD_POPULATION_OF_LIMIT";

    /// <summary>The Faction and the Age the Player is in; <c>{0}</c> is the name of the Faction and <c>{1}</c> of the Age.</summary>
    public const string FactionAndAge = "HUD_FACTION_AND_AGE";

    /// <summary>Hit points against the hit points when whole; <c>{0}</c> is the hit points left and <c>{1}</c> the whole.</summary>
    public const string HitPointsOfMax = "HUD_HIT_POINTS_OF_MAX";

    /// <summary>A whole percentage; <c>{0}</c> is the number, from 0 to 100.</summary>
    public const string Percentage = "HUD_PERCENTAGE";

    /// <summary>The amount of one Resource in a cost; <c>{0}</c> is the amount and <c>{1}</c> the name of the Resource.</summary>
    public const string CostAmount = "HUD_COST_AMOUNT";

    /// <summary>What separates the amounts of the Resources of a cost.</summary>
    public const string CostSeparator = "HUD_COST_SEPARATOR";

    /// <summary>
    /// The text of <paramref name="key"/> in the language shown, with its <c>{0}</c>,
    /// <c>{1}</c> and so on replaced by the arguments. Numbers are written without the
    /// separators of any culture.
    /// </summary>
    /// <param name="translate">Gives the text of a key in the language shown.</param>
    /// <param name="key">The key of the text.</param>
    /// <param name="arguments">What replaces <c>{0}</c>, <c>{1}</c> and so on, in that order.</param>
    public static string Format(Func<string, string> translate, string key, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(translate);

        return string.Format(CultureInfo.InvariantCulture, translate(key), arguments);
    }

    /// <summary>
    /// A cost as the amount of each Resource it takes, in the order Food, Wood, Gold, leaving
    /// out the Resources it does not take, such as <c>60 Alimento, 20 Ouro</c>.
    /// </summary>
    /// <param name="cost">The cost.</param>
    /// <param name="translate">Gives the text of a key in the language shown.</param>
    public static string CostText(Cost cost, Func<string, string> translate)
    {
        ArgumentNullException.ThrowIfNull(translate);

        var parts = Enum.GetValues<ResourceKind>()
            .Where(kind => cost.AmountOf(kind) > 0)
            .Select(kind => Format(translate, CostAmount, cost.AmountOf(kind), translate(TextKeys.NameOf(kind))))
            .ToList();

        return parts.Count == 0 ? translate(Free) : string.Join(translate(CostSeparator), parts);
    }

    /// <summary>
    /// A share from 0 to 1 as a whole percentage, such as <c>40%</c>, rounded down so that
    /// 100% shows only once the work is done.
    /// </summary>
    /// <param name="fraction">The share.</param>
    /// <param name="translate">Gives the text of a key in the language shown.</param>
    public static string Percent(double fraction, Func<string, string> translate) =>
        Format(translate, Percentage, (int)Math.Floor(Math.Clamp(fraction, 0, 1) * 100));

    /// <summary>
    /// The keys of the names of the Ages <paramref name="player"/> reached in
    /// <paramref name="events"/>, in the order reached, for the notices that announce them.
    /// Other Players' Ages are not announced.
    /// </summary>
    /// <exception cref="ArgumentException">The match has no such Player.</exception>
    public static IEnumerable<string> AgesReachedBy(IEnumerable<MatchEvent> events, MatchState state, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(state);

        var faction = PlayerLookup.Find(state, player).Faction;

        return events
            .OfType<AgeAdvanced>()
            .Where(advance => advance.Player == player)
            .Select(advance => faction.AgeAt(advance.Age).NameKey);
    }
}
