namespace Forzion.Simulation;

/// <summary>
/// Sends units on a new way when the map changes under them. A path is found once, when a
/// walk starts, so without this a unit would walk through a building placed on its way after
/// it set out. Each job chooses again where its units walk, as it did when they set out; this
/// only asks it to.
/// </summary>
internal static class Rerouting
{
    /// <summary>
    /// Sends every walking unit whose path crosses a Cell that is no longer free on a new way.
    /// A unit with no job that walks its way walks to the last Cell of its path.
    /// </summary>
    public static void AfterBlocking(MatchState state)
    {
        foreach (var unit in state.Units)
        {
            if (unit.IsMoving && !IsStillWalkable(state.Map, unit.Position.Cell, unit.Path))
            {
                ChooseWayAgain(state, unit);
            }
        }
    }

    /// <summary>
    /// Sends every Villager standing still with a job, among them those waiting for a way to
    /// their source, a drop-off point or their site, to choose its way again: Cells have just
    /// been freed, and a way may have opened. One already where its job takes it stays.
    /// </summary>
    public static void AfterFreeing(MatchState state)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.IsMoving)
            {
                _ = GatherSystem.ChooseWayAgain(state, unit) || ConstructionSystem.ChooseWayAgain(state, unit);
            }
        }
    }

    // The Cell nearest the old destination need not be beside the source, a drop-off point or
    // the site, and a Villager that stops away from them does not do its job: its job chooses.
    private static void ChooseWayAgain(MatchState state, UnitState unit)
    {
        if (!GatherSystem.ChooseWayAgain(state, unit) && !ConstructionSystem.ChooseWayAgain(state, unit))
        {
            MovementSystem.WalkTo(state.Map, unit, unit.Path[^1]);
        }
    }

    /// <summary>
    /// Whether every step of the path, from <paramref name="from"/> on, is one the
    /// <see cref="Pathfinder"/> could still take.
    /// </summary>
    private static bool IsStillWalkable(MapState map, CellPosition from, IReadOnlyList<CellPosition> path)
    {
        var previous = from;

        foreach (var cell in path)
        {
            // A unit already past the border of its next Cell is in it: no step is left there.
            if (cell != previous && !Pathfinder.CanStep(map, previous, new CellStep(cell.X - previous.X, cell.Y - previous.Y)))
            {
                return false;
            }

            previous = cell;
        }

        return true;
    }
}
