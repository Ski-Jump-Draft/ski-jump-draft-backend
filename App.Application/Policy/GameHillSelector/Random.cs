using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.GameWorld;

namespace App.Application.Policy.GameHillSelector;

public class RandomHill(IRandom random, IHills hills) : IGameHillSelector
{
    public async Task<Guid> Select(CancellationToken ct)
    {
        var allHills = (await hills.GetAll(ct)).ToList();
        return allHills.ToList().GetRandomElement(random).Id.Item;
    }
}