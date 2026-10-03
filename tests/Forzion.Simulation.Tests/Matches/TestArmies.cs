namespace Forzion.Simulation.Tests.Matches;

/// <summary>
/// Matches whose Players start with extra units, for the tests that need units the starting
/// Villagers cannot stand in for.
/// </summary>
internal static class TestArmies
{
    /// <summary>The two-Player configuration of <see cref="TestMatches"/>, with extra units for either Player.</summary>
    public static MatchConfig Config(
        ulong seed = 42, IReadOnlyList<StartingUnit>? first = null, IReadOnlyList<StartingUnit>? second = null) =>
        new(
            seed,
            new MapConfig(64, 48),
            [new PlayerConfig(TestMatches.FirstFaction, first ?? []), new PlayerConfig(TestMatches.FirstFaction, second ?? [])]);

    /// <summary>
    /// The Cell offset by (<paramref name="x"/>, <paramref name="y"/>) from the centre of the
    /// Player's Town Center. Every Cell two steps from the centre is free: the building covers
    /// one step around it and the nearest resource sources lie three steps away.
    /// </summary>
    public static CellPosition BesideHome(Match match, PlayerId player, int x, int y)
    {
        var townCenter = match.State.Buildings.First(building => building.Owner == player);

        return new CellPosition(
            townCenter.Origin.X + (townCenter.Width / 2) + x,
            townCenter.Origin.Y + (townCenter.Height / 2) + y);
    }
}
