using App.Util;

namespace App.Application.Factory;

public interface ICompetitionHillFactory
{
    Task<Domain.Competition.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct);

    Task<Domain.Competition.Hill> CreateAsync(Domain.PreDraft.Competition.HillModule.Id preDraftCompetitionHillId,
        CancellationToken ct);
}

public interface IPreDraftCompetitionHillFactory
{
    Task<Domain.PreDraft.Competition.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHillId, CancellationToken ct);
}

// public interface IGameHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Game.Hill.Id>;

public interface
    IPreDraftHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillTypes.Id,
    Domain.PreDraft.Competition.HillModule.Id>;
//
// public interface
//     ICompetitionHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Competition.HillModule.Id>;