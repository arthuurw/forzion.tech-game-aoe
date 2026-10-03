namespace Forzion.Simulation;

/// <summary>
/// Deterministic signed fixed-point number in Q32.32 format (ADR 0002).
/// </summary>
public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
{
    private const int FractionalBits = 32;

    private readonly long raw;

    private Fix64(long raw) => this.raw = raw;

    /// <summary>
    /// The underlying representation: the value multiplied by 2^32. Exposed for hashing and
    /// serialization of simulation state.
    /// </summary>
    public long RawValue => raw;

    public static Fix64 FromRaw(long rawValue) => new(rawValue);

    public static Fix64 FromInt(int value) => new((long)value << FractionalBits);

    /// <summary>Largest integer less than or equal to this value.</summary>
    public int FloorToInt() => (int)(raw >> FractionalBits);

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

    public static Fix64 Min(Fix64 a, Fix64 b) => a.raw <= b.raw ? a : b;

    public static Fix64 Max(Fix64 a, Fix64 b) => a.raw >= b.raw ? a : b;
}
