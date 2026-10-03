using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64DivisionTests
{
    [Fact]
    public void Division_produces_exact_fractions_when_representable()
    {
        Assert.Equal(Half, Fix64.One / Fix64.FromInt(2));
        Assert.Equal(Fix64.FromInt(-3) - Half, Fix64.FromInt(7) / Fix64.FromInt(-2));
        Assert.Equal(Fix64.FromInt(4), Fix64.FromInt(2) / Half);
    }

    [Property]
    public void Dividing_by_zero_throws(Fix64 a) =>
        Assert.Throws<DivideByZeroException>(() => a / Fix64.Zero);

    [Fact]
    public void Quotients_too_fine_to_represent_round_toward_zero()
    {
        var third = Fix64.One / Fix64.FromInt(3);

        Assert.Equal(Fix64.One - Fix64.Epsilon, third * Fix64.FromInt(3));
        Assert.Equal(-third, Fix64.FromInt(-1) / Fix64.FromInt(3));
        Assert.Equal(Fix64.Zero, Fix64.Epsilon / Fix64.FromInt(2));
        Assert.Equal(Fix64.Zero, -Fix64.Epsilon / Fix64.FromInt(2));
    }

    [Fact]
    public void Dividing_past_the_limits_saturates()
    {
        Assert.Equal(Fix64.MaxValue, Fix64.MaxValue / Half);
        Assert.Equal(Fix64.MinValue, Fix64.MaxValue / -Half);
        Assert.Equal(Fix64.MaxValue, Fix64.One / Fix64.Epsilon);
        Assert.Equal(Fix64.MaxValue, Fix64.MinValue / Fix64.FromInt(-1));
        Assert.Equal(Fix64.MinValue, Fix64.MinValue / Fix64.Epsilon);
    }

    [Property]
    public bool Dividing_by_one_changes_nothing(Fix64 a) => a / Fix64.One == a;

    [Property]
    public bool A_non_zero_value_divided_by_itself_is_one(Fix64 a) =>
        a == Fix64.Zero || a / a == Fix64.One;

    [Property]
    public bool Division_undoes_multiplication_by_a_whole_number(Small a, short n)
    {
        if (n == 0)
        {
            return true;
        }

        var whole = Fix64.FromInt(n);

        return a.Value * whole / whole == a.Value;
    }

    [Property]
    public bool Negating_the_dividend_negates_the_quotient(Small a, Small b) =>
        b.Value == Fix64.Zero || -a.Value / b.Value == -(a.Value / b.Value);

    // The quotient drops less than one Epsilon, which the divisor then scales back up.
    [Property]
    public bool Multiplying_the_quotient_back_lands_within_the_divisor_magnitude(Small a, Small b)
    {
        if (b.Value == Fix64.Zero)
        {
            return true;
        }

        var error = Fix64.Abs(a.Value / b.Value * b.Value - a.Value);

        return error <= Fix64.Abs(b.Value) * Fix64.Epsilon + Fix64.Epsilon;
    }

    private static Fix64 Half => Fix64.FromRaw(1L << 31);
}
