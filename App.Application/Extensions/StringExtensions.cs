namespace App.Application.Extensions;

public static class StringExtensions
{
    public static string RemoveFromEndIfPresent(this string str, string suffix)
    {
        if (str.EndsWith(suffix))
            str = str[..^suffix.Length];
        return str;
    }

    public static string RemoveFromEndInWordsIfPresent(this string str, string suffix)
    {
        var words = str.Split(' ');
        var changedWords = words.Select(word => word.RemoveFromEndIfPresent(suffix));
        return string.Join(" ", changedWords).TrimEnd();
    }
}