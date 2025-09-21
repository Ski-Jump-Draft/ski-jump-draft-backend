using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Infrastructure.Helper.Csv;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.Repository.GameWorld.Hill;

public interface IGameWorldHillsCsvStreamProvider : ICsvStreamProvider
{
}

public class Csv(
    IGameWorldHillsCsvStreamProvider csvStreamProvider,
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

    private static Domain.GameWorld.Hill Map(CsvHill src) =>
        new(
            HillId.NewHillId(src.Id),
            HillModule.Name.NewName(src.Name),
            HillModule.Location.NewLocation(src.Location),
            CountryFisCodeModule.tryCreate(src.CountryFisCode) .OrThrow($"Invalid CountryFisCode format for a hill {src.Location} HS{src.HsPoint}"),
            HillModule.KPointModule.tryCreate(src.KPoint).Value,
            HillModule.HsPointModule.tryCreate(src.HsPoint).Value,
            HillModule.GatePointsModule.tryCreate(src.GatePoints).Value,
            HillModule.WindPointsModule.tryCreate(src.HeadwindPoints).Value,
            HillModule.WindPointsModule.tryCreate(src.TailwindPoints).Value
        );

    private async Task<List<Domain.GameWorld.Hill>> LoadAllAsync(CancellationToken ct)
    {
        var results = new List<Domain.GameWorld.Hill>();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        await foreach (var csvHill in csv.GetRecordsAsync<CsvHill>().WithCancellation(ct))
        {
            try
            {
                results.Add(Map(csvHill));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return results;
    }

    public async Task<IEnumerable<Domain.GameWorld.Hill>> GetAll(CancellationToken ct) =>
        await LoadAllAsync(ct);

    public async Task<FSharpOption<Domain.GameWorld.Hill>> GetByFormattedName(
        SearchFormattedName searchFormattedName, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var nameString = SearchFormattedNameModule.value(searchFormattedName);

        // np. "Zakopane HS140"
        var parts = nameString.Split(" HS", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var hs))
            return FSharpOption<Domain.GameWorld.Hill>.None;

        var name = parts[0].Trim();

        var found = all.FirstOrDefault(hill =>
            hill.Location.Item.Equals(name, StringComparison.OrdinalIgnoreCase)
            && HillModule.HsPointModule.value(hill.HsPoint) == hs);

        return found is null
            ? FSharpOption<Domain.GameWorld.Hill>.None
            : FSharpOption<Domain.GameWorld.Hill>.Some(found);
    }


    public async Task<FSharpOption<Domain.GameWorld.Hill>> GetById(HillId id, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var found = all.FirstOrDefault(j => j.Id.Equals(id));
        return found is null
            ? FSharpOption<Domain.GameWorld.Hill>.None
            : FSharpOption<Domain.GameWorld.Hill>.Some(found);
    }

    public async Task<IEnumerable<Domain.GameWorld.Hill>> GetByCountryFisCode(CountryFisCode countryFisCode, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        return all.Where(j => j.CountryCode.Equals(countryFisCode)).ToList();
    }
}