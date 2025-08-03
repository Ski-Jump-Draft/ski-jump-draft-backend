using App.Application.CompetitionEngine;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.PreDraft.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.PreDraftHill;

public class Default(
    IGuid guid,
    IPreDraftHillMapping preDraftHillMapping,
    IPreDraftHillRepository preDraftHills)
    : IPreDraftCompetitionHillFactory
{
    public async Task<Hill> CreateAsync(Domain.GameWorld.Hill gameHill, CancellationToken ct)
    {
        if (preDraftHillMapping.TryMap(gameHill.Id_, out var preDraftHillId))
        {
            return await preDraftHills.GetByIdAsync(preDraftHillId)
                .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameHill.Id_.Item));
        }

        var id = HillModule.Id.NewId(guid.NewGuid());

        preDraftHillMapping.Add(gameHill.Id_, id);

        return new Domain.PreDraft.Competition.Hill(id);
    }
}