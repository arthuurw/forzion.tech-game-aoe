namespace Forzion.Simulation;

/// <summary>What a unit is. Its kind decides what the unit can do and how fast it walks.</summary>
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

    /// <summary>The resource source the Villager gathers from, or null when it has none.</summary>
    public EntityId? GatherSource { get; internal set; }

    /// <summary>What the Villager is doing towards gathering.</summary>
    public GatherPhase GatherPhase { get; internal set; }

    /// <summary>What the Villager carries.</summary>
    public Load Load { get; internal set; }

    /// <summary>
    /// The construction site the Villager walks up to and builds, or null when it builds none.
    /// It works only while standing still beside the site.
    /// </summary>
    public EntityId? ConstructionSite { get; internal set; }

    /// <summary>
    /// Ticks the Villager has spent beside its source towards the next unit of Resource. It
    /// goes back to zero when that unit is taken and whenever the Villager is given a new
    /// gather order or stops gathering.
    /// </summary>
    public int GatherProgress { get; internal set; }

    /// <summary>Hit points the unit has when whole.</summary>
    public int MaxHitPoints => Balance.HitPoints(Kind);

    /// <summary>Whether the unit fights: military units do, Villagers do not.</summary>
    public bool CanAttack => Balance.Attack(Kind) is not null;

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

    /// <summary>Takes the Villager off gathering. It keeps whatever it carries.</summary>
    internal void StopGathering()
    {
        GatherSource = null;
        GatherPhase = GatherPhase.None;
        GatherProgress = 0;
    }

    /// <summary>Drops the unit's target and stops it where it is.</summary>
    internal void StopAttacking()
    {
        Target = null;
        AttackProgress = 0;
        path.Clear();
    }

    /// <summary>Stops the unit where it is, even between two Cell centres.</summary>
    internal void Stop() => path.Clear();

    /// <summary>Takes the Villager off building. The site keeps the work already put into it.</summary>
    internal void StopBuilding() => ConstructionSite = null;

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
            hasher.Write(cell);
        }

        // A unit without a source writes 0, which no entity ID takes.
        hasher.Write(GatherSource?.Value ?? 0);
        hasher.Write((int)GatherPhase);
        hasher.Write(GatherProgress);
        hasher.Write((int)Load.Resource);
        hasher.Write(Load.Amount);
        hasher.Write(HitPoints);

        // Likewise a unit without a target.
        hasher.Write(Target?.Value ?? 0);
        hasher.Write(AttackProgress);

        // Likewise a unit without a construction site.
        hasher.Write(ConstructionSite?.Value ?? 0);
    }
}
