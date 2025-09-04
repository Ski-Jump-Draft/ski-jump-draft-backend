using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Utility;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application.UseCase.Matchmaking.GetMatchmaking;

public record Command(
    Guid MatchmakingId
) : ICommand<Result>;

public record Result(
    string Status,
    string? FailReason,
    int PlayersCount,
    int? MinRequiredPlayers,
    int MinPlayers,
    int MaxPlayers,
    TimeSpan? RemainingTime);

public class Handler(
    IMatchmakings matchmakings,
    IMatchmakingSchedule matchmakingSchedule)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct).AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));;

        string? failReason = null;
        if (matchmaking.Status_.Tag == Status.Tags.Failed)
        {
            var failedStatus = (Status.Failed)matchmaking.Status_;
            failReason = failedStatus.Reason;
        }

        TimeSpan? remainingTime = null;
        if (matchmaking.Status_.IsRunning)
        {
            remainingTime = matchmakingSchedule.GetRemainingTime(command.MatchmakingId);
        }

        var minRequiredPlayers = OptionModule.ToNullable(matchmaking.MinRequiredPlayers);
        return new Result(FSharpUnionHelper.GetCaseName(matchmaking.Status_), failReason, matchmaking.Players_.Count,
            minRequiredPlayers, SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount), remainingTime);
    }
}