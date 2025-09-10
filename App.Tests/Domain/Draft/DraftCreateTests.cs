using App.Domain.Draft;
using App.Domain.Draft.Order;
using App.Domain.Shared;
using FluentAssertions;
using Microsoft.FSharp.Collections;

namespace App.Tests.Domain.Draft;

public class DraftCreateTests
{
    [Fact]
    public void Create_Should_Return_State_And_DraftCreatedEvent()
    {
        // arrange
        var draftId = Id.Id.NewId(Guid.NewGuid());
        var participants = new[] { Participant.Id.NewId(Guid.NewGuid()) };
        var settings = new Settings.Settings(Order.Classic, 1, true, Picks.PickTimeout.Unlimited);

        // act
        var result = App.Domain.Draft.Draft.Create(
            draftId,
            AggregateVersion.zero,
            settings,
            ListModule.OfSeq(participants),
            
            1234ul);

        // assert
        result.IsOk.Should().BeTrue();
        var (state, events) = result.ResultValue;
        state.Id_.Should().Be(draftId);
        events.ToList().Should().ContainSingle(e => e is Event.DraftEventPayload.DraftCreatedV1);
    }
}