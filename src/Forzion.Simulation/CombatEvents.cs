namespace Forzion.Simulation;

/// <summary>A unit died or a building was destroyed, and it has left the match.</summary>
public sealed record EntityDestroyed(EntityId Entity) : MatchEvent;
