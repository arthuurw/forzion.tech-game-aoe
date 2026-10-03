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

    [Fact]
    public void The_selected_Town_Center_is_shown_with_its_hit_points_the_units_it_trains_and_the_next_Age_Advance()
    {
        var match = Portuguese();
        var townCenter = TownCenterOf(match, FirstPlayer);

        var building = SelectionPanel.For(match.State, FirstPlayer, [townCenter.Id]).Building;

        Assert.NotNull(building);
        Assert.Equal((townCenter.Id, BuildingKind.TownCenter, "BUILDING_TOWN_CENTER"), (building.Id, building.Kind, building.NameKey));
        Assert.Equal((townCenter.HitPoints, townCenter.MaxHitPoints), (building.HitPoints, building.MaxHitPoints));
        Assert.Null(building.ConstructionProgress);
        Assert.Empty(building.TrainingQueue);
        var villager = Assert.Single(building.UnitChoices);
        Assert.Equal(
            new UnitChoice(UnitKind.Villager, "FACTION_PORTUGUESE_VILLAGER", Match.UnitCost(UnitKind.Villager), null),
            villager);
        Assert.Equal(
            new AgeAdvanceChoice("FACTION_PORTUGUESE_AGE_2", Factions.Portuguese.Ages[1].AdvanceCost, null),
            building.AgeAdvance);
        Assert.Null(building.RallyPoint);
    }

    [Fact]
    public void The_training_queue_is_shown_in_order_with_how_far_the_first_unit_has_trained()
    {
        var match = Portuguese();
        var townCenter = TownCenterOf(match, FirstPlayer);
        match.Enqueue(new TrainCommand(FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Enqueue(new TrainCommand(FirstPlayer, townCenter.Id, UnitKind.Villager));
        var quarter = Match.TrainTime(UnitKind.Villager) / 4;

        Run(match, quarter);

        var queue = SelectionPanel.For(match.State, FirstPlayer, [townCenter.Id]).Building!.TrainingQueue;
        Assert.Equal([UnitKind.Villager, UnitKind.Villager], queue.Select(queued => queued.Kind));
        Assert.All(queue, queued => Assert.Equal("FACTION_PORTUGUESE_VILLAGER", queued.NameKey));
        Assert.Equal(0.25, queue[0].Progress, 2);
        Assert.Equal(0, queue[1].Progress);
    }

    [Fact]
    public void The_rally_point_of_the_selected_building_is_shown()
    {
        var match = Portuguese();
        var townCenter = TownCenterOf(match, FirstPlayer);
        match.Enqueue(new SetRallyPointCommand(FirstPlayer, townCenter.Id, new CellPosition(5, 6)));
        match.Tick();

        var building = SelectionPanel.For(match.State, FirstPlayer, [townCenter.Id]).Building;

        Assert.Equal(new CellPosition(5, 6), building!.RallyPoint);
    }

    [Fact]
    public void A_selected_construction_site_shows_how_far_its_construction_has_gone_and_takes_no_order()
    {
        var match = Portuguese();
        var builder = UnitsOf(match, FirstPlayer)[0];
        var site = PlaceNear(match, BuildingKind.Barracks, [builder.Id]);

        while (site.BuildProgress < site.BuildTime / 4)
        {
            match.Tick();
        }

        var building = SelectionPanel.For(match.State, FirstPlayer, [site.Id]).Building;

        Assert.Equal(0.25, building!.ConstructionProgress!.Value, 2);
        Assert.Empty(building.UnitChoices);
        Assert.Null(building.AgeAdvance);
    }

    [Fact]
    public void During_an_Age_Advance_the_Town_Center_shows_how_far_it_has_gone()
    {
        var match = OfThreeAges();
        var townCenter = TownCenterOf(match, FirstPlayer);
        match.Enqueue(new AgeAdvanceCommand(FirstPlayer, townCenter.Id));

        // The tick that applies the order is the first of the advance's 10.
        Run(match, 4);

        var advance = SelectionPanel.For(match.State, FirstPlayer, [townCenter.Id]).Building!.AgeAdvance;

        Assert.Equal(new AgeAdvanceChoice("TEST_AGE_2", new Cost(0, 0, 0), 0.4), advance);
    }

    [Fact]
    public void In_the_last_Age_of_its_Faction_the_Town_Center_offers_no_Age_Advance()
    {
        var match = OfThreeAges();
        AdvanceAge(match, FirstPlayer);
        AdvanceAge(match, FirstPlayer);

        var building = SelectionPanel.For(match.State, FirstPlayer, [TownCenterOf(match, FirstPlayer).Id]).Building;

        Assert.Null(building!.AgeAdvance);
    }

    [Fact]
    public void The_Barracks_offers_the_military_units_locked_ones_naming_the_Age_that_unlocks_them()
    {
        var match = OfThreeAges();
        AdvanceAge(match, FirstPlayer);
        var barracks = PlaceNear(match, BuildingKind.Barracks, UnitsOf(match, FirstPlayer).Select(unit => unit.Id).ToList());

        while (!barracks.IsComplete)
        {
            match.Tick();
        }

        var building = SelectionPanel.For(match.State, FirstPlayer, [barracks.Id]).Building;

        Assert.Equal(
            [
                new UnitChoice(UnitKind.MeleeSoldier, "UNIT_MELEE_SOLDIER", Match.UnitCost(UnitKind.MeleeSoldier), null),
                new UnitChoice(UnitKind.RangedSoldier, "UNIT_RANGED_SOLDIER", Match.UnitCost(UnitKind.RangedSoldier), null),
                new UnitChoice(UnitKind.HeavySoldier, "UNIT_HEAVY_SOLDIER", Match.UnitCost(UnitKind.HeavySoldier), "TEST_AGE_3"),
            ],
            building!.UnitChoices);
        Assert.Null(building.AgeAdvance);
    }

    /// <summary>Places a site of the first Player near its Town Center, with the given builders, and returns it.</summary>
    private static BuildingState PlaceNear(Match match, BuildingKind kind, IReadOnlyList<EntityId> builders)
    {
        var origin = FreeOriginNearFirstHome(match, kind);
        match.Enqueue(new PlaceBuildingCommand(FirstPlayer, kind, origin, builders));
        match.Tick();

        return match.State.Buildings.Single(building => building.Origin == origin);
    }
}
