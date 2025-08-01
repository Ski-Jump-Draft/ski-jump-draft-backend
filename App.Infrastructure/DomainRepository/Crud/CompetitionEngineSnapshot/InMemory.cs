using App.Domain.Competition;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionEngineSnapshot;

public class InMemory(InMemoryCrudDomainRepositoryStarter<Engine.Id, Engine.Snapshot>? starter = null)
    : InMemoryCrudDomainRepository<App.Domain.Competition.Engine.Id,
        Engine.Snapshot>(starter), ICompetitionEngineSnapshotRepository;