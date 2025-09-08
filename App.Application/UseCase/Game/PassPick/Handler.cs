using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Draft;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftPicks;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
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
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IMyLogger logger,
    IClock clock,
    IScheduler scheduler,
    IDraftPassPicker picker,
    IDraftPicksArchive draftPicksArchive,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IGameJumperAcl gameJumperAcl,
    IJumpers jumpers)
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
                var picksDictionary = pickOutcome.Picks.ToDictionary();
                draftPicksArchive.Archive(command.GameId, picksDictionary.ToEnumerableValues());
                await LogDraftPicks(gameAfterPass, picksDictionary, ct);

                var now = clock.Now();
                await scheduler.ScheduleAsync(
                    jobType: "StartMainCompetition",
                    payloadJson: json.Serialize(new { GameId = game.Id_.Item }),
                    runAt: now.AddSeconds(4),
                    uniqueKey: $"StartMainCompetition:{game.Id_.Item}",
                    ct: ct);
            }

            await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(gameAfterPass));

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

    private async Task LogDraftPicks(Domain.Game.Game game,
        Dictionary<PlayerId, FSharpSet<Domain.Game.JumperId>> picksDictionary,
        CancellationToken ct)
    {
        var pickStringsTasks = picksDictionary.Select(async kvp =>
        {
            var nick = GetPlayerNick(kvp.Key);
            var picksStr = await GetPlayerPicksString(kvp.Key);
            return $"{nick}: {picksStr}\n";
        });
        var pickStrings = await Task.WhenAll(pickStringsTasks);

        logger.Info($@"Picks: {string.Join("; ", pickStrings)}");
        return;

        async Task<string> GetPlayerPicksString(PlayerId playerId)
        {
            var picks = picksDictionary[playerId].AsEnumerable();

            var jumperNames = new List<string>();
            foreach (var gameJumperId in picks)
            {
                var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(gameJumperId.Item).Id;
                var jumper = (await jumpers.GetById(JumperId.NewJumperId(gameWorldJumperId), ct)).Value;
                jumperNames.Add($"{jumper.Name.Item} {jumper.Surname.Item}");
            }

            return string.Join(", ", jumperNames);
        }

        string GetPlayerNick(PlayerId playerId)
        {
            var player = PlayersModule.toList(game.Players).Single(player => player.Id.Equals(playerId));
            return PlayerModule.NickModule.value(player.Nick);
        }
    }
}