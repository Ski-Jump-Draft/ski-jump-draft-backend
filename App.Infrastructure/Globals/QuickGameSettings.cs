using App.Application.Abstractions;
using App.Application.UseCase.Helper;
using App.Domain.Draft;
using App.Domain.Draft.Order;
using Microsoft.FSharp.Collections;
using Settings = App.Domain.Game.Settings;

namespace App.Infrastructure.Globals;

public class DefaultQuickGameSettingsProvider(
    IQuickGameHillSelector hillSelector)
    : IQuickGameSettingsProvider
{
    public Task<Settings.Settings> Provide()
    {
        var participantLimit = Settings.ParticipantLimitModule.tryCreate(8).ResultValue;
        var classicEngineOptions = new FSharpMap<string, object>([
            Tuple.Create("Category", (object)"Individual"),
            Tuple.Create("EnableGatePoints", (object)true),
            Tuple.Create("EnableWindPoints", (object)true),
            Tuple.Create("RoundLimits", (object)new List<string> { "Soft 50", "Soft 30" })
        ]);

        var gameWorldHill = hillSelector.Select();
        var gameWorldHillId = gameWorldHill.Id_;

        var preDraftCompetitionSettings = new List<Domain.PreDraft.Competition.Settings>()
        {
            new("classic",
                classicEngineOptions),
            new("classic",
                classicEngineOptions)
        };
        // TODO: Co z subsettingsami wewnątrz Game?
        var preDraftSettings = new Domain.PreDraft.Settings.Settings(ListModule.OfSeq(preDraftCompetitionSettings));
        var pickTimeoutFixedTime =
            Picks.PickTimeoutModule.FixedTimeModule.tryCreate(TimeSpan.FromSeconds(15)).ResultValue;
        var draftSettings = new Domain.Draft.Settings.Settings(Order.Snake, 4, true,
            Picks.PickTimeout.NewFixed(pickTimeoutFixedTime));
        var competitionSettings =
            new App.Domain.Game.CompetitionModule.Settings("classic", classicEngineOptions);

        return Task.FromResult(new Settings.Settings(participantLimit,
            Settings.PhaseTransitionPolicy.StartingPreDraft.NewAutoAfter(TimeSpan.FromSeconds(15)),
            Settings.PhaseTransitionPolicy.StartingDraft.NewAutoAfter(TimeSpan.FromSeconds(15)),
            Settings.PhaseTransitionPolicy.StartingCompetition.NewAutoAfter(TimeSpan.FromSeconds(20)),
            Settings.PhaseTransitionPolicy.EndingGame.NewAutoAfter(TimeSpan.FromSeconds(25))));
    }
}