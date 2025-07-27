using App.Util;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.PreDraft.Competitions;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.PreDraftHill;

public class Default(
    IGuid guid,
    IPreDraftHillMapping preDraftHillMapping,
    IPreDraftHillRepository preDraftHills)
    : IPreDraftCompetitionHillFactory
{
    public async Task<Hill> CreateAsync(Domain.GameWorld.HillId gameHillId, CancellationToken ct)
    {
        if (preDraftHillMapping.TryMap(gameHillId, out var preDraftHillId))
        {
            return await preDraftHills.GetByIdAsync(preDraftHillId)
                .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameHillId.Item));
        }

        var id = guid.NewGuid();

        return new Domain.PreDraft.Competitions.Hill(Domain.PreDraft.Competitions.HillModule.Id.NewId(id));
    }
}