using App.Application.Abstractions;
using App.Domain.Repositories;

namespace App.Web.DependencyInjection;

public static class DomainRepositoriesDependencyInjection
{
    public static IServiceCollection AddCrudRepositories(
        this IServiceCollection services,
        IConfiguration config)
    {
        // CRUD
        services
            .AddSingleton<ICompetitionEngineSnapshotRepository,
                Infrastructure.DomainRepository.Crud.CompetitionEngineSnapshot.InMemory>();
        services
            .AddSingleton<ICompetitionEnginePluginRepository,
                Infrastructure.ApplicationRepository.CompetitionEnginePlugin.InMemory>();
        services
            .AddSingleton<IGameParticipantRepository, Infrastructure.DomainRepository.Crud.GameParticipant.InMemory>();
        services
            .AddSingleton<IMatchmakingParticipantRepository,
                Infrastructure.DomainRepository.Crud.MatchmakingParticipant.InMemory>();

        services.AddSingleton<IGameWorldHillRepository, Infrastructure.DomainRepository.Crud.GameWorldHill.InMemory>();
        services.AddSingleton<IGameHillRepository, Infrastructure.DomainRepository.Crud.GameHill.InMemory>();

        // Event-Sourced
        services
            .AddSingleton<IMatchmakingRepository, Infrastructure.DomainRepository.EventSourced.MatchmakingRepository>();
        services
            .AddSingleton<IGameRepository, Infrastructure.DomainRepository.EventSourced.GameRepository>();
        services.AddSingleton<IDraftRepository, Infrastructure.DomainRepository.EventSourced.DraftRepository>();

        return services;
    }
}