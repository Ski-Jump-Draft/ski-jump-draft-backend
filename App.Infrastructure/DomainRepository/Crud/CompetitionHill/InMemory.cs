using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionHill;

public class InMemory : InMemoryCrudDomainRepository<App.Domain.Competition.HillModule.Id,
    App.Domain.Competition.Hill>, ICompetitionHillRepository;