using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64AdditionTests
{
    [Property]
    public bool Adding_converted_integers_matches_integer_addition(short a, short b) =>
        Fix64.FromInt(a) + Fix64.FromInt(b) == Fix64.FromInt(a + b);

    [Property]
    public bool Addition_is_commutative(Fix64 a, Fix64 b) => a + b == b + a;

    [Property]
    public bool Addition_is_associative_away_from_the_limits(Small a, Small b, Small c) =>
        (a.Value + b.Value) + c.Value == a.Value + (b.Value + c.Value);

    [Property]
    public bool Adding_a_non_negative_value_never_decreases(Fix64 a, Fix64 b) =>
        b < Fix64.Zero ? a + b <= a : a + b >= a;

    [Property]
    public bool Zero_is_the_additive_identity(Fix64 a) =>
        a + Fix64.Zero == a && Fix64.Zero + a == a && Fix64.Zero == Fix64.FromInt(0);

    [Fact]
    public void Adding_past_the_largest_value_saturates()
    {
        Assert.Equal(Fix64.MaxValue, Fix64.MaxValue + Fix64.Epsilon);
        Assert.Equal(Fix64.MaxValue, Fix64.MaxValue + Fix64.MaxValue);
        Assert.Equal(Fix64.MinValue, Fix64.MinValue + Fix64.MinValue);
        Assert.Equal(Fix64.MinValue, Fix64.MinValue + Fix64.FromRaw(-1));
    }
}
