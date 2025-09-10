using FluentAssertions.Common;
using IClock = App.Domain.Time.IClock;

namespace App.Tests.Fakes.Utils;

public class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now => now;
    public DateTime UtcNow => now.UtcDateTime;
}