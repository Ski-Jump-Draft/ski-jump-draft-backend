using App.Application.Abstractions;

namespace App.Plugin.Competitions.AdvancementTieBreaker;

public class ByBib(
    Func<Guid, int> getBib,
    int howMany,
    bool lowestFirst = false
) : IAdvancementTieBreaker
{
    public IEnumerable<Guid> BreakTies(IEnumerable<Guid> tied)
    {
        var tiedList = tied.ToList();
        if (tiedList.Count <= howMany)
            return tiedList;

        var ordered = lowestFirst
            ? tiedList.OrderBy(getBib)
            : tiedList.OrderByDescending(getBib);

        return ordered.Take(howMany);
    }
}
