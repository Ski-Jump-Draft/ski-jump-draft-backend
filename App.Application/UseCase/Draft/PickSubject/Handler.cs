using App.Application.Abstractions;
using App.Application.UseCase.Draft.Exception;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Draft;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Shared.Utils;
using App.Domain.Time;

namespace App.Application.UseCase.Draft.PickSubject;

public record Command(
    App.Domain.Draft.Id.Id DraftId,
    App.Domain.Draft.Participant.Id ParticipantId,
    App.Domain.Draft.Subject.Id SubjectId
) : ICommand;

public class Handler(
    IDraftRepository drafts,
    IDraftParticipantRepository draftParticipants,
    IDraftSubjectRepository draftSubjects,
    IGuid guid) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var draft = await FSharpAsyncExt.AwaitOrThrow(drafts.LoadAsync(command.DraftId, ct),
            new IdNotFoundException<Guid>(command.DraftId.Item), ct);
        var participant = await FSharpAsyncExt.AwaitOrThrow(draftParticipants.GetByIdAsync(command.ParticipantId),
            new IdNotFoundException<Guid>(command.ParticipantId.Item), ct);
        var subject = await FSharpAsyncExt.AwaitOrThrow(draftSubjects.GetByIdAsync(command.SubjectId),
            new IdNotFoundException<Guid>(command.SubjectId.Item), ct);

        var draftResult = draft.Pick(command.ParticipantId, command.SubjectId);

        if (draftResult.IsOk)
        {
            var (state, eventPayloads) = draftResult.ResultValue;

            var expectedVersion = draft.Version_;
            var correlationId = guid.NewGuid();
            var causationId = correlationId;

            await FSharpAsyncExt.AwaitOrThrow(drafts.SaveAsync(state, eventPayloads, expectedVersion, correlationId,
                causationId, ct), new DraftPickFailedException(draft, participant, subject), ct);
        }
        else
        {
            throw new DraftPickFailedException(draft, participant, subject, draftResult.ErrorValue);
        }
    }
}