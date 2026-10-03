namespace Forzion.Simulation;

public enum UnitKind
{
    Villager = 0,
}

/// <summary>A unit. Units stand on free Cells and do not occupy them.</summary>
public sealed class UnitState
{
    private readonly List<CellPosition> path = [];

    internal UnitState(EntityId id, PlayerId owner, UnitKind kind, MapPosition position)
    {
        Id = id;
        Owner = owner;
        Kind = kind;
        Position = position;
    }

    public EntityId Id { get; }

    public PlayerId Owner { get; }

    public UnitKind Kind { get; }

    public MapPosition Position { get; internal set; }

    /// <summary>
    /// The Cells the unit still has to walk through, the next one first and its destination
    /// last. Empty while the unit stands still.
    /// </summary>
    public IReadOnlyList<CellPosition> Path => path;

    /// <summary>Whether the unit is walking somewhere.</summary>
    public bool IsMoving => path.Count > 0;

    internal void SetPath(IEnumerable<CellPosition> cells)
    {
        path.Clear();
        path.AddRange(cells);
    }

    /// <summary>Drops the next Cell of the path: the unit has reached its centre.</summary>
    internal void ReachWaypoint() => path.RemoveAt(0);

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Owner.Value);
        hasher.Write((int)Kind);
        hasher.Write(Position.X.RawValue);
        hasher.Write(Position.Y.RawValue);
        hasher.Write(path.Count);

        foreach (var cell in path)
        {
            hasher.Write(cell.X);
            hasher.Write(cell.Y);
        }
    }
}
