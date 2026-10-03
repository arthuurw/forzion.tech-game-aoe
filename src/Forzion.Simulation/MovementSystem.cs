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
    public static void WalkTo(MapState map, UnitState unit, CellPosition destination)
    {
        var start = unit.Position.Cell;
        var path = Pathfinder.FindPath(map, start, destination);

        // A unit caught between two Cell centres and with nowhere to go settles on the centre
        // of the Cell it is in.
        if (path.Count == 0 && unit.Position != MapPosition.CentreOf(start))
        {
            path.Add(start);
        }

        unit.SetPath(path);
    }

    /// <summary>
    /// Sends every walking unit whose path crosses a Cell that is no longer free on a new way
    /// to the last Cell of its path. A path is found once, when a walk starts, so without this
    /// a unit would walk through a building placed on its way after it set out.
    /// </summary>
    public static void Reroute(MatchState state)
    {
        foreach (var unit in state.Units)
        {
            if (unit.IsMoving && !IsStillWalkable(state.Map, unit.Position.Cell, unit.Path))
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
    /// <see cref="Pathfinder"/> could still take: onto a free Cell and, when diagonal, between
    /// two free Cells.
    /// </summary>
    private static bool IsStillWalkable(MapState map, CellPosition from, IReadOnlyList<CellPosition> path)
    {
        var previous = from;

        foreach (var cell in path)
        {
            var diagonal = cell.X != previous.X && cell.Y != previous.Y;

            if (map[cell] != CellKind.Free
                || (diagonal
                    && (map[new CellPosition(cell.X, previous.Y)] != CellKind.Free
                        || map[new CellPosition(previous.X, cell.Y)] != CellKind.Free)))
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
        var remaining = Balance.Speed(unit.Kind) / Fix64.FromInt(Match.TicksPerSecond);

        // What is left of the tick's distance after reaching a Cell centre is spent towards
        // the next one, so a unit covers the same distance every tick.
        while (unit.IsMoving)
        {
            var waypoint = MapPosition.CentreOf(unit.Path[0]);
            var x = waypoint.X - position.X;
            var y = waypoint.Y - position.Y;
            var distance = Fix64.Hypot(x, y);

            if (distance > remaining)
            {
                position = new MapPosition(
                    position.X + (x * remaining / distance),
                    position.Y + (y * remaining / distance));

                break;
            }

            position = waypoint;
            remaining -= distance;
            unit.ReachWaypoint();
        }

        unit.Position = position;
    }
}
