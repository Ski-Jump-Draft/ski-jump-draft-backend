using App.Domain.Draft;
using App.Domain.Draft.Order;
using App.Domain.Shared;
using static Microsoft.FSharp.Collections.ListModule;

namespace App.Tests.Fakes.Factory;

public static class FakeDraftFactory
{
    public static Draft CreateInRunningPhase(Id.Id id, IReadOnlyList<Participant.Id> participants,
        Order order)
    {
        var settings = new App.Domain.Draft.Settings.Settings(order, 4, true, Picks.PickTimeout.Unlimited);
        var initialDraft = Draft.Create(
            id,
            AggregateVersion.AggregateVersion.NewAggregateVersion(0u),
            settings,
            OfSeq(participants),
            seed: 1234ul
        ).ResultValue.Item1;
        var startedDraft = initialDraft.Start().ResultValue.Item1;
        return startedDraft;
    }

    public static Draft CreateWithOnlyOnePickLeft(Id.Id id, IReadOnlyList<Participant.Id> participants, Order order)
    {
        var settings = new App.Domain.Draft.Settings.Settings(
            order,
            maxJumpersPerPlayer: 1,
            uniqueJumpers: true,
            pickTimeout: Picks.PickTimeout.Unlimited
        );

        // 1. Utwórz draft i start
        var createdResult = Draft.Create(
            id,
            AggregateVersion.AggregateVersion.NewAggregateVersion(0u),
            settings,
            OfSeq(participants),
            seed: 1234ul
        ).ResultValue;

        var draft = createdResult.Item1;
        var startedResult = draft.Start();
        if (!startedResult.IsOk) throw new InvalidOperationException("Start failed");

        var startedDraft = startedResult.ResultValue.Item1;

        var orderList = GetOrderFromRunningPhase(startedDraft.Phase_);

        for (var i = 0; i < orderList.Count - 1; i++)
        {
            var participant = orderList[i];
            var subjectId = Subject.Id.NewId(Guid.NewGuid());

            var result = startedDraft.Pick(participant, subjectId);
            if (!result.IsOk) throw new InvalidOperationException("Pick failed");

            startedDraft = result.ResultValue.Item1;
        }

        return startedDraft;
    }

    private static List<Participant.Id> GetOrderFromRunningPhase(App.Domain.Draft.Phase phase)
    {
        if (phase is App.Domain.Draft.Phase.Running running)
            return running.ParticipantOrder.ToList();
        throw new InvalidOperationException("Not in running phase");
    }
}