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
        // services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
        //     new Fixed("Zakopane HS140", sp.GetRequiredService<IHills>()));

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
            services
                .AddSingleton<App.Application.Game.Gate.IGameStartingGateSelectorFactory,
                    App.Application.Game.Gate.IterativeSimulatedFactory>(sp =>
                {
                    const JuryBravery juryBravery = JuryBravery.High;
                    return new IterativeSimulatedFactory(sp.GetRequiredService<IJumpSimulator>(),
                        sp.GetRequiredService<IWeatherEngine>(),
                        juryBravery, sp.GetRequiredService<ICompetitionJumperAcl>(),
                        sp.GetRequiredService<IGameJumperAcl>(),
                        sp.GetRequiredService<ICompetitionHillAcl>(), sp.GetRequiredService<IJumpers>(),
                        sp.GetRequiredService<IJumperGameFormStorage>());
                });
        }
        else
        {
            services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.RandomHill>(sp =>
                new RandomHill(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IHills>(),
                [
                ], sp.GetRequiredService<IMyLogger>()));
            services
                .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
                    App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
                    new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(30)));
            services
                .AddSingleton<App.Application.Game.Gate.IGameStartingGateSelectorFactory,
                    App.Application.Game.Gate.IterativeSimulatedFactory>(sp =>
                {
                    const JuryBravery juryBravery = JuryBravery.Medium;
                    return new IterativeSimulatedFactory(sp.GetRequiredService<IJumpSimulator>(),
                        sp.GetRequiredService<IWeatherEngine>(),
                        juryBravery, sp.GetRequiredService<ICompetitionJumperAcl>(),
                        sp.GetRequiredService<IGameJumperAcl>(),
                        sp.GetRequiredService<ICompetitionHillAcl>(), sp.GetRequiredService<IJumpers>(),
                        sp.GetRequiredService<IJumperGameFormStorage>());
                });
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
            .AddSingleton<App.Application.Game.Gate.ISelectGameStartingGateService,
                App.Application.Game.Gate.SelectCompetitionStartingGateService>();


        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormAlgorithm,
                App.Application.Policy.GameFormAlgorithm.FullyRandom>();

        return services;
    }
}