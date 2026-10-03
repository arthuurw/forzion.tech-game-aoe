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

    [Fact]
    public void Raw_value_counts_units_of_two_to_the_minus_32()
    {
        Assert.Equal(1L << 32, Fix64.FromInt(1).RawValue);
        Assert.Equal(-3L << 32, Fix64.FromInt(-3).RawValue);
    }
}
