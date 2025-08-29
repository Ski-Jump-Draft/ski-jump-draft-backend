using App.Application._2.Extensions;
using App.Domain._2.GameWorld;

namespace App.Application._2.Policy.GameJumpersSelector;

public class All(IJumpers jumpers, ICountries countries) : IGameJumpersSelector
{
    public async Task<IEnumerable<SelectedGameWorldJumperDto>> Select(CancellationToken ct)
    {
        var allJumpers = await jumpers.GetAll(ct);
        var results = new List<SelectedGameWorldJumperDto>();

        foreach (var jumper in allJumpers)
        {
            var country = await countries.GetById(jumper.CountryId, ct);
            if (country.IsSome())
            {
                results.Add(new SelectedGameWorldJumperDto(
                    jumper.Id.Item,
                    FisCodeModule.value(country.Value.FisCode),
                    jumper.Name.Item,
                    jumper.Surname.Item
                ));
            }
        }

        return results;
    }
}