using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

public class EconomyHashTests
{
    [Fact]
    public void A_gather_order_makes_the_hash_diverge_before_anything_is_gathered()
    {
        var gathering = BesideFoodSource(out var source);
        var standing = BesideFoodSource(out _);
        gathering.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [TestMatches.MiddleVillager(gathering).Id], source.Id));

        gathering.Tick();
        standing.Tick();

        // Already beside the source, the Villager does not move, and one tick gathers nothing yet.
        Assert.Equal(PositionsOf(standing), PositionsOf(gathering));
        Assert.Equal(AmountsOf(standing), AmountsOf(gathering));
        Assert.Equal(GatherPhase.Gathering, TestMatches.MiddleVillager(gathering).GatherPhase);
        Assert.NotEqual(standing.StateHash, gathering.StateHash);
    }

    [Fact]
    public void Villagers_gathering_alike_but_for_how_long_they_have_worked_have_different_hashes()
    {
        var earlier = BesideFoodSource(out var source);
        var later = BesideFoodSource(out _);
        earlier.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [TestMatches.MiddleVillager(earlier).Id], source.Id));
        earlier.Tick();
        later.Tick();
        later.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [TestMatches.MiddleVillager(later).Id], source.Id));

        // One tick more of work, and still nothing gathered in either match.
        for (var tick = 0; tick < 2; tick++)
        {
            earlier.Tick();
            later.Tick();
        }

        Assert.Equal(PositionsOf(later), PositionsOf(earlier));
        Assert.Equal(AmountsOf(later), AmountsOf(earlier));
        Assert.Equal(TestMatches.MiddleVillager(later).Load, TestMatches.MiddleVillager(earlier).Load);
        Assert.Equal(TestMatches.MiddleVillager(later).GatherPhase, TestMatches.MiddleVillager(earlier).GatherPhase);
        Assert.NotEqual(later.StateHash, earlier.StateHash);
    }

    [Fact]
    public void Matches_given_the_same_gather_order_have_the_same_hash_at_every_tick()
    {
        var first = TestMatches.TwoPlayerMatch();
        var second = TestMatches.TwoPlayerMatch();
        var villagers = first.State.UnitsOf(TestMatches.FirstPlayer).Select(unit => unit.Id).ToList();
        var source = Gather.NearestSource(first.State, TestMatches.MiddleVillager(first).Position.Cell, ResourceKind.Food);
        first.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers, source.Id));
        second.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers, source.Id));

        for (var tick = 0; tick < 2000; tick++)
        {
            first.Tick();
            second.Tick();

            Assert.Equal(first.StateHash, second.StateHash);
        }

        Assert.True(Gather.Delivered(first.State.Players[0], ResourceKind.Food) > 0);
    }

    /// <summary>A match whose middle Villager has walked to a Cell beside the Food source nearest to it.</summary>
    private static Match BesideFoodSource(out ResourceSourceState source)
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = TestMatches.MiddleVillager(match);
        var reachable = MapProbe.ReachableFrom(match.State.Map, villager.Position.Cell);
        source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var beside = MapProbe.NeighboursOf(match.State.Map, source.Cell).First(reachable.Contains);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], beside));
        Walk.UntilStopped(match, villager);

        return match;
    }

    private static List<MapPosition> PositionsOf(Match match) => match.State.Units.Select(unit => unit.Position).ToList();

    private static List<int> AmountsOf(Match match) => match.State.ResourceSources.Select(source => source.Amount).ToList();
}
