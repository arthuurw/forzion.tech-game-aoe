namespace Forzion.Simulation;

/// <summary>The rectangle of whole Cells a building covers.</summary>
/// <param name="Origin">The Cell of the footprint with the lowest X and Y.</param>
/// <param name="Width">Width of the footprint in Cells.</param>
/// <param name="Height">Height of the footprint in Cells.</param>
internal readonly record struct Footprint(CellPosition Origin, int Width, int Height)
{
    /// <summary>The square footprint a building of the given kind has from <paramref name="origin"/>.</summary>
    public static Footprint Of(BuildingKind kind, CellPosition origin)
    {
        var size = Balance.Of(kind).Size;

        return new Footprint(origin, size, size);
    }

    /// <summary>The Cells of the footprint, row by row from the origin: in ascending Cell index.</summary>
    public IEnumerable<CellPosition> Cells
    {
        get
        {
            for (var y = Origin.Y; y < Origin.Y + Height; y++)
            {
                for (var x = Origin.X; x < Origin.X + Width; x++)
                {
                    yield return new CellPosition(x, y);
                }
            }
        }
    }

    /// <summary>
    /// The Cell the footprint is centred on; for an even side, the nearer of the two middle
    /// Cells to the origin along that axis.
    /// </summary>
    public CellPosition Centre => new(Origin.X + (Width / 2), Origin.Y + (Height / 2));

    /// <summary>The footprint and the ring of Cells around it, by a side or by a corner.</summary>
    public Footprint WithRing() => new(new CellPosition(Origin.X - 1, Origin.Y - 1), Width + 2, Height + 2);

    /// <summary>Whether the two footprints share a Cell.</summary>
    public bool Overlaps(Footprint other) =>
        Origin.X < other.Origin.X + other.Width && other.Origin.X < Origin.X + Width
        && Origin.Y < other.Origin.Y + other.Height && other.Origin.Y < Origin.Y + Height;

    /// <summary>Whether the Cell is one of the footprint's.</summary>
    public bool Contains(CellPosition cell) =>
        cell.X >= Origin.X && cell.X < Origin.X + Width && cell.Y >= Origin.Y && cell.Y < Origin.Y + Height;

    /// <summary>The Cell of the footprint nearest to the given Cell.</summary>
    public CellPosition NearestCellTo(CellPosition cell) => new(
        Math.Clamp(cell.X, Origin.X, Origin.X + Width - 1),
        Math.Clamp(cell.Y, Origin.Y, Origin.Y + Height - 1));

    /// <summary>Whether the given Cell lies beside the footprint, by a side or by a corner.</summary>
    public bool IsBeside(CellPosition cell) => cell.Touches(NearestCellTo(cell));

    /// <summary>Distance from the point to the nearest point of the footprint, in Cells; zero inside it.</summary>
    public Fix64 DistanceTo(MapPosition point)
    {
        var left = Fix64.FromInt(Origin.X);
        var bottom = Fix64.FromInt(Origin.Y);
        var right = Fix64.FromInt(Origin.X + Width);
        var top = Fix64.FromInt(Origin.Y + Height);
        var x = Fix64.Max(Fix64.Max(left - point.X, point.X - right), Fix64.Zero);
        var y = Fix64.Max(Fix64.Max(bottom - point.Y, point.Y - top), Fix64.Zero);

        return Fix64.Hypot(x, y);
    }
}
