using System.Text;
using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What the selection panel of the HUD shows for the selected entities: the units with their
/// hit points, or the building with its construction, training queue and the orders it takes,
/// and the buildings the selected Villagers can place. It decides no rule (ADR 0001): what is
/// locked comes from the Faction, costs from the match, and whether an order is valid is for
/// the match to say when it applies it.
/// </summary>
/// <param name="Units">The selected units of the Player, in ascending ID order.</param>
/// <param name="Building">The selected building of the Player, or null when none is selected.</param>
/// <param name="BuildingChoices">
/// The buildings the selected Villagers can be sent to place, in ascending kind order; empty
/// while no Villager is selected.
/// </param>
public sealed record SelectionPanel(
    IReadOnlyList<SelectedUnit> Units,
    SelectedBuilding? Building,
    IReadOnlyList<BuildingChoice> BuildingChoices)
{
    /// <summary>
    /// The panel for <paramref name="selected"/> in the current tick of the match. Entities no
    /// longer in the match, or of another Player, are left out.
    /// </summary>
    /// <exception cref="ArgumentException">The match has no such Player.</exception>
    public static SelectionPanel For(MatchState state, PlayerId player, IReadOnlyList<EntityId> selected)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(selected);

        var playerState = PlayerLookup.Find(state, player);
        var faction = playerState.Faction;

        var units = state.Units
            .Where(unit => unit.Owner == player && selected.Contains(unit.Id))
            .ToList();

        var buildingChoices = units.Any(unit => unit.Kind == UnitKind.Villager)
            ? PlacedBuildings(faction).Select(kind => new BuildingChoice(
                kind,
                TextKeys.NameOf(kind),
                Match.BuildingCost(kind),
                LockedUntil(faction, age => faction.Unlocks(kind, age), playerState.Age))).ToList()
            : [];

        var building = state.Buildings.FirstOrDefault(each => each.Owner == player && selected.Contains(each.Id));

        return new SelectionPanel(
            units.Select(unit => new SelectedUnit(unit.Id, TextKeys.NameOf(faction, unit.Kind), unit.HitPoints, unit.MaxHitPoints)).ToList(),
            building is null ? null : Show(building, playerState),
            buildingChoices);
    }

    /// <summary>
    /// What decides the controls of the panel: the entities shown, the choices offered and
    /// whether each is locked, the units queued and whether an Age Advance is underway. Two
    /// panels of the same layout show the same controls, so the HUD rebuilds its panel only
    /// when the layout changes; progress and hit points change without changing it.
    /// </summary>
    public string Layout
    {
        get
        {
            var layout = new StringBuilder();
            layout.Append(string.Join(',', Units.Select(unit => unit.Id.Value))).Append('|');
            layout.Append(string.Join(',', BuildingChoices.Select(choice => $"{choice.Kind}:{choice.IsLocked}"))).Append('|');

            if (Building is { } building)
            {
                layout.Append(building.Id.Value).Append(':').Append(building.ConstructionProgress is null).Append('|');
                layout.Append(string.Join(',', building.TrainingQueue.Select(queued => queued.Kind))).Append('|');
                layout.Append(string.Join(',', building.UnitChoices.Select(choice => $"{choice.Kind}:{choice.IsLocked}"))).Append('|');
                layout.Append(building.AgeAdvance?.AgeNameKey).Append(':').Append(building.AgeAdvance?.IsUnderway);
            }

            return layout.ToString();
        }
    }

    private static SelectedBuilding Show(BuildingState building, PlayerState player)
    {
        var faction = player.Faction;
        var queue = building.TrainingQueue
            .Select((kind, position) => new QueuedUnit(
                kind,
                TextKeys.NameOf(faction, kind),
                position == 0 ? Fractions.Of(building.TrainingProgress, Match.TrainTime(kind)) : 0))
            .ToList();

        // A construction site takes no order but to be built, so it offers none.
        var unitChoices = building.IsComplete
            ? Enum.GetValues<UnitKind>()
                .Where(kind => Match.TrainedAt(kind) == building.Kind && faction.Ages.Any(age => age.Units.Contains(kind)))
                .Select(kind => new UnitChoice(
                    kind,
                    TextKeys.NameOf(faction, kind),
                    Match.UnitCost(kind),
                    LockedUntil(faction, age => faction.Unlocks(kind, age), player.Age)))
                .ToList()
            : [];

        return new SelectedBuilding(
            building.Id,
            building.Kind,
            TextKeys.NameOf(building.Kind),
            building.HitPoints,
            building.MaxHitPoints,
            building.IsComplete ? null : Fractions.Of(building.BuildProgress, building.BuildTime),
            queue,
            unitChoices,
            AgeAdvanceOf(building, player),
            building.RallyPoint);
    }

    /// <summary>The Age Advance the building offers: the Town Center's to the next Age, while there is one.</summary>
    private static AgeAdvanceChoice? AgeAdvanceOf(BuildingState building, PlayerState player)
    {
        // The Town Center makes the Age Advance (see the glossary); the match refuses it elsewhere.
        if (building.Kind != BuildingKind.TownCenter || !building.IsComplete || player.NextAge is not { } next)
        {
            return null;
        }

        return new AgeAdvanceChoice(next.NameKey, next.AdvanceCost, AgeAdvanceProgress.Of(building, player)?.Progress);
    }

    /// <summary>Every kind of building some Age of the Faction unlocks, in ascending kind order.</summary>
    private static IEnumerable<BuildingKind> PlacedBuildings(Faction faction) =>
        faction.Ages.SelectMany(age => age.Buildings).Distinct().Order();

    /// <summary>
    /// The key of the name of the first Age of the Faction that unlocks something, or null when
    /// <paramref name="age"/> has already unlocked it.
    /// </summary>
    private static string? LockedUntil(Faction faction, Func<int, bool> unlockedIn, int age)
    {
        if (unlockedIn(age))
        {
            return null;
        }

        for (var later = age + 1; later <= faction.Ages.Count; later++)
        {
            if (unlockedIn(later))
            {
                return faction.AgeAt(later).NameKey;
            }
        }

        return null;
    }
}

