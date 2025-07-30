using App.Domain.Competition;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionHill;

public class InMemory(InMemoryCrudDomainRepositoryStarter<HillModule.Id, Hill>? starter = null)
    : InMemoryCrudDomainRepository<App.Domain.Competition.HillModule.Id,
        Hill>(starter), ICompetitionHillRepository;