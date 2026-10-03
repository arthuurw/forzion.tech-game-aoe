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
    /// <paramref name="destination"/> (included). Empty when the destination is the start or
    /// cannot be reached. Both Cells must be inside the map.
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

        var path = new List<CellPosition>();

        if (closed[destinationIndex])
        {
            for (var index = destinationIndex; index != startIndex; index = previous[index])
            {
                path.Add(new CellPosition(index % width, index / width));
            }

            path.Reverse();
        }

        return path;
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
