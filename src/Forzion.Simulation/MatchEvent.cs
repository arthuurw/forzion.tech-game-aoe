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
