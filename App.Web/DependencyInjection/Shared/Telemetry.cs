using App.Application.Telemetry;
using App.Infrastructure.Telemetry;
using StackExchange.Redis;

namespace App.Web.DependencyInjection.Shared;

public static class Telemetry
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            // services.AddSingleton<ITelemetry, InMemoryTelemetry>();
            // services.AddSingleton<ITelemetry, NullTelemetry>();
            services.AddSingleton<ITelemetry>(new FileTelemetry("telemetry.ndjson"));
        }
        else
        {
            var redisConnectionString = configuration["Redis:ConnectionString"];
            if (redisConnectionString is null)
                throw new InvalidOperationException("Redis connection string not configured");
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton<ITelemetry>(new RedisTelemetry(connectionMultiplexer));
        }

        return services;
    }
}