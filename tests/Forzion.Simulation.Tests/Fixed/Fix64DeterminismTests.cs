namespace Forzion.Simulation.Tests.Fixed;

public class Fix64DeterminismTests
{
    // Recorded from an independent arbitrary-precision model of the documented semantics
    // (saturation, rounding toward zero, floor square root), not from this implementation.
    // CI runs this on Windows, Linux and macOS: every system must reach the same checksum.
    private const ulong ExpectedChecksum = 9700085117764585896UL;

    [Fact]
    public void A_fixed_sequence_of_operations_reaches_the_recorded_checksum()
    {
        var state = 0x0123456789ABCDEFUL;
        var checksum = 14695981039346656037UL;

        for (var i = 0; i < 20_000; i++)
        {
            var a = NextValue(ref state);
            var b = NextValue(ref state);

            Fold(ref checksum, a + b);
            Fold(ref checksum, a - b);
            Fold(ref checksum, a * b);
            Fold(ref checksum, b == Fix64.Zero ? Fix64.Zero : a / b);
            Fold(ref checksum, Fix64.Sqrt(Fix64.Abs(a)));
            Fold(ref checksum, Fix64.Hypot(a, b));
        }

        Assert.Equal(ExpectedChecksum, checksum);
    }

    // Linear congruential generator; the top bits pick a magnitude so that tiny, ordinary and
    // saturating operands all occur.
    private static Fix64 NextValue(ref ulong state)
    {
        unchecked
        {
            state = state * 6364136223846793005UL + 1442695040888963407UL;
            var shift = (int)(state >> 59) * 2;
            state = state * 6364136223846793005UL + 1442695040888963407UL;

            return Fix64.FromRaw((long)state >> shift);
        }
    }

    private static void Fold(ref ulong checksum, Fix64 value)
    {
        unchecked
        {
            checksum = (checksum ^ (ulong)value.RawValue) * 1099511628211UL;
        }
    }
}
