using App.Application.Abstractions;
using App.Domain.Draft;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Shared.Utils;
using App.Domain.Time;

namespace App.Application.Draft.PickSubject;

public record Command(
    App.Domain.Draft.Id.Id DraftId,
    App.Domain.Draft.Participant.Id ParticipantId,
    App.Domain.Draft.Subject.Id SubjectId
);

public class Handler(
    IEventSourcedRepository<Domain.Draft.Draft, Guid> drafts,
    IDraftParticipantRepository draftParticipants,
    IDraftSubjectRepository subjects,
    IGuid guid,
    IClock clock)
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var correlationId = Guid.NewGuid();
        var draft = await drafts.LoadAsync(command.DraftId.Item);
        var lastEventId = await drafts.GetLastEventIdAsync(command.DraftId.Item);

        var pickResult = draft.Pick(command.ParticipantId, command.SubjectId);

        if (pickResult.IsOk)
        {
            var (newState, payloads) = pickResult.ResultValue;
            var events = payloads.Select(payload =>
                DomainEventFactory.create(
                    schemaVer: Event.Versioning.schemaVersion(payload),
                    correlationId: correlationId,
                    causationId: lastEventId,
                    payload: payload,
                    guid: guid,
                    clock: clock
                ));

            var untypedEvents = events
                .Select(ev => new DomainEvent<object>(ev.Header, (object)ev.Payload));

            await drafts.SaveAsync(command.DraftId.Item, untypedEvents.ToList());
        }
        else
        {
            throw new DraftException(pickResult.ErrorValue);
        }
    }
}