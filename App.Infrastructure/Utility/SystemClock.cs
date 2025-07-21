using App.Domain.Time;

namespace App.Infrastructure.Utility;

public class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}