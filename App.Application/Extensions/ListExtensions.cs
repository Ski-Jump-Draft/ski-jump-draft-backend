using App.Application.Utility;

namespace App.Application.Extensions;

public static class ListExtensions
{
    public static T GetRandomElement<T>(this IList<T> list, IRandom random) =>
        list[random.RandomInt(0, list.Count)];

    public static List<T> Shuffle<T>(this List<T> list, IRandom random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
    
    
}