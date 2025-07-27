using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using HillModule = App.Domain.PreDraft.Competitions.HillModule;

namespace App.Application.Factory.Impl.CompetitionHill;

public class Default(
    IGuid guid,
    IGameWorldHillRepository gameWorldHills,
    ICompetitionHillMapping competitionHillMapping,
    ICompetitionHillRepository competitionHills)
    : ICompetitionHillFactory
{
    public async Task<Domain.Competition.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct)
    {
        if (competitionHillMapping.TryMap(gameWorldHill.Id_, out var competitionHillId))
        {
            return await competitionHills.GetByIdAsync(competitionHillId)
                .AwaitOrWrap(_ => new IdNotFoundException<Guid>(competitionHillId.Item));
        }

        // if (!gameHillMapping.TryMapBackward(gameWorldHill.Id_, out var gameWorldHillId))
        //     throw new KeyNotFoundException("No existing competition hill or no related game world hill");

        // var gameWorldHill = await gameWorldHills.GetByIdAsync(gameWorldHillId)
        //     .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameWorldHillId.Item));
        var newCompetitionHillId = Domain.Competition.HillModule.Id.NewId(guid.NewGuid());
        var kPoint = Domain.Competition.HillModule.KPointModule
            .tryCreate(Domain.GameWorld.HillModule.KPointModule.value(gameWorldHill.KPoint_)).Value;
        var hsPoint = Domain.Competition.HillModule.HSPointModule
            .tryCreate(Domain.GameWorld.HillModule.HSPointModule.value(gameWorldHill.HSPoint_)).Value;
        var newCompetitionHill = new Domain.Competition.Hill(newCompetitionHillId, kPoint, hsPoint);
        return newCompetitionHill;
    }

    public Task<Hill> CreateAsync(HillModule.Id preDraftCompetitionHillId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}