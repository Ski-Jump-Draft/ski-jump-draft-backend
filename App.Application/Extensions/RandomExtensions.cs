using App.Application.Utility;

namespace App.Application.Extensions;

public static class RandomExtensions
{
    public static TKey WeightedRandomElement<TKey>(
        this IDictionary<TKey, double> dict, IRandom rnd)
    {
        var total = dict.Values.Sum();
        if (total <= 0) throw new InvalidOperationException("No positive weights");

        var roll = rnd.NextDouble() * total;
        double cum = 0;

        foreach (var kv in dict)
        {
            cum += kv.Value;
            if (roll < cum)
                return kv.Key;
        }

        throw new InvalidOperationException("Should not be reached.");
    }
}