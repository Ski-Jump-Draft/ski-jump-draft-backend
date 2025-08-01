using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using HillModule = App.Domain.PreDraft.Competitions.HillModule;

namespace App.Application.CompetitionEngine.Impl.CompetitionHill;

public class Default()
    : ICompetitionHillFactory
{
    public Task<Domain.Competition.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct)
    {
        var kPoint = Domain.Competition.HillModule.KPointModule
            .tryCreate(Domain.GameWorld.HillTypes.KPointModule.value(gameWorldHill.KPoint_)).Value;
        var hsPoint = Domain.Competition.HillModule.HsPointModule
            .tryCreate(Domain.GameWorld.HillTypes.HsPointModule.value(gameWorldHill.HsPoint_)).Value;

        return Task.FromResult(Domain.Competition.Hill.Create(kPoint, hsPoint));
        //
        // if (competitionHillMapping.TryMap(gameWorldHill.Id_, out var competitionHillId))
        // {
        //     return await competitionHills.GetByIdAsync(competitionHillId)
        //         .AwaitOrWrap(_ => new IdNotFoundException<Guid>(competitionHillId.Item));
        // }
        //
        // // if (!gameHillMapping.TryMapBackward(gameWorldHill.Id_, out var gameWorldHillId))
        // //     throw new KeyNotFoundException("No existing competition hill or no related game world hill");
        //
        // // var gameWorldHill = await gameWorldHills.GetByIdAsync(gameWorldHillId)
        // //     .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameWorldHillId.Item));
        // var newCompetitionHillId = Domain.Competition.HillModule.Id.NewId(guid.NewGuid());
        // var kPoint = Domain.Competition.HillModule.KPointModule
        //     .tryCreate(Domain.GameWorld.HillModule.KPointModule.value(gameWorldHill.KPoint_)).Value;
        // var hsPoint = Domain.Competition.HillModule.HsPointModule
        //     .tryCreate(Domain.GameWorld.HillModule.HsPointModule.value(gameWorldHill.HsPoint_)).Value;
        // var newCompetitionHill = new Domain.Competition.Hill(newCompetitionHillId, kPoint, hsPoint);
        // return newCompetitionHill;
    }

    public Task<Hill> CreateAsync(HillModule.Id preDraftCompetitionHillId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}