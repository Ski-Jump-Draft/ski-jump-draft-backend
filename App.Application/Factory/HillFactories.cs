using App.Util;

namespace App.Application.Factory;

// public interface IGameHillFactory
// {
//     Task<Domain.Game.Hill.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct);
// }
//
// public interface ICompetitionHillFactory
// {
//     Task<Domain.Competition.Hill> CreateAsync(Domain.Game.Hill.Hill gameHill, CancellationToken ct);
// }

public interface ICompetitionHillFactory
{
    Task<Domain.Competition.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct);

    Task<Domain.Competition.Hill> CreateAsync(Domain.PreDraft.Competitions.HillModule.Id preDraftCompetitionHillId,
        CancellationToken ct);
}

public interface IPreDraftCompetitionHillFactory
{
    Task<Domain.PreDraft.Competitions.Hill> CreateAsync(Domain.GameWorld.HillId gameWorldHillId, CancellationToken ct);
}

// public interface IGameHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Game.Hill.Id>;

public interface
    IPreDraftHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.PreDraft.Competitions.HillModule.Id>;

public interface
    ICompetitionHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Competition.HillModule.Id>;

// public interface IPreDraftHillFactory
// {
//     Task<Domain.PreDraft.Competitions.Hill> CreateAsync(Domain.Game.Hill.Id gameHillId, CancellationToken ct);
// }
//
// public interface IGameHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillId, Domain.Game.Hill.Id>;
//
// public interface
//     IPreDraftHillMapping : IBiDirectionalIdMap<Domain.Game.Hill.Id, Domain.PreDraft.Competitions.HillModule.Id>;
//
// public interface
//     ICompetitionHillMapping : IBiDirectionalIdMap<Domain.Game.Hill.Id, Domain.Competition.HillModule.Id>;