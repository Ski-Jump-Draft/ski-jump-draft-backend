namespace App.Application.UseCase.Helper;

public interface IMatchmakingParticipantFactory
{
    Domain.Matchmaking.Participant CreateFromNick(string nick);
}