using App.Application._2.Utility;
using App.Domain._2.GameWorld;
using App.Infrastructure._2.Helper.Csv;
using App.Infrastructure._2.Helper.Csv.GameWorldCountryIdProvider;
using App.Infrastructure._2.Repository.GameWorld.Hill;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.FSharp.Core;

namespace App.Infrastructure._2.Repository.GameWorld.Hill;

public interface IGameWorldHillsCsvStreamProvider : ICsvStreamProvider
{
}

public class Csv(
    IGameWorldHillsCsvStreamProvider csvStreamProvider,
    IGameWorldCountryIdProvider countryIdProvider,
    IMyLogger logger,
    CsvConfiguration csvConfig) : IHills
{
    private class CsvHill
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string CountryFisCode { get; set; } = null!;
        public int KPoint { get; set; }
        public int HsPoint { get; set; }
        public double GatePoints { get; set; }
        public double HeadwindPoints { get; set; }
        public double TailwindPoints { get; set; }
    }

    private static Domain._2.GameWorld.Hill Map(CsvHill src, Guid countryId) =>
        new(
            HillId.NewHillId(src.Id),
            HillModule.Name.NewName(src.Name),
            HillModule.Location.NewLocation(src.Location),
            CountryId.NewCountryId(countryId),
            HillModule.KPointModule.tryCreate(src.KPoint).Value,
            HillModule.HsPointModule.tryCreate(src.HsPoint).Value,
            HillModule.GatePointsModule.tryCreate(src.GatePoints).Value,
            HillModule.WindPointsModule.tryCreate(src.HeadwindPoints).Value,
            HillModule.WindPointsModule.tryCreate(src.TailwindPoints).Value
        );

    private async Task<List<Domain._2.GameWorld.Hill>> LoadAllAsync(CancellationToken ct)
    {
        var results = new List<Domain._2.GameWorld.Hill>();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        await foreach (var csvHill in csv.GetRecordsAsync<CsvHill>().WithCancellation(ct))
        {
            try
            {
                var countryId = await countryIdProvider.GetFromFisCode(csvHill.CountryFisCode, ct);
                results.Add(Map(csvHill, countryId));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return results;
    }

    public async Task<IEnumerable<Domain._2.GameWorld.Hill>> GetAll(CancellationToken ct) =>
        await LoadAllAsync(ct);

    public async Task<FSharpOption<Domain._2.GameWorld.Hill>> GetByFormattedName(
        SearchFormattedName searchFormattedName, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var nameString = SearchFormattedNameModule.value(searchFormattedName);

        // np. "Zakopane HS140"
        var parts = nameString.Split(" HS", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var hs))
            return FSharpOption<Domain._2.GameWorld.Hill>.None;

        var name = parts[0].Trim();

        var found = all.FirstOrDefault(hill =>
            hill.Location.Item.Equals(name, StringComparison.OrdinalIgnoreCase)
            && HillModule.HsPointModule.value(hill.HsPoint) == hs);

        return found is null
            ? FSharpOption<Domain._2.GameWorld.Hill>.None
            : FSharpOption<Domain._2.GameWorld.Hill>.Some(found);
    }


    public async Task<FSharpOption<Domain._2.GameWorld.Hill>> GetById(HillId id, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var found = all.FirstOrDefault(j => j.Id.Equals(id));
        return found is null
            ? FSharpOption<Domain._2.GameWorld.Hill>.None
            : FSharpOption<Domain._2.GameWorld.Hill>.Some(found);
    }

    public async Task<IEnumerable<Domain._2.GameWorld.Hill>> GetByCountryId(CountryId countryId, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        return all.Where(j => j.CountryId.Equals(countryId)).ToList();
    }
}