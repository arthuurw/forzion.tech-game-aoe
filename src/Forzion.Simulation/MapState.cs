namespace Forzion.Simulation;

/// <summary>What a Cell holds. Every kind except <see cref="Free"/> blocks movement and building.</summary>
public enum CellKind
{
    Free = 0,

    /// <summary>Occupied by a building.</summary>
    Building = 4,
}

/// <summary>
/// The map: a flat square grid of Cells. It is generated from the match seed and is symmetric
/// between the Players: a half-turn around the centre of the map, which takes the Cell (x, y)
/// to (Width - 1 - x, Height - 1 - y), takes the map onto itself and each Player's starting
/// position onto the other's.
/// </summary>
public sealed class MapState
{
    private readonly CellKind[] cells;

    internal MapState(int width, int height)
    {
        Width = width;
        Height = height;
        cells = new CellKind[width * height];
    }

    /// <summary>Width in Cells.</summary>
    public int Width { get; }

    /// <summary>Height in Cells.</summary>
    public int Height { get; }

    /// <summary>What the given Cell holds.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is outside the map.</exception>
    public CellKind this[CellPosition cell]
    {
        get => cells[IndexOf(cell)];
        internal set => cells[IndexOf(cell)] = value;
    }

    /// <summary>Whether the given Cell is inside the map.</summary>
    public bool Contains(CellPosition cell) =>
        cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

    /// <summary>The Cell the map's symmetry takes <paramref name="cell"/> to.</summary>
    internal CellPosition Mirror(CellPosition cell) => new(Width - 1 - cell.X, Height - 1 - cell.Y);

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Width);
        hasher.Write(Height);

        foreach (var cell in cells)
        {
            hasher.Write((int)cell);
        }
    }

    private int IndexOf(CellPosition cell)
    {
        if (!Contains(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell), cell, "The Cell is outside the map.");
        }

        return (cell.Y * Width) + cell.X;
    }
}
