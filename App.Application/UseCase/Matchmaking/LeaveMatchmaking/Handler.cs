using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Telemetry;
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
    IBotRegistry botRegistry,
    ITelemetry telemetry)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));
        var playerId = PlayerId.NewPlayerId(command.PlayerId);
        var leaveTime = clock.Now();
        var matchmakingAfterLeaveResult = matchmaking.Leave(playerId, leaveTime);
        if (matchmakingAfterLeaveResult.IsOk)
        {
            var matchmakingAfterLeave = matchmakingAfterLeaveResult.ResultValue;
            await matchmakings.Add(matchmakingAfterLeave, ct);

            var matchmakingUpdatedDto =
                matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterLeave, botRegistry, leaveTime);
            await matchmakingUpdatedDtoStorage.Set(command.MatchmakingId, matchmakingUpdatedDto);
            await notifier.MatchmakingUpdated(matchmakingUpdatedDto);
            var player = matchmaking.Players_.Single(p => p.Id.Item == command.PlayerId);
            var isBot = botRegistry.IsMatchmakingBot(command.MatchmakingId, command.PlayerId);
            await notifier.PlayerLeft(
                matchmakingUpdatedDtoMapper.PlayerLeftFromDomain(player, isBot, matchmakingAfterLeave));

            await telemetry.Record(new GameTelemetryEvent("PlayerLeftMatchmaking", null, command.MatchmakingId, null,
                leaveTime, new Dictionary<string, object>()
                {
                    ["PlayerId"] = command.PlayerId,
                    ["IsBot"] = isBot,
                    ["JoinedAt"] = player.JoinedAt,
                    ["LeftAt"] = leaveTime,
                    ["WaitTimeSeconds"] = (leaveTime - player.JoinedAt).TotalSeconds,
                    ["MaximumRemainingTimeSeconds"] =
                        (matchmakingAfterLeave.ForceEndAt(leaveTime) - leaveTime).TotalSeconds
                }));
        }
        else
        {
            var error = matchmakingAfterLeaveResult.ErrorValue;
            if (error.IsNotInMatchmaking)
            {
                // Make leave idempotent: if the player is already not in matchmaking, treat as success.
                // This can happen when manual leave and auto-leave (SSE disconnect) race each other.
                // We keep system robust and user-friendly by not failing in this expected case.
                return;
            }

            throw new Exception($"Unknown error when leaving matchmaking ({command.MatchmakingId}) by a player({playerId
            }): {error}");
        }
    }
}

public class IsNotInMatchmakingException(Guid playerId, Guid matchmakingId, string? message = null)
    : Exception(message);