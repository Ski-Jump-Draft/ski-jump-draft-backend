using App.Domain.Shared;

namespace App.Tests.Fakes.Utils;

public class TestGuid(Guid guid) : IGuid
{
    public Guid NewGuid() => guid;
}