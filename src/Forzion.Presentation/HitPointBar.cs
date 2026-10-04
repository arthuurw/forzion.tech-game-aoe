using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// A hit point bar to draw: over which entity, where it is on the map, and how full.
/// </summary>
/// <param name="Id">The unit or building the bar is drawn over.</param>
/// <param name="Position">
/// Where the entity is drawn: a unit's interpolated position, or the centre of a building's footprint.
/// </param>
/// <param name="OverBuilding">Whether the entity is a building, drawn taller and wider than a unit.</param>
/// <param name="Fill">The share of the entity's hit points left, from 0 to 1.</param>
public readonly record struct HitPointBar(EntityId Id, MapPoint Position, bool OverBuilding, double Fill);
