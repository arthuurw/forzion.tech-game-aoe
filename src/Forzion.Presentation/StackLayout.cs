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

    /// <summary>How many units are drawn side by side in a row: three rows span a Cell.</summary>
    private const int RowLength = 3;

    /// <summary>
    /// Fills <paramref name="places"/> with how far from where it stands each unit standing
    /// still on a Cell with others is drawn: in rows of up to <see cref="RowLength"/> along the
    /// map's X, <see cref="Spacing"/> apart and centred on the Cell, in the order of
    /// <paramref name="units"/>. The camera looks along the map's Y, so units side by side never
    /// hide one another; every other row is shifted half a step. Units alone on their Cell or
    /// walking get no place.
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
            var members = stack.ToList();
            var rows = (members.Count + RowLength - 1) / RowLength;

            for (var index = 0; index < members.Count; index++)
            {
                var row = index / RowLength;
                var inRow = Math.Min(RowLength, members.Count - (row * RowLength));
                var stagger = row % 2 == 1 ? Spacing / 2 : 0;

                places[members[index].Id] = new MapPoint(
                    ((index % RowLength) - ((inRow - 1) / 2.0)) * Spacing + stagger,
                    (row - ((rows - 1) / 2.0)) * Spacing);
            }
        }
    }
}
