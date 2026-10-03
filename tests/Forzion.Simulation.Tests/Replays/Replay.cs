namespace Forzion.Simulation.Tests.Replays;

/// <summary>A command and the tick at which it is sent: it is enqueued once the match has simulated that many ticks, so the following tick applies it.</summary>
internal sealed record ScheduledCommand(int Tick, Command Command);

/// <summary>
/// The replay harness: a whole match run without graphics from a configuration (which carries
/// the seed) and a list of commands, through the simulation's public interface only.
/// </summary>
internal static class Replay
{
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
            foreach (var scheduled in commands)
            {
                if (scheduled.Tick == tick)
                {
                    match.Enqueue(scheduled.Command);
                }
            }

            match.Tick();
            hashes.Add(match.StateHash);
        }

        return hashes;
    }
}
