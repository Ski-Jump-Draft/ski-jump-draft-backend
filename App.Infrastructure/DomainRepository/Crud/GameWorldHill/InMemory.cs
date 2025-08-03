using App.Domain.GameWorld;
using App.Domain.Repositories;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

public sealed class InMemory(
    InMemoryCrudDomainRepositoryStarter<HillTypes.Id, Hill>? starter
) : InMemoryCrudDomainRepository<HillTypes.Id, Hill>(starter), IGameWorldHillRepository;