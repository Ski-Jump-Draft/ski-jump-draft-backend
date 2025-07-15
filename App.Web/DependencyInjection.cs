using App.Application.Abstractions;
using App.Application.Projection;
using App.Domain.Repositories;
using App.Domain.Repository;
using App.Infrastructure.Persistence.Game;
using App.Infrastructure.Repository.Crud.GameParticipant;
using App.Infrastructure.Repository.EventSourced;
using App.Web.Hub;

namespace App.Web;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<GameDbContext >(opt => { /* conn string z cfg */ });
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IGameParticipantRepository, InMemory>();
        // tu też: settings, mapStores itd.
        return services;
    }
}

// App.Application/DependencyInjection.cs
public static class ApplicationServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // rejestruj command‑handlery:
        // services.Scan(scan => scan
        //     .FromAssembliesOf(typeof(FindOrCreateCommand))
        //     .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
        //     .AsImplementedInterfaces()
        //     .WithScopedLifetime()
        //     .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
        //     .AsImplementedInterfaces()
        //     .WithScopedLifetime()
        // );
        
        services.AddSingleton<MatchmakingNotifier>();
        
        return services;
    }
}
