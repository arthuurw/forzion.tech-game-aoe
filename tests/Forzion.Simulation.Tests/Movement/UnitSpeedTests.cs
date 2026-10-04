using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Movement;

public class UnitSpeedTests
{
    [Fact]
    public void A_heavy_soldier_covers_the_same_walk_in_fewer_ticks_than_a_melee_soldier()
    {
        var heavy = TicksToWalkSixCellsLeft(UnitKind.HeavySoldier);
        var melee = TicksToWalkSixCellsLeft(UnitKind.MeleeSoldier);

        Assert.True(heavy < melee, $"The heavy soldier took {heavy} ticks and the melee soldier {melee}.");
    }

    /// <summary>
    /// Starts the first Player with one unit of the kind beside its Town Center, sends it six
    /// Cells to the left and returns the ticks it walks until it stops. Every kind starts on the
    /// same Cell of the same map, so every kind walks the same path.
    /// </summary>
    private static int TicksToWalkSixCellsLeft(UnitKind kind)
    {
        var start = TestArmies.BesideHome(TestMatches.TwoPlayerMatch(), TestMatches.FirstPlayer, -2, 0);
        var match = Match.Create(TestMatches.TwoPlayerConfig(first: [new StartingUnit(kind, start)]));
        var unit = match.State.UnitsOf(TestMatches.FirstPlayer).Single(candidate => candidate.Kind == kind);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [unit.Id], new CellPosition(start.X - 6, start.Y)));

        return TestMatches.TickUntil(match, () => !unit.IsMoving);
    }
}
