using System.Diagnostics;
using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using App.Domain.Repositories;

namespace App.Application.UseCase.Game.QuickGame.JoinMatchmaking;

// TODO: Walidacja i nie raw string!
public record Command(
    Domain.Matchmaking.Id MatchmakingId,
    string Nick
) : ICommand<App.Domain.Matchmaking.ParticipantModule.Id>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IMatchmakingParticipantFactory matchmakingParticipantFactory
) : ICommandHandler<Command, App.Domain.Matchmaking.ParticipantModule.Id>
{
    public async Task<App.Domain.Matchmaking.ParticipantModule.Id> HandleAsync(Command command,
        MessageContext messageContext, CancellationToken ct)
    {
        var matchmakingId = command.MatchmakingId;
        var matchmaking = await matchmakings.LoadAsync(matchmakingId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var matchmakingParticipant = matchmakingParticipantFactory.CreateFromNick(command.Nick);

        var matchmakingJoinResult = matchmaking.Join(matchmakingParticipant);

        if (matchmakingJoinResult.IsOk)
        {
            var (matchmakingAfterJoin, events) = matchmakingJoinResult.ResultValue;

            var expectedVersion = matchmakingAfterJoin.Version_;

            await
                matchmakings.SaveAsync(matchmakingAfterJoin.Id_, events, expectedVersion, messageContext.CorrelationId,
                        messageContext.CausationId, ct)
                    .AwaitOrWrap(_ =>
                        new JoiningQuickMatchmakingFailedException(command.Nick,
                            JoiningQuickMatchmakingFailReason.Unknown));
            return matchmakingParticipant.Id;
        }

        var error = matchmakingJoinResult.ErrorValue;

        throw error switch
        {
            Error.ParticipantAlreadyJoined => new MatchmakingParticipantAlreadyJoinedException(matchmaking,
                matchmakingParticipant),
            Error.RoomFull => new MatchmakingRoomFullException(matchmaking),
            Error.InvalidPhase invalidPhaseError => new JoiningMatchmakingInvalidPhaseException(
                invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
            _ => new UnreachableException(error.ToString())
        };
    }
}