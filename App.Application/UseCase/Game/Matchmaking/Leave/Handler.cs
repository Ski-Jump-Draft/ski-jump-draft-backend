using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using App.Domain.Repositories;

namespace App.Application.UseCase.Game.Matchmaking.Leave;

public record Command(
    App.Domain.Matchmaking.Id MatchmakingId,
    ParticipantModule.Id MatchmakingParticipantId
) : ICommand<App.Domain.Matchmaking.ParticipantModule.Id>;

public class Handler(
    IMatchmakingRepository matchmakings,
    IGuid guid
) : ICommandHandler<Command, App.Domain.Matchmaking.ParticipantModule.Id>
{
    public async Task<App.Domain.Matchmaking.ParticipantModule.Id> HandleAsync(Command command,
        MessageContext messageContext, CancellationToken ct)
    {
        var matchmakingId = command.MatchmakingId;
        var matchmaking = await matchmakings.LoadAsync(matchmakingId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var matchmakingParticipantId = command.MatchmakingParticipantId;
        var matchmakingParticipant = await matchmakingParticipants.GetByIdAsync(matchmakingParticipantId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var leaveResult = matchmaking.Leave(matchmakingParticipant.Id);

        if (leaveResult.IsOk)
        {
            var (aggregate, events) = leaveResult.ResultValue;

            var expectedVersion = aggregate.Version_;
            await
                matchmakings.SaveAsync(aggregate.Id_, events, expectedVersion, messageContext.CorrelationId,
                    messageContext.CausationId, ct).AwaitOrWrap(_ =>
                    new LeavingMatchmakingFailedException(matchmaking, matchmakingParticipant,
                        LeavingMatchmakingFailReason.ErrorDuringUpdatingMatchmaking));
            return matchmakingParticipant.Id;
        }

        var error = leaveResult.ErrorValue;

        throw error switch
        {
            Error.PlayerNotJoined => new MatchmakingParticipantNotInMatchmakingException(matchmaking,
                matchmakingParticipant),
            _ => new LeavingMatchmakingFailedException(matchmaking, matchmakingParticipant,
                LeavingMatchmakingFailReason.Unknown)
        };
    }
}