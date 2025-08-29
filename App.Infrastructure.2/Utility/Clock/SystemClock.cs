using App.Application._2.Utility;

namespace App.Infrastructure._2.Utility.Clock;

public class SystemClock : IClock
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    public DateTimeOffset Now()
    {
        return DateTimeOffset.Now;
    }
}