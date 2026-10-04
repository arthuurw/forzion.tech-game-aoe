namespace Forzion.Simulation;

/// <summary>A Cell of the map by column and row, counted from 0 at the map's first corner.</summary>
public readonly record struct CellPosition(int X, int Y)
{
    /// <summary>Distance to the other Cell in king's moves: steps to one of the eight Cells around.</summary>
    internal int KingDistanceTo(CellPosition other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    /// <summary>Whether the other Cell is one of the eight around this one, by a side or by a corner.</summary>
    internal bool Touches(CellPosition other) => KingDistanceTo(other) == 1;

    /// <summary>Square of the straight-line distance between the centres of the two Cells, in Cells.</summary>
    internal int SquaredDistanceTo(CellPosition other) =>
        ((X - other.X) * (X - other.X)) + ((Y - other.Y) * (Y - other.Y));
}
