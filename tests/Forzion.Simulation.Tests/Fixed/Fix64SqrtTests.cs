using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64SqrtTests
{
    [Property]
    public bool Square_root_of_a_perfect_square_is_exact(ushort n)
    {
        var limited = n % 46341;

        return Fix64.Sqrt(Fix64.FromInt(limited * limited)) == Fix64.FromInt(limited);
    }

    [Fact]
    public void Square_root_matches_known_values()
    {
        // floor(sqrt(2) * 2^32): sqrt(2) = 1.41421356237309504880...
        Assert.Equal(6074000999L, Fix64.Sqrt(Fix64.FromInt(2)).RawValue);
        Assert.Equal(Fix64.FromRaw(1L << 31), Fix64.Sqrt(Fix64.FromRaw(1L << 30)));
        Assert.Equal(Fix64.Zero, Fix64.Sqrt(Fix64.Zero));
        // sqrt(2^-32) = 2^-16
        Assert.Equal(Fix64.FromRaw(1L << 16), Fix64.Sqrt(Fix64.Epsilon));
        // floor(sqrt(2^31 - 2^-32) * 2^32) = floor(sqrt(2^95 - 2^32)) = floor(2^47.5) = 199032864766430
        Assert.Equal(199032864766430L, Fix64.Sqrt(Fix64.MaxValue).RawValue);
    }

    // The root is rounded down, so its square never exceeds the input and the square of the
    // next representable value never falls below it.
    [Property]
    public bool Square_root_is_the_largest_value_whose_square_fits(Fix64 a)
    {
        var value = Fix64.Abs(a);
        var root = Fix64.Sqrt(value);
        var next = root + Fix64.Epsilon;

        return root >= Fix64.Zero && root * root <= value && next * next >= value;
    }

    [Property]
    public bool Square_root_preserves_order(Fix64 a, Fix64 b)
    {
        var low = Fix64.Min(Fix64.Abs(a), Fix64.Abs(b));
        var high = Fix64.Max(Fix64.Abs(a), Fix64.Abs(b));

        return Fix64.Sqrt(low) <= Fix64.Sqrt(high);
    }

    [Property]
    public void Square_root_of_a_negative_value_throws(Fix64 a)
    {
        var negative = Fix64.Min(a, -a);

        if (negative < Fix64.Zero)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fix64.Sqrt(negative));
        }
    }
}
