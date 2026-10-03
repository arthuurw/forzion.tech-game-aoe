using FsCheck;
using FsCheck.Fluent;

namespace Forzion.Simulation.Tests.Fixed;

/// <summary>
/// A <see cref="Fix64"/> with magnitude below 2^15, so sums and products of a few of them never
/// reach the limits of the type. Used by properties that only hold away from saturation.
/// </summary>
public readonly record struct Small(Fix64 Value);

/// <summary>FsCheck generators for <see cref="Fix64"/>, registered per test class.</summary>
public static class Fix64Generators
{
    private const int SmallIntegerLimit = 1 << 15;

    private static readonly Gen<int> AnyInt = Gen.Choose(int.MinValue, int.MaxValue);

    private static readonly Gen<Fix64> FullRange =
        from high in AnyInt
        from low in AnyInt
        select Fix64.FromRaw(((long)high << 32) | (uint)low);

    private static readonly Gen<Fix64> SmallRange =
        from integer in Gen.Choose(-SmallIntegerLimit, SmallIntegerLimit - 1)
        from fraction in AnyInt
        select Fix64.FromRaw(((long)integer << 32) | (uint)fraction);

    private static readonly Gen<Fix64> WholeNumbers =
        Gen.Choose(-SmallIntegerLimit, SmallIntegerLimit - 1).Select(Fix64.FromInt);

    private static readonly Gen<Fix64> EdgeCases = Gen.Elements(
        Fix64.FromRaw(0),
        Fix64.FromRaw(1),
        Fix64.FromRaw(-1),
        Fix64.FromInt(1),
        Fix64.FromInt(-1),
        Fix64.FromRaw(long.MaxValue),
        Fix64.FromRaw(long.MaxValue - 1),
        Fix64.FromRaw(long.MinValue),
        Fix64.FromRaw(long.MinValue + 1));

    public static Arbitrary<Fix64> AnyFix64() =>
        Gen.OneOf(FullRange, FullRange, SmallRange, SmallRange, WholeNumbers, EdgeCases).ToArbitrary();

    public static Arbitrary<Small> AnySmall() =>
        Gen.OneOf(SmallRange, SmallRange, WholeNumbers).Select(value => new Small(value)).ToArbitrary();
}
