using App.Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using static Microsoft.FSharp.Collections.ListModule;

namespace App.Infrastructure.DomainRepository.EventSourced;

public sealed class DraftRepository(
    IEventStore<App.Domain.Draft.Id.Id, App.Domain.Draft.Event.DraftEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    :
        DefaultEventSourcedRepository<
            App.Domain.Draft.Draft,
            App.Domain.Draft.Id.Id,
            App.Domain.Draft.Event.DraftEventPayload>(store,
            clock,
            guid,
            eventBus,
            events => Task.FromResult(Domain.Draft.Evolve.evolveFromEvents(OfSeq(events))),
            p => App.Domain.Draft.Event.Versioning.schemaVersion(p)),
        IDraftRepository;