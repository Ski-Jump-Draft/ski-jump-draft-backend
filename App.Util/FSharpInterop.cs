using Microsoft.FSharp.Collections;

namespace App.Util;

public static class FSharpInterop
{
    public static FSharpMap<TKey, FSharpList<TValue>> ToFSharpMap<TKey, TValue>(
        Dictionary<TKey, List<TValue>> dict) where TKey : notnull
    {
        return MapModule.OfSeq(
            dict.Select(kvp =>
                Tuple.Create(kvp.Key, ListModule.OfSeq(kvp.Value))
            )
        );
    }
}