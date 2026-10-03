namespace Forzion.Presentation;

/// <summary>
/// The size of what the mouse can point at, in world units (one per Cell): the shapes the
/// presentation draws, so that pointing at a shape anywhere on screen picks its entity.
/// </summary>
/// <param name="UnitRadius">How far from a unit's position the mouse still picks it, across the map.</param>
/// <param name="UnitHeight">How tall a unit stands.</param>
/// <param name="BuildingAndSourceHeight">How tall buildings and resource sources stand over their Cells.</param>
public sealed record PickSizes(double UnitRadius, double UnitHeight, double BuildingAndSourceHeight);
