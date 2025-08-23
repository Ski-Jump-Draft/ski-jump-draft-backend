using App.Application._2.Commanding;
using App.Application._2.Messaging.Notifiers;
using App.Domain._2.Matchmaking;

namespace App.Application._2.UseCase.Matchmaking.LeaveMatchmaking;

public record Command(
    Guid MatchmakingId,
    Guid PlayerId
) : ICommand;

public class Handler(IMatchmakings matchmakings, IMatchmakingNotifier notifier)
    : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct);
        var playerId = PlayerId.NewPlayerId(command.PlayerId);
        var matchmakingAfterLeave = matchmaking.Leave(playerId).ResultValue;
        await matchmakings.Add(matchmakingAfterLeave, ct);
        await notifier.MatchmakingUpdated(MatchmakingDtoMapper.FromDomain(matchmakingAfterLeave));
    }
}