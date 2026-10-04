using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class MatchDriverTests
{
    private const double OneTick = 1.0 / Match.TicksPerSecond;

    [Fact]
    public void Before_any_tick_a_unit_is_drawn_where_it_stands()
    {
        var driver = NewDriver(out var match);
        var villager = FirstVillager(match);

        var drawn = driver.PositionOf(villager);

        Assert.Equal(villager.Position.X.ToDouble(), drawn.X);
        Assert.Equal(villager.Position.Y.ToDouble(), drawn.Y);
    }

    [Fact]
    public void Units_standing_on_one_Cell_are_drawn_apart_around_it()
    {
        var cell = BesideFirstHome();
        var stacked = new StartingUnit(UnitKind.Villager, cell);
        var driver = NewDriver(out var match, PlainConfig(firstExtras: [stacked, stacked, stacked]));
        var centre = MapPosition.CentreOf(cell);

        var drawn = match.State.Units.Where(unit => unit.Position == centre).Select(driver.PositionOf).ToList();

        // Half a Cell apart: as far as two unit placeholders are wide, so none hides another.
        Assert.Equal(3, drawn.Count);
        Assert.All(Pairs(drawn), pair => Assert.True(Distance(pair.First, pair.Second) >= 0.5 - 1e-9));
        Assert.All(drawn, point => Assert.True(Distance(point, new MapPoint(centre.X.ToDouble(), centre.Y.ToDouble())) <= 0.5));
    }

    [Fact]
    public void Advancing_runs_the_ticks_that_are_due()
    {
        var driver = NewDriver(out var match);

        driver.Advance(OneTick * 2.5);

        Assert.Equal(2, match.State.Tick);
    }

    [Fact]
    public void A_walking_unit_is_drawn_between_where_it_stood_before_and_after_the_last_tick()
    {
        var driver = NewDriver(out var match);
        var villager = FirstVillager(match);
        match.Enqueue(new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(32, 24)));

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
        var driver = NewDriver(out var match);
        var villager = FirstVillager(match);
        var outsideTheMap = new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(-1, 0));
        match.Enqueue(outsideTheMap);

        var events = driver.Advance(OneTick * 2);

        Assert.Equal(2, match.State.Tick);
        Assert.Equal([new CommandRejected(outsideTheMap, RejectionReason.DestinationOutsideMap)], events);
    }

    [Fact]
    public void A_frame_that_runs_no_tick_reports_no_event()
    {
        var driver = NewDriver(out var match);
        var villager = FirstVillager(match);
        match.Enqueue(new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(-1, 0)));
        Assert.NotEmpty(driver.Advance(OneTick));

        Assert.Empty(driver.Advance(OneTick / 2));
    }

    private static UnitState FirstVillager(Match match) => match.State.Units.First(unit => unit.Owner == FirstPlayer);

    private static double Distance(MapPoint from, MapPoint to) => Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));

    private static IEnumerable<(MapPoint First, MapPoint Second)> Pairs(List<MapPoint> points) =>
        points.SelectMany((first, index) => points.Skip(index + 1).Select(second => (first, second)));
}
