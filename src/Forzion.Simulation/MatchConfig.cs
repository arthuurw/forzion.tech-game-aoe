namespace Forzion.Simulation;

/// <summary>Everything a match is created from. The same configuration always produces the same match.</summary>
/// <param name="Seed">Seed of the match's random generator.</param>
/// <param name="Map">The map the match is played on.</param>
/// <param name="Players">The Players, in order: the first receives <see cref="PlayerId"/> 1, the second 2, and so on.</param>
public sealed record MatchConfig(ulong Seed, MapConfig Map, IReadOnlyList<PlayerConfig> Players);

/// <param name="Width">Width of the map in Cells.</param>
/// <param name="Height">Height of the map in Cells.</param>
public sealed record MapConfig(int Width, int Height);

/// <param name="Faction">The Faction the Player controls.</param>
public sealed record PlayerConfig(FactionId Faction);
