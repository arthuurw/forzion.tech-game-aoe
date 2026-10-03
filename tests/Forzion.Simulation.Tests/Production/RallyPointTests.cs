using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Production;

public class RallyPointTests
{
    [Fact]
    public void A_unit_trained_at_a_building_with_a_rally_point_walks_to_it_on_its_own()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        var rallyPoint = TestArmies.BesideHome(match, TestMatches.FirstPlayer, 0, 8);
        match.Enqueue(new SetRallyPointCommand(TestMatches.FirstPlayer, townCenter.Id, rallyPoint));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(rallyPoint, townCenter.RallyPoint);

        var trained = TrainOne(match);

        Assert.True(trained.IsMoving);
        Assert.Equal(rallyPoint, trained.Path[^1]);

        Walk.UntilStopped(match, trained);

        Assert.Equal(MapPosition.CentreOf(rallyPoint), trained.Position);
    }

    [Theory]
    [InlineData(-8, 0)]
    [InlineData(8, 0)]
    [InlineData(0, -8)]
    [InlineData(0, 8)]
    public void A_unit_trained_at_a_building_with_a_rally_point_appears_on_the_side_facing_it(int x, int y)
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new SetRallyPointCommand(
            TestMatches.FirstPlayer, townCenter.Id, TestArmies.BesideHome(match, TestMatches.FirstPlayer, x, y)));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Tick();

        var cell = TrainOne(match).Position.Cell;

        // Just outside the footprint, on the side the rally point lies.
        Assert.Equal(
            (Math.Sign(x), Math.Sign(y)),
            (Side(cell.X, townCenter.Origin.X, townCenter.Width), Side(cell.Y, townCenter.Origin.Y, townCenter.Height)));
    }

    [Fact]
    public void A_unit_trained_at_a_building_without_a_rally_point_stands_where_it_appears()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Tick();

        var trained = TrainOne(match);
        match.Tick();

        Assert.Null(townCenter.RallyPoint);
        Assert.False(trained.IsMoving);
    }

    [Fact]
    public void A_rally_point_outside_the_map_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.DestinationOutsideMap, match =>
            new SetRallyPointCommand(TestMatches.FirstPlayer, Train.TownCenter(match, TestMatches.FirstPlayer).Id, new CellPosition(-1, 0)));
    }

    [Fact]
    public void A_rally_point_for_another_Players_building_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingOfAnotherPlayer, match =>
            new SetRallyPointCommand(TestMatches.FirstPlayer, Train.TownCenter(match, TestMatches.SecondPlayer).Id, new CellPosition(1, 1)));
    }

    [Fact]
    public void A_rally_point_for_a_building_that_trains_nothing_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingCannotTrain, match =>
            new SetRallyPointCommand(
                TestMatches.FirstPlayer, Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.House).Id, new CellPosition(1, 1)));
    }

    [Fact]
    public void Buildings_alike_but_for_their_rally_point_have_different_hashes()
    {
        var with = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        with.Enqueue(new SetRallyPointCommand(
            TestMatches.FirstPlayer, Train.TownCenter(with, TestMatches.FirstPlayer).Id, new CellPosition(1, 1)));

        with.Tick();
        without.Tick();

        Assert.NotEqual(without.StateHash, with.StateHash);
    }

    /// <summary>Ticks until the next unit is trained and returns it.</summary>
    private static UnitState TrainOne(Match match)
    {
        var count = match.State.Units.Count;
        Gather.Until(match, () => match.State.Units.Count > count);

        return match.State.Units[^1];
    }

    /// <summary>-1 before the footprint's span along one axis, 1 past it, 0 within it.</summary>
    private static int Side(int coordinate, int origin, int length) =>
        coordinate < origin ? -1 : coordinate >= origin + length ? 1 : 0;
}
