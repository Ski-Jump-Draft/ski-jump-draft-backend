using App.Domain.Shared;

namespace App.Infrastructure.Utility;

public class DefaultGuid : IGuid
{
    public System.Guid NewGuid()
    {
        return System.Guid.NewGuid();
    }
}