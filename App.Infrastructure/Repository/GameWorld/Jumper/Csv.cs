using App.Application.Extensions;
using App.Application.Utility;
using CsvHelper;
using Microsoft.FSharp.Core;
using App.Domain.GameWorld;
using App.Infrastructure.Helper.Csv;
using CsvHelper.Configuration;

namespace App.Infrastructure.Repository.GameWorld.Jumper;

public interface IGameWorldJumpersCsvStreamProvider : ICsvStreamProvider
{
}

public class Csv(
    IGameWorldJumpersCsvStreamProvider csvStreamProvider,
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

    private static Domain.GameWorld.Jumper Map(CsvJumper src) =>
        new(
            JumperId.NewJumperId(src.Id),
            JumperModule.Name.NewName(src.Name),
            JumperModule.Surname.NewSurname(src.Surname),
            CountryFisCodeModule.tryCreate(src.CountryFisCode)
                .OrThrow($"Invalid CountryFisCode format for a jumper {src.Name} {src.Surname}"),
            JumperModule.BigSkillModule.tryCreate(src.Takeoff).Value,
            JumperModule.BigSkillModule.tryCreate(src.Flight).Value,
            JumperModule.LandingSkillModule.tryCreate(src.Landing).Value,
            JumperModule.LiveFormModule.tryCreate(src.LiveForm).Value
        );

    private async Task<List<Domain.GameWorld.Jumper>> LoadAllAsync(CancellationToken ct)
    {
        var results = new List<Domain.GameWorld.Jumper>();

        await using var stream = await csvStreamProvider.Open(ct);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        await foreach (var csvJumper in csv.GetRecordsAsync<CsvJumper>().WithCancellation(ct))
        {
            try
            {
                results.Add(Map(csvJumper));
            }
            catch (Exception ex)
            {
                logger.Error($"bad row: {ex.Message}");
            }
        }

        return results;
    }
    //
    // App[0]
    // Scheduler job SimulateJumpInGame failed: The given key was not present in the dictionary.
    //     fail: App[0]
    // Stack trace:    at App.Application.Extensions.FSharpAsyncExt.AwaitOrWrap[T](Task`1 task, Func`2 wrap) in /home/konrad/programming-projects/real_apps/sj_draft/Game/App.Application/Extensions/FSharpAsyncExt.cs:line 71
    // at App.Application.UseCase.Game.SimulateJump.Handler.HandleAsync(Command command, CancellationToken ct) in /home/konrad/programming-projects/real_apps/sj_draft/Game/App.Application/UseCase/Game/SimulateJump/Handler.cs:line 69
    // at App.Infrastructure.Commanding.Scheduler.InMemory.HandleSimulateJumpInGame(String payloadJson, CancellationToken ct) in /home/konrad/programming-projects/real_apps/sj_draft/Game/App.Infrastructure/Commanding/Scheduler/InMemory.cs:line 122
    // at App.Infrastructure.Commanding.Scheduler.InMemory.<>c__DisplayClass5_0.<<ScheduleAsync>b__0>d.MoveNext() in /home/konrad/programming-projects/real_apps/sj_draft/Game/App.Infrastructure/Commanding/Scheduler/InMemory.cs:line 45
    //

    public async Task<IEnumerable<Domain.GameWorld.Jumper>> GetAll(CancellationToken ct) =>
        await LoadAllAsync(ct);

    public async Task<IEnumerable<Domain.GameWorld.Jumper>> GetFromIds(IEnumerable<JumperId> ids, CancellationToken ct)
    {
        var idSet = new HashSet<JumperId>(ids);
        var all = await LoadAllAsync(ct);
        return all.Where(j => idSet.Contains(j.Id)).ToList();
    }

    public async Task<FSharpOption<Domain.GameWorld.Jumper>> GetById(JumperId id, CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        var found = all.FirstOrDefault(j => j.Id.Equals(id));
        return found is null
            ? FSharpOption<Domain.GameWorld.Jumper>.None
            : FSharpOption<Domain.GameWorld.Jumper>.Some(found);
    }

    public async Task<IEnumerable<Domain.GameWorld.Jumper>> GetByCountryFisCode(CountryFisCode countryFisCode,
        CancellationToken ct)
    {
        var all = await LoadAllAsync(ct);
        return all.Where(j => j.FisCountryCode.Equals(countryFisCode)).ToList();
    }
}