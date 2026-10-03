namespace Forzion.Simulation;

public enum UnitKind
{
    Villager = 0,
}

/// <summary>A unit. Units stand on free Cells and do not occupy them.</summary>
public sealed class UnitState
{
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

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Owner.Value);
        hasher.Write((int)Kind);
        hasher.Write(Position.X.RawValue);
        hasher.Write(Position.Y.RawValue);
    }
}
