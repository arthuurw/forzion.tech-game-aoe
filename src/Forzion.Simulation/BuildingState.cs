namespace Forzion.Simulation;

/// <summary>What a building is. Its kind decides what the building does.</summary>
public enum BuildingKind
{
    TownCenter = 0,
}

/// <summary>A building. It occupies a rectangle of whole Cells.</summary>
public sealed class BuildingState
{
    internal BuildingState(EntityId id, PlayerId owner, BuildingKind kind, CellPosition origin, int width, int height)
    {
        Id = id;
        Owner = owner;
        Kind = kind;
        Origin = origin;
        Width = width;
        Height = height;
    }

    public EntityId Id { get; }

    public PlayerId Owner { get; }

    public BuildingKind Kind { get; }

    /// <summary>The Cell of the footprint with the lowest X and Y.</summary>
    public CellPosition Origin { get; }

    /// <summary>Width of the footprint in Cells.</summary>
    public int Width { get; }

    /// <summary>Height of the footprint in Cells.</summary>
    public int Height { get; }

    /// <summary>
    /// Whether Villagers deliver their loads here. Every kind of drop-off point takes every
    /// Resource; the Storehouse joins this list when it is added.
    /// </summary>
    internal bool IsDropOffPoint => Kind is BuildingKind.TownCenter;

    /// <summary>The Cell of the footprint nearest to the given Cell.</summary>
    internal CellPosition NearestCellTo(CellPosition cell) => new(
        Math.Clamp(cell.X, Origin.X, Origin.X + Width - 1),
        Math.Clamp(cell.Y, Origin.Y, Origin.Y + Height - 1));

    /// <summary>Whether the given Cell lies beside the footprint, by a side or by a corner.</summary>
    internal bool IsBeside(CellPosition cell)
    {
        var nearest = NearestCellTo(cell);

        return cell != nearest && Math.Abs(cell.X - nearest.X) <= 1 && Math.Abs(cell.Y - nearest.Y) <= 1;
    }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Owner.Value);
        hasher.Write((int)Kind);
        hasher.Write(Origin);
        hasher.Write(Width);
        hasher.Write(Height);
    }
}
