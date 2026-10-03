namespace Forzion.Simulation;

/// <summary>Identifies a Player within a match. Assigned from 1 upward in configuration order.</summary>
public readonly record struct PlayerId(int Value);

/// <summary>
/// Identifies an entity of a match: a resource source, a building or a unit. All entities share
/// one sequence, assigned from 1 upward in creation order and never reused.
/// </summary>
public readonly record struct EntityId(int Value);

/// <summary>Identifies a Faction.</summary>
public readonly record struct FactionId(int Value);
