using App.Application._2.Utility;

namespace App.Infrastructure._2.Utility.Guid;

public class SystemGuid : IGuid
{
    public global::System.Guid NewGuid()
    {
        return global::System.Guid.NewGuid();
    }
}