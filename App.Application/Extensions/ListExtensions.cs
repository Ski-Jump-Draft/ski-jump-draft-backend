using App.Application.Utility;

namespace App.Application.Extensions;

public static class ListExtensions
{
    public static T GetRandomElement<T>(this IList<T> list, IRandom random) =>
        list[random.RandomInt(0, list.Count)];
}