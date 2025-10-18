namespace App.Web.DependencyInjection.Shared;

public static class Storages
{
    public static IServiceCollection AddStorages(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormStorage,
                App.Infrastructure.Storage.JumperGameForm.InMemory>();
        services.AddSingleton<App.Application.Matchmaking.IMatchmakingUpdatedDtoStorage, App.Infrastructure.Storage.MatchmakingUpdated.InMemory>();
        services
            .AddSingleton<App.Application.Matchmaking.IPremiumMatchmakingConfigurationStorage,
                App.Infrastructure.Storage.PremiumMatchmakingConfigs.Hardcoded>();
        services.AddSingleton<App.Application.Matchmaking.IPremiumMatchmakingGames, App.Infrastructure.PremiumMatchmakings.InMemory>();
        return services;
    }
}
