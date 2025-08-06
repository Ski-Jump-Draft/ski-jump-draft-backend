using App.Domain.Game;

namespace App.Application.UseCase.Game.Exception;

public class LeavingGameFailedException : System.Exception
{
    public Id.Id GameId { get; set; }
    public Participant.Id ParticipantId { get; set; }
    
    public LeavingGameFailedException(Id.Id gameId, Participant.Id participantId)
    {
        GameId = gameId;
        ParticipantId = participantId;
    }

    public LeavingGameFailedException(string message, Id.Id gameId, Participant.Id participantId) : base(message)
    {
        GameId = gameId;
        ParticipantId = participantId;
    }

    public LeavingGameFailedException(string message, System.Exception inner, Id.Id gameId, Participant.Id participantId) : base(message, inner)
    {
        GameId = gameId;
        ParticipantId = participantId;
    }
}