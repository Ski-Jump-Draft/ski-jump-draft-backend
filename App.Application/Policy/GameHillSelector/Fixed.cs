using App.Application.Extensions;
using App.Domain.GameWorld;

namespace App.Application.Policy.GameHillSelector;

public class Fixed(string formattedHillName, IHills hills) : IGameHillSelector
{
    public async Task<Guid> Select(CancellationToken ct)
    {
        var formattedName = SearchFormattedNameModule.tryCreate(formattedHillName).Value;
        var hill = await hills.GetByFormattedName(formattedName, ct).AwaitOrWrap(_ =>
            throw new Exception($"GameWorld Hill ({SearchFormattedNameModule.value(formattedName)}) not found"));
        return hill.Id.Item;
    }
}