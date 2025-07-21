using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.CompetitionHillFactory;

public class DefaultCompetitionHillFactory(
    IGuid guid,
    IGameWorldHillRepository gameWorldHills,
    IGameHillMapping gameHillMapping,
    ICompetitionHillMapping competitionHillMapping,
    ICompetitionHillRepository competitionHills)
    : ICompetitionHillFactory
{
    public async Task<Domain.Competition.Hill> CreateAsync(Domain.Game.Hill.Hill gameHill, CancellationToken ct)
    {
        if (competitionHillMapping.TryMap(gameHill.Id, out var competitionHillId))
        {
            return await FSharpAsyncExt.AwaitOrThrow(
                competitionHills.GetByIdAsync(competitionHillId, ct),
                new IdNotFoundException<Guid>(competitionHillId.Item), ct);
        }

        if (!gameHillMapping.TryMapBackward(gameHill.Id, out var gameWorldHillId))
            throw new KeyNotFoundException("No existing competition hill or no related game world hill");

        var gameWorldHill = await FSharpAsyncExt.AwaitOrThrow(
            gameWorldHills.GetByIdAsync(gameWorldHillId, ct),
            new IdNotFoundException<Guid>(gameWorldHillId.Item), ct);
        var newCompetitionHillId = Domain.Competition.HillModule.Id.NewId(guid.NewGuid());
        var kPoint = Domain.Competition.HillModule.KPointModule
            .tryCreate(Domain.GameWorld.HillModule.KPointModule.value(gameWorldHill.KPoint)).Value;
        var hsPoint = Domain.Competition.HillModule.HSPointModule
            .tryCreate(Domain.GameWorld.HillModule.HSPointModule.value(gameWorldHill.HSPoint)).Value;
        var newCompetitionHill = new Domain.Competition.Hill(newCompetitionHillId, kPoint, hsPoint);
        return newCompetitionHill;
    }
}