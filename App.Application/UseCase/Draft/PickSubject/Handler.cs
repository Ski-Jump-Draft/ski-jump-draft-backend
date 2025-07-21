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
        var draft = await drafts.LoadAsync(command.DraftId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.DraftId.Item));
        var participant = await draftParticipants.GetByIdAsync(command.ParticipantId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.ParticipantId.Item));
        var subject = await draftSubjects.GetByIdAsync(command.SubjectId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.SubjectId.Item));

        var draftResult = draft.Pick(command.ParticipantId, command.SubjectId);

        if (draftResult.IsOk)
        {
            var (state, eventPayloads) = draftResult.ResultValue;

            var expectedVersion = draft.Version_;
            var correlationId = guid.NewGuid();
            var causationId = correlationId;

            await drafts.SaveAsync(state, eventPayloads, expectedVersion, correlationId,
                causationId, ct).AwaitOrWrap(_ => new DraftPickFailedException(draft, participant, subject));
        }
        else
        {
            throw new DraftPickFailedException(draft, participant, subject, draftResult.ErrorValue);
        }
    }
}