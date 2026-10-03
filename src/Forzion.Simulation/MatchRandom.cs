namespace Forzion.Simulation;

/// <summary>
/// The match's own random generator (ADR 0002): SplitMix64, seeded from the match
/// configuration. Its state is part of the match state and of the state hash, so every draw
/// is reproduced by a replay. Every random decision in the core must come from here.
/// </summary>
internal sealed class MatchRandom
{
    private ulong state;

    public MatchRandom(ulong seed) => state = seed;

    public ulong NextUInt64()
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;

            var z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }
    }

    /// <summary>
    /// A number from 0 up to, but not including, <paramref name="exclusiveMax"/>. Plain
    /// remainder: the bias it carries is far below anything a match can notice.
    /// </summary>
    public int NextInt(int exclusiveMax) => (int)(NextUInt64() % (ulong)exclusiveMax);

    public void WriteTo(StateHasher hasher) => hasher.Write(state);
}
