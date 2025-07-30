using App.Domain.Competition;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionEngineSnapshot;

public class InMemory(InMemoryCrudDomainRepositoryStarter<Engine.Id, Engine.EngineSnapshotBlob>? starter = null)
    : InMemoryCrudDomainRepository<App.Domain.Competition.Engine.Id,
        Engine.EngineSnapshotBlob>(starter), ICompetitionEngineSnapshotRepository;