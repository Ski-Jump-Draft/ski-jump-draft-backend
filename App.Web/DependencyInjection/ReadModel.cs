using App.Application.Abstractions;
using App.Application.ReadModel.Projection;

namespace App.Web.DependencyInjection;

public static class ProjectionDependencyInjection
{
    public static IServiceCollection AddReadModel(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IServersProjection, Infrastructure.Projection.Hosting.Servers.Test>();

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
            .AddSingleton<IGameParticipantsProjection, Infrastructure.Projection.Game.Participants.InMemory>();
        services
            .AddSingleton<IEventHandler<Domain.Game.Event.GameEventPayload>,
                Infrastructure.Projection.Game.Participants.InMemory>();

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

        // --- Queries --- //
        services.AddSingleton<IGameWorldHillQuery, Infrastructure.Query.GameWorld.Hill.Predefined>();

        services.AddSingleton<IGameWorldJumperQuery, Infrastructure.Query.GameWorld.Jumper.CsvStorage>(sp =>
            new Infrastructure.Query.GameWorld.Jumper.CsvStorage(config["GameWorldJumpersCsv"]!));

        services.AddSingleton<IGameWorldCountryQuery, Infrastructure.Query.GameWorld.Country.Predefined>();

        return services;
    }
}