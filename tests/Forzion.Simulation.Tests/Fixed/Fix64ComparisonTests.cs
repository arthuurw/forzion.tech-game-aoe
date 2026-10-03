using FsCheck.Xunit;

namespace Forzion.Simulation.Tests.Fixed;

[Properties(Arbitrary = new[] { typeof(Fix64Generators) })]
public class Fix64ComparisonTests
{
    [Property]
    public bool Values_are_equal_exactly_when_they_convert_from_the_same_integer(int a, int b)
    {
        var left = Fix64.FromInt(a);
        var right = Fix64.FromInt(b);
        var same = a == b;

        return (left == right) == same
            && (left != right) == !same
            && left.Equals(right) == same
            && left.Equals((object)right) == same
            && (!same || left.GetHashCode() == right.GetHashCode());
    }

    [Property]
    public bool Converted_integers_keep_their_order(int a, int b)
    {
        var left = Fix64.FromInt(a);
        var right = Fix64.FromInt(b);

        return (left < right) == (a < b)
            && (left <= right) == (a <= b)
            && (left > right) == (a > b)
            && (left >= right) == (a >= b)
            && Math.Sign(left.CompareTo(right)) == a.CompareTo(b);
    }

    [Property]
    public bool Exactly_one_of_less_equal_greater_holds(Fix64 a, Fix64 b) =>
        new[] { a < b, a == b, a > b }.Count(holds => holds) == 1;

    [Property]
    public bool Sorting_puts_any_three_values_in_non_decreasing_order(Fix64 a, Fix64 b, Fix64 c)
    {
        var sorted = new[] { a, b, c };
        Array.Sort(sorted);

        return sorted[0] <= sorted[1] && sorted[1] <= sorted[2] && sorted[0] <= sorted[2];
    }

    [Fact]
    public void Fractions_order_between_their_neighbouring_integers()
    {
        var half = Fix64.FromRaw(1L << 31);
        var minusHalf = Fix64.FromRaw(-(1L << 31));

        Assert.True(Fix64.FromInt(0) < half);
        Assert.True(half < Fix64.FromInt(1));
        Assert.True(Fix64.FromInt(-1) < minusHalf);
        Assert.True(minusHalf < Fix64.FromInt(0));
    }

    [Property]
    public bool Min_and_max_split_a_pair_in_order(Fix64 a, Fix64 b)
    {
        var min = Fix64.Min(a, b);
        var max = Fix64.Max(a, b);

        return min <= max && ((min == a && max == b) || (min == b && max == a));
    }
}
