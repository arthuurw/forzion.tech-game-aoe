namespace Forzion.Simulation;

/// <summary>The three Resources.</summary>
public enum ResourceKind
{
    Food = 0,
    Wood = 1,
    Gold = 2,
}

/// <summary>A place on the map where one Resource is gathered. It occupies one Cell and holds a finite amount.</summary>
public sealed class ResourceSourceState
{
    internal ResourceSourceState(EntityId id, ResourceKind kind, CellPosition cell, int amount)
    {
        Id = id;
        Kind = kind;
        Cell = cell;
        Amount = amount;
    }

    public EntityId Id { get; }

    public ResourceKind Kind { get; }

    public CellPosition Cell { get; }

    /// <summary>How much of the Resource is left to gather.</summary>
    public int Amount { get; internal set; }

    /// <summary>Whether the given Cell lies beside the source, by a side or by a corner.</summary>
    internal bool IsBeside(CellPosition cell) => cell.Touches(Cell);

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write((int)Kind);
        hasher.Write(Cell);
        hasher.Write(Amount);
    }
}
