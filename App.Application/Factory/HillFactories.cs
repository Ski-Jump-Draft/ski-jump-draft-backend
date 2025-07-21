using App.Application.Abstractions;

namespace App.Application.Factory;

public interface IGameHillFactory
{
    Task<Domain.Game.Hill.Hill> CreateAsync(Domain.GameWorld.Hill gameWorldHill, CancellationToken ct);
}

public interface ICompetitionHillFactory
{
    Task<Domain.Competition.Hill> CreateAsync(Domain.Game.Hill.Hill gameHill, CancellationToken ct);
}

public interface IGameHillMapping : IBiDirectionalIdMap<Domain.GameWorld.HillModule.Id, Domain.Game.Hill.Id>;

public interface
    ICompetitionHillMapping : IBiDirectionalIdMap<Domain.Game.Hill.Id, Domain.Competition.HillModule.Id>;