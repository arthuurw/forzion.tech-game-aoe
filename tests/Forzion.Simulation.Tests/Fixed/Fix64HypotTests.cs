using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64HypotTests
{
    [Property]
    public bool Hypotenuse_of_a_scaled_3_4_5_triangle_is_exact(short scale) =>
        Fix64.Hypot(Fix64.FromInt(3 * scale), Fix64.FromInt(-4 * scale)) == Fix64.FromInt(5 * Math.Abs((int)scale));

    [Property]
    public bool Hypotenuse_ignores_argument_order_and_signs(Fix64 x, Fix64 y)
    {
        var length = Fix64.Hypot(x, y);

        return length == Fix64.Hypot(y, x) && length == Fix64.Hypot(-x, y) && length == Fix64.Hypot(x, -y)
            || x == Fix64.MinValue
            || y == Fix64.MinValue;
    }

    [Property]
    public bool Hypotenuse_along_an_axis_is_the_magnitude(Fix64 x) =>
        Fix64.Hypot(x, Fix64.Zero) == Fix64.Abs(x) || x == Fix64.MinValue;

    // Triangle inequality: no shorter than the longer leg, no longer than both legs end to end.
    [Property]
    public bool Hypotenuse_lies_between_the_longer_leg_and_the_sum_of_legs(Fix64 x, Fix64 y)
    {
        var length = Fix64.Hypot(x, y);

        return length >= Fix64.Max(Fix64.Abs(x), Fix64.Abs(y)) && length <= Fix64.Abs(x) + Fix64.Abs(y);
    }

    [Fact]
    public void Hypotenuse_saturates_only_when_the_result_is_out_of_range()
    {
        Assert.Equal(Fix64.MaxValue, Fix64.Hypot(Fix64.MaxValue, Fix64.MaxValue));
        Assert.Equal(Fix64.MaxValue, Fix64.Hypot(Fix64.MinValue, Fix64.MinValue));
        Assert.Equal(Fix64.MaxValue, Fix64.Hypot(Fix64.MinValue, Fix64.Zero));
        Assert.Equal(Fix64.FromInt(1_500_000_000), Fix64.Hypot(Fix64.FromInt(900_000_000), Fix64.FromInt(1_200_000_000)));
    }
}
