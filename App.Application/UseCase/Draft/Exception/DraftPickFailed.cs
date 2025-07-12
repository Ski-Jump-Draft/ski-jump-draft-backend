using App.Domain.Draft;

namespace App.Application.UseCase.Draft.Exception;

public class DraftPickFailedException : System.Exception
{
    Domain.Draft.Draft Draft { get; }
    Participant.Participant Participant { get; }
    Subject.Subject Subject { get; }
    private object? AdditionalData { get; }

    public DraftPickFailedException(Domain.Draft.Draft draft, Participant.Participant participant,
        Subject.Subject subject, object? additionalData = null)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
        AdditionalData = additionalData;
    }

    public DraftPickFailedException(string message, Domain.Draft.Draft draft, Participant.Participant participant,
        Subject.Subject subject, object? additionalData = null) : base(message)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
        AdditionalData = additionalData;
    }

    public DraftPickFailedException(string message, System.Exception inner, Domain.Draft.Draft draft,
        Participant.Participant participant, Subject.Subject subject, object? additionalData = null) : base(message, inner)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
        AdditionalData = additionalData;
    }
}