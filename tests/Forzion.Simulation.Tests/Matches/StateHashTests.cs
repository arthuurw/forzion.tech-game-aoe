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
        var wide = Match.Create(TestMatches.SinglePlayerConfig() with { Map = new MapConfig(64, 48) });
        var tall = Match.Create(TestMatches.SinglePlayerConfig() with { Map = new MapConfig(48, 64) });

        Assert.NotEqual(wide.StateHash, tall.StateHash);
    }

    [Fact]
    public void Matches_with_different_Factions_have_different_hashes()
    {
        var first = Match.Create(TestMatches.SinglePlayerConfig() with { Players = [new PlayerConfig(new FactionId(1))] });
        var second = Match.Create(TestMatches.SinglePlayerConfig() with { Players = [new PlayerConfig(new FactionId(2))] });

        Assert.NotEqual(first.StateHash, second.StateHash);
    }

    [Fact]
    public void Matches_with_a_different_number_of_Players_have_different_hashes()
    {
        var solo = Match.Create(TestMatches.SinglePlayerConfig(seed: 1));
        var duel = Match.Create(TestMatches.TwoPlayerConfig(seed: 1));

        Assert.NotEqual(solo.StateHash, duel.StateHash);
    }

    [Fact]
    public void Matches_created_from_different_seeds_have_different_hashes()
    {
        var first = TestMatches.TwoPlayerMatch(seed: 1);
        var second = TestMatches.TwoPlayerMatch(seed: 2);

        Assert.NotEqual(first.StateHash, second.StateHash);
    }
}
