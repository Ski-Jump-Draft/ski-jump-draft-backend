using App.Application.Acl;
using App.Application.Game.Gate;
using App.Application.JumpersForm;
using App.Application.Matchmaking;
using App.Application.Policy.GameGateSelector;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Domain.GameWorld;
using App.Domain.Simulation;

namespace App.Web.DependencyInjection.Local;

public static class Application
{
    public static IServiceCollection AddLocalApplication(this IServiceCollection services)
    {
        services.AddSingleton<IGameHillSelector, App.Application.Policy.GameHillSelector.Fixed>(sp =>
            new Fixed("Zakopane HS140", sp.GetRequiredService<IHills>()));
        services.AddSingleton<IGameJumpersSelector, App.Application.Policy.GameJumpersSelector.All>();
        services
            .AddSingleton<App.Application.Policy.DraftPassPicker.IDraftPassPicker, App.Application.Policy.DraftPassPicker.BestPicker>();
        services
            .AddSingleton<App.Application.Game.Ranking.IGameRankingFactorySelector,
                App.Application.Game.Ranking.DefaultSelector>();
        services
            .AddSingleton<App.Application.Game.Gate.ISelectGameStartingGateService,
                App.Application.Game.Gate.SelectCompetitionStartingGateService>();

        services
            .AddSingleton<App.Application.Matchmaking.IMatchmakingDurationCalculator,
                App.Application.Matchmaking.FixedMatchmakingDurationCalculator>(sp =>
                new FixedMatchmakingDurationCalculator(TimeSpan.FromSeconds(20)));
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
        services
            .AddSingleton<App.Application.JumpersForm.IJumperGameFormAlgorithm,
                App.Application.Policy.GameFormAlgorithm.FullyRandom>();

        return services;
    }
}