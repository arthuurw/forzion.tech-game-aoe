namespace Forzion.Simulation;

/// <summary>A unit died or a building was destroyed, and it has left the match.</summary>
public sealed record EntityDestroyed(EntityId Entity) : MatchEvent;

/// <summary>
/// The match is over: at most one Player remains undefeated. <paramref name="Winner"/> is that
/// Player, or null when the last Players were defeated together.
/// </summary>
public sealed record MatchEnded(PlayerId? Winner) : MatchEvent;
