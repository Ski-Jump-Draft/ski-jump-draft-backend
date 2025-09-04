using App.Application.Commanding;
using App.Application.Draft;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Game;


namespace App.Application.UseCase.Game.PassPick;

public record Command(
    Guid GameId,
    Guid PlayerId
) : ICommand<Result>;

public record Result(Guid JumperId);

public class Handler(
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IMyLogger logger,
    IClock clock,
    IScheduler scheduler,
    IDraftPassPicker picker,
    IDraftPicksArchive draftPicksArchive)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        var jumperToPick = picker.Pick(game);

        logger.Info($"Pass-picking a Jumper ({jumperToPick.Item}) by a Player ({command.PlayerId}) in a Game ({
            command.GameId})");

        var gameAfterPassResult =
            game.PickInDraft(PlayerId.NewPlayerId(command.PlayerId), jumperToPick);

        if (gameAfterPassResult.IsOk)
        {
            var pickOutcome = gameAfterPassResult.ResultValue;

            var gameAfterPass = pickOutcome.Game;
            var phaseChangedTo = pickOutcome.PhaseChangedTo;

            await games.Add(gameAfterPass, ct);

            await DraftPassHelper.MaybeScheduleDraftPass(gameAfterPass, scheduler, json, clock, ct);

            if (phaseChangedTo.IsSome() && phaseChangedTo.Value.IsBreak)
            {
                draftPicksArchive.Archive(command.GameId,
                    pickOutcome.Picks.ToDictionary().ToEnumerableValues());
                var now = clock.Now();
                await scheduler.ScheduleAsync(
                    jobType: "StartMainCompetition",
                    payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
                    runAt: now.AddSeconds(15),
                    uniqueKey: $"StartMainCompetition:{game.Id_.Item}",
                    ct: ct);
            }

            await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(gameAfterPass));

            return new Result(jumperToPick.Item);
        }

        var error = gameAfterPassResult.ErrorValue;
        if (error.IsDraftError && ((GameError.DraftError)error).Error.IsJumperNotAllowed)
        {
            throw new Exception("Draft auto-picker tried to pick not allowed jumper");
        }


        throw new Exception($@"
Error during auto-picking a Jumper ({jumperToPick})
by a Player (ID: {command.PlayerId})
in Game (ID: {command.GameId})
Draft: {error}");
    }
}