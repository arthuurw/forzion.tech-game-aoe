namespace Forzion.Simulation;

public enum UnitKind
{
    Villager = 0,

    /// <summary>Military unit that fights at close quarters.</summary>
    MeleeSoldier = 1,

    /// <summary>Military unit that fights from a distance.</summary>
    RangedSoldier = 2,
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
        HitPoints = MaxHitPoints;
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

    /// <summary>Hit points the unit has when whole.</summary>
    public int MaxHitPoints => Balance.HitPoints(Kind);

    /// <summary>Hit points left. The unit dies when they reach zero.</summary>
    public int HitPoints { get; internal set; }

    /// <summary>The unit or building this unit is attacking, or null when it is attacking nothing.</summary>
    public EntityId? Target { get; private set; }

    /// <summary>Ticks spent within range of the target since the last hit, or since the target was set.</summary>
    internal int AttackProgress { get; set; }

    /// <summary>
    /// Makes the unit attack the given entity, dropping wherever it was walking and starting a
    /// fresh attack interval.
    /// </summary>
    internal void Attack(EntityId target)
    {
        Target = target;
        AttackProgress = 0;
        path.Clear();
    }

    internal void SetPath(IEnumerable<CellPosition> cells)
    {
        path.Clear();
        path.AddRange(cells);
    }

    /// <summary>Stops the unit where it is, even between two Cell centres.</summary>
    internal void Stop() => path.Clear();

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

        hasher.Write(HitPoints);
        hasher.Write(Target?.Value ?? 0);
        hasher.Write(AttackProgress);
    }
}
