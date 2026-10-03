namespace Forzion.Simulation;

/// <summary>
/// Something that happened during a tick, reported for the presentation layer to read.
/// Events never feed back into the state.
/// </summary>
public abstract record MatchEvent
{
    private protected MatchEvent()
    {
    }
}

/// <summary>A Player was defeated and takes no further part in the match.</summary>
public sealed record PlayerDefeated(PlayerId Player) : MatchEvent;

/// <summary>A resource source was gathered to the end and left the map, freeing its Cell.</summary>
public sealed record ResourceSourceDepleted(EntityId Source) : MatchEvent;

/// <summary>Villagers finished building a construction site: the building is complete and does its work from now on.</summary>
public sealed record BuildingCompleted(EntityId Building) : MatchEvent;

/// <summary>A command was refused and changed nothing.</summary>
public sealed record CommandRejected(Command Command, RejectionReason Reason) : MatchEvent;

/// <summary>Why a command was refused.</summary>
public enum RejectionReason
{
    /// <summary>The issuing Player is not in the match.</summary>
    UnknownPlayer,

    /// <summary>The issuing Player has already been defeated.</summary>
    DefeatedPlayer,

    /// <summary>The command names a unit that is not in the match.</summary>
    UnknownUnit,

    /// <summary>The command names a unit that belongs to another Player.</summary>
    UnitOfAnotherPlayer,

    /// <summary>The destination Cell is outside the map.</summary>
    DestinationOutsideMap,

    /// <summary>The command names a resource source that is not in the match, or no longer is.</summary>
    UnknownResourceSource,

    /// <summary>
    /// The command names a kind of building that Players do not place, such as the Town
    /// Center each Player starts with.
    /// </summary>
    BuildingNotPlaceable,

    /// <summary>
    /// A Cell of the building's footprint is outside the map, is not free or has a unit
    /// standing on it.
    /// </summary>
    InvalidPlacement,

    /// <summary>The Player has less of some Resource than the cost asks for.</summary>
    NotEnoughResources,
}
