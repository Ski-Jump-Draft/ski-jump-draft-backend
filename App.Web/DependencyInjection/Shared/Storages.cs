namespace App.Web.DependencyInjection.Shared;

public static class Storages
{
    public static IServiceCollection AddStorages(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormStorage,
                App.Infrastructure.Storage.JumperGameForm.InMemory>();
        return services;
    }
}
