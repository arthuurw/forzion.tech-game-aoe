using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class MatchDriverTests
{
    private const double OneTick = 1.0 / Match.TicksPerSecond;

    [Fact]
    public void Before_any_tick_a_unit_is_drawn_where_it_stands()
    {
        var driver = NewDriver();
        var villager = FirstVillager(driver.Match);

        var drawn = driver.PositionOf(villager);

        Assert.Equal(villager.Position.X.ToDouble(), drawn.X);
        Assert.Equal(villager.Position.Y.ToDouble(), drawn.Y);
    }

    [Fact]
    public void Advancing_runs_the_ticks_that_are_due()
    {
        var driver = NewDriver();

        driver.Advance(OneTick * 2.5);

        Assert.Equal(2, driver.Match.State.Tick);
    }

    [Fact]
    public void A_walking_unit_is_drawn_between_where_it_stood_before_and_after_the_last_tick()
    {
        var driver = NewDriver();
        var villager = FirstVillager(driver.Match);
        driver.Match.Enqueue(new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(32, 24)));

        driver.Advance(OneTick);
        var beforeLastTick = villager.Position;
        driver.Advance(OneTick * 1.5);
        var afterLastTick = villager.Position;

        var drawn = driver.PositionOf(villager);

        // A Villager walks 2 Cells per second, 0.1 Cell per tick: half a tick into the next
        // tick it is drawn 0.05 Cell past where it stood before the last tick, towards where
        // it stands now.
        var fromBefore = new MapPoint(drawn.X - beforeLastTick.X.ToDouble(), drawn.Y - beforeLastTick.Y.ToDouble());
        var toAfter = new MapPoint(afterLastTick.X.ToDouble() - drawn.X, afterLastTick.Y.ToDouble() - drawn.Y);

        Assert.Equal(0.05, Math.Sqrt((fromBefore.X * fromBefore.X) + (fromBefore.Y * fromBefore.Y)), precision: 3);
        Assert.Equal(fromBefore.X, toAfter.X, precision: 3);
        Assert.Equal(fromBefore.Y, toAfter.Y, precision: 3);
    }

    [Fact]
    public void Advancing_reports_the_events_of_every_tick_it_ran_not_only_the_last()
    {
        var driver = NewDriver();
        var villager = FirstVillager(driver.Match);
        var outsideTheMap = new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(-1, 0));
        driver.Match.Enqueue(outsideTheMap);

        var events = driver.Advance(OneTick * 2);

        Assert.Equal(2, driver.Match.State.Tick);
        Assert.Equal([new CommandRejected(outsideTheMap, RejectionReason.DestinationOutsideMap)], events);
    }

    [Fact]
    public void A_frame_that_runs_no_tick_reports_no_event()
    {
        var driver = NewDriver();
        var villager = FirstVillager(driver.Match);
        driver.Match.Enqueue(new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(-1, 0)));
        Assert.NotEmpty(driver.Advance(OneTick));

        Assert.Empty(driver.Advance(OneTick / 2));
    }

    private static UnitState FirstVillager(Match match) => match.State.Units.First(unit => unit.Owner == FirstPlayer);
}
