namespace App.Web.DependencyInjection.Shared;

public static class Storages
{
    public static IServiceCollection AddStorages(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormStorage,
                App.Infrastructure.Storage.JumperGameForm.InMemory>();
        services.AddSingleton<App.Application.Matchmaking.IMatchmakingUpdatedDtoStorage, App.Infrastructure.Storage.MatchmakingUpdated.InMemory>();
        return services;
    }
}
