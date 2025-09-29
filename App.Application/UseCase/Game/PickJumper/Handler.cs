using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.UseCase.Game.PickJumper;

public record Command(
    Guid GameId,
    Guid PlayerId,
    Guid JumperId
) : ICommand<Result>;

public record Result();

public class Handler(
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IMyLogger logger,
    IClock clock,
    IBotRegistry botRegistry,
    IBotPickLock botPickLock,
    IScheduler scheduler,
    IDraftPicksArchive draftPicksArchive,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IGameSchedule gameSchedule,
    DraftSystemSchedulerService draftSystemSchedulerService)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        var playerIsBot = botRegistry.IsGameBot(command.GameId, command.PlayerId);
        var passPickIsLocked = botPickLock.IsLocked(command.GameId, command.PlayerId);
        logger.Info($"Picking a Jumper ({command.JumperId}) by a Player ({command.PlayerId}) (IsBot={playerIsBot
        }), in a Game ({command.GameId
        }). LOCK: {passPickIsLocked}");

        var gameAfterPickResult =
            game.PickInDraft(PlayerId.NewPlayerId(command.PlayerId), JumperId.NewJumperId(command.JumperId));

        if (gameAfterPickResult.IsOk)
        {
            var pickOutcome = gameAfterPickResult.ResultValue;

            var gameAfterPick = pickOutcome.Game;
            var phaseChangedTo = pickOutcome.PhaseChangedTo;

            await games.Add(gameAfterPick, ct);

            await draftSystemSchedulerService.ScheduleSystemDraftEvents(gameAfterPick, ct);

            if (phaseChangedTo.IsSome() && phaseChangedTo.Value.IsBreak)
            {
                draftPicksArchive.Archive(command.GameId,
                    pickOutcome.Picks.ToDictionary().ToEnumerableValues());
                var timeToMainCompetition = gameAfterPick.Settings.BreakSettings.BreakBeforeMainCompetition.Value;
                gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.MainCompetition, timeToMainCompetition);
                logger.Info($"Scheduled Main Competition in {timeToMainCompetition}");
                var now = clock.Now();
                await scheduler.ScheduleAsync(
                    jobType: "StartMainCompetition",
                    payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
                    runAt: now.Add(timeToMainCompetition),
                    uniqueKey: $"StartMainCompetition:{game.Id_.Item}",
                    ct: ct);
            }

            await gameNotifier.GameUpdated(
                await gameUpdatedDtoMapper.FromDomain(gameAfterPick, lastDraftState: pickOutcome.Draft, ct: ct));

            return new Result();
        }

        var error = gameAfterPickResult.ErrorValue;
        if (error.IsDraftError && ((GameError.DraftError)error).Error.IsJumperNotAllowed)
        {
            throw new JumperTakenException(command.JumperId);
        }

        if (error.IsDraftError && ((GameError.DraftError)error).Error.IsInvalidPlayer)
        {
            throw new NotYourTurnException();
        }

        throw new Exception($@"
Error during picking a Jumper ({command.JumperId})
by a Player (ID: {command.PlayerId})
in Game (ID: {command.GameId})
Draft: {error}");
    }
}

public class JumperTakenException(Guid jumperId, string? message = null) : Exception(message);

public class NotYourTurnException(string? message = null) : Exception(message);