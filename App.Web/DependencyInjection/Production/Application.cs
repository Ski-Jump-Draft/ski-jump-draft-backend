using App.Application.Acl;
using App.Application.Game.DraftPicks;
using App.Application.Game.Gate;
using App.Application.JumpersForm;
using App.Application.Matchmaking;
using App.Application.Policy.GameGateSelector;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Domain.Simulation;
using App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace App.Web.DependencyInjection.Production;

public static class Application
{
    public static IServiceCollection AddProductionApplication(this IServiceCollection services, bool isMocked)
    {
        if (isMocked)
        {
            services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.RandomHill>(sp =>
                new RandomHill(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IHills>(),
                [
                    "Zakopane HS140",
                    "Oslo HS134"
                ], sp.GetRequiredService<IMyLogger>()));
            services
                .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
                    App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
                    new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(3)));
        }
        else
        {
            services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.RandomHill>(sp =>
                new RandomHill(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IHills>(),
                [
                ], sp.GetRequiredService<IMyLogger>()));
            // services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
            //     new Fixed("Vikersund HS240", sp.GetRequiredService<IHills>()));
        }

        services.AddSingleton<IGameJumpersSelector, App.Application.Policy.GameJumpersSelector.All>();
        services
            .AddSingleton<App.Application.Policy.DraftPicker.IDraftPicker,
                App.Application.Policy.DraftPicker.BestPicker>();
        services
            .AddSingleton<App.Application.Policy.DraftPicker.IDraftPassPicker,
                App.Application.Policy.DraftPicker.RandomPicker>();
        services
            .AddSingleton<App.Application.Policy.DraftBotPickTime.IDraftBotPickTime,
                App.Application.Policy.DraftBotPickTime.GaussianDistribution>();
        services
            .AddSingleton<App.Application.Policy.GameCompetitionStartlist.IGameCompetitionStartlist,
                App.Application.Policy.GameCompetitionStartlist.Classic>();
        services
            .AddSingleton<App.Application.Game.Ranking.IGameRankingFactorySelector,
                App.Application.Game.Ranking.DefaultSelector>();

        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormAlgorithm,
                App.Application.Policy.GameFormAlgorithm.FullyRandom>();

        if (isMocked)
        {
            // services.AddSingleton<App.Application.UseCase.Rankings.WeeklyTopJumps.IWeeklyTopJumpsQuery,
            //     App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps.InMemoryWeeklyTopJumpsQuery>();
            services.AddSingleton<App.Application.UseCase.Rankings.WeeklyTopJumps.IWeeklyTopJumpsQuery,
                App.Infrastructure.ReadModels.Rankings.WeeklyTopJumps.MockWeeklyTopJumpsQuery>();
        }
        else
        {
            services.Configure<WeeklyTopJumpsCacheOptions>(opts =>
            {
                opts.CacheKey = "weeklytopjumps:redis:top20:last7days";
                opts.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                opts.Size = 1;
            });

            services.AddSingleton<RedisWeeklyTopJumpsQuery, RedisWeeklyTopJumpsQuery>();
            services.AddSingleton<App.Application.UseCase.Rankings.WeeklyTopJumps.IWeeklyTopJumpsQuery>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WeeklyTopJumpsCacheOptions>>();
                return new CachedWeeklyTopJumpsQuery(sp.GetRequiredService<RedisWeeklyTopJumpsQuery>(),
                    sp.GetRequiredService<IMemoryCache>(),
                    options);
            });
        }

        return services;
    }
}