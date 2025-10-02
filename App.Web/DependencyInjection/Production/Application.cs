using App.Application.Acl;
using App.Application.Game.Gate;
using App.Application.JumpersForm;
using App.Application.Matchmaking;
using App.Application.Policy.GameGateSelector;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.GameWorld;
using App.Domain.Simulation;

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
            // services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.RandomHill>(sp =>
            //     new RandomHill(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IHills>(),
            //     [
            //     ], sp.GetRequiredService<IMyLogger>()));
            services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
                new Fixed("Vikersund HS240", sp.GetRequiredService<IHills>()));
            services
                .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
                    App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
                    new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(30)));
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

        return services;
    }
}