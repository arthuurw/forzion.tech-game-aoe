using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ages;

public class FactionTests
{
    [Fact]
    public void Every_Player_starts_the_match_in_the_first_Age()
    {
        var match = TestMatches.TwoPlayerMatch();

        Assert.All(match.State.Players, player => Assert.Equal(1, player.Age));
    }

    [Fact]
    public void Without_Factions_in_the_configuration_Players_control_the_Factions_of_the_game()
    {
        var match = TestMatches.TwoPlayerMatch();

        Assert.All(match.State.Players, player => Assert.Same(Factions.Portuguese, player.Faction));
    }

    [Fact]
    public void A_Player_controls_the_Faction_of_the_configuration_that_has_its_Faction_id()
    {
        var match = TestFactions.ThreeAgeMatch();

        Assert.Same(TestFactions.ThreeAges, match.State.Players[0].Faction);
    }

    [Fact]
    public void A_match_whose_Player_controls_a_Faction_the_configuration_does_not_have_is_refused()
    {
        var config = TestMatches.SinglePlayerConfig() with { Factions = [TestFactions.OneAge(2)] };

        Assert.Throws<ArgumentException>(() => Match.Create(config));
    }

    [Fact]
    public void A_configuration_with_two_Factions_of_the_same_id_is_refused()
    {
        var config = TestMatches.SinglePlayerConfig() with { Factions = [TestFactions.OneAge(1), TestFactions.OneAge(1)] };

        Assert.Throws<ArgumentException>(() => Match.Create(config));
    }

    [Fact]
    public void A_Faction_without_Ages_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new Faction(new FactionId(9), "TEST", [], new Dictionary<UnitKind, string>()));
    }

    [Fact]
    public void An_Age_of_a_Faction_is_found_by_its_number_from_1()
    {
        var faction = TestFactions.ThreeAges;

        Assert.Equal(faction.Ages, [faction.AgeAt(1), faction.AgeAt(2), faction.AgeAt(3)]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void A_number_outside_the_Ages_of_a_Faction_names_no_Age(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestFactions.ThreeAges.AgeAt(number));
    }

    [Fact]
    public void A_Player_is_in_the_Age_of_its_Faction_that_its_number_names_and_can_advance_to_the_one_after()
    {
        var match = TestFactions.ThreeAgeMatch();
        var player = match.State.Players[0];

        Advance.ToNextAge(match, player.Id);

        Assert.Same(TestFactions.ThreeAges.Ages[1], player.CurrentAge);
        Assert.Same(TestFactions.ThreeAges.Ages[2], player.NextAge);
    }

    [Fact]
    public void The_Portuguese_have_two_Ages_and_a_name_for_each_of_their_units()
    {
        var portuguese = Factions.Portuguese;

        Assert.Equal(2, portuguese.Ages.Count);
        Assert.Equal(Enum.GetValues<UnitKind>().Order(), portuguese.UnitNameKeys.Keys.Order());
    }
}
