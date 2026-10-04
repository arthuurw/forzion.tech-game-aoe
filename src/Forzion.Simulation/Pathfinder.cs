namespace Forzion.Simulation;

/// <summary>
/// Finds the way a unit walks between two Cells, A* over the grid, or to the nearest of a set
/// of Cells, Dijkstra over the grid. Only free Cells can be walked on; units do not block one
/// another.
/// </summary>
/// <remarks>
/// <para>
/// A step goes to one of the eight Cells around. A diagonal step also needs the two Cells it
/// passes between to be free, so a unit never cuts the corner of a blocked Cell and can reach
/// exactly the Cells that steps between Cells sharing a side reach.
/// </para>
/// <para>
/// Everything is integer arithmetic and ties are broken by Cell index, so the same map and
/// the same Cells always give the same path (ADR 0002).
/// </para>
/// </remarks>
internal static class Pathfinder
{
    // Cost of a step, in tenths of a Cell side: 14 approximates the diagonal, 10 times the
    // square root of 2.
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    /// <summary>
    /// The Cells to walk through, in order, from <paramref name="start"/> (not included) to
    /// <paramref name="destination"/> (included). When the destination cannot be reached, the
    /// path ends on the reachable Cell nearest to it instead. Empty when that leaves the unit
    /// where it is. Both Cells must be inside the map.
    /// </summary>
    public static List<CellPosition> FindPath(MapState map, CellPosition start, CellPosition destination)
    {
        var destinationIndex = map.IndexOf(destination);
        var search = Search(map, start, index => index == destinationIndex, cell => Estimate(cell, destination));

        // The search ended either on the destination or with every Cell that can be reached
        // closed, and then the walk goes to the nearest of those.
        var target = search.Goal ?? NearestClosed(map, search, destination);

        return search.PathTo(map, target);
    }

    /// <summary>
    /// The Cells to walk through, in order, from <paramref name="start"/> (not included) to
    /// the goal Cell with the shortest way to it (included): a goal is a Cell for which
    /// <paramref name="isGoal"/> holds. Between goals equally far, the one with the lowest
    /// Cell index. Empty when <paramref name="start"/> is itself a goal or when no goal can be
    /// reached, so the unit stays where it is.
    /// </summary>
    public static List<CellPosition> FindPathToNearest(MapState map, CellPosition start, Func<CellPosition, bool> isGoal)
    {
        // No estimate guides the search, the goals may lie anywhere: the A* is then Dijkstra's
        // search and the goal taken first is the nearest one.
        var search = Search(map, start, index => isGoal(map.CellAt(index)), _ => 0);

        return search.Goal is { } goal ? search.PathTo(map, goal) : [];
    }

    /// <summary>
    /// Whether a unit on <paramref name="cell"/> can take the step: the Cell it leads to is
    /// free and, for a diagonal step, so are the two Cells it passes between.
    /// </summary>
    public static bool CanStep(MapState map, CellPosition cell, CellStep step)
    {
        var next = step.From(cell);

        return map.IsFree(next)
            && (!step.IsDiagonal
                || (map.IsFree(new CellPosition(next.X, cell.Y)) && map.IsFree(new CellPosition(cell.X, next.Y))));
    }

    /// <summary>
    /// A* from <paramref name="start"/> until it closes a Cell <paramref name="isGoal"/> holds
    /// for, or until it has closed every Cell that can be reached. <paramref name="estimate"/>
    /// must never overestimate the cost still to go.
    /// </summary>
    private static SearchResult Search(MapState map, CellPosition start, Func<int, bool> isGoal, Func<CellPosition, int> estimate)
    {
        var startIndex = map.IndexOf(start);
        var costs = new int[map.CellCount];
        var previous = new int[costs.Length];
        var closed = new bool[costs.Length];
        int? goal = null;

        // Ordered by estimated total cost, then by estimated cost still to go, then by Cell
        // index: a total order, so the Cell taken next never depends on how the queue
        // settles ties.
        var open = new PriorityQueue<int, (int Total, int ToGo, int Index)>();

        Array.Fill(costs, int.MaxValue);
        costs[startIndex] = 0;
        open.Enqueue(startIndex, (estimate(start), estimate(start), startIndex));

        while (open.TryDequeue(out var index, out _))
        {
            if (closed[index])
            {
                continue;
            }

            closed[index] = true;

            if (isGoal(index))
            {
                goal = index;

                break;
            }

            var cell = map.CellAt(index);

            foreach (var step in CellStep.All)
            {
                if (!CanStep(map, cell, step))
                {
                    continue;
                }

                var next = step.From(cell);
                var nextIndex = map.IndexOf(next);
                var cost = costs[index] + (step.IsDiagonal ? DiagonalCost : StraightCost);

                if (!closed[nextIndex] && cost < costs[nextIndex])
                {
                    var toGo = estimate(next);

                    costs[nextIndex] = cost;
                    previous[nextIndex] = index;
                    open.Enqueue(nextIndex, (cost + toGo, toGo, nextIndex));
                }
            }
        }

        return new SearchResult(startIndex, costs, previous, closed, goal);
    }

    /// <summary>
    /// The closed Cell nearest to the destination in a straight line. Between Cells equally
    /// near, the one with the shortest way to it and then the one with the lowest index.
    /// </summary>
    private static int NearestClosed(MapState map, SearchResult search, CellPosition destination)
    {
        var nearest = -1;
        var nearestDistance = int.MaxValue;

        for (var index = 0; index < search.Closed.Length; index++)
        {
            if (!search.Closed[index])
            {
                continue;
            }

            var distance = map.CellAt(index).SquaredDistanceTo(destination);

            if (distance < nearestDistance || (distance == nearestDistance && search.Costs[index] < search.Costs[nearest]))
            {
                nearest = index;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Cost of the way between the two Cells on an empty map: diagonal steps while both
    /// coordinates differ, straight ones after. It never overestimates and never drops by more
    /// than the cost of a step, so a Cell's cost is final the first time it leaves the queue.
    /// </summary>
    private static int Estimate(CellPosition from, CellPosition to)
    {
        var deltaX = Math.Abs(from.X - to.X);
        var deltaY = Math.Abs(from.Y - to.Y);

        return (DiagonalCost * Math.Min(deltaX, deltaY)) + (StraightCost * Math.Abs(deltaX - deltaY));
    }

    /// <summary>What a search found: the cost of the way to every Cell it reached, the Cell each was reached from, which it closed, and the goal it stopped on, if any.</summary>
    private sealed record SearchResult(int StartIndex, int[] Costs, int[] Previous, bool[] Closed, int? Goal)
    {
        /// <summary>The Cells from the start (not included) to the target (included), following the way the search reached it.</summary>
        public List<CellPosition> PathTo(MapState map, int target)
        {
            var path = new List<CellPosition>();

            for (var index = target; index != StartIndex; index = Previous[index])
            {
                path.Add(map.CellAt(index));
            }

            path.Reverse();

            return path;
        }
    }
}
