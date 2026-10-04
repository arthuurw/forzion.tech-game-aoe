namespace Forzion.Simulation;

/// <summary>Everything a match is created from. The same configuration always produces the same match.</summary>
/// <param name="Seed">Seed of the match's random generator.</param>
/// <param name="Map">The map the match is played on.</param>
/// <param name="Players">The Players, in order: the first receives <see cref="PlayerId"/> 1, the second 2, and so on.</param>
/// <param name="Factions">
/// The Factions the Players may control, each with an ID of its own, or null for the Factions
/// of the game (<see cref="Simulation.Factions.All"/>).
/// </param>
public sealed record MatchConfig(
    ulong Seed, MapConfig Map, IReadOnlyList<PlayerConfig> Players, IReadOnlyList<Faction>? Factions = null);

/// <param name="Width">Width of the map in Cells.</param>
/// <param name="Height">Height of the map in Cells.</param>
public sealed record MapConfig(int Width, int Height);

/// <param name="Faction">The ID of the Faction the Player controls, one of the match's Factions.</param>
/// <param name="ExtraUnits">
/// Units the Player starts with on top of what every Player starts with. Each must stand on a
/// free Cell of the map. A Skirmish leaves it empty: it lets tests and replays start with
/// soldiers without training them first.
/// </param>
/// <param name="IsAi">
/// Whether the Player is an AI, played by the match itself through the same commands a human
/// gives, rather than by commands enqueued from outside.
/// </param>
public sealed record PlayerConfig(FactionId Faction, IReadOnlyList<StartingUnit>? ExtraUnits = null, bool IsAi = false);

/// <summary>A unit a Player starts the match with, standing on the centre of <paramref name="Cell"/>.</summary>
public sealed record StartingUnit(UnitKind Kind, CellPosition Cell);
