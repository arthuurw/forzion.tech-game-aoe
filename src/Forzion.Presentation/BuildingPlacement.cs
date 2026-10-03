using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>Where a building being placed would go, as the placement preview shows it.</summary>
/// <param name="Kind">The kind of building.</param>
/// <param name="Origin">The Cell of the footprint with the lowest X and Y.</param>
/// <param name="Size">Side of the square footprint, in Cells.</param>
/// <param name="IsValid">
/// Whether the match would place it there now, Cost aside: every Cell of the footprint inside
/// the map, free and with no unit on it.
/// </param>
public sealed record BuildingPlacement(BuildingKind Kind, CellPosition Origin, int Size, bool IsValid);
