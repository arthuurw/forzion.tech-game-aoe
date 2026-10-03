namespace Forzion.Simulation;

/// <summary>
/// Finds the way a unit walks between two Cells: A* over the grid. Only free Cells can be
/// walked on; units do not block one another.
/// </summary>
/// <remarks>
/// <para>
/// A step goes to one of the eight Cells around. A diagonal step also needs the two Cells it
/// passes between to be free, so a unit never cuts the corner of a blocked Cell and can reach
/// exactly the Cells that steps between Cells sharing a side reach.
/// </para>
/// <para>
/// Everything is integer arithmetic and ties are broken by Cell index, so the same map and
/// the same two Cells always give the same path (ADR 0002).
/// </para>
/// </remarks>
internal static class Pathfinder
{
    // Cost of a step, in tenths of a Cell side: 14 approximates the diagonal, 10 times the
    // square root of 2.
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    private static readonly (int X, int Y)[] Steps =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1),
    ];

    /// <summary>
    /// The Cells to walk through, in order, from <paramref name="start"/> (not included) to
    /// <paramref name="destination"/> (included). When the destination cannot be reached, the
    /// path ends on the reachable Cell nearest to it instead. Empty when that leaves the unit
    /// where it is. Both Cells must be inside the map.
    /// </summary>
    public static List<CellPosition> FindPath(MapState map, CellPosition start, CellPosition destination)
    {
        var width = map.Width;
        var startIndex = (start.Y * width) + start.X;
        var destinationIndex = (destination.Y * width) + destination.X;
        var costs = new int[width * map.Height];
        var previous = new int[costs.Length];
        var closed = new bool[costs.Length];

        // Ordered by estimated total cost, then by estimated cost still to go, then by Cell
        // index: a total order, so the Cell taken next never depends on how the queue
        // settles ties.
        var open = new PriorityQueue<int, (int Total, int ToGo, int Index)>();

        Array.Fill(costs, int.MaxValue);
        costs[startIndex] = 0;
        open.Enqueue(startIndex, (Estimate(start, destination), Estimate(start, destination), startIndex));

        while (open.TryDequeue(out var index, out _))
        {
            if (closed[index])
            {
                continue;
            }

            closed[index] = true;

            if (index == destinationIndex)
            {
                break;
            }

            var cell = new CellPosition(index % width, index / width);

            foreach (var (stepX, stepY) in Steps)
            {
                var next = new CellPosition(cell.X + stepX, cell.Y + stepY);
                var diagonal = stepX != 0 && stepY != 0;

                if (!IsFree(map, next)
                    || (diagonal
                        && !(IsFree(map, new CellPosition(next.X, cell.Y)) && IsFree(map, new CellPosition(cell.X, next.Y)))))
                {
                    continue;
                }

                var nextIndex = (next.Y * width) + next.X;
                var cost = costs[index] + (diagonal ? DiagonalCost : StraightCost);

                if (!closed[nextIndex] && cost < costs[nextIndex])
                {
                    var toGo = Estimate(next, destination);

                    costs[nextIndex] = cost;
                    previous[nextIndex] = index;
                    open.Enqueue(nextIndex, (cost + toGo, toGo, nextIndex));
                }
            }
        }

        // The search ended either on the destination or with every Cell that can be reached
        // closed, and then the walk goes to the nearest of those.
        var target = closed[destinationIndex] ? destinationIndex : NearestClosed(closed, costs, width, destination);
        var path = new List<CellPosition>();

        for (var index = target; index != startIndex; index = previous[index])
        {
            path.Add(new CellPosition(index % width, index / width));
        }

        path.Reverse();

        return path;
    }

    /// <summary>
    /// The closed Cell nearest to the destination in a straight line. Between Cells equally
    /// near, the one with the shortest way to it and then the one with the lowest index.
    /// </summary>
    private static int NearestClosed(bool[] closed, int[] costs, int width, CellPosition destination)
    {
        var nearest = -1;
        var nearestDistance = long.MaxValue;

        for (var index = 0; index < closed.Length; index++)
        {
            if (!closed[index])
            {
                continue;
            }

            long x = (index % width) - destination.X;
            long y = (index / width) - destination.Y;
            var distance = (x * x) + (y * y);

            if (distance < nearestDistance || (distance == nearestDistance && costs[index] < costs[nearest]))
            {
                nearest = index;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static bool IsFree(MapState map, CellPosition cell) =>
        map.Contains(cell) && map[cell] == CellKind.Free;

    /// <summary>
    /// Cost of the way between the two Cells on an empty map: diagonal steps while both
    /// coordinates differ, straight ones after. It never overestimates and never drops by more
    /// than the cost of a step, so a Cell's cost is final the first time it leaves the queue.
    /// </summary>
    private static int Estimate(CellPosition from, CellPosition to)
    {
        var x = Math.Abs(from.X - to.X);
        var y = Math.Abs(from.Y - to.Y);

        return (DiagonalCost * Math.Min(x, y)) + (StraightCost * Math.Abs(x - y));
    }
}
