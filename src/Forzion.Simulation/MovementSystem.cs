namespace Forzion.Simulation;

/// <summary>
/// Moves every walking unit along its path by what its speed covers in one tick. A unit walks
/// in a straight line to the centre of the next Cell of its path and stops on the centre of
/// the last one. A unit that stops has its job, if it has one, choose its way again.
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
    /// When none can be reached, the unit walks instead to the Cell it can reach that is
    /// nearest in a straight line to <paramref name="destination"/>, as <see cref="WalkTo"/>
    /// picks it. This is how a job sends a unit up to where it is done: short of it, the unit
    /// still goes as near as it can.
    /// </summary>
    public static void WalkToNearestOrTowards(MapState map, UnitState unit, Func<CellPosition, bool> isGoal, CellPosition destination)
    {
        var start = unit.Position.Cell;
        var path = Pathfinder.FindPathToNearest(map, start, isGoal);

        // An empty path leaves a unit already on a goal where it is; otherwise no goal can be reached.
        if (path.Count == 0 && !isGoal(start))
        {
            path = Pathfinder.FindPath(map, start, destination);
        }

        Follow(unit, path);
    }

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

    public void Run(TickContext context)
    {
        foreach (var unit in context.State.Units)
        {
            if (!unit.IsMoving)
            {
                continue;
            }

            Advance(unit);

            if (!unit.IsMoving)
            {
                Rerouting.AfterStopping(context.State, unit);
            }
        }
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
