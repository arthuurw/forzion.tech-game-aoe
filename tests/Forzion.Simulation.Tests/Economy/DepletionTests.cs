using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Economy;

public class DepletionTests
{
    [Fact]
    public void A_source_gathered_to_the_end_leaves_the_map_and_frees_its_Cell()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var villagers = match.State.UnitsOf(TestMatches.FirstPlayer).ToList();
        var source = Gather.NearestSource(match.State, TestMatches.MiddleVillager(match).Position.Cell, ResourceKind.Food);
        var initial = source.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), source.Id));

        TestMatches.TickUntil(match, () => Gather.FindSource(match.State, source.Id) is null);

        Assert.Contains(new ResourceSourceDepleted(source.Id), match.Events);
        Assert.Equal(0, source.Amount);
        Assert.Equal(CellKind.Free, match.State.Map[source.Cell]);

        // Every unit of it went to a Villager: none was lost or taken twice.
        Assert.Equal(initial, player.AmountOf(ResourceKind.Food) + villagers.Sum(villager => villager.Load.Amount));
    }

    [Fact]
    public void A_source_reports_its_depletion_only_once()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = match.State.UnitsOf(TestMatches.FirstPlayer).ToList();
        var source = Gather.NearestSource(match.State, TestMatches.MiddleVillager(match).Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), source.Id));
        var reports = 0;

        for (var tick = 0; tick < TestMatches.TickLimit; tick++)
        {
            match.Tick();
            reports += match.Events.Count(matchEvent => matchEvent == new ResourceSourceDepleted(source.Id));
        }

        Assert.Equal(1, reports);
    }
}
