using App.Application._2.Extensions;
using App.Domain._2.GameWorld;

namespace App.Infrastructure._2.Helper.Csv.GameWorldCountryIdProvider.Impl;

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