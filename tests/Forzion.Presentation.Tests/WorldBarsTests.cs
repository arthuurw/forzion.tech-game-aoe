using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class WorldBarsTests
{
    [Fact]
    public void Whole_units_and_complete_buildings_get_no_bar()
    {
        var match = Match.Create(PlainConfig());

        Assert.Empty(WorldBars.Of(match.State, []));
    }

    [Fact]
    public void A_selected_whole_unit_gets_a_full_hit_point_bar()
    {
        var match = Match.Create(PlainConfig());
        var villager = match.State.Units.First(unit => unit.Owner == FirstPlayer);

        var bar = Assert.Single(WorldBars.Of(match.State, [villager.Id]));

        Assert.Equal(new WorldBar(villager.Id, WorldBarKind.HitPoints, 1), bar);
    }

    [Fact]
    public void A_selected_entity_no_longer_in_the_match_gets_no_bar()
    {
        var match = Match.Create(PlainConfig());

        Assert.Empty(WorldBars.Of(match.State, [new EntityId(9_999)]));
    }

    [Fact]
    public void A_wounded_unit_gets_a_bar_filled_with_the_share_of_hit_points_it_has_left()
    {
        // An enemy soldier beside the first Player's Villagers attacks them on its own.
        var match = Match.Create(WithEnemySoldierAtHome());

        while (match.State.Units.All(unit => unit.HitPoints == unit.MaxHitPoints))
        {
            match.Tick();
        }

        var wounded = match.State.Units.Single(unit => unit.HitPoints < unit.MaxHitPoints);
        var bar = Assert.Single(WorldBars.Of(match.State, []));

        Assert.Equal((wounded.Id, WorldBarKind.HitPoints), (bar.Entity, bar.Kind));
        Assert.Equal((double)wounded.HitPoints / wounded.MaxHitPoints, bar.Fill, 6);
        Assert.InRange(bar.Fill, 0.01, 0.99);
    }

    [Fact]
    public void A_construction_site_gets_a_bar_filled_with_how_far_its_construction_has_gone()
    {
        var match = Match.Create(PlainConfig());
        var origin = FreeOriginNearFirstHome(match, BuildingKind.House);
        match.Enqueue(new PlaceBuildingCommand(FirstPlayer, BuildingKind.House, origin, [UnitsOf(match, FirstPlayer)[0].Id]));
        match.Tick();
        var site = match.State.Buildings.Single(building => building.Origin == origin);

        while (site.BuildProgress < site.BuildTime / 2)
        {
            match.Tick();
        }

        var bar = Assert.Single(WorldBars.Of(match.State, []));

        Assert.Equal((site.Id, WorldBarKind.Construction), (bar.Entity, bar.Kind));
        Assert.Equal(0.5, bar.Fill, 2);
    }
}
