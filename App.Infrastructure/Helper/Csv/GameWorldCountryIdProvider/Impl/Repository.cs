using App.Application.Extensions;
using App.Domain.GameWorld;

namespace App.Infrastructure.Helper.Csv.GameWorldCountryIdProvider.Impl;

public class Repository(ICountries countries) : IGameWorldCountryIdProvider
{
    public async Task<Guid> GetFromFisCode(string fisCode, CancellationToken ct = default)
    {
        var domainFisCode = FisCodeModule.tryCreate(fisCode);
        if (domainFisCode == null)
        {
            throw new Exception($"fisCode ({fisCode}) is not in valid format");
        }

        var country = await countries.GetByFisCode(domainFisCode.Value, ct).AwaitOrWrap(_ =>
            throw new KeyNotFoundException($"Country with Fis Code '{fisCode}' not found"));
        return country.Id.Item;
    }
}