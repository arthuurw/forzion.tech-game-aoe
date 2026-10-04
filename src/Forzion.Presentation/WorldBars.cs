using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>What a bar drawn over an entity on the map measures.</summary>
public enum WorldBarKind
{
    /// <summary>The share of its hit points the entity has left.</summary>
    HitPoints = 0,

    /// <summary>How far the construction of a construction site has gone.</summary>
    Construction = 1,
}

/// <summary>A bar drawn over an entity on the map.</summary>
/// <param name="Entity">The unit or building it is drawn over.</param>
/// <param name="Kind">What it measures.</param>
/// <param name="Fill">How full it is, from 0 to 1.</param>
public sealed record WorldBar(EntityId Entity, WorldBarKind Kind, double Fill);

/// <summary>
/// Which bars to draw over the entities on the map: hit points over the selected units and
/// buildings and over the wounded ones of every Player, so the person can follow a fight, and
/// construction over every construction site.
/// </summary>
public static class WorldBars
{
    /// <summary>
    /// The bars of the current tick: units first, then buildings, each in ascending ID order. A
    /// wounded or selected construction site gets both bars.
    /// </summary>
    /// <param name="state">The state of the match.</param>
    /// <param name="selected">The selected entities; those no longer in the match are skipped.</param>
    public static IEnumerable<WorldBar> Of(MatchState state, IReadOnlyCollection<EntityId> selected)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(selected);

        foreach (var unit in state.Units.Where(unit => unit.HitPoints < unit.MaxHitPoints || selected.Contains(unit.Id)))
        {
            yield return new WorldBar(unit.Id, WorldBarKind.HitPoints, Fractions.Of(unit.HitPoints, unit.MaxHitPoints));
        }

        foreach (var building in state.Buildings)
        {
            if (building.HitPoints < building.MaxHitPoints || selected.Contains(building.Id))
            {
                yield return new WorldBar(building.Id, WorldBarKind.HitPoints, Fractions.Of(building.HitPoints, building.MaxHitPoints));
            }

            if (!building.IsComplete)
            {
                yield return new WorldBar(building.Id, WorldBarKind.Construction, Fractions.Of(building.BuildProgress, building.BuildTime));
            }
        }
    }
}
