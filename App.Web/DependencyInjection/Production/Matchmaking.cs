using App.Domain.Matchmaking;

namespace App.Web.DependencyInjection.Production;

public static class Matchmaking
{
    public static IServiceCollection AddProductionMatchmaking(this IServiceCollection services, bool isMocked)
    {
        if (isMocked)
        {
            services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
                App.Domain.Matchmaking.Settings.Create(
                    SettingsModule.Duration.NewDuration(TimeSpan.FromSeconds(60)),
                    // autoStartPolicy: SettingsModule.MatchmakingEndPolicy.NewAfterNoUpdate(
                    //     TimeSpan.FromSeconds(10)),
                    autoStartPolicy: SettingsModule.MatchmakingEndPolicy.NewAfterReachingMaxPlayers(
                        TimeSpan.FromSeconds(12)),
                    App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(3).Value,
                    App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(6).Value).ResultValue);
        }
        else
        {
            services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
                App.Domain.Matchmaking.Settings.Create(
                    SettingsModule.Duration.NewDuration(TimeSpan.FromSeconds(60)),
                    autoStartPolicy: SettingsModule.MatchmakingEndPolicy.NewAfterNoUpdate(
                        TimeSpan.FromSeconds(10)),
                    App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(3).Value,
                    App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(12).Value).ResultValue);
        }

        return services;
    }
}