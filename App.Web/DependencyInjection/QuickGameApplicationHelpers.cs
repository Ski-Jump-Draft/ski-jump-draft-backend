using App.Application.UseCase.Helper;
using App.Application.UseCase.Helper.Impl.QuickGameHillSelector;
using App.Infrastructure.Globals;

namespace App.Web.DependencyInjection;

public static class QuickGameApplicationHelpers
{
    public static IServiceCollection AddQuickGameApplicationHelpers(this IServiceCollection services)
    {
        services.AddSingleton<IQuickGameServerProvider, OnlyQuickGameServerProvider>();

        services.AddSingleton<IQuickGameHillSelector, RandomQuickGameHillSelector>();
        services.AddSingleton<IQuickGameSettingsProvider, DefaultQuickGameSettingsProvider>();
        services.AddSingleton<IQuickGameMatchmakingSettingsProvider, DefaultQuickGameMatchmakingSettingsProvider>();
        
        return services;
    }
}