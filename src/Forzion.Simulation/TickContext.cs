namespace Forzion.Simulation;

/// <summary>What the code running inside one tick works with: the state to change and the tick's events.</summary>
internal sealed class TickContext
{
    private readonly List<MatchEvent> events = [];

    public TickContext(MatchState state) => State = state;

    public MatchState State { get; }

    public IReadOnlyList<MatchEvent> Events => events;

    public void Emit(MatchEvent matchEvent) => events.Add(matchEvent);

    public void Reject(Command command, RejectionReason reason) =>
        Emit(new CommandRejected(command, reason));

    /// <summary>
    /// Whether something in this tick may have opened a way for Villagers waiting for one:
    /// Cells were freed or a drop-off point was completed.
    /// </summary>
    public bool WaysMayHaveOpened { get; private set; }

    /// <summary>Notes that Cells were freed or a drop-off point was completed in this tick.</summary>
    public void NoteWaysMayHaveOpened() => WaysMayHaveOpened = true;
}
