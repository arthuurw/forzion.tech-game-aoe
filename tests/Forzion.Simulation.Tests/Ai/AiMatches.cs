using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

/// <summary>Matches with AI Players, and running them, through the public interface only.</summary>
internal static class AiMatches
{
    /// <summary>The two-Player configuration of <see cref="TestMatches"/>, with either Player as AI.</summary>
    public static MatchConfig Config(bool firstIsAi, bool secondIsAi, ulong seed = 42) =>
        new(
            seed,
            new MapConfig(64, 48),
            [
                new PlayerConfig(TestMatches.FirstFaction, IsAi: firstIsAi),
                new PlayerConfig(TestMatches.FirstFaction, IsAi: secondIsAi),
            ]);
}
