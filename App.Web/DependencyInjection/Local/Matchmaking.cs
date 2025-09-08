namespace App.Web.DependencyInjection.Local;

public static class Matchmaking
{
    public static IServiceCollection AddLocalMatchmaking(this IServiceCollection services)
    {
        services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
            App.Domain.Matchmaking.Settings.Create(
                App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(5).Value,
                App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(8).Value).ResultValue);
        return services;
    }
}