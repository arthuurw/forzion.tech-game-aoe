namespace Forzion.Simulation;

/// <summary>
/// Moves every walking unit along its path by what its speed covers in one tick. A unit walks
/// in a straight line to the centre of the next Cell of its path and stops on the centre of
/// the last one.
/// </summary>
internal sealed class MovementSystem : ISystem
{
    /// <summary>
    /// Sends the unit walking to the given Cell, replacing whatever path it had. This is how
    /// any rule makes a unit walk; the unit then moves a little every tick until
    /// <see cref="UnitState.IsMoving"/> turns false.
    /// </summary>
    public static void WalkTo(MapState map, UnitState unit, CellPosition destination) =>
        Follow(unit, Pathfinder.FindPath(map, unit.Position.Cell, destination));

    /// <summary>
    /// Sends the unit walking to the Cell with the shortest way to it among those for which
    /// <paramref name="isGoal"/> holds, as <see cref="Pathfinder.FindPathToNearest"/> picks it.
    /// When none can be reached, the unit stays where it is.
    /// </summary>
    public static void WalkToNearest(MapState map, UnitState unit, Func<CellPosition, bool> isGoal) =>
        Follow(unit, Pathfinder.FindPathToNearest(map, unit.Position.Cell, isGoal));

    private static void Follow(UnitState unit, List<CellPosition> path)
    {
        var start = unit.Position.Cell;

        // A unit caught between two Cell centres and with nowhere to go settles on the centre
        // of the Cell it is in.
        if (path.Count == 0 && unit.Position != MapPosition.CentreOf(start))
        {
            path.Add(start);
        }

        unit.SetPath(path);
    }

    /// <summary>
    /// Sends every walking unit whose path crosses a Cell that is no longer free on a new way.
    /// A Villager walking up to its source, carrying its load or walking up to a construction
    /// site chooses again where it walks, as it did when it set out; any other unit walks to
    /// the last Cell of its path. A path is found once, when a walk starts, so without this a
    /// unit would walk through a building placed on its way after it set out.
    /// </summary>
    public static void Reroute(MatchState state)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.IsMoving || IsStillWalkable(state.Map, unit.Position.Cell, unit.Path))
            {
                continue;
            }

            // The Cell nearest the old destination need not be beside the source, a drop-off
            // point or the site, and a Villager that stops away from them stands idle.
            if (unit.GatherPhase == GatherPhase.ToSource)
            {
                GatherSystem.WalkUpToSource(state, unit);
            }
            else if (unit.GatherPhase == GatherPhase.ToDropOffPoint)
            {
                GatherSystem.CarryToDropOffPoint(state, unit);
            }
            else if (unit.ConstructionSite is { } site)
            {
                ConstructionSystem.WalkUpTo(state.Map, unit, state.FindBuilding(site)!);
            }
            else
            {
                WalkTo(state.Map, unit, unit.Path[^1]);
            }
        }
    }

    public void Run(TickContext context)
    {
        foreach (var unit in context.State.Units)
        {
            if (unit.IsMoving)
            {
                Advance(unit);
            }
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

    private static void Advance(UnitState unit)
    {
        var position = unit.Position;
        var remaining = Balance.Of(unit.Kind).Speed / Fix64.FromInt(Match.TicksPerSecond);

        // What is left of the tick's distance after reaching a Cell centre is spent towards
        // the next one, so a unit covers the same distance every tick.
        while (unit.IsMoving)
        {
            var waypoint = MapPosition.CentreOf(unit.Path[0]);
            var deltaX = waypoint.X - position.X;
            var deltaY = waypoint.Y - position.Y;
            var distance = Fix64.Hypot(deltaX, deltaY);

            if (distance > remaining)
            {
                position = new MapPosition(
                    position.X + (deltaX * remaining / distance),
                    position.Y + (deltaY * remaining / distance));

                break;
            }

            position = waypoint;
            remaining -= distance;
            unit.ReachWaypoint();
        }

        unit.Position = position;
    }
}