/// <summary>A selected unit, as the panel shows it.</summary>
/// <param name="Id">The unit.</param>
/// <param name="NameKey">Key of the text naming its kind in its Player's Faction.</param>
/// <param name="HitPoints">Hit points it has left.</param>
/// <param name="MaxHitPoints">Hit points it has when whole.</param>
public sealed record SelectedUnit(EntityId Id, string NameKey, int HitPoints, int MaxHitPoints);

/// <summary>A kind of building the selected Villagers can be sent to place.</summary>
/// <param name="Kind">The kind of building.</param>
/// <param name="NameKey">Key of the text naming it.</param>
/// <param name="Cost">What placing it costs.</param>
/// <param name="LockedUntilAgeNameKey">
/// Key of the name of the Age that unlocks it, while the Player's Age has not; null once unlocked.
/// </param>
public sealed record BuildingChoice(BuildingKind Kind, string NameKey, Cost Cost, string? LockedUntilAgeNameKey)
{
    /// <summary>Whether the Player's Age has not unlocked it yet: the match would refuse to place it.</summary>
    public bool IsLocked => LockedUntilAgeNameKey is not null;
}

/// <summary>The selected building, as the panel shows it.</summary>
/// <param name="Id">The building.</param>
/// <param name="Kind">Its kind.</param>
/// <param name="NameKey">Key of the text naming its kind.</param>
/// <param name="HitPoints">Hit points it has left.</param>
/// <param name="MaxHitPoints">Hit points it has when whole.</param>
/// <param name="ConstructionProgress">
/// How far its construction has gone, from 0 towards 1, while it is a construction site; null once complete.
/// </param>
/// <param name="TrainingQueue">The units it is to train, the one in training first.</param>
/// <param name="UnitChoices">The kinds of unit it trains, in ascending kind order; empty for a construction site.</param>
/// <param name="AgeAdvance">The Age Advance it offers, or null when it offers none.</param>
/// <param name="RallyPoint">The Cell the units it trains walk to, or null when it has none.</param>
public sealed record SelectedBuilding(
    EntityId Id,
    BuildingKind Kind,
    string NameKey,
    int HitPoints,
    int MaxHitPoints,
    double? ConstructionProgress,
    IReadOnlyList<QueuedUnit> TrainingQueue,
    IReadOnlyList<UnitChoice> UnitChoices,
    AgeAdvanceChoice? AgeAdvance,
    CellPosition? RallyPoint);

/// <summary>A unit in a training queue.</summary>
/// <param name="Kind">The kind of unit.</param>
/// <param name="NameKey">Key of the text naming it in the Player's Faction.</param>
/// <param name="Progress">How far it has trained, from 0 towards 1. Only the first unit of a queue trains; the others stay at 0.</param>
public sealed record QueuedUnit(UnitKind Kind, string NameKey, double Progress);

/// <summary>A kind of unit the selected building trains.</summary>
/// <param name="Kind">The kind of unit.</param>
/// <param name="NameKey">Key of the text naming it in the Player's Faction.</param>
/// <param name="Cost">What training it costs.</param>
/// <param name="LockedUntilAgeNameKey">
/// Key of the name of the Age that unlocks it, while the Player's Age has not; null once unlocked.
/// </param>
public sealed record UnitChoice(UnitKind Kind, string NameKey, Cost Cost, string? LockedUntilAgeNameKey)
{
    /// <summary>Whether the Player's Age has not unlocked it yet: the match would refuse to train it.</summary>
    public bool IsLocked => LockedUntilAgeNameKey is not null;
}

/// <summary>The Age Advance the selected Town Center offers.</summary>
/// <param name="AgeNameKey">Key of the text naming the Age it leads to.</param>
/// <param name="Cost">What it costs, paid in full when ordered.</param>
/// <param name="Progress">How far it has gone, from 0 towards 1, while underway; null while not ordered.</param>
public sealed record AgeAdvanceChoice(string AgeNameKey, Cost Cost, double? Progress)
{
    /// <summary>Whether the Age Advance has been ordered and is underway.</summary>
    public bool IsUnderway => Progress is not null;
}
