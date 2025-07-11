using App.Domain.Draft;

namespace App.Application.UseCase.Draft.Exception;

public class DraftPickFailedException : System.Exception
{
    Domain.Draft.Draft Draft { get; }
    Participant.Participant Participant { get; }
    Subject.Subject Subject { get; }

    public DraftPickFailedException(Domain.Draft.Draft draft, Participant.Participant participant,
        Subject.Subject subject)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
    }

    public DraftPickFailedException(string message, Domain.Draft.Draft draft, Participant.Participant participant,
        Subject.Subject subject) : base(message)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
    }

    public DraftPickFailedException(string message, System.Exception inner, Domain.Draft.Draft draft,
        Participant.Participant participant, Subject.Subject subject) : base(message, inner)
    {
        Draft = draft;
        Participant = participant;
        Subject = subject;
    }
}