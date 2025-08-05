namespace App.Util;

public static class Enumerables
{
    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        => source.ToList();

    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) => source.ToList();
}