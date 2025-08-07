using App.Application.Abstractions;
using App.Domain.GameWorld;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

public sealed class InMemory(
    IEventBus eventBus,
    IGuid guid,
    IClock clock,
    IEnumerable<Hill>? starter,
    Func<Hill, HillTypes.Id>? mapToId) : InMemoryCrudDomainEventsRepository<Hill, HillTypes.Id, Event.HillEventPayload>(eventBus, guid,
    payload => Event.Versioning.schemaVersion(payload), clock,
    starter, mapToId), IGameWorldHillRepository;