using System.Security.Cryptography;
using Microsoft.FSharp.Collections;

namespace App.Infrastructure.Utility;

using System.Linq;

public class SystemRandom : Domain.Shared.Random.IRandom
{
    private readonly System.Random _random = new();

    public FSharpList<T> ShuffleList<T>(int seed, FSharpList<T> list)
    {
        var rnd = new System.Random(seed);
        return ListModule.OfSeq(list.OrderBy(_ => rnd.Next()));
    }

    public int RandomInt(int min, int max)
    {
        return _random.Next(min, max + 1);
    }

    public ulong NextUInt64()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        return BitConverter.ToUInt64(buffer);
    }
}