using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

/// <summary>Villagers that cannot walk to where their gathering takes them.</summary>
public class WalledInTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void A_Villager_with_a_full_load_that_can_reach_no_drop_off_point_stands_idle_keeping_its_load()
    {
        var match = TestMatches.TwoPlayerMatch();
        Site.Stockpile(match, First, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var food = FoodOf(match);
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var load = villager.Load;
        var cell = WallIn(match, villager);

        match.Enqueue(new GatherCommand(First, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.GatherPhase == GatherPhase.None);

        Assert.Equal(cell, villager.Position.Cell);
        Assert.False(villager.IsMoving);
        Assert.Equal(load, villager.Load);
        Assert.Equal(food, FoodOf(match));
    }

    [Fact]
    public void A_Villager_sent_to_a_source_it_cannot_reach_stands_idle_where_it_is()
    {
        var match = TestMatches.TwoPlayerMatch();
        Site.Stockpile(match, First, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var cell = WallIn(match, villager);

        match.Enqueue(new GatherCommand(First, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.GatherPhase == GatherPhase.None);

        Assert.Equal(cell, villager.Position.Cell);
        Assert.False(villager.IsMoving);
        Assert.Null(villager.GatherSource);
    }

    private static int FoodOf(Match match) => match.State.Players.Single(player => player.Id == First).AmountOf(ResourceKind.Food);

    /// <summary>
    /// Walks the Villager to the middle of a free square five Cells on a side and places four Houses
    /// of its Player around it, with no builder, like the blades of a pinwheel: together they
    /// cover the eight Cells around the middle one, so the Villager can walk nowhere. Returns
    /// the Cell it is walled in on. The Player must hold the Wood for the four Houses.
    /// </summary>
    private static CellPosition WallIn(Match match, UnitState villager)
    {
        var origin = Site.FreeOriginNear(match.State, villager.Position.Cell, 5);
        var middle = new CellPosition(origin.X + 2, origin.Y + 2);
        match.Enqueue(new MoveCommand(villager.Owner, [villager.Id], middle));
        Walk.UntilStopped(match, villager);

        (int X, int Y)[] blades = [(-1, 1), (1, 0), (0, -2), (-2, -1)];

        foreach (var (x, y) in blades)
        {
            match.Enqueue(new PlaceBuildingCommand(
                villager.Owner, BuildingKind.House, new CellPosition(middle.X + x, middle.Y + y), []));
        }

        match.Tick();

        Assert.Empty(match.Events.OfType<CommandRejected>());
        Assert.Equal(middle, villager.Position.Cell);

        return middle;
    }
}
