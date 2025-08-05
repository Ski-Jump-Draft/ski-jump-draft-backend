namespace App.Util;

public enum Month
{
    January = 1,
    February = 2,
    March = 3,
    April = 4,
    May = 5,
    June = 6,
    July = 7,
    August = 8,
    September = 9,
    October = 10,
    November = 11,
    December = 12
}

public static class MonthExtensions
{
    /// <summary>
    /// Value should be from 1 to 12
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Month FromInt(int value)
        => (Month)value;
}