using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Construction;

public class BuildCommandTests
{
    [Fact]
    public void A_Villager_sent_to_an_unfinished_site_of_its_Player_builds_it_to_completion()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[2];
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);
        match.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [villager.Id], house.Id));

        TestMatches.TickUntil(match, () => house.IsComplete);

        Assert.True(MapProbe.IsBeside(house, villager.Position.Cell));
        Assert.Null(villager.ConstructionSite);
    }

    [Fact]
    public void A_build_order_for_a_building_that_does_not_exist_is_rejected_and_changes_nothing()
    {
        AssertRejected(RejectionReason.UnknownBuilding, (match, _, villager) =>
            new BuildCommand(TestMatches.FirstPlayer, [villager.Id], match.State.ResourceSources[0].Id));
    }

    [Fact]
    public void A_build_order_for_another_Players_building_is_rejected_and_changes_nothing()
    {
        AssertRejected(RejectionReason.BuildingOfAnotherPlayer, (match, _, villager) =>
            new BuildCommand(TestMatches.FirstPlayer, [villager.Id], match.State.Buildings[1].Id));
    }

    [Fact]
    public void A_build_order_for_a_complete_building_is_rejected_and_changes_nothing()
    {
        AssertRejected(RejectionReason.BuildingAlreadyComplete, (match, _, villager) =>
            new BuildCommand(TestMatches.FirstPlayer, [villager.Id], match.State.Buildings[0].Id));
    }

    [Fact]
    public void A_unit_that_no_longer_exists_is_skipped_and_the_other_units_of_the_build_order_set_out()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[2];
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);
        match.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [new EntityId(100_000), villager.Id], house.Id));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(house.Id, villager.ConstructionSite);
    }

    [Fact]
    public void A_build_order_by_units_none_of_which_exist_is_rejected_and_changes_nothing()
    {
        AssertRejected(RejectionReason.UnknownUnit, (_, house, _) =>
            new BuildCommand(TestMatches.FirstPlayer, [new EntityId(100_000), new EntityId(100_001)], house.Id));
    }

    [Fact]
    public void A_build_order_by_another_Players_Villager_is_rejected_and_changes_nothing()
    {
        AssertRejected(RejectionReason.UnitOfAnotherPlayer, (match, house, villager) =>
            new BuildCommand(TestMatches.FirstPlayer, [villager.Id, Site.VillagersOf(match, TestMatches.SecondPlayer)[0].Id], house.Id));
    }

    [Fact]
    public void A_build_order_given_to_Villagers_and_soldiers_sends_the_Villagers_and_leaves_the_soldiers_to_what_they_were_doing()
    {
        var match = TestArmies.MatchWithSoldier();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[0];
        var soldier = match.State.SoldierOf(TestMatches.FirstPlayer);
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);
        var destination = TestArmies.WalkAway(match, soldier);

        match.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [soldier.Id, villager.Id], house.Id));
        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(house.Id, villager.ConstructionSite);
        Assert.Null(soldier.ConstructionSite);
        Assert.Equal(destination, soldier.Path[^1]);
    }

    [Fact]
    public void A_build_order_given_to_soldiers_alone_is_rejected_and_sends_none_of_them()
    {
        var match = TestArmies.MatchWithSoldier();
        var soldier = match.State.SoldierOf(TestMatches.FirstPlayer);
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);
        var command = new BuildCommand(TestMatches.FirstPlayer, [soldier.Id], house.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitCannotBuild)], match.Events);
        Assert.Null(soldier.ConstructionSite);
        Assert.False(soldier.IsMoving);
    }

    /// <summary>
    /// Places a House of the first Player with no builder in two equal matches, sends the
    /// command <paramref name="build"/> makes from one of them, its House and its first Villager,
    /// and checks the command is rejected for <paramref name="reason"/> and leaves that match
    /// as the other.
    /// </summary>
    private static void AssertRejected(RejectionReason reason, Func<Match, BuildingState, UnitState, BuildCommand> build)
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var house = Site.Place(withRejection, TestMatches.FirstPlayer, BuildingKind.House, []);
        Site.Place(without, TestMatches.FirstPlayer, BuildingKind.House, []);
        var command = build(withRejection, house, Site.VillagersOf(withRejection, TestMatches.FirstPlayer)[0]);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, reason)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
        Assert.All(withRejection.State.Units, unit => Assert.Null(unit.ConstructionSite));
    }
}
