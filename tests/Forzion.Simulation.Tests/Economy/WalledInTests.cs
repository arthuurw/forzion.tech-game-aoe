using Forzion.Simulation.Tests.Combat;
using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

/// <summary>
/// Villagers that cannot walk to where their job takes them: they wait where they are, keeping
/// the job, and set out once a building in their way is destroyed.
/// </summary>
public class WalledInTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    /// <summary>Ticks the tests let a walled-in Villager wait, to show it does not give up.</summary>
    private const int Waiting = 200;

    [Fact]
    public void A_Villager_with_a_full_load_that_can_reach_no_drop_off_point_waits_and_sets_out_once_its_way_opens()
    {
        var match = Battle.Raiders();
        Site.Stockpile(match, First, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var food = FoodOf(match);
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var load = villager.Load;
        var (cell, walls) = WallIn(match, villager);

        match.Enqueue(new GatherCommand(First, [villager.Id], source.Id));
        Battle.Run(match, Waiting);

        Assert.Equal(GatherPhase.ToDropOffPoint, villager.GatherPhase);
        Assert.Equal(cell, villager.Position.Cell);
        Assert.False(villager.IsMoving);
        Assert.Equal(load, villager.Load);
        Assert.Equal(food, FoodOf(match));

        DestroyOne(match, walls);

        Assert.Equal(GatherPhase.ToDropOffPoint, villager.GatherPhase);
        Assert.True(villager.IsMoving);
        Assert.True(MapProbe.IsBeside(Battle.TownCenter(match, First), villager.Path[^1]));
    }

    [Fact]
    public void A_Villager_sent_to_a_source_it_cannot_reach_waits_and_sets_out_once_its_way_opens()
    {
        var match = Battle.Raiders();
        Site.Stockpile(match, First, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var (cell, walls) = WallIn(match, villager);

        match.Enqueue(new GatherCommand(First, [villager.Id], source.Id));
        Battle.Run(match, Waiting);

        Assert.Equal(GatherPhase.ToSource, villager.GatherPhase);
        Assert.Equal(source.Id, villager.GatherSource);
        Assert.Equal(cell, villager.Position.Cell);
        Assert.False(villager.IsMoving);

        DestroyOne(match, walls);

        Assert.True(villager.IsMoving);
        Assert.True(MapProbe.Touch(villager.Path[^1], source.Cell));
    }

    [Fact]
    public void A_Villager_sent_to_a_site_it_cannot_reach_waits_and_sets_out_once_its_way_opens()
    {
        var match = Battle.Raiders();
        Site.Stockpile(match, First, 5 * Match.BuildingCost(BuildingKind.House).Wood);
        var villager = TestMatches.MiddleVillager(match);
        var (cell, walls) = WallIn(match, villager);
        var origin = Site.FreeOriginNear(match.State, cell, Match.BuildingSize(BuildingKind.House));

        match.Enqueue(new PlaceBuildingCommand(First, BuildingKind.House, origin, [villager.Id]));
        match.Tick();
        var site = match.State.Buildings[^1];
        Battle.Run(match, Waiting);

        Assert.Equal(site.Id, villager.ConstructionSite);
        Assert.Equal(cell, villager.Position.Cell);
        Assert.False(villager.IsMoving);
        Assert.Equal(0, site.BuildProgress);

        DestroyOne(match, walls);

        Assert.True(villager.IsMoving);
        Assert.True(MapProbe.IsBeside(site, villager.Path[^1]));
    }

    private static int FoodOf(Match match) => match.State.Players.Single(player => player.Id == First).AmountOf(ResourceKind.Food);

    /// <summary>Has the raiders destroy the first of the walls and ticks until the tick it falls in.</summary>
    private static void DestroyOne(Match match, IReadOnlyList<BuildingState> walls)
    {
        Battle.Raid(match, walls[0]);
        TestMatches.TickUntil(match, () => Battle.Building(match, walls[0].Id) is null);
    }

    /// <summary>
    /// Walks the Villager to the middle of a free square five Cells on a side and places four
    /// Houses of its Player around it, with no builder, like the blades of a pinwheel: together
    /// they cover the eight Cells around the middle one, so the Villager can walk nowhere.
    /// Returns the Cell it is walled in on and the four Houses. The Player must hold the Wood
    /// for them.
    /// </summary>
    private static (CellPosition Cell, List<BuildingState> Walls) WallIn(Match match, UnitState villager)
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

        return (middle, match.State.Buildings.TakeLast(blades.Length).ToList());
    }
}
