namespace Forzion.Simulation;

/// <summary>What a building is. Its kind decides what the building does.</summary>
public enum BuildingKind
{
    TownCenter = 0,

    /// <summary>Raises its Player's population limit once complete.</summary>
    House = 1,

    /// <summary>A drop-off point once complete.</summary>
    Storehouse = 2,

    /// <summary>Trains military units once complete.</summary>
    Barracks = 3,
}

/// <summary>
/// A building. It occupies a rectangle of whole Cells from the moment it is placed, and is a
/// construction site until Villagers have built it to completion.
/// </summary>
public sealed class BuildingState
{
    internal BuildingState(EntityId id, PlayerId owner, BuildingKind kind, CellPosition origin)
    {
        Id = id;
        Owner = owner;
        Kind = kind;
        Footprint = Footprint.Of(kind, origin);
        HitPoints = MaxHitPoints;
    }

    public EntityId Id { get; }

    public PlayerId Owner { get; }

    public BuildingKind Kind { get; }

    /// <summary>The Cell of the footprint with the lowest X and Y.</summary>
    public CellPosition Origin => Footprint.Origin;

    /// <summary>Width of the footprint in Cells.</summary>
    public int Width => Footprint.Width;

    /// <summary>Height of the footprint in Cells.</summary>
    public int Height => Footprint.Height;

    /// <summary>The Cells the building covers, the size its kind gives.</summary>
    internal Footprint Footprint { get; }

    /// <summary>Hit points the building has when whole.</summary>
    public int MaxHitPoints => Balance.Of(Kind).HitPoints;

    /// <summary>Hit points left. The building is destroyed when they reach zero.</summary>
    public int HitPoints { get; internal set; }

    /// <summary>Ticks of Villager work put into the building so far, up to <see cref="BuildTime"/>.</summary>
    public int BuildProgress { get; internal set; }

    /// <summary>
    /// Ticks of Villager work the building takes to complete. Each Villager building it adds
    /// one tick of work per tick, so two finish it in half the time.
    /// </summary>
    public int BuildTime => Balance.Of(Kind).BuildTime;

    /// <summary>Whether the building is complete. Until then it is a construction site and does nothing but block its Cells.</summary>
    public bool IsComplete => BuildProgress == BuildTime;

    /// <summary>
    /// Whether Villagers deliver their loads here: a complete Town Center or Storehouse. Every
    /// kind of drop-off point takes every Resource.
    /// </summary>
    internal bool IsDropOffPoint => Kind is (BuildingKind.TownCenter or BuildingKind.Storehouse) && IsComplete;

    /// <summary>Whether the given Cell lies beside the footprint, by a side or by a corner.</summary>
    internal bool IsBeside(CellPosition cell) => Footprint.IsBeside(cell);

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Owner.Value);
        hasher.Write((int)Kind);
        hasher.Write(Origin);
        hasher.Write(Width);
        hasher.Write(Height);
        hasher.Write(HitPoints);
        hasher.Write(BuildProgress);
    }
}
