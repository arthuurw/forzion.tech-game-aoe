namespace Forzion.Presentation;

/// <summary>
/// Turns the variable time between rendered frames into the fixed ticks of the simulation,
/// and says how far real time has gone towards the next tick so the frame can interpolate.
/// </summary>
public sealed class TickClock
{
    private readonly int ticksPerSecond;
    private readonly int maxTicksPerAdvance;

    // Time not yet turned into ticks, measured in ticks.
    private double pendingTicks;

    /// <param name="ticksPerSecond">Ticks in one second of real time.</param>
    /// <param name="maxTicksPerAdvance">
    /// Most ticks one <see cref="Advance"/> returns. Time beyond it is dropped, so after a stall
    /// the match slows down instead of running so many ticks at once that the next frame stalls too.
    /// </param>
    public TickClock(int ticksPerSecond, int maxTicksPerAdvance = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTicksPerAdvance);

        this.ticksPerSecond = ticksPerSecond;
        this.maxTicksPerAdvance = maxTicksPerAdvance;
    }

    /// <summary>
    /// How far real time has gone from the last tick towards the next, from 0 (just ticked)
    /// up to but excluding 1 (about to tick). It is how far to blend from the state before the
    /// last tick to the state after it.
    /// </summary>
    public double InterpolationFactor => pendingTicks;

    /// <summary>Lets <paramref name="elapsedSeconds"/> of real time pass and returns how many ticks are now due.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elapsedSeconds"/> is negative.</exception>
    public int Advance(double elapsedSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedSeconds);

        pendingTicks += elapsedSeconds * ticksPerSecond;

        var whole = Math.Floor(pendingTicks);
        pendingTicks -= whole;

        return (int)Math.Min(whole, maxTicksPerAdvance);
    }
}
