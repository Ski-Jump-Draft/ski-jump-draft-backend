using Microsoft.FSharp.Collections;

namespace App.Application.Extensions;

public static class FSharpSetExt
{
    public static Dictionary<TK, IEnumerable<TV>> ToEnumerableValues<TK, TV>(
        this Dictionary<TK, FSharpSet<TV>> source) where TK : notnull
    {
        // Bez kopiowania: FSharpSet<V> już implementuje IEnumerable<V>
        return source.ToDictionary(kvp => kvp.Key, IEnumerable<TV> (kvp) => kvp.Value);
    }

    public static Dictionary<TK, IEnumerable<TV>> ToEnumerableValues<TK, TV>(
        this Dictionary<TK, FSharpList<TV>> source) where TK : notnull
    {
        // Bez kopiowania: FSharpSet<V> już implementuje IEnumerable<V>
        return source.ToDictionary(kvp => kvp.Key, IEnumerable<TV> (kvp) => kvp.Value);
    }
}