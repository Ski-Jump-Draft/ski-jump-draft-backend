using System.Globalization;
using CsvHelper.Configuration;

namespace App.Web.DependencyInjection.Shared;

public static class Repositories
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                App.Infrastructure.Repository.Matchmaking.InMemory>();
        services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();
        services
            .AddSingleton<App.Domain.GameWorld.ICountries, App.Infrastructure.Repository.GameWorld.Country.Csv>();
        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Country.IGameWorldCountriesCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Countries:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });

        services
            .AddSingleton<App.Domain.GameWorld.IJumpers, App.Infrastructure.Repository.GameWorld.Jumper.Csv>();

        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Jumper.IGameWorldJumpersCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Jumpers:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });
        services
            .AddSingleton<App.Domain.GameWorld.IHills, App.Infrastructure.Repository.GameWorld.Hill.Csv>();
        services
            .AddSingleton<App.Infrastructure.Repository.GameWorld.Hill.IGameWorldHillsCsvStreamProvider>(sp =>
            {
                var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Hills:File");
                var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
                return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
            });

        services.AddSingleton(new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            Delimiter = ","
        });
        return services;
    }
}