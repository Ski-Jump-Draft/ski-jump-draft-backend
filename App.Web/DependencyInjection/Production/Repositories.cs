using App.Application.Utility;
using StackExchange.Redis;

namespace App.Web.DependencyInjection.Production;

public static class Repositories
{
    public static IServiceCollection AddProductionRepositories(this IServiceCollection services,
        IConfiguration configuration, bool isMocked)
    {
        if (isMocked)
        {
            services
                .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                    App.Infrastructure.Repository.Matchmaking.InMemory>();
            services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<IMyLogger>();
                var redisConnectionString = configuration["Redis:ConnectionString"]
                                            ?? throw new InvalidOperationException(
                                                "Redis connection string not configured");
                // Console.WriteLine("Redis connection string: " + redisConnectionString);

                // logger.Info("Redis connection string: " + redisConnectionString);

                var uri = new Uri(redisConnectionString);
                var options = new ConfigurationOptions
                {
                    EndPoints = { { uri.Host, uri.Port } },
                    // User = uri.UserInfo.Split(':')[0],
                    // Password = uri.UserInfo.Split(':')[1],
                    Ssl = false,
                    AbortOnConnectFail = false,
                    ClientName = "ski-jump-draft-backend",
                    ConnectTimeout = 3000,
                    SyncTimeout = 3000,
                    KeepAlive = 15,
                    ConnectRetry = 3,
                    ReconnectRetryPolicy = new ExponentialRetry(1000)
                };
                
                var connectionMultiplexer = ConnectionMultiplexer.Connect(options);
                
                connectionMultiplexer.ConnectionFailed += (_, e) => logger.Error("Redis connection failed: " + e.Exception?.Message);
                connectionMultiplexer.ConnectionRestored += (_, e) => logger.Info("Redis connection restored: " + e.EndPoint);
                connectionMultiplexer.ErrorMessage += (_, e) => logger.Warn("Redis error: " + e.Message);
                connectionMultiplexer.InternalError += (_, e) => logger.Error("Redis internal error: " + e.Exception?.Message);
                
                logger.Info("Redis connected: " + string.Join(", ", connectionMultiplexer.GetEndPoints().Select(e => e.ToString())));
                
                return connectionMultiplexer;
            });
            services
                .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                    App.Infrastructure.Repository.Matchmaking.Redis>();
            services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.Redis>();
        }


        return services;
    }
}