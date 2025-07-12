using App.Domain.Draft;
using App.Domain.Draft.Order;
using App.Domain.Shared;
using FluentAssertions;
using Microsoft.FSharp.Collections;

namespace App.Tests.Domain.Draft;

public class DraftStartTests
{
    private static App.Domain.Draft.Draft NewDraftNotStarted() =>
        App.Domain.Draft.Draft.Create(
            Id.Id.NewId(Guid.NewGuid()),
            AggregateVersion.AggregateVersion.NewAggregateVersion(0u),
            new Settings.Settings(Order.Classic, 1, true, Picks.PickTimeout.Unlimited),
            ListModule.OfSeq([Participant.Id.NewId(Guid.NewGuid())]),
            1ul).ResultValue.Item1;

    [Fact]
    public void Start_From_NotStarted_Should_Move_To_Running()
    {
        var draft = NewDraftNotStarted();

        var res = draft.Start();

        res.IsOk.Should().BeTrue();
        var (state, events) = res.ResultValue;
        state.Phase_.Should().BeOfType<Phase.Running>();
        events.ToList().Should().ContainSingle(e => e is Event.DraftEventPayload.DraftStartedV1);
    }

    [Fact]
    public void Start_From_Running_Should_Return_InvalidPhase_Error()
    {
        var draft = NewDraftNotStarted().Start().ResultValue.Item1;

        var res = draft.Start();

        res.IsError.Should().BeTrue();
        res.ErrorValue.Should().Be(Error.NewInvalidPhase([PhaseTag.NotStartedTag], PhaseTag.RunningTag));
    }
}