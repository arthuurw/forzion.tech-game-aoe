using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class PlayerStatusTests
{
    [Fact]
    public void The_status_shows_the_Resources_and_population_of_the_Player_and_not_of_another()
    {
        var match = Match.Create(PlainConfig());
        match.Enqueue(new TrainCommand(FirstPlayer, TownCenterOf(match, FirstPlayer).Id, UnitKind.Villager));
        match.Tick();
        var player = match.State.Players[0];

        var status = PlayerStatus.Of(match.State, FirstPlayer);

        Assert.Equal(
            [
                new ResourceAmount(ResourceKind.Food, player.AmountOf(ResourceKind.Food)),
                new ResourceAmount(ResourceKind.Wood, player.AmountOf(ResourceKind.Wood)),
                new ResourceAmount(ResourceKind.Gold, player.AmountOf(ResourceKind.Gold)),
            ],
            status.Resources);
        Assert.NotEqual(match.State.Players[1].AmountOf(ResourceKind.Food), status.Resources[0].Amount);
        Assert.Equal(match.State.PopulationOf(FirstPlayer), status.Population);
        Assert.NotEqual(match.State.PopulationOf(SecondPlayer), status.Population);
        Assert.Equal(match.State.PopulationLimitOf(FirstPlayer), status.PopulationLimit);
    }

    [Fact]
    public void The_status_names_the_Faction_and_its_name_for_the_Age_of_the_Player()
    {
        var match = Match.Create(PlainConfig());

        var status = PlayerStatus.Of(match.State, FirstPlayer);

        Assert.Equal("FACTION_PORTUGUESE", status.FactionNameKey);
        Assert.Equal("FACTION_PORTUGUESE_AGE_1", status.AgeNameKey);
        Assert.Null(status.AgeAdvance);
    }

    [Fact]
    public void During_an_Age_Advance_the_status_names_the_next_Age_and_how_far_the_advance_has_gone()
    {
        var match = OfThreeAges();
        match.Enqueue(new AgeAdvanceCommand(FirstPlayer, TownCenterOf(match, FirstPlayer).Id));

        // The tick that applies the order is the first of the advance's 10.
        Run(match, 4);

        var status = PlayerStatus.Of(match.State, FirstPlayer);

        Assert.Equal("TEST_AGE_1", status.AgeNameKey);
        Assert.Equal(new AgeAdvanceProgress("TEST_AGE_2", 0.4), status.AgeAdvance);
    }

    [Fact]
    public void Once_the_Age_Advance_is_done_the_status_names_the_Age_reached()
    {
        var match = OfThreeAges();

        AdvanceAge(match, FirstPlayer);

        var status = PlayerStatus.Of(match.State, FirstPlayer);

        Assert.Equal("TEST_AGE_2", status.AgeNameKey);
        Assert.Null(status.AgeAdvance);
    }
}
