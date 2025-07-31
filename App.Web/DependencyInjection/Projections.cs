using App.Application.Abstractions;
using App.Application.Projection;
using App.Application.ReadModel.Projection;

namespace App.Web.DependencyInjection;

public static class ProjectionDependencyInjection
{
    public static IServiceCollection AddProjections(this IServiceCollection services)
    {
        services.AddSingleton<IServersProjection, Infrastructure.Projection.Hosting.Servers.Test>();

        services.AddSingleton<IGameWorldHillProjection, Infrastructure.Projection.GameWorld.Hill.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.GameWorld.Event.HillEventPayload>,
                Infrastructure.Projection.GameWorld.Hill.InMemory>();

        services.AddSingleton<IActiveGamesProjection, Infrastructure.Projection.Game.ActiveGames.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.Game.Event.GameEventPayload>,
                Infrastructure.Projection.Game.ActiveGames.InMemory>();

        services
            .AddSingleton<IActiveMatchmakingsProjection,
                Infrastructure.Projection.Matchmaking.ActiveMatchmakings.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.Matchmaking.Event.MatchmakingEventPayload>,
                Infrastructure.Projection.Matchmaking.ActiveMatchmakings.InMemory>();

        services
            .AddSingleton<IMatchmakingParticipantsProjection,
                Infrastructure.Projection.Matchmaking.MatchmakingParticipants.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.Matchmaking.Event.MatchmakingEventPayload>,
                Infrastructure.Projection.Matchmaking.MatchmakingParticipants.InMemory>();

        services.AddSingleton<IGameByDraftProjection, Infrastructure.Projection.Game.GameByDraft.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.Game.Event.GameEventPayload.DraftPhaseStartedV1>,
                Infrastructure.Projection.Game.GameByDraft.InMemory>();

        return services;
    }
}