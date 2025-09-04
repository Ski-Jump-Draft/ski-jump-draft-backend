using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Messaging.Notifiers;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Matchmaking.LeaveMatchmaking;

public record Command(
    Guid MatchmakingId,
    Guid PlayerId
) : ICommand;

public class Handler(IMatchmakings matchmakings, IMatchmakingNotifier notifier)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct).AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));
        var playerId = PlayerId.NewPlayerId(command.PlayerId);
        var matchmakingAfterLeave = matchmaking.Leave(playerId).ResultValue;
        await matchmakings.Add(matchmakingAfterLeave, ct);
        await notifier.MatchmakingUpdated(MatchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterLeave));
    }
}