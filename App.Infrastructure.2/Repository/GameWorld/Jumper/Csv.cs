using App.Application._2.Utility;
using CsvHelper;
using Microsoft.FSharp.Core;
using App.Domain._2.GameWorld;
using App.Infrastructure._2.Helper.Csv;
using App.Infrastructure._2.Helper.Csv.GameWorldCountryIdProvider;
using CsvHelper.Configuration;

namespace App.Infrastructure._2.Repository.GameWorld.Jumper;

public interface IGameWorldJumpersCsvStreamProvider : ICsvStreamProvider { }

public class Csv(
    IGameWorldJumpersCsvStreamProvider csvStreamProvider,
    IGameWorldCountryIdProvider countryIdProvider,
    IMyLogger logger,
    CsvConfiguration csvConfig) : IJumpers
{
    private class CsvJumper
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string CountryFisCode { get; set; } = null!;
        public double Takeoff { get; set; }
        public double Flight { get; set; }
        public int Landing { get; set; }
        public int LiveForm { get; set; }
    }

    private static Domain._2.GameWorld.Jumper Map(CsvJumper src, Guid countryId) =>
        new(
            JumperId.NewJumperId(src.Id),
            JumperModule.Name.NewName(src.Name),
            JumperModule.Surname.NewSurname(src.Surname),
            CountryId.NewCountryId(countryId),
            JumperModule.BigSkillModule.tryCreate(src.Takeoff).Value,
            JumperModule.BigSkillModule.tryCreate(src.Flight).Value,
            JumperModule.LandingSkillModule.tryCreate(src.Landing).Value,
            JumperModule.LiveFormModule.tryCreate(src.LiveForm).Value
        );

    private async Task<List<Domain._2.GameWorld.Jumper>> LoadAllAsync(CancellationToken ct)
    {
        var results = new List<Domain._2.GameWorld.Jumper>();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        await foreach (var csvJumper in csv.GetRecordsAsync<CsvJumper>().WithCancellation(ct))
        {
            try
            {
                var countryId = await countryIdProvider.GetFromFisCode(csvJumper.CountryFisCode, ct);
                results.Add(Map(csvJumper, countryId));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return results;
    }

    public async Task<IEnumerable<Domain._2.GameWorld.Jumper>> GetAll(CancellationToken ct) =>
        await LoadAllAsync(ct);

    public async Task<FSharpOption<Domain._2.GameWorld.Jumper>> GetById(JumperId id, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var found = all.FirstOrDefault(j => j.Id.Equals(id));
        return found is null
            ? FSharpOption<Domain._2.GameWorld.Jumper>.None
            : FSharpOption<Domain._2.GameWorld.Jumper>.Some(found);
    }

    public async Task<IEnumerable<Domain._2.GameWorld.Jumper>> GetByCountryId(CountryId countryId, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        return all.Where(j => j.CountryId.Equals(countryId)).ToList();
    }
}
