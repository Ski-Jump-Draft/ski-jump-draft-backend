using App.Application.Commanding;
using App.Web.Sse;
using App.Web.Sse.Notifier;

namespace App.Web.DependencyInjection;

public static class SseInfrastructureDependencyInjection1
{
    public static IServiceCollection AddSseInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<ISseStream, InMemorySseStream>();
        services
            .AddScoped<IEventHandler<Domain.Matchmaking.Event.MatchmakingEventPayload>,
                MatchmakingNotifierByDomainEvents>();

        return services;
    }
}