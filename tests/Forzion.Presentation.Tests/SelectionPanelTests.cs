using Forzion.Simulation;
using static Forzion.Presentation.Tests.HudMatches;

namespace Forzion.Presentation.Tests;

public class SelectionPanelTests
{
    [Fact]
    public void With_nothing_selected_the_panel_shows_nothing()
    {
        var match = Portuguese();

        var panel = SelectionPanel.For(match.State, FirstPlayer, []);

        Assert.Empty(panel.Units);
        Assert.Null(panel.Building);
        Assert.Empty(panel.BuildingChoices);
    }

    [Fact]
    public void Selected_units_are_shown_with_their_Faction_name_and_hit_points()
    {
        var match = Portuguese();
        var villagers = UnitsOf(match, FirstPlayer);

        var panel = SelectionPanel.For(match.State, FirstPlayer, villagers.Select(unit => unit.Id).ToList());

        Assert.Equal(villagers.Select(unit => unit.Id), panel.Units.Select(unit => unit.Id));
        Assert.All(panel.Units, unit =>
        {
            Assert.Equal("FACTION_PORTUGUESE_VILLAGER", unit.NameKey);
            Assert.Equal(villagers[0].MaxHitPoints, unit.MaxHitPoints);
            Assert.Equal(villagers[0].HitPoints, unit.HitPoints);
        });
    }

    [Fact]
    public void With_Villagers_selected_the_panel_offers_every_building_the_Faction_places_with_its_cost()
    {
        var match = Portuguese();

        var panel = SelectionPanel.For(match.State, FirstPlayer, [UnitsOf(match, FirstPlayer)[0].Id]);

        Assert.Equal(
            [BuildingKind.House, BuildingKind.Storehouse, BuildingKind.Barracks],
            panel.BuildingChoices.Select(choice => choice.Kind));
        Assert.All(panel.BuildingChoices, choice =>
        {
            Assert.Equal(Match.BuildingCost(choice.Kind), choice.Cost);
            Assert.False(choice.IsLocked);
        });
        Assert.Equal(
            ["BUILDING_HOUSE", "BUILDING_STOREHOUSE", "BUILDING_BARRACKS"],
            panel.BuildingChoices.Select(choice => choice.NameKey));
    }

    [Fact]
    public void A_building_the_Age_of_the_Player_has_not_unlocked_is_offered_locked_naming_the_Age_that_unlocks_it()
    {
        var match = OfThreeAges();

        var panel = SelectionPanel.For(match.State, FirstPlayer, [UnitsOf(match, FirstPlayer)[0].Id]);

        Assert.Equal(
            [(BuildingKind.House, null), (BuildingKind.Storehouse, "TEST_AGE_2"), (BuildingKind.Barracks, "TEST_AGE_2")],
            panel.BuildingChoices.Select(choice => (choice.Kind, choice.LockedUntilAgeNameKey)));
    }

    [Fact]
    public void Without_Villagers_selected_the_panel_offers_no_building()
    {
        var match = Portuguese(firstExtras: [new StartingUnit(UnitKind.MeleeSoldier, BesideFirstHome())]);
        var soldier = UnitsOf(match, FirstPlayer).Single(unit => unit.Kind == UnitKind.MeleeSoldier);

        var panel = SelectionPanel.For(match.State, FirstPlayer, [soldier.Id]);

        Assert.Equal("FACTION_PORTUGUESE_MELEE_SOLDIER", Assert.Single(panel.Units).NameKey);
        Assert.Empty(panel.BuildingChoices);
    }
}
