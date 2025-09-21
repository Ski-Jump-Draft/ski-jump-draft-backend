using App.Application.Acl;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.DraftPicker;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using Microsoft.FSharp.Collections;
using JumperId = App.Domain.GameWorld.JumperId;

namespace App.Application.UseCase.Game.PassPick;

public record Command(
    Guid GameId,
    Guid PlayerId
) : ICommand<Result>;

public record Result(Guid JumperId);

public class Handler(
    IGames games,
    IMyLogger logger,
    IDraftPassPicker passPicker,
    ICommandBus commandBus,
    DraftSystemSchedulerService draftSystemSchedulerService,
    IBotPassPickLock botPassPickLock)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var passPickIsLocked = botPassPickLock.IsLocked(command.GameId, command.PlayerId);
        if (passPickIsLocked)
        {
            logger.Warn($"Tried to pass-pick, but pass-pick is locked (Game: ${command.GameId}, Player: ${
                command.PlayerId}).");
            throw new PassPickingLockedException(command.GameId, command.PlayerId);
        }

        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        var pickedGameJumperId = await passPicker.Pick(game, ct);

        logger.Info($"Pass-picking a Jumper ({pickedGameJumperId}) by a Player ({command.PlayerId}) in a Game ({
            command.GameId})");

        await commandBus.SendAsync<PickJumper.Command, PickJumper.Result>(new PickJumper.Command(command.GameId,
            command.PlayerId, pickedGameJumperId), ct);

        return new Result(pickedGameJumperId);
    }
    //
    // var gameAfterPassResult =
    //     game.PickInDraft(PlayerId.NewPlayerId(command.PlayerId), Domain.Game.JumperId.NewJumperId(jumperToPick));
//
//         if (gameAfterPassResult.IsOk)
//         {
//             var pickOutcome = gameAfterPassResult.ResultValue;
//
//             var gameAfterPass = pickOutcome.Game;
//             var draftAfterPass = pickOutcome.Draft;
//             var phaseChangedTo = pickOutcome.PhaseChangedTo;
//
//             await games.Add(gameAfterPass, ct);
//
//             var passPickIsScheduled =
//                 await DraftPassHelper.MaybeScheduleDraftPass(gameAfterPass, scheduler, json, clock, ct);
//             logger.Info($"Scheduled a Draft Pass? {passPickIsScheduled}");
//
//             var phaseChangedToBreak = phaseChangedTo.IsSome() && phaseChangedTo.Value.IsBreak;
//
//             if (!passPickIsScheduled && phaseChangedToBreak)
//             {
//                 var picksDictionary = pickOutcome.Picks.ToDictionary();
//                 draftPicksArchive.Archive(command.GameId, picksDictionary.ToEnumerableValues());
//                 await LogDraftPicks(gameAfterPass, picksDictionary, ct);
//                 var timeToMainCompetition = gameAfterPass.Settings.BreakSettings.BreakBeforeMainCompetition.Value;
//                 gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.MainCompetition, timeToMainCompetition);
//                 var now = clock.Now();
//                 await scheduler.ScheduleAsync(
//                     jobType: "StartMainCompetition",
//                     payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
//                     runAt: now.Add(timeToMainCompetition),
//                     uniqueKey: $"StartMainCompetition:{game.Id_.Item}",
//                     ct: ct);
//             }
//             else if (passPickIsScheduled && phaseChangedToBreak)
//             {
//                 logger.Warn($"Phase changed to a break but pass pick is scheduled");
//             }
//
//             await gameNotifier.GameUpdated(
//                 await gameUpdatedDtoMapper.FromDomain(gameAfterPass, lastDraftState: draftAfterPass, ct: ct));
//
//             return new Result(jumperToPick);
//         }
//
//         var error = gameAfterPassResult.ErrorValue;
//         if (error.IsDraftError && ((GameError.DraftError)error).Error.IsJumperNotAllowed)
//         {
//             throw new Exception("Draft auto-picker tried to pick not allowed jumper");
//         }
//
//         throw new Exception($@"
// Error during auto-picking a Jumper ({jumperToPick})
// by a Player (ID: {command.PlayerId})
// in Game (ID: {command.GameId})
// Draft: {error}");
//     }
//
//     private async Task LogDraftPicks(Domain.Game.Game game,
//         Dictionary<PlayerId, FSharpList<Domain.Game.JumperId>> picksDictionary,
//         CancellationToken ct)
//     {
//         var pickStringsTasks = picksDictionary.Select(async kvp =>
//         {
//             var nick = GetPlayerNick(kvp.Key);
//             var picksStr = await GetPlayerPicksString(kvp.Key);
//             return $"{nick}: {picksStr}\n";
//         });
//         var pickStrings = await Task.WhenAll(pickStringsTasks);
//
//         logger.Info($@"Picks: {string.Join("; ", pickStrings)}");
//         return;
//
//         async Task<string> GetPlayerPicksString(PlayerId playerId)
//         {
//             var picks = picksDictionary[playerId].AsEnumerable();
//
//             var jumperNames = new List<string>();
//             foreach (var gameJumperId in picks)
//             {
//                 var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(gameJumperId.Item).Id;
//                 var jumper = (await jumpers.GetById(JumperId.NewJumperId(gameWorldJumperId), ct)).Value;
//                 jumperNames.Add($"{jumper.Name.Item} {jumper.Surname.Item}");
//             }
//
//             return string.Join(", ", jumperNames);
//         }
//
//         string GetPlayerNick(PlayerId playerId)
//         {
//             var player = PlayersModule.toList(game.Players).Single(player => player.Id.Equals(playerId));
//             return PlayerModule.NickModule.value(player.Nick);
//         }
//     }
}

public class PassPickingLockedException(Guid gameId, Guid playerId, string? message = null) : Exception(message);