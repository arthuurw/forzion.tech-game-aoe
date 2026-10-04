using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Production;

namespace Forzion.Simulation.Tests.Ages;

public class AgeAdvanceTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void An_Age_Advance_at_the_Town_Center_pays_the_cost_of_the_next_Age_at_once()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var cost = player.NextAge!.AdvanceCost;
        Advance.Afford(match, First, cost);
        var before = Train.Stock(player);
        match.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(match, First).Id));

        match.Tick();

        Assert.Equal(
            [before[0] - cost.Food, before[1] - cost.Wood, before[2] - cost.Gold],
            Train.Stock(player));
        Assert.Equal(1, player.Age);
        Assert.Equal(1, Train.TownCenter(match, First).AgeAdvanceProgress);
    }

    [Fact]
    public void The_Player_reaches_the_next_Age_once_its_advance_time_has_passed_and_an_event_reports_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        Advance.Afford(match, First, player.NextAge!.AdvanceCost);
        var advanceTime = player.NextAge!.AdvanceTime;
        match.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(match, First).Id));
        var start = match.State.Tick;

        var advanced = Train.Run(match, advanceTime).Where(happened => happened.Event is AgeAdvanced).ToList();

        Assert.Equal([(start + advanceTime, (MatchEvent)new AgeAdvanced(First, 2))], advanced);
        Assert.Equal(2, player.Age);
        Assert.Null(Train.TownCenter(match, First).AgeAdvanceProgress);
    }

    [Fact]
    public void The_Player_stays_in_its_Age_until_the_last_tick_of_the_advance()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        Advance.Afford(match, First, player.NextAge!.AdvanceCost);
        var advanceTime = player.NextAge!.AdvanceTime;
        match.Enqueue(new AgeAdvanceCommand(First, Train.TownCenter(match, First).Id));

        Train.Run(match, advanceTime - 1);

        Assert.Equal(1, player.Age);
        Assert.Equal(advanceTime - 1, Train.TownCenter(match, First).AgeAdvanceProgress);
    }

    [Fact]
    public void An_Age_Advance_the_Player_cannot_afford_is_rejected_and_changes_nothing()
    {
        // The Portuguese Age II costs more Food than any Player starts with.
        Assert.True(Factions.Portuguese.Ages[1].AdvanceCost.Food > TestMatches.TwoPlayerMatch().State.Players[0].AmountOf(ResourceKind.Food));

        Train.AssertRejected(RejectionReason.NotEnoughResources, match =>
            new AgeAdvanceCommand(First, Train.TownCenter(match, First).Id));
    }

    [Fact]
    public void An_Age_Advance_at_a_building_other_than_the_Town_Center_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingCannotAdvanceAge, match =>
            new AgeAdvanceCommand(First, Train.Complete(match, First, BuildingKind.House).Id));
    }

    [Fact]
    public void An_Age_Advance_at_another_Players_Town_Center_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(RejectionReason.BuildingOfAnotherPlayer, match =>
            new AgeAdvanceCommand(First, Train.TownCenter(match, TestMatches.SecondPlayer).Id));
    }

    [Fact]
    public void A_second_Age_Advance_while_one_is_underway_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(
            RejectionReason.AgeAdvanceInProgress,
            match =>
            {
                var townCenter = Train.TownCenter(match, First);
                match.Enqueue(new AgeAdvanceCommand(First, townCenter.Id));
                match.Tick();

                return new AgeAdvanceCommand(First, townCenter.Id);
            },
            TestFactions.ThreeAgeMatch);
    }

    [Fact]
    public void An_Age_Advance_in_the_last_Age_of_the_Faction_is_rejected_and_changes_nothing()
    {
        var oneAge = TestFactions.OneAge(3);

        Train.AssertRejected(
            RejectionReason.LastAgeReached,
            match => new AgeAdvanceCommand(First, Train.TownCenter(match, First).Id),
            () => Match.Create(TestMatches.SinglePlayerConfig() with { Players = [new PlayerConfig(oneAge.Id)], Factions = [oneAge] }));
    }

    [Fact]
    public void A_Faction_of_three_Ages_advances_twice_paying_and_waiting_for_each_Age_and_then_no_more()
    {
        var match = TestFactions.ThreeAgeMatch();
        var player = match.State.Players[0];
        var townCenter = Train.TownCenter(match, First);
        var ages = TestFactions.ThreeAges.Ages;
        var happened = new List<(int Tick, MatchEvent Event)>();

        foreach (var age in new[] { ages[1], ages[2] })
        {
            var before = Train.Stock(player);
            match.Enqueue(new AgeAdvanceCommand(First, townCenter.Id));
            var start = match.State.Tick;

            happened.AddRange(Train.Run(match, age.AdvanceTime).Where(each => each.Event is AgeAdvanced));

            Assert.Equal(start + age.AdvanceTime, happened[^1].Tick);
            Assert.Equal(
                [before[0] - age.AdvanceCost.Food, before[1] - age.AdvanceCost.Wood, before[2] - age.AdvanceCost.Gold],
                Train.Stock(player));
        }

        Assert.Equal([new AgeAdvanced(First, 2), new AgeAdvanced(First, 3)], happened.Select(each => each.Event));
        Assert.Equal(3, player.Age);

        match.Enqueue(new AgeAdvanceCommand(First, townCenter.Id));
        match.Tick();

        Assert.Equal(RejectionReason.LastAgeReached, Assert.IsType<CommandRejected>(Assert.Single(match.Events)).Reason);
    }
}
