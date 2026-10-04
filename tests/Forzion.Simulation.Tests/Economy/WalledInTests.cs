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

    // A depleted source frees its Cell like a destroyed building frees its own.
    [Fact]
    public void A_Villager_walled_in_by_its_source_sets_out_with_its_load_once_others_deplete_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        Site.Stockpile(match, First, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var villagers = Site.VillagersOf(match, First);
        var walledIn = villagers[1];
        var source = Gather.NearestSource(match.State, walledIn.Position.Cell, ResourceKind.Food);

        // The other two step well away, out of where the Houses go.
        var away = Site.FreeOriginNear(match.State, new CellPosition(source.Cell.X + 10, source.Cell.Y), 1);
        match.Enqueue(new MoveCommand(First, [villagers[0].Id, villagers[2].Id], away));
        TestMatches.TickUntil(match, () => !villagers[0].IsMoving && !villagers[2].IsMoving);

        var (cell, houses) = MapProbe.NeighboursOf(match.State.Map, source.Cell)
            .Where(each => match.State.Map[each] == CellKind.Free)
            .Select(each => (Cell: each, Houses: HousesAroundBut(match, each, source.Cell)))
            .First(each => each.Houses is not null);
        match.Enqueue(new MoveCommand(First, [walledIn.Id], cell));
        Walk.UntilStopped(match, walledIn);

        foreach (var origin in houses!)
        {
            match.Enqueue(new PlaceBuildingCommand(First, BuildingKind.House, origin, []));
        }

        match.Enqueue(new GatherCommand(First, [walledIn.Id], source.Id));
        match.Tick();
        Assert.Empty(match.Events.OfType<CommandRejected>());
        TestMatches.TickUntil(match, () => walledIn.GatherPhase == GatherPhase.ToDropOffPoint);
        Battle.Run(match, Waiting);

        Assert.Equal(cell, walledIn.Position.Cell);
        Assert.False(walledIn.IsMoving);
        Assert.True(walledIn.Load.IsFull);

        match.Enqueue(new GatherCommand(First, [villagers[0].Id, villagers[2].Id], source.Id));
        TestMatches.TickUntil(match, () => Gather.FindSource(match.State, source.Id) is null);

        Assert.True(walledIn.IsMoving);
        Assert.True(MapProbe.IsBeside(Battle.TownCenter(match, First), walledIn.Path[^1]));
    }

    /// <summary>
    /// The origins of at most four Houses that can be placed now and together cover every free
    /// Cell around <paramref name="cell"/> but <paramref name="opening"/>, and neither of the
    /// two; null when no such Houses fit.
    /// </summary>
    private static List<CellPosition>? HousesAroundBut(Match match, CellPosition cell, CellPosition opening)
    {
        var size = Match.BuildingSize(BuildingKind.House);
        var around = MapProbe.Square(new CellPosition(cell.X - 1, cell.Y - 1), 3)
            .Where(each => each != cell && each != opening && match.State.Map[each] == CellKind.Free)
            .ToHashSet();
        var candidates = MapProbe.Square(new CellPosition(cell.X - size, cell.Y - size), size + 2)
            .Where(origin => match.CanPlace(BuildingKind.House, origin))
            .Where(origin => !MapProbe.Square(origin, size).Any(each => each == cell || each == opening))
            .ToList();

        IEnumerable<List<CellPosition>> Choose(int start, int count) => count == 0
            ? [[]]
            : Enumerable.Range(start, candidates.Count - start)
                .SelectMany(index => Choose(index + 1, count - 1).Select(rest => rest.Prepend(candidates[index]).ToList()));

        return Enumerable.Range(1, 4)
            .SelectMany(count => Choose(0, count))
            .FirstOrDefault(houses =>
            {
                var covered = houses.SelectMany(origin => MapProbe.Square(origin, size)).ToList();

                return covered.Distinct().Count() == covered.Count && around.All(covered.Contains);
            });
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
