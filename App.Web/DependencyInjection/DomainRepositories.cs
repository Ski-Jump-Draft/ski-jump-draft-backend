using App.Application.Abstractions;
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
        services.AddSingleton(
            new InMemoryCrudDomainRepositoryStarter<Domain.GameWorld.HillTypes.Id, Domain.GameWorld.Hill>(
                StarterItems:
                Infrastructure.Temporaries.GameWorld.ConstructHills(),
                MapToId: hill => hill.Id_
            ));

        services
            .AddSingleton<IGameWorldHillRepository, Infrastructure.DomainRepository.Crud.GameWorldHill.Predefined>();
        services
            .AddSingleton<IGameWorldJumperRepository,
                Infrastructure.DomainRepository.Crud.GameWorldJumper.CsvStorage>(sp =>
                new Infrastructure.DomainRepository.Crud.GameWorldJumper.CsvStorage(
                    config["GameWorldJumpersCsv"]!));

        // Event-Sourced
        services
            .AddSingleton<IMatchmakingRepository, Infrastructure.DomainRepository.EventSourced.MatchmakingRepository>();
        services
            .AddSingleton<IGameRepository, Infrastructure.DomainRepository.EventSourced.GameRepository>();
        services.AddSingleton<IPreDraftRepository, Infrastructure.DomainRepository.EventSourced.PreDraftRepository>();
        services.AddSingleton<IDraftRepository, Infrastructure.DomainRepository.EventSourced.DraftRepository>();
        services
            .AddSingleton<ICompetitionRepository,
                Infrastructure.DomainRepository.EventSourced.SimpleCompetitionRepository>();

        return services;
    }
}