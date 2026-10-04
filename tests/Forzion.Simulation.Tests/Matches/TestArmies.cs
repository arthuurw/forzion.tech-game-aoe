namespace Forzion.Simulation.Tests.Matches;

/// <summary>
/// Matches whose Players start with extra units, for the tests that need units the starting
/// Villagers cannot stand in for.
/// </summary>
internal static class TestArmies
{
    /// <summary>
    /// The two-Player match, with the first Player starting with a melee soldier two Cells left
    /// of the centre of its Town Center.
    /// </summary>
    public static Match MatchWithSoldier() =>
        Match.Create(TestMatches.TwoPlayerConfig(
            first: [new StartingUnit(UnitKind.MeleeSoldier, BesideHome(TestMatches.TwoPlayerMatch(), TestMatches.FirstPlayer, -2, 0))]));

    /// <summary>
    /// Sends the unit walking to the Cell six to the left of it, ticks once to set it on its way
    /// and returns the last Cell of its path: a unit busy with an order of its own.
    /// </summary>
    public static CellPosition WalkAway(Match match, UnitState unit)
    {
        match.Enqueue(new MoveCommand(unit.Owner, [unit.Id], new CellPosition(unit.Position.Cell.X - 6, unit.Position.Cell.Y)));
        match.Tick();

        return unit.Path[^1];
    }

    /// <summary>The Player's only melee soldier.</summary>
    public static UnitState SoldierOf(this MatchState state, PlayerId player) =>
        state.UnitsOf(player).Single(unit => unit.Kind == UnitKind.MeleeSoldier);

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
