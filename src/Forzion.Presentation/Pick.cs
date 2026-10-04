using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What a line of sight points at, and the Cell of the ground it meets: where a unit sent
/// there walks when nothing else fits.
/// </summary>
/// <param name="Ground">The Cell the line of sight meets the ground in.</param>
internal abstract record Pick(CellPosition Ground);

/// <summary>A unit the line of sight points at.</summary>
internal sealed record UnitPick(UnitState Unit, CellPosition Ground) : Pick(Ground);

/// <summary>A building the line of sight points at.</summary>
internal sealed record BuildingPick(BuildingState Building, CellPosition Ground) : Pick(Ground);

/// <summary>A resource source the line of sight points at.</summary>
internal sealed record SourcePick(ResourceSourceState Source, CellPosition Ground) : Pick(Ground);

/// <summary>Bare ground: the line of sight points at no entity.</summary>
internal sealed record GroundPick(CellPosition Ground) : Pick(Ground);
