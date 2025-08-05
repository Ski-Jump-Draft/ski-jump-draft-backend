namespace App.Web.DependencyInjection;

public static class PredefinedCollections
{
    public static IServiceCollection AddPredefinedCollections(this IServiceCollection services)
    {
        services.AddSingleton<IReadOnlyCollection<Domain.GameWorld.Country>>(sp =>
        {
            var preloadedCountries = Infrastructure.Temporaries.GameWorld.ConstructCountries();
            return preloadedCountries;
        });
        
        services.AddSingleton<IReadOnlyCollection<Domain.GameWorld.Hill>>(sp =>
        {
            var preloadedHills = Infrastructure.Temporaries.GameWorld.ConstructHills();
            return preloadedHills;
        });

        return services;
    }
}