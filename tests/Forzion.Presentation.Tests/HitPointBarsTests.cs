using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class HitPointBarsTests
{
    [Fact]
    public void Whole_entities_that_are_not_selected_show_no_bar()
    {
        var driver = NewDriver();

        Assert.Empty(HitPointBars.Shown(driver, []));
    }

    [Fact]
    public void A_selected_whole_unit_shows_a_full_bar_where_it_is_drawn()
    {
        var driver = NewDriver(out var match);
        var villager = UnitsOf(match, FirstPlayer)[0];

        var bar = Assert.Single(HitPointBars.Shown(driver, [villager.Id]));

        Assert.Equal(new HitPointBar(villager.Id, driver.PositionOf(villager), OverBuilding: false, Fill: 1), bar);
    }

    [Fact]
    public void A_wounded_unit_shows_a_bar_filled_to_the_share_of_hit_points_it_has_left()
    {
        var driver = NewDriver(out var match, WithEnemySoldierAtHome());
        TickUntil(driver, () => match.State.Units.Any(unit => unit.HitPoints < unit.MaxHitPoints));
        var wounded = match.State.Units.First(unit => unit.HitPoints < unit.MaxHitPoints);

        var bar = Assert.Single(HitPointBars.Shown(driver, []));

        Assert.Equal(wounded.Id, bar.Id);
        Assert.Equal((double)wounded.HitPoints / wounded.MaxHitPoints, bar.Fill);
        Assert.True(bar.Fill is > 0 and < 1);
    }

    [Fact]
    public void A_damaged_building_shows_a_bar_over_the_centre_of_its_footprint()
    {
        var driver = NewDriver(out var match, WithEnemySoldierAtHome());
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);
        var soldier = UnitsOf(match, SecondPlayer).Single(unit => unit.Kind == UnitKind.MeleeSoldier);
        match.Enqueue(new AttackCommand(SecondPlayer, [soldier.Id], townCenter.Id));
        TickUntil(driver, () => townCenter.HitPoints < townCenter.MaxHitPoints);

        var bar = Assert.Single(HitPointBars.Shown(driver, []));

        var centre = new MapPoint(townCenter.Origin.X + (townCenter.Width / 2.0), townCenter.Origin.Y + (townCenter.Height / 2.0));
        Assert.Equal(new HitPointBar(townCenter.Id, centre, OverBuilding: true, Fill: (double)townCenter.HitPoints / townCenter.MaxHitPoints), bar);
    }

    [Fact]
    public void Selected_and_wounded_entities_show_one_bar_each_in_ascending_ID_order()
    {
        var driver = NewDriver(out var match, WithEnemySoldierAtHome());
        TickUntil(driver, () => match.State.Units.Any(unit => unit.HitPoints < unit.MaxHitPoints));
        var wounded = match.State.Units.First(unit => unit.HitPoints < unit.MaxHitPoints);
        var townCenter = match.State.Buildings.First(building => building.Owner == FirstPlayer);

        var bars = HitPointBars.Shown(driver, [wounded.Id, townCenter.Id]);

        Assert.Equal(new[] { townCenter.Id, wounded.Id }.OrderBy(id => id.Value), bars.Select(bar => bar.Id));
    }

    [Fact]
    public void A_selected_entity_no_longer_in_the_match_shows_no_bar()
    {
        var driver = NewDriver();

        Assert.Empty(HitPointBars.Shown(driver, [new EntityId(9_999)]));
    }
}
