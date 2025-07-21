using App.Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using static Microsoft.FSharp.Collections.ListModule;

namespace App.Infrastructure.DomainRepository.EventSourced;

public sealed class GameRepository(
    IEventStore<App.Domain.Game.Id.Id, Domain.Game.Event.GameEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    :
        DefaultEventSourcedRepository<
            Domain.Game.Game,
            Domain.Game.Id.Id,
            Domain.Game.Event.GameEventPayload>(store,
            clock,
            guid,
            eventBus,
            evts => Domain.Game.Evolve.evolveFromEvents(OfSeq(evts)),
            p => Domain.Game.Event.Versioning.schemaVersion(p),
            agg => agg.Id_),
        IGameRepository;