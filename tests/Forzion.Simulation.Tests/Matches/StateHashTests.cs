namespace Forzion.Simulation.Tests.Matches;

public class StateHashTests
{
    [Fact]
    public void Matches_created_from_equal_configurations_have_the_same_hash()
    {
        var first = TestMatches.TwoPlayerMatch();
        var second = TestMatches.TwoPlayerMatch();

        Assert.Equal(first.StateHash, second.StateHash);
    }

    [Fact]
    public void Reading_the_hash_does_not_change_it()
    {
        var match = TestMatches.TwoPlayerMatch();

        Assert.Equal(match.StateHash, match.StateHash);
    }

    [Fact]
    public void The_hash_changes_when_a_tick_passes()
    {
        var match = TestMatches.TwoPlayerMatch();
        var before = match.StateHash;

        match.Tick();

        Assert.NotEqual(before, match.StateHash);
    }

    [Fact]
    public void Matches_on_maps_of_different_size_have_different_hashes()
    {
        var players = new[] { new PlayerConfig(TestMatches.FirstFaction) };
        var wide = Match.Create(new MatchConfig(1, new MapConfig(64, 48), players));
        var tall = Match.Create(new MatchConfig(1, new MapConfig(48, 64), players));

        Assert.NotEqual(wide.StateHash, tall.StateHash);
    }

    [Fact]
    public void Matches_with_different_Factions_have_different_hashes()
    {
        var map = new MapConfig(64, 48);
        var first = Match.Create(new MatchConfig(1, map, [new PlayerConfig(new FactionId(1))]));
        var second = Match.Create(new MatchConfig(1, map, [new PlayerConfig(new FactionId(2))]));

        Assert.NotEqual(first.StateHash, second.StateHash);
    }

    [Fact]
    public void Matches_with_a_different_number_of_Players_have_different_hashes()
    {
        var map = new MapConfig(64, 48);
        var player = new PlayerConfig(TestMatches.FirstFaction);
        var duel = Match.Create(new MatchConfig(1, map, [player, player]));
        var trio = Match.Create(new MatchConfig(1, map, [player, player, player]));

        Assert.NotEqual(duel.StateHash, trio.StateHash);
    }
}
