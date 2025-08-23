using App.Application._2.Utility;

namespace App.Infrastructure._2.Utility.Clock;

public class System : IClock
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