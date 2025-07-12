using App.Domain.Draft;
using App.Domain.Draft.Order;
using App.Tests.Fakes.Factory;
using FluentAssertions;

namespace App.Tests.Domain.Draft;

public class DraftPickTests
{
    private static (App.Domain.Draft.Draft Draft, Participant.Id Part, Subject.Id Subj) RunningDraft()
    {
        var part = Participant.Id.NewId(Guid.NewGuid());
        var subj = Subject.Id.NewId(Guid.NewGuid());

        var draft =
            FakeDraftFactory.CreateInRunningPhase(
                Id.Id.NewId(Guid.NewGuid()),
                [part],
                Order.Classic);

        return (draft, part, subj);
    }

    [Fact]
    public void Pick_By_CurrentParticipant_Should_Emit_PickEvent()
    {
        var (draft, part, subj) = RunningDraft();

        var res = draft.Pick(part, subj);

        res.IsOk.Should().BeTrue();
        var (_, events) = res.ResultValue;
        events.ToList().Should().ContainSingle(e => e is Event.DraftEventPayload.DraftSubjectPickedV2);
    }

    [Fact]
    public void Pick_Not_CurrentParticipant_Should_Error()
    {
        var (draft, _, subj) = RunningDraft();
        var other = Participant.Id.NewId(Guid.NewGuid());

        var res = draft.Pick(other, subj);

        res.IsError.Should().BeTrue();
        res.ErrorValue.Should().Be(Error.NewParticipantNotAllowedToPick(other));
    }

    [Fact]
    public void Last_Pick_Should_Emit_DraftEnded_And_Set_Done()
    {
        var id1 = Participant.Id.NewId(Guid.NewGuid());
        var id2 = Participant.Id.NewId(Guid.NewGuid());
        var draft = FakeDraftFactory.CreateWithOnlyOnePickLeft(
            Id.Id.NewId(Guid.NewGuid()),
            [id1, id2],
            Order.Classic);

        var lastSubj = Subject.Id.NewId(Guid.NewGuid());
        var current = ((Phase.Running)draft.Phase_).CurrentIndex == 0 ? id1 : id2;

        var res = draft.Pick(current, lastSubj);

        res.IsOk.Should().BeTrue();
        var (state, evts) = res.ResultValue;
        state.Phase_.Should().BeOfType<Phase.Done>();
        evts.ToList().Should().Contain(e => e is Event.DraftEventPayload.DraftEndedV1);
    }
}