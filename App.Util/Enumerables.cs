namespace App.Util;

public static class Enumerables
{
    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        => source.ToList();
}