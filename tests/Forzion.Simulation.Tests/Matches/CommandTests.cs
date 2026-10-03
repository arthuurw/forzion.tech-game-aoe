namespace Forzion.Simulation.Tests.Matches;

public class CommandTests
{
    [Fact]
    public void An_enqueued_command_changes_nothing_until_the_next_tick()
    {
        var match = TestMatches.TwoPlayerMatch();
        var hashBefore = match.StateHash;

        match.Enqueue(new ResignCommand(TestMatches.FirstPlayer));

        Assert.False(match.State.Players[0].IsDefeated);
        Assert.Empty(match.Events);
        Assert.Equal(hashBefore, match.StateHash);
    }

    [Fact]
    public void An_enqueued_command_is_applied_by_the_next_tick()
    {
        var match = TestMatches.TwoPlayerMatch();
        match.Enqueue(new ResignCommand(TestMatches.FirstPlayer));

        match.Tick();

        Assert.True(match.State.Players[0].IsDefeated);
        Assert.False(match.State.Players[1].IsDefeated);
        Assert.Equal([new PlayerDefeated(TestMatches.FirstPlayer)], match.Events);
    }

    [Fact]
    public void A_command_is_applied_only_once()
    {
        var match = TestMatches.TwoPlayerMatch();
        match.Enqueue(new ResignCommand(TestMatches.FirstPlayer));
        match.Tick();

        match.Tick();

        Assert.Empty(match.Events);
    }

    [Fact]
    public void Events_read_after_a_tick_are_not_changed_by_later_ticks()
    {
        var match = TestMatches.TwoPlayerMatch();
        match.Enqueue(new ResignCommand(TestMatches.FirstPlayer));
        match.Tick();
        var events = match.Events;

        match.Tick();

        Assert.Equal([new PlayerDefeated(TestMatches.FirstPlayer)], events);
    }

    [Fact]
    public void A_command_makes_the_hash_diverge_from_a_match_without_it()
    {
        var withCommand = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withCommand.Enqueue(new ResignCommand(TestMatches.FirstPlayer));

        withCommand.Tick();
        without.Tick();

        Assert.NotEqual(without.StateHash, withCommand.StateHash);
    }

    [Fact]
    public void A_command_from_a_Player_that_is_not_in_the_match_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var command = new ResignCommand(new PlayerId(3));
        match.Enqueue(command);
        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownPlayer)], match.Events);
        Assert.All(match.State.Players, player => Assert.False(player.IsDefeated));
    }

    [Fact]
    public void A_command_from_a_defeated_Player_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var command = new ResignCommand(TestMatches.FirstPlayer);
        match.Enqueue(command);
        match.Tick();

        match.Enqueue(command);
        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.DefeatedPlayer)], match.Events);
    }

    [Fact]
    public void A_rejected_command_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withRejection.Enqueue(new ResignCommand(new PlayerId(3)));

        withRejection.Tick();
        without.Tick();

        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void Commands_of_one_tick_are_applied_in_Player_order_whatever_the_order_they_arrived_in()
    {
        var match = TestMatches.TwoPlayerMatch();
        match.Enqueue(new ResignCommand(TestMatches.SecondPlayer));
        match.Enqueue(new ResignCommand(TestMatches.FirstPlayer));

        match.Tick();

        Assert.Equal(
            [new PlayerDefeated(TestMatches.FirstPlayer), new PlayerDefeated(TestMatches.SecondPlayer)],
            match.Events);
    }
}
