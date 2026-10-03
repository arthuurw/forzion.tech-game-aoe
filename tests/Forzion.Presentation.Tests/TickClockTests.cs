namespace Forzion.Presentation.Tests;

public class TickClockTests
{
    [Fact]
    public void Frame_time_shorter_than_a_tick_runs_no_tick()
    {
        var clock = new TickClock(ticksPerSecond: 20);

        Assert.Equal(0, clock.Advance(0.03));
    }

    [Fact]
    public void Time_left_over_from_one_frame_counts_towards_the_next()
    {
        var clock = new TickClock(ticksPerSecond: 20);

        clock.Advance(0.03);

        Assert.Equal(1, clock.Advance(0.03));
        Assert.Equal(0, clock.Advance(0.03));
        Assert.Equal(1, clock.Advance(0.03));
    }

    [Fact]
    public void A_long_frame_runs_every_tick_it_covers()
    {
        var clock = new TickClock(ticksPerSecond: 20);

        Assert.Equal(3, clock.Advance(0.16));
    }

    [Fact]
    public void One_second_of_frames_runs_one_second_of_ticks()
    {
        var clock = new TickClock(ticksPerSecond: 20);
        var ticks = 0;

        for (var frame = 0; frame < 60; frame++)
        {
            ticks += clock.Advance(1.0 / 60);
        }

        Assert.Equal(20, ticks);
    }

    [Fact]
    public void The_interpolation_factor_is_the_part_of_the_next_tick_already_elapsed()
    {
        var clock = new TickClock(ticksPerSecond: 20);

        Assert.Equal(0.0, clock.InterpolationFactor);

        clock.Advance(0.0125);
        Assert.Equal(0.25, clock.InterpolationFactor, precision: 9);

        clock.Advance(0.05);
        Assert.Equal(0.25, clock.InterpolationFactor, precision: 9);

        clock.Advance(0.0375);
        Assert.Equal(0.0, clock.InterpolationFactor, precision: 9);
    }

    // After a stall (a breakpoint, a dragged window) the match slows down instead of running
    // so many ticks at once that the next frame stalls too.
    [Fact]
    public void A_stalled_frame_runs_at_most_the_catch_up_limit_and_drops_the_rest()
    {
        var clock = new TickClock(ticksPerSecond: 20, maxTicksPerAdvance: 5);

        Assert.Equal(5, clock.Advance(1.0125));
        Assert.Equal(0.25, clock.InterpolationFactor, precision: 9);
        Assert.Equal(0, clock.Advance(0.0));
    }

    [Fact]
    public void Time_cannot_run_backwards()
    {
        var clock = new TickClock(ticksPerSecond: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-0.01));
    }
}
