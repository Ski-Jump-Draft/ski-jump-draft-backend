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
                logger.Info("Redis connection string: " + redisConnectionString);

                var uri = new Uri(redisConnectionString);
                var options = new ConfigurationOptions
                {
                    EndPoints = { { uri.Host, uri.Port } },
                    User = uri.UserInfo.Split(':')[0],
                    Password = uri.UserInfo.Split(':')[1],
                    Ssl = true,
                    AbortOnConnectFail = false,
                    ConnectTimeout = 10000,
                    SyncTimeout = 10000,
                    KeepAlive = 60
                };
                return ConnectionMultiplexer.Connect(options);
            });
            services
                .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                    App.Infrastructure.Repository.Matchmaking.Redis>();
            services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.Redis>();
        }


        return services;
    }
}