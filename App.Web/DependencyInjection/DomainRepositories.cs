using App.Application.Commanding;
using App.Domain.Repositories;
using App.Infrastructure.DomainRepository.Crud;

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
        // services
        //     .AddSingleton<ICompetitionEnginePluginRepository,
        //         Infrastructure.ApplicationRepository.CompetitionEnginePlugin.InMemory>();

        services.AddSingleton(
            new InMemoryCrudDomainRepositoryStarter<Domain.GameWorld.HillTypes.Id, Domain.GameWorld.Hill>(
                StarterItems:
                Infrastructure.Temporaries.GameWorld.ConstructHills(),
                MapToId: hill => hill.Id_
            ));

        services.AddSingleton<IGameWorldHillRepository, Infrastructure.DomainRepository.Crud.GameWorldHill.InMemory>();
        services.AddSingleton<IPreDraftHillRepository, Infrastructure.DomainRepository.Crud.PreDraftHill.InMemory>();

        // Event-Sourced
        services
            .AddSingleton<IMatchmakingRepository, Infrastructure.DomainRepository.EventSourced.MatchmakingRepository>();
        services
            .AddSingleton<IGameRepository, Infrastructure.DomainRepository.EventSourced.GameRepository>();
        services.AddSingleton<IDraftRepository, Infrastructure.DomainRepository.EventSourced.DraftRepository>();

        return services;
    }
}