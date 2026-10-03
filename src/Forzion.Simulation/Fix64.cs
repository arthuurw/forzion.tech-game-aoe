namespace Forzion.Simulation;

/// <summary>
/// Deterministic signed fixed-point number in Q32.32 format (ADR 0002).
/// </summary>
public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
{
    private const int FractionalBits = 32;
    private const long RawOne = 1L << FractionalBits;
    private const long FractionMask = RawOne - 1;

    private readonly long raw;

    private Fix64(long raw) => this.raw = raw;

    public static Fix64 Zero => default;

    public static Fix64 One => new(RawOne);

    /// <summary>Largest representable value: 2^31 - 2^-32.</summary>
    public static Fix64 MaxValue => new(long.MaxValue);

    /// <summary>Smallest representable value: -2^31.</summary>
    public static Fix64 MinValue => new(long.MinValue);

    /// <summary>Smallest positive value and the resolution of the type: 2^-32.</summary>
    public static Fix64 Epsilon => new(1);

    /// <summary>
    /// The underlying representation: the value multiplied by 2^32. Exposed for hashing and
    /// serialization of simulation state.
    /// </summary>
    public long RawValue => raw;

    public static Fix64 FromRaw(long rawValue) => new(rawValue);

    public static Fix64 FromInt(int value) => new((long)value << FractionalBits);

    /// <summary>Largest integer less than or equal to this value.</summary>
    public int FloorToInt() => (int)(raw >> FractionalBits);

    /// <summary>
    /// Smallest integer greater than or equal to this value, saturating at
    /// <see cref="int.MaxValue"/> for values above it.
    /// </summary>
    public int CeilingToInt()
    {
        var floor = raw >> FractionalBits;
        var ceiling = (raw & FractionMask) == 0 ? floor : floor + 1;

        return (int)Math.Min(ceiling, int.MaxValue);
    }

    /// <summary>
    /// Nearest integer, with halves rounded away from zero, saturating at
    /// <see cref="int.MaxValue"/> for values that round above it.
    /// </summary>
    public int RoundToInt()
    {
        const long RawHalf = RawOne / 2;

        Int128 wide = raw;
        var rounded = wide >= 0
            ? (wide + RawHalf) >> FractionalBits
            : -((-wide + RawHalf) >> FractionalBits);

        return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
    }

    /// <summary>
    /// Nearest <see cref="double"/>. For the presentation layer only: the simulation core must
    /// never feed a floating-point value back into its state (ADR 0002).
    /// </summary>
    public double ToDouble() => raw / (double)RawOne;

    /// <summary>
    /// Nearest <see cref="float"/>, the precision the engine renders with. Presentation layer
    /// only, like <see cref="ToDouble"/>.
    /// </summary>
    public float ToFloat() => (float)ToDouble();

    public static bool operator ==(Fix64 left, Fix64 right) => left.raw == right.raw;

    public static bool operator !=(Fix64 left, Fix64 right) => left.raw != right.raw;

    public bool Equals(Fix64 other) => raw == other.raw;

    public override bool Equals(object? obj) => obj is Fix64 other && Equals(other);

    public override int GetHashCode() => raw.GetHashCode();

    public static bool operator <(Fix64 left, Fix64 right) => left.raw < right.raw;

    public static bool operator <=(Fix64 left, Fix64 right) => left.raw <= right.raw;

    public static bool operator >(Fix64 left, Fix64 right) => left.raw > right.raw;

    public static bool operator >=(Fix64 left, Fix64 right) => left.raw >= right.raw;

    public int CompareTo(Fix64 other) => raw.CompareTo(other.raw);

    public static Fix64 operator +(Fix64 left, Fix64 right)
    {
        var sum = unchecked(left.raw + right.raw);

        // Overflow happened exactly when both operands share a sign that the wrapped sum lacks.
        if (((left.raw ^ sum) & (right.raw ^ sum)) < 0)
        {
            return left.raw < 0 ? MinValue : MaxValue;
        }

        return new(sum);
    }

    public static Fix64 operator -(Fix64 left, Fix64 right)
    {
        var difference = unchecked(left.raw - right.raw);

        // Overflow happened exactly when the operands differ in sign and the wrapped
        // difference lost the sign of the left operand.
        if (((left.raw ^ right.raw) & (left.raw ^ difference)) < 0)
        {
            return left.raw < 0 ? MinValue : MaxValue;
        }

        return new(difference);
    }

    public static Fix64 operator -(Fix64 value) =>
        value.raw == long.MinValue ? MaxValue : new(-value.raw);

    public static Fix64 operator *(Fix64 left, Fix64 right) =>
        Saturate((Int128)left.raw * right.raw / RawOne);

    /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
    public static Fix64 operator /(Fix64 left, Fix64 right)
    {
        // Int128 division does not reliably report a zero divisor as DivideByZeroException.
        if (right.raw == 0)
        {
            throw new DivideByZeroException();
        }

        return Saturate((Int128)left.raw * RawOne / right.raw);
    }

    /// <summary>Square root, rounded down to the nearest representable value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public static Fix64 Sqrt(Fix64 value)
    {
        if (value.raw < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Square root of a negative value.");
        }

        return new((long)IntegerSqrt((UInt128)value.raw << FractionalBits));
    }

    /// <summary>
    /// Length of the vector (x, y), rounded down: the distance between two points given their
    /// coordinate differences. The squares are held in 128 bits, so only a result beyond
    /// <see cref="MaxValue"/> saturates.
    /// </summary>
    public static Fix64 Hypot(Fix64 x, Fix64 y)
    {
        var sumOfSquares = (UInt128)((Int128)x.raw * x.raw) + (UInt128)((Int128)y.raw * y.raw);
        var length = IntegerSqrt(sumOfSquares);

        return length > (UInt128)long.MaxValue ? MaxValue : new((long)length);
    }

    /// <summary>Floor of the square root, computed digit by digit in base 4.</summary>
    private static UInt128 IntegerSqrt(UInt128 n)
    {
        UInt128 root = 0;
        var bit = UInt128.One << 126;

        while (bit > n)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (n >= root + bit)
            {
                n -= root + bit;
                root = (root >> 1) + bit;
            }
            else
            {
                root >>= 1;
            }

            bit >>= 2;
        }

        return root;
    }

    private static Fix64 Saturate(Int128 wideRaw)
    {
        if (wideRaw > long.MaxValue)
        {
            return MaxValue;
        }

        if (wideRaw < long.MinValue)
        {
            return MinValue;
        }

        return new((long)wideRaw);
    }

    /// <summary>Magnitude of the value. The magnitude of <see cref="MinValue"/> saturates.</summary>
    public static Fix64 Abs(Fix64 value) => value.raw < 0 ? -value : value;

    public static Fix64 Min(Fix64 a, Fix64 b) => a.raw <= b.raw ? a : b;

    public static Fix64 Max(Fix64 a, Fix64 b) => a.raw >= b.raw ? a : b;
}
