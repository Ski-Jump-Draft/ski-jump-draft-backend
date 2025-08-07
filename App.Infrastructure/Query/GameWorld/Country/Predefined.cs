using App.Domain.GameWorld;

namespace App.Infrastructure.Query.GameWorld.Country;

using Application.ReadModel.Projection;

public class Predefined(
    IReadOnlyCollection<Domain.GameWorld.Country> gameWorldCountries
) : IGameWorldCountryQuery
{
    private readonly IReadOnlyDictionary<Guid, Domain.GameWorld.Country> _countries =
        gameWorldCountries.ToDictionary(c => c.Id.Item);

    public Task<GameWorldCountryCodeDto?> GetCountryCodeByIdAsync(CountryModule.Id countryId)
    {
        var country = _countries[countryId.Item];
        var countryCodeString = Domain.Shared.CountryCodeModule.value(country.Code);
        return Task.FromResult(new GameWorldCountryCodeDto(countryId.Item, countryCodeString))!;
    }
}