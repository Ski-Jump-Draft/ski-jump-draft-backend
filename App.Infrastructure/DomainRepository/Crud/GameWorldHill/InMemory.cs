using App.Domain.GameWorld;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

public sealed class InMemory(
    InMemoryCrudDomainRepositoryStarter<HillId, Hill>? starter
) : InMemoryCrudDomainRepository<HillId, Hill>(starter), IGameWorldHillRepository;