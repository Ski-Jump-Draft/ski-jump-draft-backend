using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

public class InMemory : InMemoryCrudDomainRepository<Domain.GameWorld.HillModule.Id,
    Domain.GameWorld.Hill>, IGameWorldHillRepository;