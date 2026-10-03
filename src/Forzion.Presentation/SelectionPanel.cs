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

        var playerState = state.Players.FirstOrDefault(each => each.Id == player)
            ?? throw new ArgumentException($"The match has no Player {player.Value}.", nameof(player));
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

        return new SelectionPanel(
            units.Select(unit => new SelectedUnit(unit.Id, TextKeys.NameOf(faction, unit.Kind), unit.HitPoints, unit.MaxHitPoints)).ToList(),
            null,
            buildingChoices);
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
                return faction.Ages[later - 1].NameKey;
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
public sealed record SelectedBuilding(EntityId Id, BuildingKind Kind, string NameKey, int HitPoints, int MaxHitPoints);
