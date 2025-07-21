using StackExchange.Redis;

namespace App.Web.DependencyInjection.Clients;

public static class RedisDependencyInjection
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Redis")
                               ?? throw new InvalidOperationException("Redis connection string missing");

        var multiplexer = ConnectionMultiplexer.Connect(connectionString);

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddSingleton(_ => multiplexer.GetDatabase());

        return services;
    }
}