namespace App.Infrastructure.DomainRepository.EventSourced;

using Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Collections;

public sealed class SimpleCompetitionRepository(
    IEventStore<Domain.SimpleCompetition.CompetitionId, Domain.SimpleCompetition.Event.CompetitionEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    :
        DefaultEventSourcedRepository<
            Domain.SimpleCompetition.Competition,
            Domain.SimpleCompetition.CompetitionId,
            Domain.SimpleCompetition.Event.CompetitionEventPayload>(store,
            clock,
            guid,
            eventBus,
            domainEvents =>
                Task.FromResult(Domain.SimpleCompetition.Evolve.evolveFromEvents(ListModule.OfSeq(domainEvents))),
            p => Domain.SimpleCompetition.Event.Versioning.schemaVersion(p)),
        ICompetitionRepository;