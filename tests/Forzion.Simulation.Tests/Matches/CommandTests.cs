using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Matches;

public class CommandTests
{
    [Fact]
    public void An_enqueued_command_changes_nothing_until_the_next_tick()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var hashBefore = match.StateHash;

        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));

        Assert.False(villager.IsMoving);
        Assert.Empty(match.Events);
        Assert.Equal(hashBefore, match.StateHash);
    }

    [Fact]
    public void An_enqueued_command_is_applied_by_the_next_tick()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var command = OutsideTheMap(match, TestMatches.FirstPlayer);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));
        match.Enqueue(command);

        match.Tick();

        Assert.True(villager.IsMoving);
        Assert.Equal([new CommandRejected(command, RejectionReason.DestinationOutsideMap)], match.Events);
    }

    [Fact]
    public void A_command_is_applied_only_once()
    {
        var match = TestMatches.TwoPlayerMatch();
        match.Enqueue(OutsideTheMap(match, TestMatches.FirstPlayer));
        match.Tick();

        match.Tick();

        Assert.Empty(match.Events);
    }

    [Fact]
    public void Events_read_after_a_tick_are_not_changed_by_later_ticks()
    {
        var match = TestMatches.TwoPlayerMatch();
        var command = OutsideTheMap(match, TestMatches.FirstPlayer);
        match.Enqueue(command);
        match.Tick();
        var events = match.Events;

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.DestinationOutsideMap)], events);
    }

    [Fact]
    public void A_command_makes_the_hash_diverge_from_a_match_without_it()
    {
        var withCommand = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withCommand.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, [Walk.MiddleVillager(withCommand).Id], Walk.BehindTownCenter(withCommand)));

        withCommand.Tick();
        without.Tick();

        Assert.NotEqual(without.StateHash, withCommand.StateHash);
    }

    [Fact]
    public void A_command_from_a_Player_that_is_not_in_the_match_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var command = new MoveCommand(new PlayerId(3), [Walk.MiddleVillager(match).Id], Walk.BehindTownCenter(match));
        match.Enqueue(command);
        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownPlayer)], match.Events);
        Assert.False(Walk.MiddleVillager(match).IsMoving);
    }

    [Fact]
    public void A_rejected_command_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withRejection.Enqueue(new MoveCommand(
            new PlayerId(3), [Walk.MiddleVillager(withRejection).Id], Walk.BehindTownCenter(withRejection)));

        withRejection.Tick();
        without.Tick();

        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void Commands_of_one_tick_are_applied_in_Player_order_whatever_the_order_they_arrived_in()
    {
        var match = TestMatches.TwoPlayerMatch();
        var second = OutsideTheMap(match, TestMatches.SecondPlayer);
        var first = OutsideTheMap(match, TestMatches.FirstPlayer);
        match.Enqueue(second);
        match.Enqueue(first);

        match.Tick();

        Assert.Equal(
            [
                new CommandRejected(first, RejectionReason.DestinationOutsideMap),
                new CommandRejected(second, RejectionReason.DestinationOutsideMap),
            ],
            match.Events);
    }

    /// <summary>A move of one of the Player's Villagers to a Cell outside the map: always rejected, changing nothing.</summary>
    private static MoveCommand OutsideTheMap(Match match, PlayerId player) =>
        new(player, [match.State.Units.First(unit => unit.Owner == player).Id], new CellPosition(-1, -1));
}
