using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionEngineSnapshot;

public class InMemory : InMemoryCrudDomainRepository<App.Domain.Competition.Engine.Id,
    App.Domain.Competition.Engine.EngineSnapshotBlob>, ICompetitionEngineSnapshotRepository;