using App.Application._2.Utility;

namespace App.Infrastructure._2.Utility.Guid;

public class System : IGuid
{
    public global::System.Guid NewGuid()
    {
        return global::System.Guid.NewGuid();
    }
}