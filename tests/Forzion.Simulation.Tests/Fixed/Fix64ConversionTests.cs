using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

public class Fix64ConversionTests
{
    [Property]
    public bool Integer_survives_round_trip(int value) =>
        Fix64.FromInt(value).FloorToInt() == value;

    [Property]
    public bool Raw_value_survives_round_trip(long raw) =>
        Fix64.FromRaw(raw).RawValue == raw;

    [Property]
    public bool Floor_drops_the_fraction_toward_negative_infinity(int whole, uint fraction) =>
        WholePlusFraction(whole, fraction).FloorToInt() == whole;

    [Property]
    public bool Ceiling_of_a_whole_number_is_itself(int whole) =>
        Fix64.FromInt(whole).CeilingToInt() == whole;

    [Property]
    public bool Ceiling_lifts_any_fraction_to_the_next_integer(short whole, uint fraction) =>
        fraction == 0 || WholePlusFraction(whole, fraction).CeilingToInt() == whole + 1;

    [Fact]
    public void Ceiling_beyond_the_integer_range_saturates() =>
        Assert.Equal(int.MaxValue, Fix64.MaxValue.CeilingToInt());

    [Property]
    public bool Rounding_a_whole_number_gives_itself(int whole) =>
        Fix64.FromInt(whole).RoundToInt() == whole;

    // Halves round away from zero, so rounding stays symmetric around the origin.
    [Fact]
    public void Rounding_picks_the_nearest_integer_with_halves_away_from_zero()
    {
        var half = Fix64.FromRaw(1L << 31);

        Assert.Equal(1, half.RoundToInt());
        Assert.Equal(-1, (-half).RoundToInt());
        Assert.Equal(0, (half - Fix64.Epsilon).RoundToInt());
        Assert.Equal(0, (Fix64.Epsilon - half).RoundToInt());
        Assert.Equal(3, (Fix64.FromInt(2) + half).RoundToInt());
        Assert.Equal(-3, (Fix64.FromInt(-2) - half).RoundToInt());
        Assert.Equal(-2, (Fix64.FromInt(-2) - half + Fix64.Epsilon).RoundToInt());
        Assert.Equal(int.MaxValue, Fix64.MaxValue.RoundToInt());
        Assert.Equal(int.MinValue, Fix64.MinValue.RoundToInt());
    }

    [Property]
    public bool Rounding_is_symmetric_around_zero(Small a) =>
        (-a.Value).RoundToInt() == -a.Value.RoundToInt();

    private static Fix64 WholePlusFraction(int whole, uint fraction) =>
        Fix64.FromRaw(((long)whole << 32) + fraction);

    [Fact]
    public void Raw_value_counts_units_of_two_to_the_minus_32()
    {
        Assert.Equal(1L << 32, Fix64.FromInt(1).RawValue);
        Assert.Equal(-3L << 32, Fix64.FromInt(-3).RawValue);
    }
}
