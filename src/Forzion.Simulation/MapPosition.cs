namespace Forzion.Simulation;

/// <summary>
/// A point on the map, in Cells: the Cell (x, y) spans from (x, y) to (x + 1, y + 1) and its
/// centre is (x + 0.5, y + 0.5).
/// </summary>
public readonly record struct MapPosition(Fix64 X, Fix64 Y)
{
    /// <summary>The Cell the point is in.</summary>
    public CellPosition Cell => new(X.FloorToInt(), Y.FloorToInt());

    /// <summary>The centre of the given Cell.</summary>
    public static MapPosition CentreOf(CellPosition cell)
    {
        var half = Fix64.One / Fix64.FromInt(2);

        return new MapPosition(Fix64.FromInt(cell.X) + half, Fix64.FromInt(cell.Y) + half);
    }
}
