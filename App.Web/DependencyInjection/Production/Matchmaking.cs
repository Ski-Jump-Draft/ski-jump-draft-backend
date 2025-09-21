namespace App.Web.DependencyInjection.Production;

public static class Matchmaking
{
    public static IServiceCollection AddProductionMatchmaking(this IServiceCollection services)
    {
        services.AddSingleton<App.Domain.Matchmaking.Settings>(sp =>
            App.Domain.Matchmaking.Settings.Create(
                App.Domain.Matchmaking.SettingsModule.MinPlayersModule.create(2).Value,
                App.Domain.Matchmaking.SettingsModule.MaxPlayersModule.create(7).Value).ResultValue);
        return services;
    }
}