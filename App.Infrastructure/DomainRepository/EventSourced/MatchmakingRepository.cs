using App.Application.Commanding;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Collections;

namespace App.Infrastructure.DomainRepository.EventSourced;

public sealed class MatchmakingRepository(
    IEventStore<Domain.Matchmaking.Id, Domain.Matchmaking.Event.MatchmakingEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    :
        DefaultEventSourcedRepository<
            Domain.Matchmaking.Matchmaking,
            Domain.Matchmaking.Id,
            Domain.Matchmaking.Event.MatchmakingEventPayload>(store,
            clock,
            guid,
            eventBus,
            evts => Domain.Matchmaking.Evolve.evolveFromEvents(ListModule.OfSeq(evts)),
            p => Domain.Matchmaking.Event.Versioning.schemaVersion(p),
            agg => agg.Id_),
        IMatchmakingRepository;