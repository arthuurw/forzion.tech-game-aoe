namespace Forzion.Simulation;

/// <summary>
/// Accumulates the state hash: 64-bit FNV-1a over the written values, each taken as eight
/// little-endian bytes. Integer arithmetic only, so the result is identical on every platform.
/// </summary>
/// <remarks>
/// Collections must be written as their count followed by their elements in ascending ID
/// order, so that neighbouring collections cannot be mistaken for one another.
/// </remarks>
internal sealed class StateHasher
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public ulong Hash { get; private set; } = OffsetBasis;

    public void Write(ulong value)
    {
        unchecked
        {
            for (var shift = 0; shift < 64; shift += 8)
            {
                Hash = (Hash ^ ((value >> shift) & 0xFF)) * Prime;
            }
        }
    }

    public void Write(long value) => Write(unchecked((ulong)value));

    public void Write(int value) => Write((long)value);
}
