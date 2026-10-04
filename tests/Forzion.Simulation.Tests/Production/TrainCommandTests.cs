using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class TrainCommandTests
{
    [Fact]
    public void Training_a_Villager_at_the_Town_Center_pays_its_cost_and_puts_it_in_the_training_queue()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        var cost = Match.UnitCost(UnitKind.Villager);
        var before = Train.Stock(player);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal([UnitKind.Villager], townCenter.TrainingQueue);
        Assert.Equal([before[0] - cost.Food, before[1] - cost.Wood, before[2] - cost.Gold], Train.Stock(player));
        Assert.True(cost.Food > 0);
    }

    [Fact]
    public void A_queued_Villager_appears_on_a_free_Cell_beside_the_Town_Center_once_its_training_time_has_passed()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        var unitsBefore = match.State.Units.Count;
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));

        // The tick that applies the order is the first of training.
        for (var tick = 1; tick < Match.TrainTime(UnitKind.Villager); tick++)
        {
            match.Tick();

            Assert.Equal(tick, townCenter.TrainingProgress);
            Assert.Equal(unitsBefore, match.State.Units.Count);
        }

        match.Tick();

        var villager = match.State.Units[^1];
        Assert.Equal(unitsBefore + 1, match.State.Units.Count);
        Assert.Equal([new UnitTrained(villager.Id, townCenter.Id)], match.Events);
        Assert.Equal(UnitKind.Villager, villager.Kind);
        Assert.Equal(TestMatches.FirstPlayer, villager.Owner);
        Assert.Equal(MapPosition.CentreOf(villager.Position.Cell), villager.Position);
        Assert.True(MapProbe.IsBeside(townCenter, villager.Position.Cell));
        Assert.Equal(CellKind.Free, match.State.Map[villager.Position.Cell]);
        Assert.False(villager.IsMoving);
        Assert.Empty(townCenter.TrainingQueue);
        Assert.Equal(0, townCenter.TrainingProgress);
    }

    [Fact]
    public void A_building_trains_the_units_of_its_queue_one_after_another()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));

        var trained = Train.Run(match, 2 * Match.TrainTime(UnitKind.Villager)).Where(happened => happened.Event is UnitTrained).ToList();

        Assert.Equal([Match.TrainTime(UnitKind.Villager), 2 * Match.TrainTime(UnitKind.Villager)], trained.Select(happened => happened.Tick));
        Assert.Empty(townCenter.TrainingQueue);
    }

    [Fact]
    public void The_Town_Center_trains_Villagers_and_the_Barracks_the_military_units()
    {
        Assert.Equal(BuildingKind.TownCenter, Match.TrainedAt(UnitKind.Villager));
        Assert.Equal(BuildingKind.Barracks, Match.TrainedAt(UnitKind.MeleeSoldier));
        Assert.Equal(BuildingKind.Barracks, Match.TrainedAt(UnitKind.RangedSoldier));
        Assert.Equal(BuildingKind.Barracks, Match.TrainedAt(UnitKind.HeavySoldier));
    }
}
