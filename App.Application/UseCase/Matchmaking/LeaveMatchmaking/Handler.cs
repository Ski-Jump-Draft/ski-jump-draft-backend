using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Matchmaking.LeaveMatchmaking;

public record Command(
    Guid MatchmakingId,
    Guid PlayerId
) : ICommand;

public class Handler(
    IMatchmakings matchmakings,
    IMatchmakingNotifier notifier,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));
        var playerId = PlayerId.NewPlayerId(command.PlayerId);
        var matchmakingAfterLeaveResult = matchmaking.Leave(playerId);
        if (matchmakingAfterLeaveResult.IsOk)
        {
            var matchmakingAfterLeave = matchmakingAfterLeaveResult.ResultValue;
            await matchmakings.Add(matchmakingAfterLeave, ct);
            await notifier.MatchmakingUpdated(matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterLeave));
            var playerNick = matchmaking.Players_.Single(p => p.Id.Item == playerId.Item).Nick;
            await notifier.PlayerLeft(matchmakingUpdatedDtoMapper.PlayerLeftFromDomain(playerId.Item,
                PlayerModule.NickModule.value(playerNick), matchmakingAfterLeave));
        }
        else
        {
            var error = matchmakingAfterLeaveResult.ErrorValue;
            if (error.IsNotInMatchmaking)
            {
                throw new IsNotInMatchmakingException(command.PlayerId, command.MatchmakingId);
            }

            throw new Exception($"Unknown error when leaving matchmaking ({command.MatchmakingId}) by a player({playerId
            }): {error}");
        }
    }
}

public class IsNotInMatchmakingException(Guid playerId, Guid matchmakingId, string? message = null)
    : Exception(message);