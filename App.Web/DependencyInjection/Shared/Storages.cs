using Environment = App.Infrastructure.Storage.PremiumMatchmakingConfigs.Environment;

namespace App.Web.DependencyInjection.Shared;

public static class Storages
{
    public static IServiceCollection AddStorages(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormStorage,
                App.Infrastructure.Storage.JumperGameForm.InMemory>();
        services
            .AddSingleton<App.Application.Matchmaking.IMatchmakingUpdatedDtoStorage,
                App.Infrastructure.Storage.MatchmakingUpdated.InMemory>();


        services
            .AddSingleton<App.Application.Matchmaking.IPremiumMatchmakingConfigurationStorage,
                App.Infrastructure.Storage.PremiumMatchmakingConfigs.Environment>(sp =>
            {
                var premiumPasswordsString = configuration["PREMIUM_PASSWORDS"];
                return new Environment(premiumPasswordsString);
            });
        services
            .AddSingleton<App.Application.Matchmaking.IPremiumMatchmakingGames,
                App.Infrastructure.PremiumMatchmakings.InMemory>();
        return services;
    }
}