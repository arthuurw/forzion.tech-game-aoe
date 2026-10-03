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
