using Forzion.Simulation.Tests.Ages;

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
            Players: [new PlayerConfig(new FactionId(5)), new PlayerConfig(new FactionId(3))],
            Factions: [TestFactions.OneAge(3), TestFactions.OneAge(5)]);

        var match = Match.Create(config);

        Assert.Collection(
            match.State.Players,
            first =>
            {
                Assert.Equal(new PlayerId(1), first.Id);
                Assert.Equal(new FactionId(5), first.Faction.Id);
            },
            second =>
            {
                Assert.Equal(new PlayerId(2), second.Id);
                Assert.Equal(new FactionId(3), second.Faction.Id);
            });
    }

    [Fact]
    public void Each_Player_starts_with_200_Food_200_Wood_and_100_Gold()
    {
        var match = TestMatches.TwoPlayerMatch();

        Assert.All(match.State.Players, player =>
        {
            Assert.Equal(200, player.AmountOf(ResourceKind.Food));
            Assert.Equal(200, player.AmountOf(ResourceKind.Wood));
            Assert.Equal(100, player.AmountOf(ResourceKind.Gold));
        });
    }

    [Fact]
    public void The_map_has_the_configured_size()
    {
        var match = Match.Create(TestMatches.SinglePlayerConfig());

        Assert.Equal(64, match.State.Map.Width);
        Assert.Equal(48, match.State.Map.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void A_match_has_one_or_two_Players(int playerCount)
    {
        var players = Enumerable.Repeat(new PlayerConfig(TestMatches.FirstFaction), playerCount).ToList();

        Assert.Throws<ArgumentException>(() => Match.Create(new MatchConfig(1, new MapConfig(64, 48), players)));
    }

    [Theory]
    [InlineData(31, 64)]
    [InlineData(64, 31)]
    public void A_map_too_small_to_keep_the_Players_apart_is_refused(int width, int height)
    {
        var players = new[] { new PlayerConfig(TestMatches.FirstFaction), new PlayerConfig(TestMatches.FirstFaction) };

        Assert.Throws<ArgumentException>(() => Match.Create(new MatchConfig(1, new MapConfig(width, height), players)));
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
