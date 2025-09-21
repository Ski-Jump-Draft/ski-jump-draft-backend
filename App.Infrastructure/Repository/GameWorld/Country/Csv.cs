using App.Application.Utility;
using CsvHelper;
using Microsoft.FSharp.Core;
using App.Domain.GameWorld;
using App.Infrastructure.Helper.Csv;
using CsvHelper.Configuration;

namespace App.Infrastructure.Repository.GameWorld.Country;

public interface IGameWorldCountriesCsvStreamProvider : ICsvStreamProvider
{
}

public class Csv(
    IGameWorldCountriesCsvStreamProvider csvStreamProvider,
    IMyLogger logger,
    CsvConfiguration csvConfiguration)
    : ICountries
{
    private class CsvCountry
    {
        public string Alpha2 { get; set; } = null!;
        public string Alpha3 { get; set; } = null!;
        public string Fis { get; set; } = null!;
    }

    private static Domain.GameWorld.Country Map(CsvCountry csvCountry)
    {
        return new Domain.GameWorld.Country(
            Alpha2CodeModule.tryCreate(csvCountry.Alpha2.ToLower()).Value,
            Alpha3CodeModule.tryCreate(csvCountry.Alpha3.ToLower()).Value,
            CountryFisCodeModule.tryCreate(csvCountry.Fis.ToLower()).Value
        );
    }

    public async Task<FSharpOption<Domain.GameWorld.Country>> GetByFisCode(CountryFisCode countryFisCode,
        CancellationToken ct)
    {
        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfiguration);

        await foreach (var csvCountry in csv.GetRecordsAsync<CsvCountry>().WithCancellation(ct))
        {
            try
            {
                if (csvCountry.Fis.Equals(CountryFisCodeModule.value(countryFisCode),
                        StringComparison.InvariantCultureIgnoreCase))
                    return FSharpOption<Domain.GameWorld.Country>.Some(Map(csvCountry));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return FSharpOption<Domain.GameWorld.Country>.None;
    }

    public async Task<IEnumerable<Domain.GameWorld.Country>> GetAll(CancellationToken ct)
    {
        var results = new List<Domain.GameWorld.Country>();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfiguration);

        await foreach (var csvCountry in csv.GetRecordsAsync<CsvCountry>().WithCancellation(ct))
        {
            try
            {
                results.Add(Map(csvCountry));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return results;
    }
}