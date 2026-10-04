namespace Forzion.Simulation.Tests.Replays;

/// <summary>A command and the tick at which it is sent: it is enqueued once the match has simulated that many ticks, so the following tick applies it.</summary>
internal sealed record ScheduledCommand(int Tick, Command Command);

/// <summary>
/// How a replayed match ended: the tick it ended in, its winner (null for none) and the state
/// hash after that tick. A replay records the three together, so that a change of rules that
/// moves the end of the match is recorded again from a single failure message.
/// </summary>
internal sealed record ReplayEnd(int Tick, PlayerId? Winner, ulong FinalHash);

/// <summary>
/// The replay harness: a whole match run without graphics from a configuration (which carries
/// the seed) and a list of commands, through the simulation's public interface only.
/// </summary>
internal static class Replay
{
    /// <summary>
    /// Runs the match until it ends, enqueuing each command as <see cref="Run"/> does, and
    /// returns how it ended with every event of every tick, in order. Fails the test when the
    /// match has not ended after <paramref name="limit"/> ticks.
    /// </summary>
    public static (ReplayEnd End, IReadOnlyList<MatchEvent> Events) RunToEnd(
        MatchConfig config, IReadOnlyList<ScheduledCommand> commands, int limit)
    {
        var match = Match.Create(config);
        var events = new List<MatchEvent>();

        for (var tick = 0; tick < limit && !match.State.IsOver; tick++)
        {
            EnqueueAt(match, commands, tick);
            match.Tick();
            events.AddRange(match.Events);
        }

        Assert.True(match.State.IsOver, $"The match did not end within {limit} ticks.");

        return (new ReplayEnd(match.State.Tick, match.State.Winner, match.StateHash), events);
    }

    /// <summary>
    /// Runs the match for <paramref name="ticks"/> ticks and returns the state hash after each
    /// one: element 0 is the hash after the first tick, the last element is the final hash.
    /// Commands scheduled for the same tick are enqueued in list order.
    /// </summary>
    public static IReadOnlyList<ulong> Run(MatchConfig config, IReadOnlyList<ScheduledCommand> commands, int ticks)
    {
        var match = Match.Create(config);
        var hashes = new List<ulong>(ticks);

        for (var tick = 0; tick < ticks; tick++)
        {
            EnqueueAt(match, commands, tick);
            match.Tick();
            hashes.Add(match.StateHash);
        }

        return hashes;
    }

    /// <summary>Enqueues, in list order, the commands sent once the match has simulated <paramref name="tick"/> ticks.</summary>
    private static void EnqueueAt(Match match, IReadOnlyList<ScheduledCommand> commands, int tick)
    {
        foreach (var scheduled in commands)
        {
            if (scheduled.Tick == tick)
            {
                match.Enqueue(scheduled.Command);
            }
        }
    }
}
