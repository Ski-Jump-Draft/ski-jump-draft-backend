using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
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
    IScheduler scheduler,
    IDraftPicksArchive draftPicksArchive)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        logger.Info($"Picking a Jumper ({command.JumperId}) by a Player ({command.PlayerId}) in a Game ({command.GameId
        })");

        var gameAfterPickResult =
            game.PickInDraft(PlayerId.NewPlayerId(command.PlayerId), JumperId.NewJumperId(command.JumperId));

        if (gameAfterPickResult.IsOk)
        {
            var pickOutcome = gameAfterPickResult.ResultValue;

            var gameAfterPick = pickOutcome.Game;
            var phaseChangedTo = pickOutcome.PhaseChangedTo;

            await games.Add(gameAfterPick, ct);

            await DraftPassHelper.MaybeScheduleDraftPass(gameAfterPick, scheduler, json, clock, ct);

            if (phaseChangedTo.IsSome() && phaseChangedTo.Value.IsBreak)
            {
                draftPicksArchive.Archive(command.GameId,
                    pickOutcome.Picks.ToDictionary().ToEnumerableValues());
                var now = clock.Now();
                await scheduler.ScheduleAsync(
                    jobType: "StartMainCompetition",
                    payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
                    runAt: now.AddSeconds(5),
                    uniqueKey: $"StartMainCompetition:{game.Id_.Item}",
                    ct: ct);
            }

            await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(gameAfterPick));

            return new Result();
        }

        var error = gameAfterPickResult.ErrorValue;
        if (error.IsDraftError && ((GameError.DraftError)error).Error.IsJumperNotAllowed)
        {
            throw new JumperTakenException(command.JumperId);
        }

        throw new Exception($@"
Error during picking a Jumper ({command.JumperId})
by a Player (ID: {command.PlayerId})
in Game (ID: {command.GameId})
Draft: {error}");
    }
}

public class JumperTakenException(Guid jumperId, string? message = null) : Exception(message);