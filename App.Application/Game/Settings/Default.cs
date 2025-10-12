using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Game;
using Microsoft.FSharp.Collections;

namespace App.Application.Game.Settings;

public class Default(IRandom random) : IGameSettingsFactory
{
    public Domain.Game.Settings Create()
    {
 
        var picksNumber = new Dictionary<int, double>
        {
            { 3, 0.8 },
            { 4, 1 },
            { 5, 1 },
            { 6, 0.5 },
        }.WeightedRandomElement(random);

        var draftOrder = new Dictionary<Domain.Game.DraftModule.SettingsModule.Order, double>
        {
            { Domain.Game.DraftModule.SettingsModule.Order.Snake, 50 },
            { Domain.Game.DraftModule.SettingsModule.Order.Random, 50 },
        }.WeightedRandomElement(random);

        var rankingPolicy = new Dictionary<Domain.Game.RankingPolicy, double>
        {
            { Domain.Game.RankingPolicy.Classic, 50 },
            { Domain.Game.RankingPolicy.PodiumAtAllCosts, 0 },
        }.WeightedRandomElement(random);


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
            App.Domain.Game.DraftModule.SettingsModule.TargetPicksModule.create(picksNumber).Value,
            App.Domain.Game.DraftModule.SettingsModule.MaxPicksModule.create(picksNumber).Value,
            App.Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
            draftOrder,
            App.Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(
                TimeSpan.FromSeconds(15)));
        var breakSettings =
            new App.Domain.Game.BreakSettings(App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)),
                App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(15)),
                App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(20)),
                App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(20)),
                App.Domain.Game.PhaseDuration.Create(TimeSpan.FromSeconds(20)));
        var jumpInterval = App.Domain.Game.PhaseDuration.Create(TimeSpan.FromMilliseconds(6000));
        return new App.Domain.Game.Settings(breakSettings, preDraftSettings, draftSettings,
            mainCompetitionSettings,
            jumpInterval,
            rankingPolicy);
    }
}