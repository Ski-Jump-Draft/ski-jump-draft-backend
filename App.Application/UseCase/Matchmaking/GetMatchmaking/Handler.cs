using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application.UseCase.Matchmaking.GetMatchmaking;

public record Command(
    Guid MatchmakingId
) : ICommand<Result>;

public record Result(
    MatchmakingUpdatedDto MatchmakingUpdatedDto);

public class Handler(
    IMatchmakingUpdatedDtoStorage matchmakingUpdatedDtoStorage)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var dto = await matchmakingUpdatedDtoStorage.Get(command.MatchmakingId);
        if (dto is null)
        {
            throw new Exception("Failed to get matchmaking dto because there is no dto in storage");
        }

        return new Result(dto);
    }
}