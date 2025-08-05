using App.Application.Abstractions;
using App.Domain.GameWorld;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

public sealed class InMemory(
    IEventBus eventBus,
    IGuid guid,
    Func<Event.HillEventPayload, int> schemaVersion,
    IClock clock,
    InMemoryCrudDomainEventsRepositoryStarter<HillTypes.Id, Hill>? starter
) : InMemoryCrudDomainEventsRepository<Hill, HillTypes.Id, Event.HillEventPayload>(eventBus, guid, schemaVersion, clock,
    starter), IGameWorldHillRepository;