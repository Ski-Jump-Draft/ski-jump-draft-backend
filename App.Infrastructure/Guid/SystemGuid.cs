using App.Domain.Shared;

namespace App.Infrastructure.Guid;

public class SystemGuid : IGuid
{
    public System.Guid NewGuid()
    {
        return System.Guid.NewGuid();
    }
}