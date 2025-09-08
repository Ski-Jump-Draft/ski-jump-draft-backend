using App.Application.Extensions;
using App.Domain.GameWorld;

namespace App.Application.Policy.GameJumpersSelector;

public class All(IJumpers jumpers, ICountries countries) : IGameJumpersSelector
{
    public async Task<IEnumerable<SelectedGameWorldJumperDto>> Select(CancellationToken ct)
    {
        var allJumpers = await jumpers.GetAll(ct);

        return allJumpers.Select(jumper => new SelectedGameWorldJumperDto(jumper.Id.Item,
            CountryFisCodeModule.value(jumper.FisCountryCode), jumper.Name.Item, jumper.Surname.Item)).ToList();
    }
}