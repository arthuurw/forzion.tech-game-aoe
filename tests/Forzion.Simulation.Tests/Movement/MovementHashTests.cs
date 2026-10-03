using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Movement;

public class MovementHashTests
{
    [Fact]
    public void Matches_whose_units_stand_alike_but_are_walking_to_different_Cells_have_different_hashes()
    {
        var near = TestMatches.TwoPlayerMatch();
        var far = TestMatches.TwoPlayerMatch();
        var destination = Walk.BehindTownCenter(near);
        var besideDestination = new CellPosition(destination.X - 1, destination.Y);
        near.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [Walk.MiddleVillager(near).Id], besideDestination));
        far.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [Walk.MiddleVillager(far).Id], destination));

        near.Tick();
        far.Tick();

        // Both set out the same way around the Town Center, so only what is left of the walk tells them apart.
        Assert.Equal(near.State.Units.Select(unit => unit.Position), far.State.Units.Select(unit => unit.Position));
        Assert.NotEqual(Walk.MiddleVillager(near).Path, Walk.MiddleVillager(far).Path);
        Assert.NotEqual(near.StateHash, far.StateHash);
    }

    [Fact]
    public void A_move_makes_the_hash_diverge_from_a_match_without_it()
    {
        var withMove = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withMove.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, [Walk.MiddleVillager(withMove).Id], Walk.BehindTownCenter(withMove)));

        withMove.Tick();
        without.Tick();

        Assert.NotEqual(without.StateHash, withMove.StateHash);
    }

    [Fact]
    public void Matches_given_the_same_move_have_the_same_hash_at_every_tick_of_the_walk()
    {
        var first = TestMatches.TwoPlayerMatch();
        var second = TestMatches.TwoPlayerMatch();
        first.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, [Walk.MiddleVillager(first).Id], Walk.BehindTownCenter(first)));
        second.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, [Walk.MiddleVillager(second).Id], Walk.BehindTownCenter(second)));

        for (var tick = 0; tick < 100; tick++)
        {
            first.Tick();
            second.Tick();

            Assert.Equal(first.StateHash, second.StateHash);
        }

        Assert.False(Walk.MiddleVillager(first).IsMoving);
    }
}
