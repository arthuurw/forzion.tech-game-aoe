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
    public void Units_standing_on_one_Cell_are_drawn_apart_side_by_side_across_it()
    {
        var drawn = DrawnStack(3, out var centre);

        // Half a Cell apart: as far as two unit placeholders are wide. In one row across the
        // map, so that a camera looking along the map's Y sees none behind another.
        Assert.Equal(3, drawn.Count);
        Assert.All(drawn, point => Assert.Equal(drawn[0].Y, point.Y));
        Assert.All(Pairs(drawn), pair => Assert.True(Distance(pair.First, pair.Second) >= 0.5 - 1e-9));
        Assert.All(drawn, point => Assert.True(Distance(point, centre) <= 0.5));
    }

    [Fact]
    public void Two_units_standing_on_one_Cell_are_drawn_either_side_of_its_centre()
    {
        var drawn = DrawnStack(2, out var centre);

        Assert.Equal([centre.X - 0.25, centre.X + 0.25], drawn.Select(point => point.X).Order());
        Assert.All(drawn, point => Assert.Equal(centre.Y, point.Y));
    }

    [Fact]
    public void A_row_of_a_stack_behind_another_is_drawn_between_the_units_in_front()
    {
        var drawn = DrawnStack(5, out var centre);

        // Three in front and two behind, none straight behind another, all on the Cell.
        Assert.Equal(5, drawn.Count);
        Assert.Equal(5, drawn.Select(point => Math.Round(point.X, 6)).Distinct().Count());
        Assert.All(Pairs(drawn), pair => Assert.True(Distance(pair.First, pair.Second) >= 0.5 - 1e-9));
        Assert.All(drawn, point => Assert.True(Math.Abs(point.X - centre.X) <= 0.5 && Math.Abs(point.Y - centre.Y) <= 0.5));
    }

    [Fact]
    public void Units_slide_apart_when_a_unit_stops_on_another_units_Cell_instead_of_jumping()
    {
        var cell = BesideFirstHome();
        var driver = NewDriver(out var match, PlainConfig(firstExtras: [new StartingUnit(UnitKind.Villager, cell)]));
        var walker = FirstVillager(match);
        var standing = match.State.Units.Single(unit => unit.Owner == FirstPlayer && unit.Position.Cell == cell);
        var centre = PointOf(MapPosition.CentreOf(cell));
        match.Enqueue(new MoveCommand(FirstPlayer, [walker.Id], cell));

        TickUntil(driver, () => !walker.IsMoving && walker.Position.Cell == cell);

        // In the frame the walker stops, one tick of real time later, the unit already there
        // has slid at most 0.1 Cell off the centre; a second later the two stand apart.
        Assert.True(Distance(driver.PositionOf(standing), centre) <= 0.1 + 1e-9);
        driver.Advance(1);
        Assert.True(Distance(driver.PositionOf(standing), driver.PositionOf(walker)) >= 0.5 - 1e-9);
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

    [Fact]
    public void While_paused_the_time_that_passes_runs_no_tick()
    {
        var driver = NewDriver(out var match);
        driver.Advance(OneTick);

        driver.IsPaused = true;
        driver.Advance(OneTick * 5);

        Assert.Equal(1, match.State.Tick);
    }

    [Fact]
    public void A_walking_unit_stays_drawn_where_it_was_while_paused()
    {
        var driver = NewDriver(out var match);
        var villager = FirstVillager(match);
        match.Enqueue(new MoveCommand(FirstPlayer, [villager.Id], new CellPosition(32, 24)));
        driver.Advance(OneTick * 2.5);
        var drawn = driver.PositionOf(villager);

        driver.IsPaused = true;
        driver.Advance(OneTick * 0.4);

        Assert.Equal(drawn, driver.PositionOf(villager));
    }

    [Fact]
    public void Once_resumed_the_match_goes_on_from_where_it_stopped_without_making_up_the_time_paused()
    {
        var driver = NewDriver(out var match);
        driver.Advance(OneTick * 1.5);
        driver.IsPaused = true;
        driver.Advance(10);

        driver.IsPaused = false;
        driver.Advance(OneTick);

        Assert.Equal(2, match.State.Tick);
    }

    private static UnitState FirstVillager(Match match) => match.State.Units.First(unit => unit.Owner == FirstPlayer);

    /// <summary>
    /// Where the driver of a new match draws <paramref name="count"/> Villagers of the first
    /// Player standing on one Cell, in ascending ID order, and the centre of that Cell.
    /// </summary>
    private static List<MapPoint> DrawnStack(int count, out MapPoint centre)
    {
        var cell = BesideFirstHome();
        var driver = NewDriver(out var match, PlainConfig(firstExtras: [.. Enumerable.Repeat(new StartingUnit(UnitKind.Villager, cell), count)]));
        var position = MapPosition.CentreOf(cell);
        centre = PointOf(position);

        return match.State.Units.Where(unit => unit.Position == position).Select(driver.PositionOf).ToList();
    }

    private static double Distance(MapPoint from, MapPoint to) => Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));

    private static IEnumerable<(MapPoint First, MapPoint Second)> Pairs(List<MapPoint> points) =>
        points.SelectMany((first, index) => points.Skip(index + 1).Select(second => (first, second)));
}
