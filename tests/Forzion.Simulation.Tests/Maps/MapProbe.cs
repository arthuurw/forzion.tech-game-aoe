namespace Forzion.Simulation.Tests.Maps;

/// <summary>Questions the map tests ask of a match's public state.</summary>
internal static class MapProbe
{
    public static IEnumerable<CellPosition> AllCells(MapState map)
    {
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                yield return new CellPosition(x, y);
            }
        }
    }

    /// <summary>The Cell a half-turn around the centre of the map takes <paramref name="cell"/> to.</summary>
    public static CellPosition Mirror(MapState map, CellPosition cell) =>
        new(map.Width - 1 - cell.X, map.Height - 1 - cell.Y);

    /// <summary>Whether the two Cells share a side.</summary>
    public static bool AreNeighbours(CellPosition a, CellPosition b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    public static IEnumerable<CellPosition> Footprint(BuildingState building)
    {
        for (var y = building.Origin.Y; y < building.Origin.Y + building.Height; y++)
        {
            for (var x = building.Origin.X; x < building.Origin.X + building.Width; x++)
            {
                yield return new CellPosition(x, y);
            }
        }
    }
}
