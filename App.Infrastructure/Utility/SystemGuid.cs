using App.Domain.Shared;

namespace App.Infrastructure.Utility;

public class SystemGuid : IGuid
{
    public System.Guid NewGuid()
    {
        return System.Guid.NewGuid();
    }
}