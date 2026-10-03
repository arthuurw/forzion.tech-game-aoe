namespace Forzion.Simulation;

/// <summary>Identifies a Player within a match. Assigned from 1 upward in configuration order.</summary>
public readonly record struct PlayerId(int Value);

/// <summary>Identifies a Faction.</summary>
public readonly record struct FactionId(int Value);
