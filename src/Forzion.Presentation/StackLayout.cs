using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Where the units standing still on one Cell are drawn. Units do not block one another, so
/// several may stand on a Cell; drawn apart, each stays visible and can be clicked.
/// </summary>
internal static class StackLayout
{
    /// <summary>How far apart, in Cells, the units of a stack are drawn: as wide as two of their placeholders.</summary>
    private const double Spacing = 0.5;

    // Rows run along the map's X, alternately three places and two between them, so that a
    // camera looking along the map's Y sees each unit of a row between two of the next. The
    // middle place comes first, so that a short row stays on the Cell's centre line.
    private static readonly double[] WideRow = [0, -Spacing, Spacing];
    private static readonly double[] NarrowRow = [-Spacing / 2, Spacing / 2];

    /// <summary>
    /// Fills <paramref name="places"/> with how far from where it stands each unit standing
    /// still on a Cell with others is drawn. Each stack fills its rows in the order of
    /// <paramref name="units"/>, the rows <see cref="Spacing"/> apart and centred on the Cell.
    /// Units alone on their Cell or walking get no place. Up to eight units fit on the Cell;
    /// more spill past its edges.
    /// </summary>
    public static void Lay(IReadOnlyList<UnitState> units, Dictionary<EntityId, MapPoint> places)
    {
        places.Clear();

        var stacks = units
            .Where(unit => !unit.IsMoving)
            .GroupBy(unit => unit.Position.Cell)
            .Where(stack => stack.Count() > 1);

        foreach (var stack in stacks)
        {
            var seats = new List<(EntityId Unit, int Row, double X)>();
            var row = 0;
            var place = 0;

            foreach (var unit in stack)
            {
                var rowPlaces = row % 2 == 0 ? WideRow : NarrowRow;

                seats.Add((unit.Id, row, rowPlaces[place]));

                if (++place == rowPlaces.Length)
                {
                    row++;
                    place = 0;
                }
            }

            var middleRow = seats[^1].Row / 2.0;

            foreach (var (unit, seatRow, x) in seats)
            {
                places[unit] = new MapPoint(x, (seatRow - middleRow) * Spacing);
            }
        }
    }
}
