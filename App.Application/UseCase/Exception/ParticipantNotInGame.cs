using App.Domain.Game;

namespace App.Application.UseCase.Game.Exception;

public class ParticipantNotInGameException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    public App.Domain.Game.Participant.Id ParticipantId { get; }
    
    public ParticipantNotInGameException(Domain.Game.Game game, Participant.Id participantId)
    {
        Game = game;
        ParticipantId = participantId;
    }

    public ParticipantNotInGameException(string message, Domain.Game.Game game, Participant.Id participantId) : base(message)
    {
        Game = game;
        ParticipantId = participantId;
    }

    public ParticipantNotInGameException(string message, System.Exception inner, Domain.Game.Game game, Participant.Id participantId) : base(message, inner)
    {
        Game = game;
        ParticipantId = participantId;
    }
}