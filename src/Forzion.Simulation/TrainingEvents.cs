namespace Forzion.Simulation;

/// <summary>A building finished training a unit, which now stands beside it.</summary>
/// <param name="Unit">The unit trained.</param>
/// <param name="Building">The building that trained it.</param>
public sealed record UnitTrained(EntityId Unit, EntityId Building) : MatchEvent;
