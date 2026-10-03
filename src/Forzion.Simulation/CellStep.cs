namespace Forzion.Simulation;

/// <summary>A step from a Cell to one of the eight Cells around it, as a change of column and row.</summary>
internal readonly record struct CellStep(int DeltaX, int DeltaY)
{
    /// <summary>The steps to the four Cells that share a side with the Cell.</summary>
    public static readonly CellStep[] Sides = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    /// <summary>The steps to all eight Cells around the Cell: the four sides first, then the four corners.</summary>
    public static readonly CellStep[] All = [.. Sides, new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)];

    /// <summary>Whether the step goes to a corner, changing both column and row.</summary>
    public bool IsDiagonal => DeltaX != 0 && DeltaY != 0;

    /// <summary>The Cell the step leads to from <paramref name="cell"/>. It may be outside the map.</summary>
    public CellPosition From(CellPosition cell) => new(cell.X + DeltaX, cell.Y + DeltaY);
}
