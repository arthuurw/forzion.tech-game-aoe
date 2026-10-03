namespace Forzion.Simulation.Tests.Matches;

public class MatchCreationTests
{
    [Fact]
    public void A_new_match_starts_at_tick_zero()
    {
        var match = TestMatches.TwoPlayerMatch();

        Assert.Equal(0, match.State.Tick);
    }

    [Fact]
    public void Players_receive_ascending_ids_in_configuration_order_with_their_Factions()
    {
        var config = new MatchConfig(
            Seed: 7,
            Map: new MapConfig(32, 32),
            Players: [new PlayerConfig(new FactionId(5)), new PlayerConfig(new FactionId(3))]);

        var match = Match.Create(config);

        Assert.Collection(
            match.State.Players,
            first =>
            {
                Assert.Equal(new PlayerId(1), first.Id);
                Assert.Equal(new FactionId(5), first.Faction);
            },
            second =>
            {
                Assert.Equal(new PlayerId(2), second.Id);
                Assert.Equal(new FactionId(3), second.Faction);
            });
    }

    [Fact]
    public void The_map_has_the_configured_size()
    {
        var match = Match.Create(new MatchConfig(1, new MapConfig(64, 48), [new PlayerConfig(new FactionId(1))]));

        Assert.Equal(64, match.State.Map.Width);
        Assert.Equal(48, match.State.Map.Height);
    }

    [Fact]
    public void A_new_match_has_no_events()
    {
        Assert.Empty(TestMatches.TwoPlayerMatch().Events);
    }

    [Fact]
    public void Each_tick_advances_the_tick_counter_by_one()
    {
        var match = TestMatches.TwoPlayerMatch();

        match.Tick();
        match.Tick();
        match.Tick();

        Assert.Equal(3, match.State.Tick);
    }
}
