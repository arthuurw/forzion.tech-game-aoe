using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64SubtractionTests
{
    [Property]
    public bool Negating_a_converted_integer_matches_integer_negation(short a) =>
        -Fix64.FromInt(a) == Fix64.FromInt(-a);

    [Property]
    public bool A_value_plus_its_negation_is_zero(Fix64 a) =>
        a == Fix64.MinValue || a + -a == Fix64.Zero;

    [Fact]
    public void Negating_the_smallest_value_saturates_to_the_largest() =>
        Assert.Equal(Fix64.MaxValue, -Fix64.MinValue);

    [Property]
    public bool Subtracting_converted_integers_matches_integer_subtraction(short a, short b) =>
        Fix64.FromInt(a) - Fix64.FromInt(b) == Fix64.FromInt(a - b);

    [Property]
    public bool A_value_minus_itself_is_zero(Fix64 a) => a - a == Fix64.Zero;

    [Property]
    public bool Subtraction_undoes_addition_away_from_the_limits(Small a, Small b) =>
        (a.Value + b.Value) - b.Value == a.Value;

    [Property]
    public bool Absolute_value_is_non_negative_and_keeps_the_magnitude(Fix64 a)
    {
        var magnitude = Fix64.Abs(a);

        return magnitude >= Fix64.Zero
            && magnitude == Fix64.Abs(-a)
            && (a == Fix64.MinValue ? magnitude == Fix64.MaxValue : magnitude == a || magnitude == -a);
    }

    [Fact]
    public void Subtracting_past_the_limits_saturates()
    {
        Assert.Equal(Fix64.MinValue, Fix64.MinValue - Fix64.Epsilon);
        Assert.Equal(Fix64.MaxValue, Fix64.MaxValue - Fix64.MinValue);
        Assert.Equal(Fix64.MaxValue, Fix64.Zero - Fix64.MinValue);
        Assert.Equal(Fix64.MinValue, Fix64.MinValue - Fix64.MaxValue);
    }
}
