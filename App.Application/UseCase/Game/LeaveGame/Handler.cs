using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy;
using App.Application.Utility;
using App.Domain.Game;
using Microsoft.FSharp.Collections;

namespace App.Application.UseCase.Game.LeaveGame;

public record Command(
    Guid GameId,
    Guid PlayerId
) : ICommand<Result>;

public record Result(bool HasLeft = true);

public class Handler(
    IJson json,
    IGames games,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameNotifier gameNotifier,
    IScheduler scheduler,
    IClock clock,
    IGuid guid)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(Domain.Game.GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        return new Result(HasLeft: false);

        throw new NotImplementedException();

        //
        // if (gameAfterPreDraftStartResult.IsOk)
        // {
        //     var gameAfterPreDraftStart = gameAfterPreDraftStartResult.ResultValue;
        //     await games.Add(gameAfterPreDraftStart, ct);
        //     var now = clock.Now();
        //     await scheduler.ScheduleAsync(
        //         jobType: "SimulateJumpInGame",
        //         payloadJson: json.Serialize(new { GameId = gameGuid }),
        //         runAt: now.AddSeconds(15),
        //         uniqueKey: $"SimulateJumpInGame:{gameGuid}_{now.ToUnixTimeSeconds()}",
        //         ct: ct);
        //     await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(gameAfterPreDraftStart));       
        // }
        // else
        // {
        //     throw new Exception("Game start pre draft failed", new Exception(gameAfterPreDraftStartResult.ErrorValue.ToString()));       
        // }
        //
        // return new Result();
    }
}