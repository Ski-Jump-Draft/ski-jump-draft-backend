namespace App.Application.UseCase.Game.Exception;

public class ParticipantAlreadyJoinedException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    public App.Domain.Game.Participant.Id ParticipantId { get; }

    public ParticipantAlreadyJoinedException(Domain.Game.Game game, App.Domain.Game.Participant.Id participantId)
    {
        Game = game;
        ParticipantId = participantId;
    }

    public ParticipantAlreadyJoinedException(string message, Domain.Game.Game game, App.Domain.Game.Participant.Id participantId) :
        base(message)
    {
        Game = game;
        ParticipantId = participantId;
    }

    public ParticipantAlreadyJoinedException(string message, System.Exception inner, Domain.Game.Game game,
        App.Domain.Game.Participant.Id participantId) : base(message, inner)
    {
        Game = game;
        ParticipantId = participantId;
    }
}