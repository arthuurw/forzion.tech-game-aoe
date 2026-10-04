using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Movement;

public class UnitSpeedTests
{
    private const int Distance = 6;

    [Fact]
    public void A_heavy_soldier_covers_the_same_walk_in_fewer_ticks_than_a_melee_soldier()
    {
        var heavy = TicksToWalkLeft(UnitKind.HeavySoldier);
        var melee = TicksToWalkLeft(UnitKind.MeleeSoldier);

        Assert.True(heavy < melee, $"The heavy soldier took {heavy} ticks and the melee soldier {melee}.");
    }

    /// <summary>
    /// Starts the first Player with one unit of the kind beside its Town Center, sends it
    /// <see cref="Distance"/> Cells to the left and returns the ticks it walks until it gets there. Every kind starts on the
    /// same Cell of the same map, so every kind walks the same path.
    /// </summary>
    private static int TicksToWalkLeft(UnitKind kind)
    {
        var start = TestArmies.BesideHome(TestMatches.TwoPlayerMatch(), TestMatches.FirstPlayer, -2, 0);
        var match = Match.Create(TestMatches.TwoPlayerConfig(first: [new StartingUnit(kind, start)]));
        var unit = match.State.UnitsOf(TestMatches.FirstPlayer).Single(candidate => candidate.Kind == kind);
        var target = new CellPosition(start.X - Distance, start.Y);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [unit.Id], target));

        var ticks = TestMatches.TickUntil(match, () => !unit.IsMoving);
        Assert.Equal(target, unit.Position.Cell);
        return ticks;
    }
}
