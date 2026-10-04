using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Production;

namespace Forzion.Simulation.Tests.Ages;

public class AgeHashTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void Players_alike_but_for_their_Age_have_different_hashes()
    {
        // Age II costs nothing and takes no time, so advancing changes the Age and nothing else.
        var instant = new Faction(
            new FactionId(1),
            "TEST_INSTANT",
            [
                new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [], []),
                new FactionAge("TEST_AGE_2", new Cost(0, 0, 0), 0, [], []),
            ],
            new Dictionary<UnitKind, string>());
        var config = TestMatches.SinglePlayerConfig() with { Factions = [instant] };
        var advanced = Match.Create(config);
        var staying = Match.Create(config);
        advanced.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(advanced, First).Id));

        advanced.Tick();
        staying.Tick();

        Assert.Equal(2, advanced.State.Players[0].Age);
        Assert.Equal(Train.Stock(staying.State.Players[0]), Train.Stock(advanced.State.Players[0]));
        Assert.NotEqual(staying.StateHash, advanced.StateHash);
    }

    [Fact]
    public void Age_Advances_ordered_a_tick_apart_have_different_hashes_while_underway()
    {
        var earlier = TestFactions.ThreeAgeMatch();
        var later = TestFactions.ThreeAgeMatch();
        earlier.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(earlier, First).Id));
        earlier.Tick();
        later.Tick();
        later.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(later, First).Id));

        earlier.Tick();
        later.Tick();

        Assert.Equal(Train.Stock(earlier.State.Players[0]), Train.Stock(later.State.Players[0]));
        Assert.Equal(1, earlier.State.Players[0].Age);
        Assert.Equal(1, later.State.Players[0].Age);
        Assert.NotEqual(earlier.StateHash, later.StateHash);
    }

    [Theory]
    [InlineData(51, 10, UnitKind.HeavySoldier, BuildingKind.Barracks)]
    [InlineData(50, 11, UnitKind.HeavySoldier, BuildingKind.Barracks)]
    [InlineData(50, 10, UnitKind.MeleeSoldier, BuildingKind.Barracks)]
    [InlineData(50, 10, UnitKind.HeavySoldier, BuildingKind.House)]
    public void Factions_of_the_same_id_that_differ_in_an_Age_have_different_hashes(int food, int time, UnitKind unit, BuildingKind building)
    {
        static Faction WithSecondAge(int food, int time, UnitKind unit, BuildingKind building) => new(
            new FactionId(1),
            "TEST",
            [
                new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [UnitKind.Villager], []),
                new FactionAge("TEST_AGE_2", new Cost(food, 0, 0), time, [unit], [building]),
            ],
            new Dictionary<UnitKind, string>());

        var usual = Match.Create(TestMatches.SinglePlayerConfig() with
        {
            Factions = [WithSecondAge(50, 10, UnitKind.HeavySoldier, BuildingKind.Barracks)],
        });
        var other = Match.Create(TestMatches.SinglePlayerConfig() with { Factions = [WithSecondAge(food, time, unit, building)] });

        Assert.NotEqual(usual.StateHash, other.StateHash);
    }

    [Fact]
    public void Factions_alike_but_for_the_order_of_what_an_Age_unlocks_have_the_same_hash()
    {
        static Faction Unlocking(IReadOnlyList<UnitKind> units) => new(
            new FactionId(1),
            "TEST",
            [new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, units, [])],
            new Dictionary<UnitKind, string>());

        var first = Match.Create(TestMatches.SinglePlayerConfig() with { Factions = [Unlocking([UnitKind.Villager, UnitKind.MeleeSoldier])] });
        var second = Match.Create(TestMatches.SinglePlayerConfig() with { Factions = [Unlocking([UnitKind.MeleeSoldier, UnitKind.Villager])] });

        Assert.Equal(first.StateHash, second.StateHash);
    }
}
