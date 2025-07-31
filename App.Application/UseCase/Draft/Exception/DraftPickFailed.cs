using App.Domain.Draft;

namespace App.Application.UseCase.Draft.Exception;

public class DraftPickFailedException : System.Exception
{
    public Id.Id DraftId { get; }
    public Participant.Id ParticipantId { get; }
    public Subject.Id SubjectId { get; }
    public object? AdditionalData { get; }

    public DraftPickFailedException(
        Id.Id draftId,
        Participant.Id participantId,
        Subject.Id subjectId,
        object? additionalData = null)
    {
        DraftId = draftId;
        ParticipantId = participantId;
        SubjectId = subjectId;
        AdditionalData = additionalData;
    }

    public DraftPickFailedException(
        string message,
        Id.Id draftId,
        Participant.Id participantId,
        Subject.Id subjectId,
        object? additionalData = null)
        : base(message)
    {
        DraftId = draftId;
        ParticipantId = participantId;
        SubjectId = subjectId;
        AdditionalData = additionalData;
    }

    public DraftPickFailedException(
        string message,
        System.Exception inner,
        Id.Id draftId,
        Participant.Id participantId,
        Subject.Id subjectId,
        object? additionalData = null)
        : base(message, inner)
    {
        DraftId = draftId;
        ParticipantId = participantId;
        SubjectId = subjectId;
        AdditionalData = additionalData;
    }
}