namespace Forzion.Simulation.Tests.Matches;

/// <summary>Match configurations shared by the tests of the simulation's public interface.</summary>
internal static class TestMatches
{
    public static readonly FactionId FirstFaction = new(1);

    public static readonly PlayerId FirstPlayer = new(1);

    public static readonly PlayerId SecondPlayer = new(2);

    public static MatchConfig TwoPlayerConfig(ulong seed = 42) =>
        new(seed, new MapConfig(64, 48), [new PlayerConfig(FirstFaction), new PlayerConfig(FirstFaction)]);

    public static Match TwoPlayerMatch(ulong seed = 42) => Match.Create(TwoPlayerConfig(seed));
}
