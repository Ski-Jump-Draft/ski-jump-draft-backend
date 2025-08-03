using App.Domain.PreDraft.Competition;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.PreDraftHill;

public class InMemory(InMemoryCrudDomainRepositoryStarter<HillModule.Id, Hill>? starter = null)
    : InMemoryCrudDomainRepository<HillModule.Id,
        Hill>(starter), IPreDraftHillRepository;