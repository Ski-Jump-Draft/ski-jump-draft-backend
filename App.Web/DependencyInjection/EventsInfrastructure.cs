using App.Application.Commanding;

namespace App.Web.DependencyInjection;

public static class EventsInfrastructureDependencyInjection
{
    public static IServiceCollection AddEventsInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, Infrastructure.EventBus.InMemory>();

        services
            .AddSingleton<IEventStore<Domain.Game.Id.Id, Domain.Game.Event.GameEventPayload>, Infrastructure.EventStore.
                InMemoryEventStore<Domain.Game.Id.Id, Domain.Game.Event.GameEventPayload>>();
        services
            .AddSingleton<IEventStore<Domain.Draft.Id.Id, Domain.Draft.Event.DraftEventPayload>, Infrastructure.
                EventStore.
                InMemoryEventStore<Domain.Draft.Id.Id, Domain.Draft.Event.DraftEventPayload>>();
        services
            .AddSingleton<IEventStore<Domain.Matchmaking.Id, Domain.Matchmaking.Event.MatchmakingEventPayload>,
                Infrastructure.EventStore.
                InMemoryEventStore<Domain.Matchmaking.Id, Domain.Matchmaking.Event.MatchmakingEventPayload>>();

        return services;
    }
}