using App.Application.Projection;
using App.Domain.Repositories;
using App.Infrastructure.Persistence.Game;
using App.Infrastructure.Projection.Game;
using MongoDB.Driver;

namespace App.Web;

public static class CrudDependencyInjection
{
    public static IServiceCollection AddCrudRepositories(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddSingleton<IMongoClient>(_ => new MongoClient(config["Mongo:ConnectionString"]));

        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(config["Mongo:ConnectionString"]));
        services.AddScoped(sp =>
            sp.GetRequiredService<IMongoClient>()
                .GetDatabase(config["Mongo:DatabaseName"]));
        services.AddScoped<IGameParticipantRepository, 
            Infrastructure.Repository.Crud.GameParticipant.Mongo>();
        
        services.AddScoped<IGamesProjection, SqlGamesProjection>();

        return services;
    }
}
