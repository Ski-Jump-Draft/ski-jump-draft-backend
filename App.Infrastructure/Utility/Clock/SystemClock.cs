using App.Application.Utility;

namespace App.Infrastructure.Utility.Clock;

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