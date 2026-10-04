namespace Forzion.Simulation;

/// <summary>A Player finished an Age Advance and is in a new Age, whose content is unlocked from now on.</summary>
/// <param name="Player">The Player who advanced.</param>
/// <param name="Age">The number of the Age the Player reached: 2 for Age II.</param>
public sealed record AgeAdvanced(PlayerId Player, int Age) : MatchEvent;
