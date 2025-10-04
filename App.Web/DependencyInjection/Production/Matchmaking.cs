using App.Domain.Matchmaking;

namespace App.Web.DependencyInjection.Production;

public static class Matchmaking
{
    public static IServiceCollection AddProductionMatchmaking(this IServiceCollection services, bool isMocked)
    {
        services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
            App.Domain.Matchmaking.Settings.Create(
                SettingsModule.Duration.NewDuration(TimeSpan.FromSeconds(60)),
                autoStartPolicy: SettingsModule.MatchmakingEndPolicy.AfterTimeout,
                App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(isMocked ? 2 : 5).Value,
                App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(isMocked ? 7 : 12).Value).ResultValue);
        return services;
    }
}