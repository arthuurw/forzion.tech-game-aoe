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

/// <summary>A command was refused and changed nothing.</summary>
public sealed record CommandRejected(Command Command, RejectionReason Reason) : MatchEvent;

/// <summary>Why a command was refused.</summary>
public enum RejectionReason
{
    /// <summary>The issuing Player is not in the match.</summary>
    UnknownPlayer,

    /// <summary>The issuing Player has already been defeated.</summary>
    DefeatedPlayer,

    /// <summary>None of the units the command names is in the match.</summary>
    UnknownUnit,

    /// <summary>The command names a unit that belongs to another Player.</summary>
    UnitOfAnotherPlayer,

    /// <summary>The destination Cell is outside the map.</summary>
    DestinationOutsideMap,

    /// <summary>The command names a resource source that is not in the match, or no longer is.</summary>
    UnknownResourceSource,

    /// <summary>The attack target is neither a unit nor a building of the match.</summary>
    UnknownTarget,

    /// <summary>The attack target belongs to the issuing Player.</summary>
    OwnTarget,

    /// <summary>The command orders an attack from a unit that cannot attack.</summary>
    UnitCannotAttack,

    /// <summary>The command orders a gather from a unit that cannot gather: only Villagers gather.</summary>
    UnitCannotGather,
}
