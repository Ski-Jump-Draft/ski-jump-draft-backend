using App.Application.Commanding;
using App.Application.UseCase.Draft.Exception;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Repositories;

namespace App.Application.UseCase.Draft.PickSubject;

public record Command(
    App.Domain.Draft.Id.Id DraftId,
    App.Domain.Draft.Participant.Id ParticipantId,
    App.Domain.Draft.Subject.Id SubjectId
) : ICommand;

public class Handler(
    IDraftRepository drafts) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var draft = await drafts.LoadAsync(command.DraftId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.DraftId.Item));

        var draftResult = draft.Pick(command.ParticipantId, command.SubjectId);

        if (draftResult.IsOk)
        {
            var (draftAggregate, eventPayloads) = draftResult.ResultValue;

            var expectedVersion = draft.Version_;

            await drafts.SaveAsync(draftAggregate.Id_, eventPayloads, expectedVersion, messageContext.CorrelationId,
                    messageContext.CausationId, ct)
                .AwaitOrWrap(_ => new DraftPickFailedException(draft.Id_, command.ParticipantId, command.SubjectId));
        }
        else
        {
            throw new DraftPickFailedException(draft.Id_, command.ParticipantId, command.SubjectId,
                draftResult.ErrorValue);
        }
    }
}