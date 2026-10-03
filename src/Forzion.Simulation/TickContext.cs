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
}
