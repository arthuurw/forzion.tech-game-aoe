using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64MultiplicationTests
{
    [Property]
    public bool Multiplying_converted_integers_matches_integer_multiplication(short a, short b) =>
        Fix64.FromInt(a) * Fix64.FromInt(b) == Fix64.FromInt(a * b);

    [Fact]
    public void Fractions_multiply_exactly_when_the_product_is_representable()
    {
        Assert.Equal(Quarter, Half * Half);
        Assert.Equal(Fix64.FromInt(-3) - Half - Quarter, (Fix64.FromInt(1) + Half) * (Fix64.FromInt(-2) - Half));
    }

    // Rounding toward zero keeps the simulation symmetric: mirrored inputs give mirrored outputs.
    [Fact]
    public void Products_too_fine_to_represent_round_toward_zero()
    {
        Assert.Equal(Fix64.Zero, Fix64.Epsilon * Half);
        Assert.Equal(Fix64.Zero, -Fix64.Epsilon * Half);
        Assert.Equal(Fix64.Epsilon, Fix64.FromRaw(3) * Half);
        Assert.Equal(-Fix64.Epsilon, Fix64.FromRaw(-3) * Half);
    }

    [Property]
    public bool Negating_a_factor_negates_the_product(Small a, Small b) =>
        -a.Value * b.Value == -(a.Value * b.Value);

    [Property]
    public bool Multiplication_is_commutative(Fix64 a, Fix64 b) => a * b == b * a;

    [Property]
    public bool One_is_the_multiplicative_identity(Fix64 a) =>
        a * Fix64.One == a && Fix64.One * a == a && Fix64.One == Fix64.FromInt(1);

    [Property]
    public bool Zero_annihilates_every_value(Fix64 a) => a * Fix64.Zero == Fix64.Zero;

    [Property]
    public bool Multiplying_by_three_is_adding_three_times(Small a) =>
        a.Value * Fix64.FromInt(3) == a.Value + a.Value + a.Value;

    // Each product drops less than one Epsilon, so the two sides differ by at most one Epsilon.
    [Property]
    public bool Multiplication_distributes_over_addition_within_one_epsilon(Small a, Small b, Small c)
    {
        var difference = a.Value * (b.Value + c.Value) - (a.Value * b.Value + a.Value * c.Value);

        return Fix64.Abs(difference) <= Fix64.Epsilon;
    }

    [Property]
    public bool Multiplying_by_a_non_negative_value_preserves_order(Fix64 a, Fix64 b, Fix64 c)
    {
        var factor = Fix64.Abs(a);
        var low = Fix64.Min(b, c);
        var high = Fix64.Max(b, c);

        return factor * low <= factor * high;
    }

    [Fact]
    public void Multiplying_past_the_limits_saturates()
    {
        var large = Fix64.FromInt(1 << 20);

        Assert.Equal(Fix64.MaxValue, large * large);
        Assert.Equal(Fix64.MinValue, large * -large);
        Assert.Equal(Fix64.MaxValue, Fix64.MinValue * Fix64.MinValue);
        Assert.Equal(Fix64.MaxValue, Fix64.MinValue * Fix64.FromInt(-1));
        Assert.Equal(Fix64.MinValue, Fix64.MaxValue * Fix64.MinValue);
    }

    private static Fix64 Half => Fix64.FromRaw(1L << 31);

    private static Fix64 Quarter => Fix64.FromRaw(1L << 30);
}
