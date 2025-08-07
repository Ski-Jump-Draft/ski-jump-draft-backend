using App.Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Collections;

namespace App.Infrastructure.DomainRepository.EventSourced;

public sealed class PreDraftRepository(
    IEventStore<Domain.PreDraft.Id.Id, Domain.PreDraft.Event.PreDraftEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    :
        DefaultEventSourcedRepository<
            Domain.PreDraft.PreDraft,
            Domain.PreDraft.Id.Id,
            Domain.PreDraft.Event.PreDraftEventPayload>(store,
            clock,
            guid,
            eventBus,
            domainEvents => Task.FromResult(Domain.PreDraft.Evolve.evolveFromEvents(ListModule.OfSeq(domainEvents))),
            p => Domain.PreDraft.Event.Versioning.schemaVersion(p)),
        IPreDraftRepository;