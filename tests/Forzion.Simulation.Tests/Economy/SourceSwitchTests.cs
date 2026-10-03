using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

public class SourceSwitchTests
{
    // A map whose only Food sources are the two each Player has beside its Town Center.
    private const ulong TwoFoodSourcesEachSeed = 4;

    [Fact]
    public void Villagers_whose_source_runs_out_move_on_to_the_nearest_source_of_the_same_Resource()
    {
        var match = TestMatches.TwoPlayerMatch(TwoFoodSourcesEachSeed);
        var villagers = OwnVillagers(match);
        var first = Gather.NearestSource(match.State, Walk.MiddleVillager(match).Position.Cell, ResourceKind.Food);
        var next = match.State.ResourceSources
            .Where(source => source.Kind == ResourceKind.Food && source != first)
            .OrderBy(source => Walk.SquaredDistance(source.Cell, first.Cell))
            .First();
        var nextInitial = next.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), first.Id));

        Gather.Until(match, () => Gather.FindSource(match.State, first.Id) is null);

        // Both of the Player's Food sources lie beside its Town Center: the next one is nearby.
        Assert.True(Walk.SquaredDistance(first.Cell, next.Cell) <= 10 * 10);
        Assert.All(villagers, villager => Assert.Equal(next.Id, villager.GatherSource));

        // With no further command, they gather from it.
        Gather.Until(match, () => next.Amount < nextInitial);

        Assert.Contains(villagers, villager =>
            villager.GatherPhase == GatherPhase.Gathering && Gather.Touch(villager.Position.Cell, next.Cell));
    }

    [Fact]
    public void Villagers_with_no_source_of_the_same_Resource_nearby_stand_idle()
    {
        var match = TestMatches.TwoPlayerMatch(TwoFoodSourcesEachSeed);
        var player = match.State.Players[0];
        var villagers = OwnVillagers(match);
        var home = match.State.Buildings[0].Origin;
        var food = match.State.ResourceSources.Where(source => source.Kind == ResourceKind.Food).ToList();
        var own = food.Where(source => Walk.SquaredDistance(source.Cell, home) < 10 * 10).ToList();
        var initial = own.Sum(source => source.Amount);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), own[0].Id));

        // The other Food sources are the enemy's, across the map.
        Assert.Equal(2, own.Count);
        Assert.All(food.Except(own), source => Assert.True(Walk.SquaredDistance(source.Cell, home) > 40 * 40));

        Gather.Until(match, () =>
            own.All(source => Gather.FindSource(match.State, source.Id) is null)
            && villagers.All(villager => villager.GatherPhase == GatherPhase.None && !villager.IsMoving));

        // Idle: no source, no walk, and nothing changes as time goes by.
        var positions = villagers.Select(villager => villager.Position).ToList();
        var loads = villagers.Select(villager => villager.Load.Amount).ToList();

        for (var tick = 0; tick < 200; tick++)
        {
            match.Tick();
        }

        Assert.All(villagers, villager => Assert.Null(villager.GatherSource));
        Assert.All(villagers, villager => Assert.Equal(GatherPhase.None, villager.GatherPhase));
        Assert.Equal(positions, villagers.Select(villager => villager.Position));
        Assert.Equal(loads, villagers.Select(villager => villager.Load.Amount));
        Assert.All(villagers, villager => Assert.Equal(MapPosition.CentreOf(villager.Position.Cell), villager.Position));

        // Both sources went, in full, to the Player or to the loads its Villagers still carry.
        Assert.Equal(initial, Gather.Delivered(player, ResourceKind.Food) + loads.Sum());
    }

    private static List<UnitState> OwnVillagers(Match match) =>
        match.State.UnitsOf(TestMatches.FirstPlayer).ToList();
}
