using Microsoft.FSharp.Collections;

namespace App.Web.DependencyInjection.Production;

public static class Game
{
    public static IServiceCollection AddProductionGame(this IServiceCollection services)
    {
        services.AddSingleton<App.Domain.Game.Settings>(sp =>
        {
            var preDraftCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq([
                    new App.Domain.Competition.RoundSettings(App.Domain.Competition.RoundLimit.NoneLimit, false,
                        false)
                ]))
                .ResultValue;
            var preDraftSettings = App.Domain.Game.PreDraftSettings.Create(ListModule.OfSeq(
                    new List<App.Domain.Competition.Settings>
                        { preDraftCompetitionSettings, preDraftCompetitionSettings }))
                .Value;
            var mainCompetitionSettings = App.Domain.Competition.Settings.Create(ListModule.OfSeq([
                new App.Domain.Competition.RoundSettings(
                    App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule
                        .tryCreate(50)
                        .ResultValue), false, false),
                new App.Domain.Competition.RoundSettings(
                    App.Domain.Competition.RoundLimit.NewSoft(App.Domain.Competition.RoundLimitValueModule
                        .tryCreate(30)
                        .ResultValue), true, false)
            ])).ResultValue;
            var draftSettings = new App.Domain.Game.DraftModule.Settings(
                App.Domain.Game.DraftModule.SettingsModule.TargetPicksModule.create(4).Value,
                App.Domain.Game.DraftModule.SettingsModule.MaxPicksModule.create(4).Value,
                App.Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
                App.Domain.Game.DraftModule.SettingsModule.Order.Random,
                App.Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(
                    TimeSpan.FromSeconds(3)));
            var breakSettings =
                new App.Domain.Game.BreakSettings(App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(5)).Value,
                    App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)).Value,
                    App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)).Value,
                    App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)).Value,
                    App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)).Value);
            var jumpInterval = App.Domain.Game.PhaseDuration.Create(TimeSpan.FromMilliseconds(3200)).Value;
            return new App.Domain.Game.Settings(breakSettings, preDraftSettings, draftSettings, mainCompetitionSettings,
                jumpInterval,
                App.Domain.Game.RankingPolicy.Classic);
        });

        return services;
    }
}