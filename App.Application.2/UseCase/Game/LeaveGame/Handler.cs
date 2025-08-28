using App.Application._2.Acl;
using App.Application._2.Commanding;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Messaging.Notifiers.Mapper;
using App.Application._2.Policy;
using App.Application._2.Utility;
using App.Domain._2.Game;
using Microsoft.FSharp.Collections;

namespace App.Application._2.UseCase.Game.LeaveGame;

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
        var game = await games.GetById(Domain._2.Game.GameId.NewGameId(command.GameId), ct);
        var gameGuid = game.Id_.Item;

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