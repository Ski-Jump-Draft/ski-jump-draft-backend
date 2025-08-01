using App.Domain.Competition;

namespace App.Infrastructure.Utility.Rng;

public sealed class XorShift64Star(ulong seed) : Engine.IRng<ulong>
{
    public ulong State { get; private set; } = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

    public ulong NextUInt64()
    {
        var x = State;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        State = x;
        return x * 0x2545F4914F6CDD1DUL;
    }

    Engine.IRng<ulong> Engine.IRng<ulong>.WithState(ulong s) => new XorShift64Star(s);
}