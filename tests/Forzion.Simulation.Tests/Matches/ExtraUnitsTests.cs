namespace Forzion.Simulation.Tests.Matches;

public class ExtraUnitsTests
{
    [Fact]
    public void A_Player_configured_with_extra_units_starts_with_them_on_the_centres_of_their_Cells()
    {
        var plain = TestMatches.TwoPlayerMatch();
        var west = TestArmies.BesideHome(plain, TestMatches.FirstPlayer, -2, 0);
        var east = TestArmies.BesideHome(plain, TestMatches.SecondPlayer, 2, 0);

        var match = Match.Create(TestArmies.Config(
            first: [new StartingUnit(UnitKind.MeleeSoldier, west)],
            second: [new StartingUnit(UnitKind.RangedSoldier, east)]));

        var units = match.State.Units;
        Assert.Equal(plain.State.Units.Count + 2, units.Count);
        Assert.Equal(plain.State.Units.Select(unit => unit.Id), units.Take(plain.State.Units.Count).Select(unit => unit.Id));
        Assert.Collection(
            units.Skip(plain.State.Units.Count),
            melee =>
            {
                Assert.Equal(TestMatches.FirstPlayer, melee.Owner);
                Assert.Equal(UnitKind.MeleeSoldier, melee.Kind);
                Assert.Equal(MapPosition.CentreOf(west), melee.Position);
            },
            ranged =>
            {
                Assert.Equal(TestMatches.SecondPlayer, ranged.Owner);
                Assert.Equal(UnitKind.RangedSoldier, ranged.Kind);
                Assert.Equal(MapPosition.CentreOf(east), ranged.Position);
            });
    }

    [Fact]
    public void Extra_units_change_the_hash()
    {
        var plain = TestMatches.TwoPlayerMatch();
        var cell = TestArmies.BesideHome(plain, TestMatches.FirstPlayer, -2, 0);

        var match = Match.Create(TestArmies.Config(first: [new StartingUnit(UnitKind.MeleeSoldier, cell)]));

        Assert.NotEqual(plain.StateHash, match.StateHash);
    }

    [Fact]
    public void An_extra_unit_on_a_blocked_Cell_is_refused()
    {
        var plain = TestMatches.TwoPlayerMatch();
        var underTownCenter = plain.State.Buildings[0].Origin;

        Assert.Throws<ArgumentException>(() => Match.Create(TestArmies.Config(
            first: [new StartingUnit(UnitKind.MeleeSoldier, underTownCenter)])));
    }

    [Fact]
    public void An_extra_unit_outside_the_map_is_refused()
    {
        Assert.Throws<ArgumentException>(() => Match.Create(TestArmies.Config(
            first: [new StartingUnit(UnitKind.MeleeSoldier, new CellPosition(-1, 0))])));
    }
}
