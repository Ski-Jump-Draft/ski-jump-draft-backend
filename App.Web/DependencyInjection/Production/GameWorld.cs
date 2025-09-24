using System.Globalization;
using Amazon.S3;
using App.Infrastructure.Helper.Csv;
using App.Infrastructure.Repository.GameWorld.Country;
using App.Infrastructure.Repository.GameWorld.Hill;
using App.Infrastructure.Repository.GameWorld.Jumper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace App.Web.DependencyInjection.Production;

public static class GameWorld
{
    public static IServiceCollection AddGameWorld(this IServiceCollection services, IConfiguration configuration,
        bool isMocked)
    {
        if (isMocked)
        {
        }
        else
        {
            ConfigureNotMocked(services, configuration);
        }


        services.AddSingleton(new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            Delimiter = ","
        });

        return services;
    }

    private static void ConfigureMocked(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureNotMocked(services, configuration);
        // services
        //     .AddSingleton<App.Domain.GameWorld.ICountries, App.Infrastructure.Repository.GameWorld.Country.Csv>();
        // services
        //     .AddSingleton<App.Infrastructure.Repository.GameWorld.Country.IGameWorldCountriesCsvStreamProvider>(sp =>
        //     {
        //         var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Countries:File");
        //         var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
        //         return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        //     });
        //
        // services
        //     .AddSingleton<App.Domain.GameWorld.IJumpers, App.Infrastructure.Repository.GameWorld.Jumper.Csv>();
        //
        // services
        //     .AddSingleton<App.Infrastructure.Repository.GameWorld.Jumper.IGameWorldJumpersCsvStreamProvider>(sp =>
        //     {
        //         var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Jumpers:File");
        //         var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
        //         return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        //     });
        // services
        //     .AddSingleton<App.Domain.GameWorld.IHills, App.Infrastructure.Repository.GameWorld.Hill.Csv>();
        // services
        //     .AddSingleton<App.Infrastructure.Repository.GameWorld.Hill.IGameWorldHillsCsvStreamProvider>(sp =>
        //     {
        //         var relPath = sp.GetRequiredService<IConfiguration>().GetValue<string>("Csv:Hills:File");
        //         var absPath = Path.Combine(AppContext.BaseDirectory, relPath!);
        //         return new App.Infrastructure.Helper.Csv.FileCsvStreamProvider(absPath);
        //     });
    }

    private static void ConfigureNotMocked(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var cfg = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(
                    configuration["AWS:Region"])
            };
            return new AmazonS3Client(
                configuration["AWS:AccessKeyId"],
                configuration["AWS:SecretAccessKey"],
                cfg);
        });

        services
            .AddSingleton<App.Domain.GameWorld.ICountries, App.Infrastructure.Repository.GameWorld.Country.Csv>();
        services.AddSingleton<IGameWorldCountriesCsvStreamProvider>(sp =>
            new CachingCsvStreamProvider(
                new S3CsvStreamProvider(
                    sp.GetRequiredService<IAmazonS3>(),
                    bucket: "ski-jump-draft",
                    key: "GameWorld/Countries/current.csv"),
                sp.GetRequiredService<IMemoryCache>(),
                cacheKey: "countries_csv",
                ttl: TimeSpan.FromMinutes(30)
            ));

        services
            .AddSingleton<App.Domain.GameWorld.IJumpers, App.Infrastructure.Repository.GameWorld.Jumper.Csv>();

        services.AddSingleton<IGameWorldJumpersCsvStreamProvider>(sp =>
            new CachingCsvStreamProvider(
                new S3CsvStreamProvider(
                    sp.GetRequiredService<IAmazonS3>(),
                    bucket: "ski-jump-draft",
                    key: "GameWorld/Jumpers/current.csv"),
                sp.GetRequiredService<IMemoryCache>(),
                cacheKey: "jumpers_csv",
                ttl: TimeSpan.FromMinutes(10)
            ));

        services
            .AddSingleton<App.Domain.GameWorld.IHills, App.Infrastructure.Repository.GameWorld.Hill.Csv>();
        services.AddSingleton<IGameWorldHillsCsvStreamProvider>(sp =>
            new CachingCsvStreamProvider(
                new S3CsvStreamProvider(
                    sp.GetRequiredService<IAmazonS3>(),
                    bucket: "ski-jump-draft",
                    key: "GameWorld/Hills/current.csv"),
                sp.GetRequiredService<IMemoryCache>(),
                cacheKey: "hills_csv",
                ttl: TimeSpan.FromMinutes(10)
            ));
    }
}