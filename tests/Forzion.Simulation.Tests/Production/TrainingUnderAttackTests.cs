using Forzion.Simulation.Tests.Combat;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class TrainingUnderAttackTests
{
    [Fact]
    public void Units_queued_before_a_House_is_destroyed_are_still_trained_past_the_lower_population_limit()
    {
        var match = Battle.Raiders();
        var first = TestMatches.FirstPlayer;
        var house = Train.Complete(match, first, BuildingKind.House);
        var townCenter = Train.TownCenter(match, first);

        while (match.State.Players[0].AmountOf(ResourceKind.Food) >= Match.UnitCost(UnitKind.Villager).Food)
        {
            match.Enqueue(new TrainCommand(first, townCenter.Id, UnitKind.Villager));
            match.Tick();

            Assert.Empty(match.Events);
        }

        var population = match.State.PopulationOf(first);
        Battle.Raid(match, house);
        Battle.TickUntil(match, () => house.HitPoints <= 0);

        // The raiders head home before they turn on the Villagers.
        match.Enqueue(new MoveCommand(
            TestMatches.SecondPlayer,
            Battle.RaidersOf(match).Select(raider => raider.Id).ToList(),
            TestArmies.BesideHome(match, TestMatches.SecondPlayer, 2, 0)));

        Assert.NotEmpty(townCenter.TrainingQueue);
        Assert.True(population > match.State.PopulationLimitOf(first));

        Battle.TickUntil(match, () => townCenter.TrainingQueue.Count == 0);

        Assert.Equal(population, match.State.UnitsOf(first).Count());
    }
}
