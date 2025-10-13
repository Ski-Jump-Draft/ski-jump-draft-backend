using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Matchmaking.LeaveMatchmaking;

public record Command(
    Guid MatchmakingId,
    Guid PlayerId
) : ICommand;

public class Handler(
    IMatchmakings matchmakings,
    IMatchmakingNotifier notifier,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper,
    IClock clock,
    IMatchmakingUpdatedDtoStorage matchmakingUpdatedDtoStorage,
    IPremiumMatchmakings premiumMatchmakings)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));
        var playerId = PlayerId.NewPlayerId(command.PlayerId);
        var now = clock.Now();
        var matchmakingAfterLeaveResult = matchmaking.Leave(playerId, now);
        if (matchmakingAfterLeaveResult.IsOk)
        {
            var matchmakingAfterLeave = matchmakingAfterLeaveResult.ResultValue;
            await matchmakings.Add(matchmakingAfterLeave, ct);
            now = clock.Now();
            var isPremium = await premiumMatchmakings.PremiumMatchmakingIsRunning(command.MatchmakingId);
            var matchmakingUpdatedDto =
                matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterLeave, isPremium, now);
            await matchmakingUpdatedDtoStorage.Set(command.MatchmakingId, matchmakingUpdatedDto);
            await notifier.MatchmakingUpdated(matchmakingUpdatedDto);
            var player = matchmaking.Players_.Single(p => p.Id.Item == command.PlayerId);
            await notifier.PlayerLeft(matchmakingUpdatedDtoMapper.PlayerLeftFromDomain(player, matchmakingAfterLeave));
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