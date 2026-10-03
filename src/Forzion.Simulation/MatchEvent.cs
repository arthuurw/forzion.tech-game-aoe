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
