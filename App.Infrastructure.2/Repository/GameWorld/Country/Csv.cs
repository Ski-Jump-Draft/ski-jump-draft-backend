using App.Application._2.Utility;
using CsvHelper;
using Microsoft.FSharp.Core;
using App.Domain._2.GameWorld;
using App.Infrastructure._2.Helper.Csv;
using CsvHelper.Configuration;

namespace App.Infrastructure._2.Repository.GameWorld.Country;

public interface IGameWorldCountriesCsvStreamProvider : ICsvStreamProvider
{
}

public class Csv(IGameWorldCountriesCsvStreamProvider csvStreamProvider, IMyLogger logger, CsvConfiguration csvConfiguration)
    : ICountries
{
    private class CsvCountry
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Alpha2 { get; set; } = null!;
        public string Alpha3 { get; set; } = null!;
        public string Fis { get; set; } = null!;
    }

    private static Domain._2.GameWorld.Country Map(CsvCountry csvCountry)
    {
        return new Domain._2.GameWorld.Country(
            CountryId.NewCountryId(csvCountry.Id),
            Alpha2CodeModule.tryCreate(csvCountry.Alpha2.ToLower()).Value,
            Alpha3CodeModule.tryCreate(csvCountry.Alpha3.ToLower()).Value,
            FisCodeModule.tryCreate(csvCountry.Fis.ToLower()).Value
        );
    }

    public async Task<FSharpOption<Domain._2.GameWorld.Country>> GetById(CountryId jumperId, CancellationToken ct)
    {
        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfiguration);

        await foreach (var csvCountry in csv.GetRecordsAsync<CsvCountry>().WithCancellation(ct))
        {
            try
            {
                if (csvCountry.Id == jumperId.Item)
                    return FSharpOption<Domain._2.GameWorld.Country>.Some(Map(csvCountry));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return FSharpOption<Domain._2.GameWorld.Country>.None;
    }

    public async Task<IEnumerable<Domain._2.GameWorld.Country>> GetAll(CountryId countryId, CancellationToken ct)
    {
        var results = new List<Domain._2.GameWorld.Country>();

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

    public async Task<FSharpOption<Domain._2.GameWorld.Country>> GetByFisCode(FisCode fisCode, CancellationToken ct)
    {
        var target = FisCodeModule.value(fisCode).Trim();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfiguration);

        await foreach (var csvCountry in csv.GetRecordsAsync<CsvCountry>().WithCancellation(ct))
        {
            try
            {
                if (string.Equals(csvCountry.Fis?.Trim(), target, StringComparison.OrdinalIgnoreCase))
                    return FSharpOption<Domain._2.GameWorld.Country>.Some(Map(csvCountry));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return FSharpOption<Domain._2.GameWorld.Country>.None;
    }
}