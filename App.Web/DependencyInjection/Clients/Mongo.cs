using MongoDB.Driver;

namespace App.Web.DependencyInjection.Clients;

public static class MongoDependencyInjection
{
    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Mongo")
                               ?? throw new InvalidOperationException("Mongo connection string missing");

        services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var dbName = config.GetSection("Mongo")["Database"] ?? "app"; // fallback
            return client.GetDatabase(dbName);
        });

        return services;
    }
}