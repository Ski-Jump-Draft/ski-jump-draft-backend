using App.Application.Utility;

namespace App.Infrastructure.Utility.GuidUtilities;

public class SystemGuid : IGuid
{
    public global::System.Guid NewGuid()
    {
        return global::System.Guid.NewGuid();
    }
}